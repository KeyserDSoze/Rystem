using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Api.Models;
using System.Text.Json;

namespace Rystem.PlayFramework.Api;

/// <summary>
/// Extension methods for mapping PlayFramework HTTP endpoints.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps PlayFramework endpoints.
    /// - If factoryName is null: Creates generic endpoints accepting factoryName in route (/{factoryName}, /{factoryName}/streaming)
    /// - If factoryName is specified: Creates factory-specific endpoints (/, /streaming) with factory-specific policies
    /// </summary>
    /// <param name="app">WebApplication instance.</param>
    /// <param name="factoryName">Optional factory name. If null, endpoints accept factoryName as route parameter.</param>
    /// <param name="configure">Optional configuration for API settings.</param>
    /// <returns>RouteGroupBuilder for additional configuration.</returns>
    public static RouteGroupBuilder MapPlayFramework(
        this WebApplication app,
        string? factoryName = null,
        Action<PlayFrameworkApiSettings>? configure = null)
    {
        var settings = new PlayFrameworkApiSettings();
        configure?.Invoke(settings);

        var group = app.MapGroup(settings.BasePath)
            .WithTags(factoryName is null ? "PlayFramework" : $"PlayFramework-{factoryName}");

        if (settings.RequireAuthentication)
        {
            group.RequireAuthorization();
        }

        // Apply global authorization policies
        foreach (var policy in settings.AuthorizationPolicies)
        {
            group.RequireAuthorization(policy);
        }

        // Apply factory-specific policies (only if factoryName is specified)
        if (factoryName is not null && settings.FactoryPolicies.TryGetValue(factoryName, out var factoryPolicies))
        {
            foreach (var policy in factoryPolicies)
            {
                group.RequireAuthorization(policy);
            }
        }

        // Determine route pattern and endpoint names
        // When factoryName is null: root route (uses "default" factory internally)
        // When factoryName is specified: static route with factory name (e.g., /default, /myBot)
        var stepRoute = factoryName is null ? "" : $"/{factoryName}";
        var streamingRoute = factoryName is null ? "/streaming" : $"/{factoryName}/streaming";
        var stepEndpointName = factoryName is null ? "ExecutePlayFrameworkStepByStep" : $"Execute{factoryName}StepByStep";
        var streamingEndpointName = factoryName is null ? "ExecutePlayFrameworkTokenStreaming" : $"Execute{factoryName}TokenStreaming";
        var stepSummary = factoryName is null ? "Execute PlayFramework scene (step-by-step streaming)" : $"Execute {factoryName} scene (step-by-step streaming)";
        var streamingSummary = factoryName is null ? "Execute PlayFramework scene (token-level streaming)" : $"Execute {factoryName} scene (token-level streaming)";

        // Endpoint: Step-by-step streaming
        group.MapPost(stepRoute, async (
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Get factory from closure or route parameter
            var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
            var targetFactory = factoryName ?? routeFactory ?? "default";
            await ExecutePlayFrameworkAsync(targetFactory, request, sceneManagerFactory, logger, httpContext, settings, cancellationToken);
        })
        .WithName(stepEndpointName)
        .WithSummary(stepSummary)
        .Produces(200, contentType: "text/event-stream");

        // Endpoint: Token-level streaming
        group.MapPost(streamingRoute, async (
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            // Get factory from closure or route parameter
            var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
            var targetFactory = factoryName ?? routeFactory ?? "default";

            // Force enableStreaming for token-level streaming
            request.Settings ??= new SceneRequestSettings();
            request.Settings.EnableStreaming = true;

            await ExecutePlayFrameworkAsync(targetFactory, request, sceneManagerFactory, logger, httpContext, settings, cancellationToken);
        })
        .WithName(streamingEndpointName)
        .WithSummary(streamingSummary)
        .Produces(200, contentType: "text/event-stream");

        return group;
    }

    /// <summary>
    /// Core execution logic shared by all endpoints.
    /// Handles SSE streaming, error handling, and response serialization.
    /// </summary>
    private static async Task ExecutePlayFrameworkAsync(
        string factoryName,
        PlayFrameworkRequest request,
        IFactory<ISceneManager> sceneManagerFactory,
        ILogger<ISceneManager> logger,
        HttpContext httpContext,
        PlayFrameworkApiSettings settings,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get SceneManager for factory
            var sceneManager = sceneManagerFactory.Create(factoryName);

            // Extract metadata from HTTP context
            var metadata = BuildMetadata(request.Metadata, httpContext, settings);

            // Convert request to MultiModalInput and merge settings
            var input = request.ToMultiModalInput();
            var mergedSettings = request.GetMergedSettings();

            // Set response headers for SSE
            httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            httpContext.Response.Headers.Append("Cache-Control", "no-cache");
            httpContext.Response.Headers.Append("Connection", "keep-alive");

            // Execute PlayFramework with streaming
            await foreach (var response in sceneManager.ExecuteAsync(input, metadata, mergedSettings, cancellationToken))
            {
                // Serialize response as SSE event
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }

            // No need to send explicit completion marker - SceneManager always sends Completed as final event
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PlayFramework execution failed for factory '{FactoryName}'", factoryName);

            // Send error event
            var errorJson = JsonSerializer.Serialize(new
            {
                status = "error",
                errorMessage = ex.Message
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            await httpContext.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static Dictionary<string, object> BuildMetadata(
        Dictionary<string, object>? requestMetadata,
        HttpContext httpContext,
        PlayFrameworkApiSettings settings)
    {
        var metadata = requestMetadata ?? new Dictionary<string, object>();

        if (settings.EnableAutoMetadata)
        {
            // Extract userId from claims (if authenticated)
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst("sub")?.Value
                    ?? httpContext.User.FindFirst("userId")?.Value
                    ?? httpContext.User.Identity.Name;

                if (!string.IsNullOrEmpty(userId) && !metadata.ContainsKey("userId"))
                {
                    metadata["userId"] = userId;
                }
            }

            // Add IP address
            if (!metadata.ContainsKey("ipAddress"))
            {
                metadata["ipAddress"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            // Add request ID (for tracing)
            if (!metadata.ContainsKey("requestId"))
            {
                metadata["requestId"] = httpContext.TraceIdentifier;
            }

            // Add timestamp
            if (!metadata.ContainsKey("timestamp"))
            {
                metadata["timestamp"] = DateTimeOffset.UtcNow;
            }
        }

        return metadata;
    }
}
