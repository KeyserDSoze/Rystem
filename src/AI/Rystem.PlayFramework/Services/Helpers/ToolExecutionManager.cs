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
        var deduplicatedCalls = DeduplicateToolCallsInternal(functionCalls);
        if (deduplicatedCalls.Count != functionCalls.Count)
        {
            _logger.LogInformation("🔧 Deduplicated {Removed} tool calls ({Original} → {New})",
                functionCalls.Count - deduplicatedCalls.Count, functionCalls.Count, deduplicatedCalls.Count);
        }

        var jsonService = new DefaultJsonService();

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
                // CLIENT TOOL - Save state and yield
                SaveClientToolState(context, sceneName, clientRequest, functionCall, deduplicatedCalls, i);

                _logger.LogInformation("🎯 Client tool '{ToolName}' detected. Awaiting client execution.",
                    functionCall.Name);

                yield return new ToolExecutionResult
                {
                    Status = ToolExecutionStatus.AwaitingClient,
                    ToolName = functionCall.Name,
                    Message = $"Awaiting client execution of tool: {functionCall.Name}",
                    ClientRequest = clientRequest
                };

                // IMPORTANT: Caller should yield break after receiving AwaitingClient
                yield break;
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

    private void SaveClientToolState(
        SceneContext context,
        string sceneName,
        ClientInteractionRequest clientRequest,
        FunctionCallContent currentCall,
        List<FunctionCallContent> allCalls,
        int currentIndex)
    {
        context.SetProperty("_continuation_sceneName", sceneName);
        context.SetProperty("_continuation_interactionId", clientRequest.InteractionId);
        context.SetProperty("_continuation_callId", currentCall.CallId);
        context.SetProperty("_continuation_toolName", currentCall.Name);

        // Save remaining tools as pending
        var remainingCalls = allCalls.Skip(currentIndex + 1).ToList();
        if (remainingCalls.Count > 0)
        {
            var serializedCalls = remainingCalls.Select(fc => new SerializedFunctionCall
            {
                CallId = fc.CallId,
                Name = fc.Name,
                Arguments = fc.Arguments
            }).ToList();

            var pendingJson = JsonSerializer.Serialize(serializedCalls);
            context.SetProperty("_pending_tools", pendingJson);

            _logger.LogInformation("Saved {Count} pending tool calls for execution after client response",
                remainingCalls.Count);
        }

        // Set phase to AwaitingClient
        context.ExecutionPhase = ExecutionPhase.AwaitingClient;
    }

    private void ClearContinuationState(SceneContext context)
    {
        context.Properties.Remove("_continuation_sceneName");
        context.Properties.Remove("_continuation_interactionId");
        context.Properties.Remove("_continuation_callId");
        context.Properties.Remove("_continuation_toolName");
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

    #region Sanitization

    /// <inheritdoc />
    public List<ChatMessage> GetMessagesForLLM(SceneContext context)
    {
        // Get active messages from context
        var messages = context.ConversationHistory
            .Where(m => m.IsActiveMessage)
            .Select(m => m.Message)
            .ToList();

        _logger.LogDebug("📋 GetMessagesForLLM: ExecutionPhase={Phase}, MessageCount={Count}",
            context.ExecutionPhase, messages.Count);

        // Skip sanitization if we're awaiting client response
        // In this state, tool_calls are intentionally incomplete (waiting for client execution)
        if (context.ExecutionPhase == ExecutionPhase.AwaitingClient)
        {
            _logger.LogDebug("⏭️ Skipping sanitization (AwaitingClient phase)");
            return messages;
        }

        return SanitizeMessages(messages);
    }

    /// <summary>
    /// Core sanitization logic - can be reused.
    /// </summary>
    private List<ChatMessage> SanitizeMessages(List<ChatMessage> messages)
    {
        // Track which tool_call_ids have been responded to
        var allToolCallIds = new HashSet<string>();
        var respondedToolCallIds = new HashSet<string>();

        // First pass: identify all tool_calls and their responses
        foreach (var message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                var toolCalls = message.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                foreach (var tc in toolCalls)
                {
                    if (!string.IsNullOrEmpty(tc.CallId))
                    {
                        allToolCallIds.Add(tc.CallId);
                    }
                }
            }
            else if (message.Role == ChatRole.Tool)
            {
                var toolResults = message.Contents?
                    .OfType<FunctionResultContent>()
                    .ToList() ?? [];

                foreach (var tr in toolResults)
                {
                    if (!string.IsNullOrEmpty(tr.CallId))
                    {
                        respondedToolCallIds.Add(tr.CallId);
                    }
                }
            }
        }

        // Identify missing responses
        var missingResponses = allToolCallIds.Except(respondedToolCallIds).ToList();
        if (missingResponses.Count > 0)
        {
            _logger.LogWarning("⚠️ Found {Count} tool_calls without responses: [{CallIds}]",
                missingResponses.Count, string.Join(", ", missingResponses));
        }

        // Second pass: build sanitized message list
        var result = new List<ChatMessage>();
        var currentPendingCallIds = new HashSet<string>();

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                var toolCalls = message.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                if (toolCalls.Count > 0)
                {
                    // Deduplicate tool calls within this message
                    var deduplicatedToolCalls = DeduplicateToolCallsInternal(toolCalls);

                    if (deduplicatedToolCalls.Count != toolCalls.Count)
                    {
                        _logger.LogInformation("🔧 Deduplicated {Removed} duplicate tool calls in assistant message",
                            toolCalls.Count - deduplicatedToolCalls.Count);
                    }

                    // Filter out calls that don't have responses (will cause errors)
                    var validToolCalls = deduplicatedToolCalls
                        .Where(tc => string.IsNullOrEmpty(tc.CallId) || respondedToolCallIds.Contains(tc.CallId))
                        .ToList();

                    var removedCalls = deduplicatedToolCalls.Count - validToolCalls.Count;
                    if (removedCalls > 0)
                    {
                        _logger.LogWarning("🔧 Removed {Count} tool_calls without responses from assistant message",
                            removedCalls);
                    }

                    if (validToolCalls.Count > 0)
                    {
                        // Rebuild message with only valid tool calls
                        var otherContents = message.Contents?
                            .Where(c => c is not FunctionCallContent)
                            .ToList() ?? [];

                        var newContents = new List<AIContent>(otherContents);
                        newContents.AddRange(validToolCalls);

                        result.Add(new ChatMessage(message.Role, newContents));

                        // Track these as pending (need tool responses after)
                        foreach (var tc in validToolCalls)
                        {
                            if (!string.IsNullOrEmpty(tc.CallId))
                            {
                                currentPendingCallIds.Add(tc.CallId);
                            }
                        }
                    }
                    else
                    {
                        // All tool calls were invalid - keep only text content if any
                        var textOnlyContents = message.Contents?
                            .Where(c => c is not FunctionCallContent)
                            .ToList() ?? [];

                        if (textOnlyContents.Count > 0)
                        {
                            result.Add(new ChatMessage(message.Role, textOnlyContents));
                        }
                        // Skip message entirely if it only had invalid tool calls
                    }
                }
                else
                {
                    // No tool calls, keep as-is
                    result.Add(message);
                }
            }
            else if (message.Role == ChatRole.Tool)
            {
                var toolResults = message.Contents?
                    .OfType<FunctionResultContent>()
                    .ToList() ?? [];

                // Only keep results that match pending calls
                var validResults = toolResults
                    .Where(tr => string.IsNullOrEmpty(tr.CallId) ||
                                 currentPendingCallIds.Contains(tr.CallId))
                    .ToList();

                if (validResults.Count > 0)
                {
                    // Mark these calls as responded
                    foreach (var vr in validResults)
                    {
                        if (!string.IsNullOrEmpty(vr.CallId))
                        {
                            currentPendingCallIds.Remove(vr.CallId);
                        }
                    }

                    // If original message had other content, preserve it
                    var otherContents = message.Contents?
                        .Where(c => c is not FunctionResultContent)
                        .ToList() ?? [];

                    var newContents = new List<AIContent>(otherContents);
                    newContents.AddRange(validResults);

                    result.Add(new ChatMessage(message.Role, newContents));
                }
                else
                {
                    _logger.LogDebug("🔧 Removed orphaned tool response message (no matching tool_calls)");
                }
            }
            else
            {
                // System, User, etc. - keep as-is
                result.Add(message);
            }
        }

        // Log final state
        if (result.Count != messages.Count)
        {
            _logger.LogInformation("🔧 Sanitized conversation: {Original} → {Sanitized} messages",
                messages.Count, result.Count);
        }

        return result;
    }

    /// <inheritdoc />
    public List<string> Validate(SceneContext context)
    {
        var messages = context.ConversationHistory
            .Where(m => m.IsActiveMessage)
            .Select(m => m.Message)
            .ToList();

        var errors = new List<string>();
        var pendingToolCallIds = new HashSet<string>();

        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];

            if (message.Role == ChatRole.Assistant)
            {
                var toolCalls = message.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                foreach (var tc in toolCalls)
                {
                    if (!string.IsNullOrEmpty(tc.CallId))
                    {
                        pendingToolCallIds.Add(tc.CallId);
                    }
                }
            }
            else if (message.Role == ChatRole.Tool)
            {
                var toolResults = message.Contents?
                    .OfType<FunctionResultContent>()
                    .ToList() ?? [];

                foreach (var tr in toolResults)
                {
                    if (!string.IsNullOrEmpty(tr.CallId))
                    {
                        if (!pendingToolCallIds.Contains(tr.CallId))
                        {
                            errors.Add($"Tool response at index {i} references unknown tool_call_id: {tr.CallId}");
                        }
                        pendingToolCallIds.Remove(tr.CallId);
                    }
                }
            }
        }

        // Check for unanswered tool calls (skip if awaiting client)
        if (context.ExecutionPhase != ExecutionPhase.AwaitingClient)
        {
            foreach (var pendingId in pendingToolCallIds)
            {
                errors.Add($"Tool call '{pendingId}' has no corresponding tool response");
            }
        }

        return errors;
    }

    /// <inheritdoc />
    public List<string> GetPendingToolCallIds(SceneContext context)
    {
        var messages = context.ConversationHistory
            .Where(m => m.IsActiveMessage)
            .Select(m => m.Message);

        var pendingToolCallIds = new HashSet<string>();

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.Assistant)
            {
                var toolCalls = message.Contents?
                    .OfType<FunctionCallContent>()
                    .ToList() ?? [];

                foreach (var tc in toolCalls)
                {
                    if (!string.IsNullOrEmpty(tc.CallId))
                    {
                        pendingToolCallIds.Add(tc.CallId);
                    }
                }
            }
            else if (message.Role == ChatRole.Tool)
            {
                var toolResults = message.Contents?
                    .OfType<FunctionResultContent>()
                    .ToList() ?? [];

                foreach (var tr in toolResults)
                {
                    if (!string.IsNullOrEmpty(tr.CallId))
                    {
                        pendingToolCallIds.Remove(tr.CallId);
                    }
                }
            }
        }

        return [.. pendingToolCallIds];
    }

    #endregion

    #region Deduplication

    /// <inheritdoc />
    public ChatResponse DeduplicateToolCalls(ChatResponse response)
    {
        if (response.Messages == null || response.Messages.Count == 0)
            return response;

        var modified = false;
        var newMessages = new List<ChatMessage>();

        foreach (var message in response.Messages)
        {
            var toolCalls = message.Contents?
                .OfType<FunctionCallContent>()
                .ToList() ?? [];

            if (toolCalls.Count > 1)
            {
                var deduplicated = DeduplicateToolCallsInternal(toolCalls);

                if (deduplicated.Count != toolCalls.Count)
                {
                    modified = true;
                    _logger.LogInformation("🔧 Deduplicated {Removed} tool calls in ChatResponse ({Original} → {New})",
                        toolCalls.Count - deduplicated.Count, toolCalls.Count, deduplicated.Count);

                    // Rebuild message with deduplicated tool calls
                    var otherContents = message.Contents?
                        .Where(c => c is not FunctionCallContent)
                        .ToList() ?? [];

                    var newContents = new List<AIContent>(otherContents);
                    newContents.AddRange(deduplicated);

                    newMessages.Add(new ChatMessage(message.Role, newContents));
                    continue;
                }
            }

            newMessages.Add(message);
        }

        if (modified)
        {
            return new ChatResponse(newMessages)
            {
                FinishReason = response.FinishReason,
                ModelId = response.ModelId,
                CreatedAt = response.CreatedAt,
                ResponseId = response.ResponseId,
                Usage = response.Usage
            };
        }

        return response;
    }

    /// <inheritdoc />
    public List<FunctionCallContent> DeduplicateToolCalls(List<FunctionCallContent> toolCalls)
    {
        return DeduplicateToolCallsInternal(toolCalls);
    }

    /// <summary>
    /// Internal deduplication logic.
    /// OpenAI sometimes returns duplicate tool_calls with different CallIds but same name+args.
    /// Keep only the first occurrence.
    /// </summary>
    private List<FunctionCallContent> DeduplicateToolCallsInternal(List<FunctionCallContent> toolCalls)
    {
        var seen = new HashSet<string>();
        var result = new List<FunctionCallContent>();

        foreach (var tc in toolCalls)
        {
            var key = GetToolCallKey(tc);

            if (seen.Add(key))
            {
                result.Add(tc);
            }
            else
            {
                _logger.LogDebug("🔧 Removed duplicate tool call: {Name} (CallId: {CallId})",
                    tc.Name, tc.CallId);
            }
        }

        return result;
    }

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
