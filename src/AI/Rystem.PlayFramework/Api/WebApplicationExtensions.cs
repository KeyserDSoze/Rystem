using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RepositoryFramework;
using Rystem.PlayFramework.Api.Models;
using Rystem.PlayFramework.Helpers;

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

        // Conversation management endpoints (optional, requires repository)
        if (settings.EnableConversationEndpoints)
        {
            var repositoryFactory = app.Services.GetService<IFactory<IRepository<StoredConversation, string>>>();
            if (repositoryFactory != null)
            {
                var conversationsRoute = factoryName is null ? "/conversations" : $"/{factoryName}/conversations";
                var conversationRoute = factoryName is null ? "/conversations/{conversationKey}" : $"/{factoryName}/conversations/{{conversationKey}}";
                var visibilityRoute = factoryName is null ? "/conversations/{conversationKey}/visibility" : $"/{factoryName}/conversations/{{conversationKey}}/visibility";

                // Endpoint: List conversations
                group.MapGet(conversationsRoute, async (
                    [AsParameters] ConversationQueryParameters parameters,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory);
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await ListConversationsAsync(parameters, repository, currentUserId, settings, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "ListConversations" : $"List{factoryName}Conversations")
                .WithSummary(factoryName is null ? "List conversations with filters" : $"List {factoryName} conversations with filters")
                .Produces<List<StoredConversation>>(200);

                // Endpoint: Get conversation by key
                group.MapGet(conversationRoute, async (
                    string conversationKey,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory);
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await GetConversationAsync(conversationKey, repository, currentUserId, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "GetConversation" : $"Get{factoryName}Conversation")
                .WithSummary(factoryName is null ? "Get conversation by key" : $"Get {factoryName} conversation by key")
                .Produces<StoredConversation>(200)
                .Produces(404)
                .Produces(403);

                // Endpoint: Delete conversation
                group.MapDelete(conversationRoute, async (
                    string conversationKey,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory);
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await DeleteConversationAsync(conversationKey, repository, currentUserId, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "DeleteConversation" : $"Delete{factoryName}Conversation")
                .WithSummary(factoryName is null ? "Delete conversation (owner only)" : $"Delete {factoryName} conversation (owner only)")
                .Produces(204)
                .Produces(404)
                .Produces(403);

                // Endpoint: Update conversation visibility
                group.MapPatch(visibilityRoute, async (
                    string conversationKey,
                    [FromBody] UpdateConversationVisibilityRequest request,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory);
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await UpdateConversationVisibilityAsync(conversationKey, request.IsPublic, repository, currentUserId, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "UpdateConversationVisibility" : $"Update{factoryName}ConversationVisibility")
                .WithSummary(factoryName is null ? "Update conversation visibility (owner only)" : $"Update {factoryName} conversation visibility (owner only)")
                .Produces<StoredConversation>(200)
                .Produces(404)
                .Produces(403);
            }
        }

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
                // Serialize response as SSE event with camelCase for properties and enums
                var json = JsonSerializer.Serialize(response, JsonHelper.JsonSerializerOptions);

                await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }

            // No need to send explicit completion marker - SceneManager always sends Completed as final event
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PlayFramework execution failed for factory '{FactoryName}'", factoryName);

            // Send error event with camelCase
            var errorJson = JsonSerializer.Serialize(new
            {
                status = "error",
                errorMessage = ex.Message
            }, JsonHelper.JsonSerializerOptions);

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

    /// <summary>
    /// Extracts current user ID from HTTP context.
    /// </summary>
    private static string? GetCurrentUserId(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
            return null;

        return httpContext.User.Identity.Name;
    }

    /// <summary>
    /// Lists conversations with filtering, sorting, and pagination.
    /// </summary>
    private static async Task<IResult> ListConversationsAsync(
        ConversationQueryParameters parameters,
        IRepository<StoredConversation, string> repository,
        string? currentUserId,
        PlayFrameworkApiSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Enforce max page size
            var pageSize = Math.Min(parameters.Take, settings.MaxConversationsPageSize);

            logger.LogDebug("Listing conversations - IncludePublic: {IncludePublic}, IncludePrivate: {IncludePrivate}, User: {UserId}",
                parameters.IncludePublic, parameters.IncludePrivate, currentUserId ?? "anonymous");

            // Build filter expression based on visibility parameters
            QueryBuilder<StoredConversation, string> queryBuilder;

            if (parameters.IncludePublic && parameters.IncludePrivate)
            {
                // Show public conversations OR user's private conversations
                queryBuilder = repository
                    .Where(x => x.IsPublic || x.UserId == currentUserId);
            }
            else if (parameters.IncludePublic && !parameters.IncludePrivate)
            {
                // Only public conversations
                queryBuilder = repository
                    .Where(x => x.IsPublic);
            }
            else
            {
                queryBuilder = repository
                    .Where(x => !x.IsPublic && x.UserId == currentUserId);
            }
            if (!string.IsNullOrWhiteSpace(parameters.SearchText))
            {
                var searchLower = parameters.SearchText.ToLowerInvariant();
                queryBuilder.Where(x => x.Messages.Any(m => m.Text != null && m.Text.ToLowerInvariant().Contains(searchLower) == true));
            }

            if (parameters.OrderBy == ConversationSortOrder.TimestampDescending)
            {
                queryBuilder.OrderByDescending(x => x.Timestamp);

            }
            else
            {
                queryBuilder.OrderBy(x => x.Timestamp);
            }

            // Pagination
            var result = await queryBuilder
                .Skip(parameters.Skip)
                .Take(pageSize)
                .ToListAsEntityAsync();

            logger.LogDebug("Found {Count} conversations.", result.Count);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list conversations");
            return Results.Problem(
                title: "Failed to list conversations",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Gets a single conversation by key.
    /// </summary>
    private static async Task<IResult> GetConversationAsync(
        string conversationKey,
        IRepository<StoredConversation, string> repository,
        string? currentUserId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await repository.GetAsync(conversationKey, cancellationToken);

            if (conversation == null)
            {
                return Results.NotFound(new { error = "Conversation not found" });
            }

            // Authorization check: private conversations require userId match
            if (!conversation.IsPublic && conversation.UserId != currentUserId)
            {
                logger.LogWarning(
                    "Unauthorized access attempt to private conversation '{ConversationKey}' by user '{UserId}'",
                    conversationKey, currentUserId ?? "anonymous");

                return Results.Forbid();
            }

            return Results.Ok(conversation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get conversation '{ConversationKey}'", conversationKey);
            return Results.Problem(
                title: "Failed to get conversation",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Deletes a conversation (owner only).
    /// </summary>
    private static async Task<IResult> DeleteConversationAsync(
        string conversationKey,
        IRepository<StoredConversation, string> repository,
        string? currentUserId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await repository.GetAsync(conversationKey, cancellationToken);

            if (conversation == null)
            {
                return Results.NotFound(new { error = "Conversation not found" });
            }

            // Authorization check: only owner can delete
            if (conversation.UserId != currentUserId)
            {
                logger.LogWarning(
                    "Unauthorized delete attempt on conversation '{ConversationKey}' by user '{UserId}' (owner: '{OwnerId}')",
                    conversationKey, currentUserId ?? "anonymous", conversation.UserId ?? "anonymous");

                return Results.Forbid();
            }

            var deleteResult = await repository.DeleteAsync(conversationKey, cancellationToken);

            if (!deleteResult.IsOk)
            {
                logger.LogError("Failed to delete conversation '{ConversationKey}': {Error}",
                    conversationKey, deleteResult.Message);

                return Results.Problem(
                    title: "Failed to delete conversation",
                    detail: deleteResult.Message,
                    statusCode: 500);
            }

            logger.LogInformation("Conversation '{ConversationKey}' deleted by user '{UserId}'",
                conversationKey, currentUserId);

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete conversation '{ConversationKey}'", conversationKey);
            return Results.Problem(
                title: "Failed to delete conversation",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    /// <summary>
    /// Updates conversation visibility (owner only).
    /// </summary>
    private static async Task<IResult> UpdateConversationVisibilityAsync(
        string conversationKey,
        bool isPublic,
        IRepository<StoredConversation, string> repository,
        string? currentUserId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await repository.GetAsync(conversationKey, cancellationToken);

            if (conversation == null)
            {
                return Results.NotFound(new { error = "Conversation not found" });
            }

            // Authorization check: only owner can update visibility
            if (conversation.UserId != currentUserId)
            {
                logger.LogWarning(
                    "Unauthorized visibility update attempt on conversation '{ConversationKey}' by user '{UserId}' (owner: '{OwnerId}')",
                    conversationKey, currentUserId ?? "anonymous", conversation.UserId ?? "anonymous");

                return Results.Forbid();
            }

            // Update visibility
            conversation.IsPublic = isPublic;

            var updateResult = await repository.UpdateAsync(conversationKey, conversation, cancellationToken);

            if (!updateResult.IsOk)
            {
                logger.LogError("Failed to update conversation '{ConversationKey}' visibility: {Error}",
                    conversationKey, updateResult.Message);

                return Results.Problem(
                    title: "Failed to update conversation visibility",
                    detail: updateResult.Message,
                    statusCode: 500);
            }

            logger.LogInformation(
                "Conversation '{ConversationKey}' visibility updated to {Visibility} by user '{UserId}'",
                conversationKey, isPublic ? "public" : "private", currentUserId);

            return Results.Ok(conversation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update conversation '{ConversationKey}' visibility", conversationKey);
            return Results.Problem(
                title: "Failed to update conversation visibility",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

