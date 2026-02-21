using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Holds all state for a single request execution.
/// ConversationHistory is the single source of truth for all messages.
/// </summary>
public sealed class SceneContext
{
    /// <summary>
    /// Service provider for dependency resolution.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// User's multi-modal input (text, images, audio, files).
    /// </summary>
    public required MultiModalInput Input { get; set; }
    /// <summary>
    /// Gets or sets the current phase of execution for the process.
    /// </summary>
    /// <remarks>The execution phase indicates the current state of the process, which can affect the behavior
    /// of subsequent operations.</remarks>
    public ExecutionPhase ExecutionPhase { get; set; }
    /// <summary>
    /// User's input message (text part only).
    /// </summary>
    public string InputMessage => Input.Text ?? string.Empty;

    /// <summary>
    /// Request metadata (userId, tenantId, sessionId, etc.).
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Chat client manager with retry, fallback, and cost calculation.
    /// </summary>
    public required IChatClientManager ChatClientManager { get; set; }

    /// <summary>
    /// All responses generated during execution (for backward compatibility).
    /// </summary>
    public List<AiSceneResponse> Responses { get; init; } = [];

    /// <summary>
    /// Current execution plan (if planning is enabled).
    /// </summary>
    public ExecutionPlan? ExecutionPlan { get; set; }

    /// <summary>
    /// Total cost accumulated during execution.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Unique key for this conversation (used by cache and memory).
    /// </summary>
    public string? ConversationKey { get; set; }

    /// <summary>
    /// Cache behavior for this request.
    /// </summary>
    public CacheBehavior CacheBehavior { get; set; } = CacheBehavior.Default;

    /// <summary>
    /// Dynamic properties for extensions.
    /// </summary>
    public Dictionary<object, object> Properties { get; init; } = [];

