using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Api.Models;
using System.Text.Json;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Rystem.PlayFramework.Api;

/// <summary>
/// Extension methods for mapping PlayFramework HTTP endpoints.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps PlayFramework endpoints with default settings.
    /// Creates two endpoints per factory:
    /// - POST {basePath}/{factoryName} - Execute with step-by-step streaming (returns each PlayFramework step as SSE event)
    /// - POST {basePath}/{factoryName}/streaming - Execute with token-level streaming (returns each text chunk as SSE event)
    /// </summary>
    /// <param name="app">WebApplication instance.</param>
    /// <param name="configure">Optional configuration for API settings.</param>
    /// <returns>RouteGroupBuilder for additional configuration.</returns>
    public static RouteGroupBuilder MapPlayFramework(
        this WebApplication app,
        Action<PlayFrameworkApiSettings>? configure = null)
    {
        var settings = new PlayFrameworkApiSettings();
        configure?.Invoke(settings);

        var group = app.MapGroup(settings.BasePath)
            .WithTags("PlayFramework");

        if (settings.RequireAuthentication)
        {
            group.RequireAuthorization();
        }

        // Apply global authorization policies
        foreach (var policy in settings.AuthorizationPolicies)
        {
            group.RequireAuthorization(policy);
        }

        // Endpoint: Execute with step-by-step streaming (each step as SSE event)
        group.MapPost("/{factoryName}", async (
            [FromRoute] string factoryName,
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Get SceneManager for factory
                var sceneManager = sceneManagerFactory.Create(factoryName);

                // Extract metadata from HTTP context
                var metadata = BuildMetadata(request.Metadata, httpContext, settings);

                // Build multi-modal input
                var input = BuildMultiModalInput(request);
                var mergedSettings = MergeRequestIntoSettings(request);

                // Set response headers for SSE
                httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                httpContext.Response.Headers.Append("Connection", "keep-alive");

                // Execute PlayFramework with step-by-step streaming
                await foreach (var response in sceneManager.ExecuteAsync(input, metadata, mergedSettings, cancellationToken))
                {
                    // Serialize each step as SSE event
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }

                // Send completion marker
                await httpContext.Response.WriteAsync("data: {\"status\":\"completed\"}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
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
        })
        .WithName("ExecutePlayFrameworkStepByStep")
        .WithSummary("Execute PlayFramework scene (step-by-step streaming)")
        .WithDescription("Executes a PlayFramework scene for any factory. Each step (Planning, Actor execution, etc.) is returned as a separate SSE event. Factory-specific policies are not applied in this endpoint. Use MapPlayFramework(factoryName) for factory-specific authorization.")
        .Produces(200, contentType: "text/event-stream");

        // Endpoint: Execute with token-level streaming (each text chunk as SSE event)
        group.MapPost("/{factoryName}/streaming", async (
            [FromRoute] string factoryName,
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Get SceneManager for factory
                var sceneManager = sceneManagerFactory.Create(factoryName);

                // Extract metadata from HTTP context
                var metadata = BuildMetadata(request.Metadata, httpContext, settings);

                // Build multi-modal input
                var input = BuildMultiModalInput(request);
                var mergedSettings = MergeRequestIntoSettings(request);

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

                // Send completion marker
                await httpContext.Response.WriteAsync("data: {\"status\":\"completed\"}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PlayFramework streaming execution failed for factory '{FactoryName}'", factoryName);

                // Send error event
                var errorJson = JsonSerializer.Serialize(new
                {
                    status = "error",
                    errorMessage = ex.Message
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await httpContext.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        })
        .WithName("ExecutePlayFrameworkTokenStreaming")
        .WithSummary("Execute PlayFramework scene (token-level streaming)")
        .WithDescription("Executes a PlayFramework scene with token-level streaming for any factory. Each text chunk is returned as a separate SSE event. Factory-specific policies are not applied in this endpoint. Use MapPlayFramework(factoryName) for factory-specific authorization.")
        .Produces(200, contentType: "text/event-stream");

        return group;
    }

    /// <summary>
    /// Maps PlayFramework endpoints for a specific factory with default settings.
    /// Creates endpoints at:
    /// - POST {basePath} - Execute with step-by-step streaming (returns each PlayFramework step as SSE event)
    /// - POST {basePath}/streaming - Execute with token-level streaming (returns each text chunk as SSE event)
    /// </summary>
    public static RouteGroupBuilder MapPlayFramework(
        this WebApplication app,
        string factoryName,
        Action<PlayFrameworkApiSettings>? configure = null)
    {
        var settings = new PlayFrameworkApiSettings();
        configure?.Invoke(settings);

        var group = app.MapGroup(settings.BasePath)
            .WithTags($"PlayFramework-{factoryName}");

        if (settings.RequireAuthentication)
        {
            group.RequireAuthorization();
        }

        // Apply global authorization policies
        foreach (var policy in settings.AuthorizationPolicies)
        {
            group.RequireAuthorization(policy);
        }

        // Apply factory-specific policies
        if (settings.FactoryPolicies.TryGetValue(factoryName, out var factoryPolicies))
        {
            foreach (var policy in factoryPolicies)
            {
                group.RequireAuthorization(policy);
            }
        }

        // Endpoint: Execute with step-by-step streaming (each step as SSE event)
        group.MapPost("", async (
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var sceneManager = sceneManagerFactory.Create(factoryName);
                var metadata = BuildMetadata(request.Metadata, httpContext, settings);
                var input = BuildMultiModalInput(request);
                var mergedSettings = MergeRequestIntoSettings(request);

                // Set response headers for SSE
                httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                httpContext.Response.Headers.Append("Connection", "keep-alive");

                // Execute PlayFramework with step-by-step streaming
                await foreach (var response in sceneManager.ExecuteAsync(input, metadata, mergedSettings, cancellationToken))
                {
                    // Serialize each step as SSE event
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }

                // Send completion marker
                await httpContext.Response.WriteAsync("data: {\"status\":\"completed\"}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
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
        })
        .WithName($"Execute{factoryName}StepByStep")
        .WithSummary($"Execute {factoryName} scene (step-by-step streaming)")
        .WithDescription($"Executes {factoryName} scene with step-by-step streaming. Each step (Planning, Actor execution, etc.) is returned as a separate SSE event.");

        // Endpoint: Execute with streaming
        group.MapPost("/streaming", async (
            [FromBody] PlayFrameworkRequest request,
            [FromServices] IFactory<ISceneManager> sceneManagerFactory,
            [FromServices] ILogger<ISceneManager> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var sceneManager = sceneManagerFactory.Create(factoryName);
                var metadata = BuildMetadata(request.Metadata, httpContext, settings);
                var input = BuildMultiModalInput(request);
                var mergedSettings = MergeRequestIntoSettings(request);

                httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                httpContext.Response.Headers.Append("Connection", "keep-alive");

                await foreach (var response in sceneManager.ExecuteAsync(input, metadata, mergedSettings, cancellationToken))
                {
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await httpContext.Response.Body.FlushAsync(cancellationToken);
                }

                await httpContext.Response.WriteAsync("data: {\"status\":\"completed\"}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PlayFramework streaming execution failed for factory '{FactoryName}'", factoryName);

                var errorJson = JsonSerializer.Serialize(new
                {
                    status = "error",
                    errorMessage = ex.Message
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                await httpContext.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        })
        .WithName($"Execute{factoryName}TokenStreaming")
        .WithSummary($"Execute {factoryName} scene (token-level streaming)")
        .WithDescription($"Executes {factoryName} scene with token-level streaming. Each text chunk is returned as a separate SSE event.");

        return group;
    }

    private static MultiModalInput BuildMultiModalInput(PlayFrameworkRequest request)
    {
        // Simple text message
        if (request.Contents == null || request.Contents.Count == 0)
        {
            return MultiModalInput.FromText(request.Message ?? string.Empty);
        }

        // Multi-modal input
        var input = new MultiModalInput
        {
            Text = request.Message
        };

        foreach (var contentItem in request.Contents)
        {
            input.Contents.Add(contentItem.ToAIContent());
        }

        return input;
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

    /// <summary>
    /// Merges top-level ContinuationToken and ClientInteractionResults from PlayFrameworkRequest
    /// into SceneRequestSettings so SceneManager can process them.
    /// </summary>
    private static SceneRequestSettings MergeRequestIntoSettings(PlayFrameworkRequest request)
    {
        var requestSettings = request.Settings ?? new SceneRequestSettings();

        // Forward top-level continuation token (HTTP convenience) into settings if not already set
        if (!string.IsNullOrEmpty(request.ContinuationToken) && string.IsNullOrEmpty(requestSettings.ContinuationToken))
        {
            requestSettings.ContinuationToken = request.ContinuationToken;
        }

        // Forward top-level client interaction results into settings if not already set
        if (request.ClientInteractionResults != null && request.ClientInteractionResults.Count > 0
            && (requestSettings.ClientInteractionResults == null || requestSettings.ClientInteractionResults.Count == 0))
        {
            requestSettings.ClientInteractionResults = request.ClientInteractionResults;
        }

        return requestSettings;
    }
}
