using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Rystem;
using Rystem.PlayFramework.Configuration;
using Rystem.PlayFramework.Helpers;
using Rystem.PlayFramework.Services;

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

    public ToolExecutionManager(
        ILogger<ToolExecutionManager> logger,
        IClientInteractionHandler clientInteractionHandler)
    {
        _logger = logger;
        _clientInteractionHandler = clientInteractionHandler;
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Deduplicate first
        var deduplicatedCalls = DeduplicateToolCalls(functionCalls);
        if (deduplicatedCalls.Count != functionCalls.Count)
        {
            _logger.LogInformation("🔧 Deduplicated {Removed} tool calls ({Original} → {New})",
                functionCalls.Count - deduplicatedCalls.Count, functionCalls.Count, deduplicatedCalls.Count);
        }

        var jsonService = new DefaultJsonService();
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
            SavePendingCommandsState(context, sceneName, pendingCommands);

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
        try
        {
            var argsJson = jsonService.Serialize(functionCall.Arguments ?? new Dictionary<string, object?>());
            var toolResult = await sceneTool.ExecuteAsync(argsJson, context, cancellationToken);

            var functionResult = CreateFunctionResult(functionCall, toolResult);
            context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

            return new ToolExecutionResult
            {
                Status = ToolExecutionStatus.Completed,
                ToolName = functionCall.Name,
                ToolResult = toolResult,
                Message = $"Tool {functionCall.Name} completed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", functionCall.Name);

            var errorResult = new FunctionResultContent(functionCall.CallId, functionCall.Name)
            {
                Result = $"Error executing tool: {ex.Message}"
            };
            context.AddToolMessage(new ChatMessage(ChatRole.Tool, [errorResult]));

            return new ToolExecutionResult
            {
                Status = ToolExecutionStatus.Error,
                ToolName = functionCall.Name,
                Error = ex.Message
            };
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
        var originalCallId = context.Properties.TryGetValue("_continuation_callId", out var cid) ? cid as string : null;
        var originalToolName = context.Properties.TryGetValue("_continuation_toolName", out var tn) ? tn as string : null;

        // Inject client results into conversation
        foreach (var clientResult in clientResults)
        {
            var resultText = BuildClientResultText(clientResult);

            var functionResult = new FunctionResultContent(
                originalCallId ?? clientResult.InteractionId,
                originalToolName ?? clientResult.InteractionId)
            {
                Result = resultText
            };
            context.AddToolMessage(new ChatMessage(ChatRole.Tool, [functionResult]));

            _logger.LogInformation("✅ Injected client result for '{InteractionId}' into conversation",
                clientResult.InteractionId);
        }

        // Execute pending server tools
        if (context.Properties.TryGetValue("_pending_tools", out var pendingJson) && pendingJson is string pendingToolsJson)
        {
            var pendingToolCalls = JsonSerializer.Deserialize<List<SerializedFunctionCall>>(pendingToolsJson);
            if (pendingToolCalls != null && pendingToolCalls.Count > 0)
            {
                _logger.LogInformation("Executing {Count} pending server tools after client response",
                    pendingToolCalls.Count);

                // Convert back to FunctionCallContent
                var functionCalls = pendingToolCalls.Select(pc => new FunctionCallContent(pc.CallId, pc.Name)
                {
                    Arguments = pc.Arguments
                }).ToList();

                await foreach (var result in ExecuteToolCallsAsync(
                    context, functionCalls, sceneTools, mcpTools, null, sceneName, cancellationToken))
                {
                    yield return result;
                }

                context.Properties.Remove("_pending_tools");
            }
        }

        // Clear continuation properties
        ClearContinuationState(context);
    }

    /// <summary>
    /// Saves state for ALL pending client tools/commands that need execution.
    /// Stores them as a JSON array so AutoCompleteIncompleteCommandAsync can process all of them.
    /// </summary>
    private void SavePendingCommandsState(
        SceneContext context,
        string sceneName,
        List<PendingCommand> pendingCommands)
    {
        // Serialize ALL pending commands to JSON array
        var serializedCommands = pendingCommands.Select(pc => new SerializedPendingCommand
        {
            InteractionId = pc.Request.InteractionId,
            CallId = pc.Call.CallId,
            ToolName = pc.Call.Name,
            IsCommand = pc.Request.IsCommand,
            FeedbackMode = pc.Request.FeedbackMode
        }).ToList();

        var commandsJson = JsonSerializer.Serialize(serializedCommands);
        context.SetProperty("_pending_commands", commandsJson);
        context.SetProperty("_continuation_sceneName", sceneName);

        // Set phase to AwaitingClient (will auto-complete on next message)
        context.ExecutionPhase = ExecutionPhase.AwaitingClient;

        _logger.LogInformation(
            "💾 Saved {Count} pending client {Type} for delayed completion. SceneName: {SceneName}",
            pendingCommands.Count,
            pendingCommands.Any(pc => pc.Request.IsCommand) ? "Commands/Tools" : "Tools",
            sceneName);

        foreach (var cmd in pendingCommands)
        {
            _logger.LogDebug("  - '{ToolName}' (CallId: {CallId}, feedbackMode: {FeedbackMode})",
                cmd.Call.Name, cmd.Call.CallId, cmd.Request.FeedbackMode);
        }
    }

    private void ClearContinuationState(SceneContext context)
    {
        context.Properties.Remove("_continuation_sceneName");
        context.Properties.Remove("_pending_commands");
        context.Properties.Remove("_pending_tools");

        // Legacy properties (kept for backward compatibility)
        context.Properties.Remove("_continuation_interactionId");
        context.Properties.Remove("_continuation_callId");
        context.Properties.Remove("_continuation_toolName");
        context.Properties.Remove("_continuation_isCommand");

        context.ExecutionPhase = ExecutionPhase.ExecutingScene;
    }

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
    /// </summary>
    private sealed class SerializedFunctionCall
    {
        public string CallId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    #endregion
}

/// <summary>
/// Serialized representation of a pending command/tool for JSON storage.
/// </summary>
internal sealed record SerializedPendingCommand
{
    public required string InteractionId { get; init; }
    public required string CallId { get; init; }
    public required string ToolName { get; init; }
    public required bool IsCommand { get; init; }
    public required CommandFeedbackMode FeedbackMode { get; init; }
}