    /// <summary>
    /// Tracks executed scenes and tools (for loop prevention and chaining).
    /// </summary>
    public Dictionary<string, HashSet<SceneRequestContext>> ExecutedScenes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Executed tool keys for quick lookup.
    /// </summary>
    public HashSet<string> ExecutedTools { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Scene results for dynamic chaining.
    /// </summary>
    public Dictionary<string, string> SceneResults { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Ordered list of executed scene names.
    /// </summary>
    public List<string> ExecutedSceneOrder { get; init; } = [];

    #region Conversation History - Single Source of Truth

    /// <summary>
    /// Complete conversation history with business metadata.
    /// Messages with Message flag are sent to LLM.
    /// </summary>
    public List<TrackedMessage> ConversationHistory { get; init; } = [];

    /// <summary>
    /// Builds the initial system message (Context + MainActors).
    /// This message always has Message flag and is never removed.
    /// </summary>
    public void BuildInitialContext(object? contextResult, IEnumerable<string> mainActorOutputs)
    {
        var builder = new StringBuilder();

        // Context result as JSON
        if (contextResult != null)
        {
            builder.AppendLine("[Request Context]");
            builder.AppendLine(JsonSerializer.Serialize(contextResult, JsonHelper.JsonSerializerOptions));
            builder.AppendLine();
        }

        // MainActors as bullet points
        var actors = mainActorOutputs.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        if (actors.Count > 0)
        {
            builder.AppendLine("[System Instructions]");
            foreach (var actor in actors)
            {
                builder.AppendLine($"- {actor}");
            }
        }

        var content = builder.ToString().Trim();
        if (!string.IsNullOrEmpty(content))
        {
            // Always insert at beginning (or replace if exists)
            if (ConversationHistory.Count > 0 && ConversationHistory[0].Label == "InitialContext")
            {
                ConversationHistory[0] = TrackedMessage.CreateInitialContext(content);
            }
            else
            {
                ConversationHistory.Insert(0, TrackedMessage.CreateInitialContext(content));
            }
        }
    }

    /// <summary>
    /// Adds user message to conversation.
    /// </summary>
    public void AddUserMessage(MultiModalInput input)
        => ConversationHistory.Add(TrackedMessage.CreateUserMessage(input.ToChatMessage(ChatRole.User)));

    /// <summary>
    /// Adds user message to conversation.
    /// </summary>
    public void AddUserMessage(string message)
        => ConversationHistory.Add(TrackedMessage.CreateUserMessage(message));

    /// <summary>
    /// Adds assistant message to conversation.
    /// </summary>
    public void AddAssistantMessage(ChatMessage message)
    {
        var noFunctionContent = message.Contents.Where(x => x is not FunctionCallContent).ToList();
        if (noFunctionContent.Count > 0)
        {
            ConversationHistory.Add(TrackedMessage.CreateAssistantMessage(new ChatMessage(ChatRole.Assistant, noFunctionContent)));
        }
        foreach (var content in message.Contents.Where(x => x is FunctionCallContent))
        {
            ConversationHistory.Add(TrackedMessage.CreateAssistantMessage(new ChatMessage(ChatRole.Assistant, [content])));
        }
    }

    /// <summary>
    /// Adds tool result to conversation.
    /// </summary>
    public void AddToolMessage(ChatMessage message)
    {
        foreach (var content in message.Contents)
        {
            if (content is FunctionResultContent functionContent)
            {
                var index = ConversationHistory.FindIndex(x => x.Message.Contents.Any(t => t is FunctionCallContent functionCallContent && functionCallContent.CallId == functionContent.CallId));
                ConversationHistory.Insert(index + 1, TrackedMessage.CreateToolMessage(new ChatMessage(ChatRole.Tool, [content])));
            }
        }
    }

    /// <summary>
    /// Adds memory context (from storage).
    /// </summary>
    public void AddMemoryContext(string memoryContent)
    {
        // Insert after InitialContext (index 1)
        var insertIndex = ConversationHistory.Count > 0 && ConversationHistory[0].Label == "InitialContext" ? 1 : 0;
        ConversationHistory.Insert(insertIndex, TrackedMessage.CreateMemoryContext(memoryContent));
    }

    /// <summary>
    /// Adds memory context from ConversationMemory object.
    /// </summary>
    public void AddMemoryContext(ConversationMemory memory)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[Previous conversation memory - Conversation #{memory.ConversationCount}]");
        builder.AppendLine();
        builder.AppendLine($"Summary: {memory.Summary}");
        builder.AppendLine();

        if (memory.ImportantFacts.Count > 0)
        {
            builder.AppendLine("Important Facts:");
            builder.AppendLine(JsonSerializer.Serialize(memory.ImportantFacts, JsonHelper.JsonSerializerOptions));
        }

        builder.AppendLine();
        builder.AppendLine($"Last Updated: {memory.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC");

        AddMemoryContext(builder.ToString());
    }
    /// <summary>
    /// Add a system message with no cache, memory or somethingelse configured.
    /// </summary>
    /// <param name="content"></param>
    public void AddSystemMessage(string content)
        => ConversationHistory.Add(TrackedMessage.CreateSystemMessage(content));
    /// <summary>
    /// Adds a scene actor's output to conversation.
    /// Included in LLM requests and cache.
    /// </summary>
    public void AddSceneActorMessage(string sceneName, string actorName, string content)
        => ConversationHistory.Add(TrackedMessage.CreateSceneActorMessage(sceneName, actorName, content));

    /// <summary>
    /// Adds MCP context (resources/prompts) to conversation.
    /// Included in LLM requests and cache.
    /// </summary>
    public void AddMcpContextMessage(string sceneName, string content)
        => ConversationHistory.Add(TrackedMessage.CreateMcpContextMessage(sceneName, content));

    /// <summary>
    /// Gets messages for LLM request (only those with Message flag).
    /// Sanitizes messages to ensure OpenAI compliance (removes orphaned tool_calls and their responses).
    /// Skips sanitization if we're awaiting client response (tool_calls will be completed when client responds).
    /// </summary>
    public List<ChatMessage> GetMessagesForLLM()
    {
        var messages = ConversationHistory
            .Where(m => m.IsActiveMessage)
            .Select(m => m.Message)
            .ToList();

        // Debug logging - track message structure before sanitization
        System.Diagnostics.Debug.WriteLine($"[GetMessagesForLLM] ExecutionPhase: {ExecutionPhase}, Message count: {messages.Count}");
        for (int idx = 0; idx < messages.Count; idx++)
        {
            var msg = messages[idx];
            var toolCalls = msg.Contents?.OfType<FunctionCallContent>().Select(tc => tc.CallId).ToList();
            var toolResults = msg.Contents?.OfType<FunctionResultContent>().Select(tr => tr.CallId).ToList();
            System.Diagnostics.Debug.WriteLine($"  [{idx}] Role: {msg.Role}, ToolCalls: [{string.Join(", ", toolCalls ?? [])}], ToolResults: [{string.Join(", ", toolResults ?? [])}]");
        }

        // Skip sanitization if we're awaiting client response
        // In this state, tool_calls are intentionally incomplete (waiting for client execution)
        if (ExecutionPhase == ExecutionPhase.AwaitingClient)
        {
            System.Diagnostics.Debug.WriteLine("[GetMessagesForLLM] Skipping sanitization (AwaitingClient)");
            return messages;
        }

        // Sanitize: remove assistant messages with incomplete tool_calls
        // AND remove orphaned tool messages (tool messages without a valid preceding assistant)
        var sanitized = new List<ChatMessage>();
        var expectedToolCallIds = new HashSet<string>(); // Track which tool_call_ids are expected

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];

            if (msg.Role == ChatRole.Assistant)
            {
                var toolCalls = msg.Contents?.OfType<FunctionCallContent>().ToList();
                if (toolCalls != null && toolCalls.Count > 0)
                {
                    // Look ahead to verify ALL tool calls have responses
                    var toolCallIds = toolCalls.Select(tc => tc.CallId).ToHashSet();
                    var respondedCallIds = new HashSet<string>();

                    for (int j = i + 1; j < messages.Count; j++)
                    {
                        var nextMsg = messages[j];

                        // Stop at next turn boundary
                        if (nextMsg.Role == ChatRole.Assistant || nextMsg.Role == ChatRole.User)
                            break;

                        if (nextMsg.Role == ChatRole.Tool)
                        {
                            var toolResults = nextMsg.Contents?.OfType<FunctionResultContent>().ToList();
                            if (toolResults != null)
                            {
                                foreach (var result in toolResults)
                                    respondedCallIds.Add(result.CallId);
                            }
                        }
                    }

                    // If any tool_call lacks a response, skip this assistant entirely
                    if (!toolCallIds.All(id => respondedCallIds.Contains(id)))
                    {
                        var missingCallIds = toolCallIds.Where(id => !respondedCallIds.Contains(id)).ToList();
                        System.Diagnostics.Debug.WriteLine($"[GetMessagesForLLM] SKIPPING assistant at [{i}] - Missing responses for: [{string.Join(", ", missingCallIds)}]");
                        System.Diagnostics.Debug.WriteLine($"  ToolCallIds: [{string.Join(", ", toolCallIds)}]");
                        System.Diagnostics.Debug.WriteLine($"  RespondedCallIds: [{string.Join(", ", respondedCallIds)}]");
                        expectedToolCallIds.Clear(); // Don't expect any tools after skipped assistant
                        continue; // Skip this assistant message
                    }

                    // Valid assistant with complete tool_calls - track expected responses
                    expectedToolCallIds = toolCallIds;
                    sanitized.Add(msg);
                }
                else
                {
                    // Assistant without tool_calls - no tools expected after this
                    expectedToolCallIds.Clear();
                    sanitized.Add(msg);
                }
            }
            else if (msg.Role == ChatRole.User)
            {
                // User message starts new turn - no tools expected after this
                expectedToolCallIds.Clear();
                sanitized.Add(msg);
            }
            else if (msg.Role == ChatRole.Tool)
            {
                // Only include tool message if its CallId is expected
                var toolResults = msg.Contents?.OfType<FunctionResultContent>().ToList();
                if (toolResults != null)
                {
                    // Check if ANY result in this message matches expected CallIds
                    var hasValidCallId = toolResults.Any(r => expectedToolCallIds.Contains(r.CallId));
                    if (hasValidCallId)
                    {
                        sanitized.Add(msg);
                        // Remove matched CallIds from expected set
                        foreach (var result in toolResults)
                            expectedToolCallIds.Remove(result.CallId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetMessagesForLLM] SKIPPING orphaned tool message at [{i}] - CallIds: [{string.Join(", ", toolResults.Select(r => r.CallId))}]");
                    }
                    // Otherwise skip this orphaned tool message
                }
            }
            else
            {
                // System or other roles - always include
                sanitized.Add(msg);
            }
        }

        // Debug: log sanitized result
        System.Diagnostics.Debug.WriteLine($"[GetMessagesForLLM] After sanitization: {sanitized.Count} messages");
        for (int idx = 0; idx < sanitized.Count; idx++)
        {
            var msg = sanitized[idx];
            var toolCalls = msg.Contents?.OfType<FunctionCallContent>().Select(tc => tc.CallId).ToList();
            var toolResults = msg.Contents?.OfType<FunctionResultContent>().Select(tr => tr.CallId).ToList();
            System.Diagnostics.Debug.WriteLine($"  [{idx}] Role: {msg.Role}, ToolCalls: [{string.Join(", ", toolCalls ?? [])}], ToolResults: [{string.Join(", ", toolResults ?? [])}]");
        }

        return sanitized;
    }

