using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Rystem;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Helpers;
using Rystem.PlayFramework.Services;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework.Services.Helpers;

/// <summary>
/// Centralizes all tool/function call management:
/// - Deduplication of tool calls (OpenAI bug)
/// - Execution of server-side tools
/// - Detection and request creation for client-side tools
/// - Conversation sanitization for LLM API compliance
/// 
/// OpenAI Requirements:
/// - Every assistant message with tool_calls MUST be followed by tool messages
///   responding to EACH tool_call_id
/// - Tool responses can be in a single Tool message with multiple FunctionResultContent,
///   or in multiple Tool messages (one per result)
/// - Missing tool responses cause HTTP 400 errors
/// </summary>
internal sealed class ToolExecutionManager : IToolExecutionManager
{
    private readonly ILogger<ToolExecutionManager> _logger;
    private readonly IClientInteractionHandler _clientInteractionHandler;
    private readonly IDistributedCache? _cache;

    public ToolExecutionManager(
        ILogger<ToolExecutionManager> logger,
        IClientInteractionHandler clientInteractionHandler,
        IDistributedCache? cache = null)
    {
        _logger = logger;
        _clientInteractionHandler = clientInteractionHandler;
        _cache = cache;
    }

    #region Tool Execution

    /// <summary>
    /// Represents a pending client-side tool or command waiting for execution.
    /// </summary>
    private sealed record PendingCommand(ClientInteractionRequest Request, FunctionCallContent Call);

    /// <inheritdoc />
    public async IAsyncEnumerable<ToolExecutionResult> ExecuteToolCallsAsync(
        SceneContext context,
        List<FunctionCallContent> functionCalls,
        List<ISceneTool> sceneTools,
        List<AIFunction> mcpTools,
        IReadOnlyList<ClientInteractionDefinition>? clientInteractionDefinitions,
        string sceneName,
        IJsonService? jsonService = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Deduplicate first
        var deduplicatedCalls = DeduplicateToolCalls(functionCalls);
        if (deduplicatedCalls.Count != functionCalls.Count)
        {
            _logger.LogInformation("🔧 Deduplicated {Removed} tool calls ({Original} → {New})",
                functionCalls.Count - deduplicatedCalls.Count, functionCalls.Count, deduplicatedCalls.Count);
        }

        jsonService ??= new DefaultJsonService();
        var pendingCommands = new List<PendingCommand>();

        // Process ALL calls - separate Commands from immediate server tools
        for (var i = 0; i < deduplicatedCalls.Count; i++)
        {
            var functionCall = deduplicatedCalls[i];

            _logger.LogDebug("Processing tool call {Index}/{Total}: '{ToolName}' (CallId: {CallId})",
                i + 1, deduplicatedCalls.Count, functionCall.Name, functionCall.CallId);

            // Check if this is a client-side tool
            var clientRequest = clientInteractionDefinitions != null
                ? _clientInteractionHandler.CreateRequestIfClientTool(
                    clientInteractionDefinitions,
                    functionCall.Name,
                    functionCall.Arguments?.ToDictionary(x => x.Key, x => x.Value))
                : null;

            if (clientRequest != null)
            {
                // CLIENT TOOL - Check if Command with feedbackMode='never'
                if (clientRequest.IsCommand && clientRequest.FeedbackMode == CommandFeedbackMode.Never)
                {
                    // Auto-complete IMMEDIATELY with success
                    var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                    {
                        Result = "true"  // Commands with 'never' auto-complete immediately
                    };
                    context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

                    _logger.LogInformation(
                        "✅ Command '{ToolName}' auto-completed immediately (feedbackMode: Never). No client response needed.",
                        functionCall.Name);

                    // Continue processing other calls
                    continue;
                }

                // CLIENT TOOL - Add to pending list (will be processed after server tools)
                pendingCommands.Add(new PendingCommand(clientRequest, functionCall));

                _logger.LogDebug("📝 Added '{ToolName}' to pending {Type} list (feedbackMode: {FeedbackMode})",
                    functionCall.Name,
                    clientRequest.IsCommand ? "Commands" : "Tools",
                    clientRequest.FeedbackMode);

                // Continue processing other calls (don't yield break here!)
                continue;
            }

            // SERVER TOOL - Execute immediately
            yield return new ToolExecutionResult
            {
                Status = ToolExecutionStatus.Started,
                ToolName = functionCall.Name,
                Message = $"Executing tool: {functionCall.Name}"
            };

            // Track tool execution
            var toolKey = $"{sceneName}.{functionCall.Name}";
            context.ExecutedTools.Add(toolKey);

            // Find the scene tool
            var normalizedFunctionName = ToolNameNormalizer.Normalize(functionCall.Name);
            var sceneTool = sceneTools.FirstOrDefault(t => t.Name == normalizedFunctionName);

            if (sceneTool == null)
            {
                // Tool not found - could be MCP tool or error
                _logger.LogWarning("Tool '{ToolName}' not found in scene tools.", functionCall.Name);

                var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                {
                    Result = $"Tool '{functionCall.Name}' not found"
                };
                context.AddToolMessage(new ChatMessage(ChatRole.Tool, [errorResult]));

                yield return new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.Error,
                    ToolName = functionCall.Name,
                    Error = $"Tool '{functionCall.Name}' not found"
                };
                continue;
            }

            // Execute scene tool and capture result
            var executionResult = await ExecuteSceneToolAsync(sceneTool, functionCall, context, jsonService, cancellationToken);
            yield return executionResult;
        }

