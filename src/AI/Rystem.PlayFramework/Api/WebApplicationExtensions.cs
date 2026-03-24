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
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Services;
using Rystem.PlayFramework.Services.Voice;

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

        var discoveryRoute = factoryName is null ? "/discovery" : $"/{factoryName}/discovery";
        group.MapGet(discoveryRoute, async (
            [FromServices] IFactory<ISceneFactory> sceneFactoryFactory,
            [FromServices] IFactory<IMcpServerManager> mcpServerManagerFactory,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
            var targetFactory = factoryName ?? routeFactory ?? "default";
            var sceneFactory = sceneFactoryFactory.Create(targetFactory)
                ?? throw new InvalidOperationException($"SceneFactory not found for factory '{targetFactory}'");

            var discovery = await BuildDiscoveryResponseAsync(
                targetFactory,
                sceneFactory,
                mcpServerManagerFactory,
                cancellationToken);

            return Results.Ok(discovery);
        })
        .WithName(factoryName is null ? "GetPlayFrameworkDiscovery" : $"Get{factoryName}PlayFrameworkDiscovery")
        .WithSummary(factoryName is null ? "Get PlayFramework discovery metadata" : $"Get {factoryName} PlayFramework discovery metadata")
        .Produces<PlayFrameworkDiscoveryResponse>(200);

        // Conversation management endpoints (optional, requires repository)
        if (settings.EnableConversationEndpoints)
        {
            var hasRepository = app.Services.GetService<IFactory<IRepository<StoredConversation, string>>>() != null;
            if (hasRepository)
            {
                var conversationsRoute = factoryName is null ? "/conversations" : $"/{factoryName}/conversations";
                var conversationRoute = factoryName is null ? "/conversations/{conversationKey}" : $"/{factoryName}/conversations/{{conversationKey}}";
                var visibilityRoute = factoryName is null ? "/conversations/{conversationKey}/visibility" : $"/{factoryName}/conversations/{{conversationKey}}/visibility";

                // Endpoint: List conversations
                group.MapGet(conversationsRoute, async (
                    [AsParameters] ConversationQueryParameters parameters,
                    [FromServices] IFactory<IRepository<StoredConversation, string>> repositoryFactory,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory)!;
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await ListConversationsAsync(parameters, repository, currentUserId, settings, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "ListConversations" : $"List{factoryName}Conversations")
                .WithSummary(factoryName is null ? "List conversations with filters" : $"List {factoryName} conversations with filters")
                .Produces<List<StoredConversation>>(200);

                // Endpoint: Get conversation by key
                group.MapGet(conversationRoute, async (
                    string conversationKey,
                    [FromQuery] bool includeContents,
                    [FromServices] IFactory<IRepository<StoredConversation, string>> repositoryFactory,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory)!;
                    var currentUserId = GetCurrentUserId(httpContext);

                    return await GetConversationAsync(conversationKey, includeContents, repository, currentUserId, logger, cancellationToken);
                })
                .WithName(factoryName is null ? "GetConversation" : $"Get{factoryName}Conversation")
                .WithSummary(factoryName is null ? "Get conversation by key" : $"Get {factoryName} conversation by key")
                .Produces<StoredConversation>(200)
                .Produces(404)
                .Produces(403);

                // Endpoint: Delete conversation
                group.MapDelete(conversationRoute, async (
                    string conversationKey,
                    [FromServices] IFactory<IRepository<StoredConversation, string>> repositoryFactory,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory)!;
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
                    [FromServices] IFactory<IRepository<StoredConversation, string>> repositoryFactory,
                    [FromServices] ILogger<ISceneManager> logger,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                    var targetFactory = factoryName ?? routeFactory ?? "default";
                    var repository = repositoryFactory.Create(targetFactory)!;
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

        // Voice pipeline endpoints (optional, requires IVoiceAdapter + WithVoice)
        if (settings.EnableVoiceEndpoints)
        {
            var voiceRoute = factoryName is null ? "/voice" : $"/{factoryName}/voice";
            var voiceEndpointName = factoryName is null ? "ExecutePlayFrameworkVoice" : $"Execute{factoryName}Voice";
            var voiceSummary = factoryName is null
                ? "Voice pipeline: audio → STT → PlayFramework → TTS → audio streaming"
                : $"Voice pipeline for {factoryName}: audio → STT → PlayFramework → TTS → audio streaming";

            group.MapPost(voiceRoute, async (
                HttpRequest httpRequest,
                [FromServices] IFactory<ISceneManager> sceneManagerFactory,
                [FromServices] IFactory<PlayFrameworkSettings> settingsFactory,
                [FromServices] IFactory<IVoiceAdapter> voiceAdapterFactory,
                [FromServices] ILogger<ISceneManager> logger,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var routeFactory = httpContext.GetRouteValue("factoryName")?.ToString();
                var targetFactory = factoryName ?? routeFactory ?? "default";

                await ExecuteVoicePipelineAsync(
                    targetFactory, httpRequest, sceneManagerFactory, settingsFactory,
                    voiceAdapterFactory, logger, httpContext, settings, cancellationToken);
            })
            .WithName(voiceEndpointName)
            .WithSummary(voiceSummary)
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(200, contentType: "text/event-stream")
            .Produces(400)
            .DisableAntiforgery();
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
        var lastKnownTotalCost = 0m;
        string? lastKnownConversationKey = null;
        try
        {
            // Get SceneManager for factory
            var sceneManager = sceneManagerFactory.Create(factoryName);

            // Extract metadata from HTTP context
            var metadata = BuildMetadata(request.Metadata, httpContext, settings);

            // Convert request to MultiModalInput and merge settings
            var input = request.ToMultiModalInput();
            var mergedSettings = request.GetMergedSettings();

            lastKnownConversationKey = mergedSettings?.ConversationKey;

            // Set response headers for SSE
            httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            httpContext.Response.Headers.Append("Cache-Control", "no-cache");
            httpContext.Response.Headers.Append("Connection", "keep-alive");

            // Execute PlayFramework with streaming
            await foreach (var response in sceneManager.ExecuteAsync(input, metadata, mergedSettings, cancellationToken))
            {
                if (response.TotalCost > 0)
                    lastKnownTotalCost = response.TotalCost;
                if (response.ConversationKey != null)
                    lastKnownConversationKey = response.ConversationKey;

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
                errorMessage = ex.Message,
                totalCost = lastKnownTotalCost,
                conversationKey = lastKnownConversationKey
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
    /// Builds discovery metadata for scenes and tools.
    /// </summary>
    private static async Task<PlayFrameworkDiscoveryResponse> BuildDiscoveryResponseAsync(
        string factoryName,
        ISceneFactory sceneFactory,
        IFactory<IMcpServerManager> mcpServerManagerFactory,
        CancellationToken cancellationToken)
    {
        var response = new PlayFrameworkDiscoveryResponse
        {
            FactoryName = factoryName
        };

        var services = new Dictionary<string, PlayFrameworkToolSourceInfo>(StringComparer.OrdinalIgnoreCase);
        var clients = new Dictionary<string, PlayFrameworkToolSourceInfo>(StringComparer.OrdinalIgnoreCase);
        var mcpServers = new Dictionary<string, PlayFrameworkToolSourceInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var scene in sceneFactory.Scenes)
        {
            var sceneInfo = new PlayFrameworkSceneInfo
            {
                Name = scene.Name,
                Description = scene.Description
            };

            foreach (var tool in scene.Tools)
            {
                var toolInfo = BuildSceneToolInfo(scene.Name, tool);
                sceneInfo.Tools.Add(toolInfo);
                AddToolToResponse(response, services, clients, toolInfo);
            }

            foreach (var mcpReference in scene.McpServerReferences)
            {
                var sourceName = mcpReference.FactoryName.ToString() ?? "default";
                var source = GetOrCreateSource(mcpServers, sourceName, PlayFrameworkToolSourceType.Mcp);

                try
                {
                    var manager = mcpServerManagerFactory.Create(mcpReference.FactoryName)
                        ?? throw new InvalidOperationException($"MCP server '{sourceName}' not found");
                    var tools = await manager.GetToolsAsync(mcpReference.FilterSettings, cancellationToken);

                    foreach (var tool in tools)
                    {
                        var toolInfo = new PlayFrameworkToolInfo
                        {
                            SceneName = scene.Name,
                            ToolName = ToolNameNormalizer.Normalize(tool.Name),
                            Description = tool.Description,
                            SourceType = PlayFrameworkToolSourceType.Mcp,
                            SourceName = tool.FactoryName,
                            MemberName = tool.Name
                        };

                        AddToolIfMissing(sceneInfo.Tools, toolInfo);
                        AddToolIfMissing(source.Tools, toolInfo);
                    }
                }
                catch (Exception ex)
                {
                    source.IsAvailable = false;
                    source.ErrorMessage ??= ex.Message;
                }
            }

            response.Scenes.Add(sceneInfo);
        }

        response.Services = [.. services.Values.OrderBy(x => x.Name)];
        response.Clients = [.. clients.Values.OrderBy(x => x.Name)];
        response.McpServers = [.. mcpServers.Values.OrderBy(x => x.Name)];
        response.Scenes = [.. response.Scenes.OrderBy(x => x.Name)];
        response.Others = [.. response.Others.OrderBy(x => x.SceneName).ThenBy(x => x.ToolName)];

        return response;
    }

    private static PlayFrameworkToolInfo BuildSceneToolInfo(string sceneName, ISceneTool tool)
    {
        if (tool is ISceneToolMetadata metadata)
        {
            return new PlayFrameworkToolInfo
            {
                SceneName = sceneName,
                ToolName = tool.Name,
                Description = tool.Description,
                SourceType = metadata.SourceType,
                SourceName = metadata.SourceName,
                MemberName = metadata.MemberName,
                IsCommand = metadata.IsCommand,
                JsonSchema = metadata.JsonSchema
            };
        }

        return new PlayFrameworkToolInfo
        {
            SceneName = sceneName,
            ToolName = tool.Name,
            Description = tool.Description,
            SourceType = PlayFrameworkToolSourceType.Other
        };
    }

    private static void AddToolToResponse(
        PlayFrameworkDiscoveryResponse response,
        Dictionary<string, PlayFrameworkToolSourceInfo> services,
        Dictionary<string, PlayFrameworkToolSourceInfo> clients,
        PlayFrameworkToolInfo tool)
    {
        switch (tool.SourceType)
        {
            case PlayFrameworkToolSourceType.Service:
                AddToolIfMissing(GetOrCreateSource(services, tool.SourceName ?? "unknown", PlayFrameworkToolSourceType.Service).Tools, tool);
                break;
            case PlayFrameworkToolSourceType.Client:
                AddToolIfMissing(GetOrCreateSource(clients, tool.SourceName ?? "client", PlayFrameworkToolSourceType.Client).Tools, tool);
                break;
            default:
                AddToolIfMissing(response.Others, tool);
                break;
        }
    }

    private static PlayFrameworkToolSourceInfo GetOrCreateSource(
        Dictionary<string, PlayFrameworkToolSourceInfo> sources,
        string name,
        PlayFrameworkToolSourceType sourceType)
    {
        if (!sources.TryGetValue(name, out var source))
        {
            source = new PlayFrameworkToolSourceInfo
            {
                Name = name,
                SourceType = sourceType
            };
            sources[name] = source;
        }

        return source;
    }

    private static void AddToolIfMissing(List<PlayFrameworkToolInfo> tools, PlayFrameworkToolInfo tool)
    {
        if (!tools.Any(x =>
            string.Equals(x.SceneName, tool.SceneName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.ToolName, tool.ToolName, StringComparison.OrdinalIgnoreCase)
            && x.SourceType == tool.SourceType
            && string.Equals(x.SourceName, tool.SourceName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.MemberName, tool.MemberName, StringComparison.OrdinalIgnoreCase)))
        {
            tools.Add(tool);
        }
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

            // Build filter expression based on visibility parameters.
            // Note: UserId comparison uses a captured local to avoid closure-over-nullable issues
            // with LINQ translators (TableStorage, Cosmos, etc.).
            // Nested collection predicates (Messages.Any) are intentionally omitted because
            // cloud LINQ translators cannot translate sub-collection expressions.
            var userId = currentUserId;
            QueryBuilder<StoredConversation, string> queryBuilder;

            if (parameters.IncludePublic && parameters.IncludePrivate)
            {
                if (userId != null)
                    queryBuilder = repository.Where(x => x.IsPublic || x.UserId == userId);
                else
                    queryBuilder = repository.Where(x => x.IsPublic);
            }
            else if (parameters.IncludePublic && !parameters.IncludePrivate)
            {
                // Only public conversations
                queryBuilder = repository.Where(x => x.IsPublic);
            }
            else
            {
                // Only private conversations of the current user - requires authentication
                if (userId == null)
                    return Results.Ok(new List<StoredConversation>());
                queryBuilder = repository.Where(x => !x.IsPublic && x.UserId == userId);
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
                .AddMetadata(nameof(parameters.IncludeContents), parameters.IncludeContents.ToString())
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
        bool includeContents,
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

            // Exclude contents if not requested (reduce payload size)
            if (!includeContents)
            {
                foreach (var message in conversation.Messages)
                {
                    message.Contents = null;
                }
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

    /// <summary>
    /// Handles voice pipeline execution: audio upload → STT → PlayFramework → sentence chunking → TTS → SSE streaming.
    /// Request: multipart/form-data with "audio" file and optional "conversationKey", "metadata" fields.
    /// Response: SSE stream of JSON events (transcription, audio chunks as base64, scene events, completion).
    /// </summary>
    private static async Task ExecuteVoicePipelineAsync(
        string factoryName,
        HttpRequest httpRequest,
        IFactory<ISceneManager> sceneManagerFactory,
        IFactory<PlayFrameworkSettings> settingsFactory,
        IFactory<IVoiceAdapter> voiceAdapterFactory,
        ILogger logger,
        HttpContext httpContext,
        PlayFrameworkApiSettings apiSettings,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate content type
            if (!httpRequest.HasFormContentType)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new { error = "Expected multipart/form-data with an 'audio' file." }, cancellationToken);
                return;
            }

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var audioFile = form.Files.GetFile("audio");

            if (audioFile is null || audioFile.Length == 0)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new { error = "Missing or empty 'audio' file in form data." }, cancellationToken);
                return;
            }

            if (audioFile.Length > apiSettings.MaxAudioUploadSize)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new { error = $"Audio file exceeds maximum size of {apiSettings.MaxAudioUploadSize / 1_048_576}MB." }, cancellationToken);
                return;
            }

            // Read audio bytes
            using var ms = new MemoryStream();
            await audioFile.CopyToAsync(ms, cancellationToken);
            var audioData = new ReadOnlyMemory<byte>(ms.ToArray());

            // Extract optional form fields
            var conversationKey = form.TryGetValue("conversationKey", out var ck) ? ck.ToString() : null;
            Dictionary<string, object>? metadata = null;
            if (form.TryGetValue("metadata", out var metaJson) && !string.IsNullOrWhiteSpace(metaJson))
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson!, JsonHelper.JsonSerializerOptions);
            }

            // Build metadata from HTTP context
            metadata = BuildMetadata(metadata, httpContext, apiSettings);

            // Build request settings
            var requestSettings = new SceneRequestSettings
            {
                EnableStreaming = true,
                ConversationKey = conversationKey
            };

            // Resolve PlayFramework settings to get VoiceSettings + voice adapter factory name
            var pfSettings = settingsFactory.Create(factoryName);

            if (!pfSettings.Voice.Enabled)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsJsonAsync(new { error = $"Voice pipeline is not enabled for factory '{factoryName}'. Call .WithVoice() in the builder." }, cancellationToken);
                return;
            }

            // Resolve voice adapter
            // The voice adapter factory name is stored in PlayFrameworkSettings when WithVoice() is called
            // But settings don't store the factory name — the builder does. We need a way to find it.
            // Convention: If no explicit VoiceAdapterFactoryName, use the same factory name as PlayFramework
            IVoiceAdapter voiceAdapter;
            try
            {
                voiceAdapter = voiceAdapterFactory.Create(factoryName);
            }
            catch
            {
                // If factory-specific voice adapter is not found, try the default
                try
                {
                    voiceAdapter = voiceAdapterFactory.Create();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "No IVoiceAdapter registered for factory '{FactoryName}' or as default", factoryName);
                    httpContext.Response.StatusCode = 400;
                    await httpContext.Response.WriteAsJsonAsync(new { error = $"No IVoiceAdapter registered. Call AddVoiceAdapterForAzureOpenAI() or similar." }, cancellationToken);
                    return;
                }
            }

            // Resolve SceneManager
            var sceneManager = sceneManagerFactory.Create(factoryName);

            // Create voice pipeline
            // Resolve optional IAudioCostCalculator for this factory (registered via VoiceAdapterSettings.CostTracking)
            var audioCostCalculatorFactory = httpContext.RequestServices.GetService<IFactory<IAudioCostCalculator>>();
            IAudioCostCalculator? audioCostCalculator = null;
            if (audioCostCalculatorFactory is not null)
            {
                try { audioCostCalculator = audioCostCalculatorFactory.Create(factoryName); }
                catch
                {
                    try { audioCostCalculator = audioCostCalculatorFactory.Create(); } catch { /* not configured */ }
                }
            }

            var pipeline = new VoicePipeline(sceneManager, voiceAdapter, pfSettings.Voice, logger, audioCostCalculator);

            // Set response headers for SSE
            httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            httpContext.Response.Headers.Append("Cache-Control", "no-cache");
            httpContext.Response.Headers.Append("Connection", "keep-alive");

            // Stream voice pipeline responses as SSE events
            await foreach (var voiceResponse in pipeline.ProcessAsync(audioData, audioFile.FileName, metadata, requestSettings, cancellationToken))
            {
                var eventData = voiceResponse.Type switch
                {
                    VoiceResponseType.Transcription => new
                    {
                        type = "transcription",
                        text = voiceResponse.Text
                    } as object,

                    VoiceResponseType.AudioChunk => new
                    {
                        type = "audio",
                        text = voiceResponse.Text,
                        audio = Convert.ToBase64String(voiceResponse.AudioData!.Value.Span)
                    } as object,

                    VoiceResponseType.SceneEvent => new
                    {
                        type = "scene",
                        status = voiceResponse.SceneResponse!.Status.ToString(),
                        message = voiceResponse.Text,
                        sceneResponse = voiceResponse.SceneResponse
                    } as object,

                    VoiceResponseType.Completed => new
                    {
                        type = "completed",
                        sttCost = voiceResponse.SttCost,
                        ttsCost = voiceResponse.TtsCost,
                        totalVoiceCost = voiceResponse.TotalVoiceCost
                    } as object,

                    _ => new { type = "unknown" } as object
                };

                var json = JsonSerializer.Serialize(eventData, JsonHelper.JsonSerializerOptions);
                await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Voice pipeline execution failed for factory '{FactoryName}'", factoryName);

            // If headers haven't been sent yet, respond with error status
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 500;
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Voice pipeline execution failed",
                    message = ex.Message
                }, cancellationToken);
            }
            else
            {
                // Headers already sent (SSE streaming started) — send error as SSE event
                var errorJson = JsonSerializer.Serialize(new
                {
                    type = "error",
                    message = ex.Message
                }, JsonHelper.JsonSerializerOptions);

                await httpContext.Response.WriteAsync($"data: {errorJson}\n\n", cancellationToken);
                await httpContext.Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}