    /// <summary>
    /// Gets messages for cache (includes InitialContext so it can be reused).
    /// </summary>
    public List<TrackedMessage> GetMessagesForCache()
        => [.. ConversationHistory.Where(m => m.ShouldCache)];

    /// <summary>
    /// Gets messages for memory storage.
    /// </summary>
    public List<TrackedMessage> GetMessagesForMemory()
        => [.. ConversationHistory.Where(m => m.ShouldSaveToMemory)];

    /// <summary>
    /// Gets messages for summarization.
    /// </summary>
    public List<TrackedMessage> GetMessagesForResume()
        => [.. ConversationHistory.Where(m => m.ShouldResume)];

    /// <summary>
    /// Applies summary: marks resumable messages as summarized and adds summary message.
    /// Summarized messages lose Message flag so they're no longer sent to LLM.
    /// </summary>
    public void ApplySummary(string summary)
    {
        // Mark all resumable messages as summarized (removes Message flag)
        foreach (var message in ConversationHistory.Where(m => m.ShouldResume))
        {
            message.MarkAsSummarized();
        }
        AddUserMessage(InputMessage);
        // Add summary message (has Message flag, will be sent to LLM)
        ConversationHistory.Add(TrackedMessage.CreateSummaryMessage(summary));
    }

    /// <summary>
    /// Restores conversation from cache.
    /// Replaces current conversation history with cached messages (including InitialContext).
    /// </summary>
    public void RestoreFromCache(IEnumerable<TrackedMessage> cachedMessages)
    {
        ConversationHistory.Clear();
        ConversationHistory.AddRange(cachedMessages);
    }