        // After processing all calls, handle pending client tools/commands (if any)
        if (pendingCommands.Count > 0)
        {
            // Save batch state for resumption
            var batch = new ClientInteractionBatch
            {
                SceneName = sceneName,
                Interactions = pendingCommands.Select(pc => new PendingClientInteraction
                {
                    InteractionId = pc.Request.InteractionId,
                    CallId = pc.Call.CallId,
                    ToolName = pc.Call.Name,
                    IsCommand = pc.Request.IsCommand,
                    FeedbackMode = pc.Request.FeedbackMode
                }).ToList()
            };

            var batchJson = JsonSerializer.Serialize(batch);
            context.SetProperty(ClientInteractionBatchKey, batchJson);
            // Also persist to distributed cache for cross-request survival
            await PersistBatchToCacheAsync(context.ConversationKey, batchJson);
            context.ExecutionPhase = ExecutionPhase.AwaitingClient;

            _logger.LogInformation(
                "Saved {Count} pending client interaction(s) for scene '{SceneName}'",
                pendingCommands.Count, sceneName);

            // Yield ALL pending client interactions
            foreach (var pending in pendingCommands)
            {
                _logger.LogInformation("🎯 Client {Type} '{ToolName}' pending execution (feedbackMode: {FeedbackMode})",
                    pending.Request.IsCommand ? "Command" : "Tool",
                    pending.Call.Name,
                    pending.Request.FeedbackMode);

                yield return new ToolExecutionResult
                {
                    Status = pending.Request.IsCommand
                        ? ToolExecutionStatus.CommandClient
                        : ToolExecutionStatus.AwaitingClient,
                    ToolName = pending.Call.Name,
                    Message = $"Awaiting client execution of {(pending.Request.IsCommand ? "command" : "tool")}: {pending.Call.Name}",
                    ClientRequest = pending.Request
                };
            }

            // IMPORTANT: Caller should yield break after receiving AwaitingClient/CommandClient
            yield break;
        }
    }

    /// <summary>
    /// Execute a scene tool and return the result without yielding in try/catch.
    /// </summary>
    private async Task<ToolExecutionResult> ExecuteSceneToolAsync(
        ISceneTool sceneTool,
        FunctionCallContent functionCall,
        SceneContext context,
        IJsonService jsonService,
        CancellationToken cancellationToken)
    {
        using var activity = PlayFrameworkActivitySource.Instance.StartActivity(
            PlayFrameworkActivitySource.Activities.ToolExecute, ActivityKind.Internal);
        activity?.SetTag(PlayFrameworkActivitySource.Tags.ToolName, functionCall.Name);
        activity?.SetTag(PlayFrameworkActivitySource.Tags.ToolType, "SceneTool");
        activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.ToolCalled));

        PlayFrameworkMetrics.IncrementActiveToolCalls();
        var startTime = DateTime.UtcNow;
        var success = false;

        try
        {
            ToolExecutionResult result;
            try
            {
                var argsJson = jsonService.Serialize(functionCall.Arguments ?? new Dictionary<string, object?>());
                var toolResult = await sceneTool.ExecuteAsync(argsJson, context, cancellationToken);

                var functionResult = CreateFunctionResult(functionCall, toolResult);
                context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

                result = new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.Completed,
                    ToolName = functionCall.Name,
                    ToolResult = toolResult,
                    Message = $"Tool {functionCall.Name} completed"
                };
                success = true;
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.ToolCompleted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", functionCall.Name);

                var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
                {
                    Result = $"Error executing tool: {ex.Message}"
                };
                context.AddToolMessage(new ChatMessage(ChatRole.Tool, [errorResult]));

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("exception.message", ex.Message);
                activity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.ToolFailed));

                result = new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.Error,
                    ToolName = functionCall.Name,
                    Error = ex.Message
                };
            }

            return result;
        }
        finally
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            PlayFrameworkMetrics.DecrementActiveToolCalls();
            PlayFrameworkMetrics.RecordToolCall(
                toolName: functionCall.Name,
                toolType: "SceneTool",
                success: success,
                durationMs: duration);
            _logger.LogDebug("Tool '{ToolName}' executed in {Duration:F1}ms (success: {Success})",
                functionCall.Name, duration, success);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ToolExecutionResult> ResumeAfterClientResponseAsync(
        SceneContext context,
        List<ClientInteractionResult> clientResults,
        List<ISceneTool> sceneTools,
        List<AIFunction> mcpTools,
        string sceneName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Delegate to the unified resolve method
        await foreach (var result in ResolveClientInteractionsAsync(context, clientResults, cancellationToken))
        {
            yield return result;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ToolExecutionResult> ResolveClientInteractionsAsync(
        SceneContext context,
        List<ClientInteractionResult>? clientResults,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Try to load batch from context Properties first, then fall back to distributed cache
        string? batchJson = null;
        if (context.Properties.TryGetValue(ClientInteractionBatchKey, out var batchObj) && batchObj is string propJson)
        {
            batchJson = propJson;
        }
        else
        {
            // Load from distributed cache (cross-request persistence)
            batchJson = await LoadBatchFromCacheAsync(context.ConversationKey);
        }

        if (string.IsNullOrEmpty(batchJson))
        {
            _logger.LogDebug("No client interaction batch found for resolution");
            yield break;
        }

        ClientInteractionBatch batch;
        try
        {
            batch = JsonSerializer.Deserialize<ClientInteractionBatch>(batchJson)
                ?? throw new InvalidOperationException("Failed to deserialize client interaction batch");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize client interaction batch. Skipping resolution.");
            yield break;
        }

        if (batch.Interactions.Count == 0)
        {
            _logger.LogDebug("Client interaction batch is empty");
            await ClearBatchStateAsync(context);
            yield break;
        }

        _logger.LogInformation("Resolving {Count} client interaction(s) for scene '{SceneName}'",
            batch.Interactions.Count, batch.SceneName);

        // Index client results by InteractionId for O(1) lookup
        var clientResultsById = clientResults?
            .ToDictionary(r => r.InteractionId, r => r)
            ?? new Dictionary<string, ClientInteractionResult>();

        // Process EACH pending interaction with correct CallId mapping
        foreach (var pending in batch.Interactions)
        {
            FunctionResultContent functionResult;

            if (clientResultsById.TryGetValue(pending.InteractionId, out var clientResult))
            {
                // Client sent explicit result for this interaction
                var resultText = BuildClientResultText(clientResult);

                // Use the ORIGINAL CallId and ToolName from OpenAI (not the InteractionId)
                functionResult = new FunctionResultContent(pending.CallId, pending.ToolName)
                {
                    Result = resultText
                };

                _logger.LogInformation(
                    "Client interaction '{ToolName}' resolved with client result (CallId: {CallId}, InteractionId: {InteractionId})",
                    pending.ToolName, pending.CallId, pending.InteractionId);
            }
            else if (pending.IsCommand && pending.FeedbackMode == CommandFeedbackMode.OnError)
            {
                // Command with OnError: no result from client means success - auto-complete
                functionResult = new FunctionResultContent(pending.CallId, pending.ToolName)
                {
                    Result = "true"
                };

                _logger.LogInformation(
                    "Command '{ToolName}' auto-completed (OnError, no client result = success) (CallId: {CallId})",
                    pending.ToolName, pending.CallId);
            }
            else if (pending.IsCommand && pending.FeedbackMode == CommandFeedbackMode.Never)
            {
                // Never commands should not be in the batch (handled immediately), but handle gracefully
                functionResult = new FunctionResultContent(pending.CallId, pending.ToolName)
                {
                    Result = "true"
                };

                _logger.LogWarning(
                    "Command '{ToolName}' with FeedbackMode.Never found in batch (unexpected). Auto-completing. (CallId: {CallId})",
                    pending.ToolName, pending.CallId);
            }
            else if (pending.IsCommand && pending.FeedbackMode == CommandFeedbackMode.Always)
            {
                // Command with Always: client MUST send result. Missing = error.
                functionResult = new FunctionResultContent(pending.CallId, pending.ToolName)
                {
                    Result = "false: No feedback received from client (FeedbackMode.Always requires explicit result)"
                };

                _logger.LogWarning(
                    "Command '{ToolName}' (Always) missing client result - reporting failure (CallId: {CallId})",
                    pending.ToolName, pending.CallId);

                yield return new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.Error,
                    ToolName = pending.ToolName,
                    Error = $"Command '{pending.ToolName}' requires feedback (FeedbackMode.Always) but no result was provided"
                };
            }
            else
            {
                // ClientTool (AwaitingClient): client MUST send result. Missing = error.
                functionResult = new FunctionResultContent(pending.CallId, pending.ToolName)
                {
                    Result = "Error: No result received from client for this tool"
                };

                _logger.LogWarning(
                    "ClientTool '{ToolName}' missing required client result (CallId: {CallId})",
                    pending.ToolName, pending.CallId);

                yield return new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.Error,
                    ToolName = pending.ToolName,
                    Error = $"Client tool '{pending.ToolName}' requires a result but none was provided"
                };
            }

            // Add tool message with the ORIGINAL CallId (OpenAI compliance)
            context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));
        }

        // Clear batch state
        await ClearBatchStateAsync(context);

        _logger.LogInformation("Resolved {Count} client interaction(s) successfully", batch.Interactions.Count);
    }

    /// <summary>
    /// The single property key used to store the ClientInteractionBatch in context.
    /// </summary>
    internal const string ClientInteractionBatchKey = "_clientInteractionBatch";

    private async Task ClearBatchStateAsync(SceneContext context)
    {
        context.Properties.Remove(ClientInteractionBatchKey);

        // Remove from distributed cache
        if (_cache != null && !string.IsNullOrEmpty(context.ConversationKey))
        {
            await _cache.RemoveAsync(GetBatchCacheKey(context.ConversationKey));
        }

        // Clean up legacy properties (backward compatibility with old cached conversations)
        context.Properties.Remove("_continuation_sceneName");
        context.Properties.Remove("_pending_commands");
        context.Properties.Remove("_pending_tools");
        context.Properties.Remove("_continuation_interactionId");
        context.Properties.Remove("_continuation_callId");
        context.Properties.Remove("_continuation_toolName");
        context.Properties.Remove("_continuation_isCommand");

        context.ExecutionPhase = ExecutionPhase.ExecutingScene;
    }

    private async Task PersistBatchToCacheAsync(string? conversationKey, string batchJson)
    {
        if (_cache == null || string.IsNullOrEmpty(conversationKey))
            return;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };
        await _cache.SetStringAsync(GetBatchCacheKey(conversationKey), batchJson, options);
    }

    public async Task<string?> LoadBatchFromCacheAsync(string? conversationKey)
    {
        if (_cache == null || string.IsNullOrEmpty(conversationKey))
            return null;

        return await _cache.GetStringAsync(GetBatchCacheKey(conversationKey));
    }

    private static string GetBatchCacheKey(string conversationKey)
        => $"pf:clientbatch:{conversationKey}";

    private static string BuildClientResultText(ClientInteractionResult clientResult)
    {
        if (!string.IsNullOrEmpty(clientResult.Error))
        {
            return $"Client tool error: {clientResult.Error}";
        }

        if (clientResult.Contents != null && clientResult.Contents.Count > 0)
        {
            var contentParts = new List<string>();
            foreach (var content in clientResult.Contents)
            {
                if (string.Equals(content.Type, "text", StringComparison.OrdinalIgnoreCase))
                {
                    contentParts.Add(content.Text ?? "");
                }
                else if (string.Equals(content.Type, "data", StringComparison.OrdinalIgnoreCase))
                {
                    contentParts.Add($"[Binary data: {content.MediaType ?? "unknown"}]");
                }
                else
                {
                    contentParts.Add($"[Content: {content.Type}]");
                }
            }
            return string.Join("\n", contentParts);
        }

        return "Client tool executed successfully (no data returned)";
    }

    private static FunctionResultContent CreateFunctionResult(FunctionCallContent functionCall, object? toolResult)
    {
        var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name);

        if (toolResult is AIContent aiContent)
        {
            functionResult.Result = aiContent;
        }
        else if (toolResult is IEnumerable<AIContent> aiContents)
        {
            functionResult.Result = aiContents;
        }
        else
        {
            functionResult.Result = toolResult;
        }

        return functionResult;
    }

    #endregion

    #region Deduplication

    /// <inheritdoc />
    public ChatResponse DeduplicateToolCalls(ChatResponse response)
    {
        if (response.Messages == null || response.Messages.Count == 0)
            return response;

        foreach (var message in response.Messages)
        {
            if (message.Contents != null)
            {
                message.Contents.RemoveWhere(x => x is TextContent text && string.IsNullOrWhiteSpace(text.Text));
                message.Contents = [.. message.Contents.GroupBy(x =>
                {
                    if (x is FunctionCallContent functionCall)
                    {
                        return GetToolCallKey(functionCall);
                    }
                    return string.Empty;
                }).Select(x => x.First())];
            }
        }

        return response;
    }

    /// <inheritdoc />
    public List<FunctionCallContent> DeduplicateToolCalls(List<FunctionCallContent> toolCalls)
        => [.. toolCalls.GroupBy(GetToolCallKey).Select(x => x.First())];

    /// <summary>
    /// Creates a unique key for a tool call based on name + serialized arguments.
    /// </summary>
    private static string GetToolCallKey(FunctionCallContent tc)
    {
        var argsString = tc.Arguments != null
            ? string.Join(",", tc.Arguments.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}"))
            : "";

        return $"{tc.Name}|{argsString}";
    }

    #endregion

    #region Internal Types

    /// <summary>
    /// Serializable representation of FunctionCallContent for Properties storage.
    /// Used to save pending tool calls when awaiting client response.
    #endregion
}
