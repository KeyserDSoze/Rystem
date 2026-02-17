using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

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
            builder.AppendLine(JsonSerializer.Serialize(contextResult, new JsonSerializerOptions { WriteIndented = true }));
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
        => ConversationHistory.Add(TrackedMessage.CreateAssistantMessage(message));

    /// <summary>
    /// Adds assistant message to conversation.
    /// </summary>
    public void AddAssistantMessage(string message)
        => ConversationHistory.Add(TrackedMessage.CreateAssistantMessage(message));

    /// <summary>
    /// Adds tool result to conversation.
    /// </summary>
    public void AddToolMessage(ChatMessage message)
        => ConversationHistory.Add(TrackedMessage.CreateToolMessage(message));

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
            builder.AppendLine(JsonSerializer.Serialize(memory.ImportantFacts, new JsonSerializerOptions { WriteIndented = true }));
        }

        builder.AppendLine();
        builder.AppendLine($"Last Updated: {memory.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC");

        AddMemoryContext(builder.ToString());
    }

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
    /// </summary>
    public List<ChatMessage> GetMessagesForLLM()
        => [.. ConversationHistory.Where(m => m.IsActiveMessage).Select(m => m.Message)];

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