    #endregion

    /// <summary>
    /// Adds cost to total.
    /// </summary>
    public decimal AddCost(decimal cost)
    {
        TotalCost += cost;
        return TotalCost;
    }

    /// <summary>
    /// Marks a tool as executed.
    /// </summary>
    public void MarkToolExecuted(string sceneName, string toolName, string? args)
    {
        var key = $"{sceneName}.{toolName}.{args ?? "null"}";
        ExecutedTools.Add(key);

        if (!ExecutedScenes.ContainsKey(sceneName))
            ExecutedScenes[sceneName] = [];

        ExecutedScenes[sceneName].Add(new SceneRequestContext { ToolName = toolName, Arguments = args });
    }

    /// <summary>
    /// Gets a property by key.
    /// </summary>
    public T? GetProperty<T>(object key)
        => Properties.TryGetValue(key, out var value) ? (T?)value : default;

    /// <summary>
    /// Sets a property.
    /// </summary>
    public void SetProperty(object key, object value)
        => Properties[key] = value;

    /// <summary>
    /// Execution state restored from cache (if resuming).
    /// </summary>
    public ExecutionState? RestoredExecutionState { get; private set; }

    /// <summary>
    /// Whether this context was restored from cache.
    /// </summary>
    public bool IsResuming => RestoredExecutionState != null;

    /// <summary>
    /// Creates current execution state for saving.
    /// </summary>
    public ExecutionState CreateExecutionState(ExecutionPhase phase, string? currentSceneName = null)
        => ExecutionState.FromContext(this, phase, currentSceneName);

    /// <summary>
    /// Restores execution state from cache.
    /// Applies the state and adds a checkpoint message for the LLM.
    /// </summary>
    public void RestoreExecutionState(ExecutionState state)
    {
        state.ApplyToContext(this);
        RestoredExecutionState = state;

        // Add checkpoint message to inform LLM about previous execution
        // Insert after InitialContext and MemoryContext
        var insertIndex = ConversationHistory.Count;
        for (int i = 0; i < ConversationHistory.Count; i++)
        {
            var label = ConversationHistory[i].Label;
            if (label != "InitialContext" && label != "MemoryContext")
            {
                insertIndex = i;
                break;
            }
        }

        ConversationHistory.Insert(insertIndex, TrackedMessage.CreateExecutionCheckpoint(state));
    }
}
