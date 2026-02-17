using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Flags indicating message behavior and storage.
/// </summary>
/// <remarks>
/// Message types and their flags:
/// <list type="table">
///     <listheader>
///         <term>Type</term>
///         <description>Message | Cache | Memory | Resume</description>
///     </listheader>
///     <item><term>InitialContext</term><description>✓ | ✓ | ✗ | ✗ (cached for reuse)</description></item>
///     <item><term>MemoryContext</term><description>✓ | ✗ | ✗ | ✗ (loaded from storage)</description></item>
///     <item><term>ExecutionCheckpoint</term><description>✓ | ✗ | ✗ | ✗ (derived from cached state)</description></item>
///     <item><term>SceneActor</term><description>✓ | ✓ | ✗ | ✗ (scene-specific context)</description></item>
///     <item><term>McpContext</term><description>✓ | ✓ | ✗ | ✗ (scene-specific context)</description></item>
///     <item><term>User</term><description>✓ | ✓ | ✓ | ✓</description></item>
///     <item><term>Assistant</term><description>✓ | ✓ | ✓ | ✓</description></item>
///     <item><term>Tool</term><description>✓ | ✓ | ✗ | ✓</description></item>
///     <item><term>Summary</term><description>✓ | ✓ | ✗ | ✗ (replaces summarized messages)</description></item>
/// </list>
/// </remarks>
[Flags]
public enum MessageBusinessType
{
    /// <summary>
    /// Message is transient and not used anywhere.
    /// </summary>
    None = 0,

    /// <summary>
    /// Message is included in LLM requests.
    /// When removed, message stops being sent to LLM but remains in history.
    /// </summary>
    Message = 1,

    /// <summary>
    /// Message is saved to cache.
    /// </summary>
    Cache = 2,

    /// <summary>
    /// Message is saved to long-term memory.
    /// </summary>
    Memory = 4,

    /// <summary>
    /// Message is included in summarization.
    /// After summarization, this flag is removed (along with Message flag).
    /// </summary>
    Resume = 8
}

/// <summary>
/// A ChatMessage with business metadata for tracking.
/// </summary>
public sealed class TrackedMessage
{
    /// <summary>
    /// Business type flags.
    /// </summary>
    public MessageBusinessType BusinessType { get; set; }

    /// <summary>
    /// The underlying ChatMessage.
    /// </summary>
    public required ChatMessage Message { get; set; }

    /// <summary>
    /// Label for debugging.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Creates the initial context system message (Context + MainActors).
    /// Always has Message flag (never removed). Cached so it can be reused across requests.
    /// </summary>
    public static TrackedMessage CreateInitialContext(string content)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache, // Cached for reuse
            Message = new ChatMessage(ChatRole.System, content),
            Label = "InitialContext"
        };

    /// <summary>
    /// Creates a user message.
    /// Included in requests, cache, memory, and summarization.
    /// </summary>
    public static TrackedMessage CreateUserMessage(ChatMessage message)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache | MessageBusinessType.Memory | MessageBusinessType.Resume,
            Message = message,
            Label = "User"
        };

    /// <summary>
    /// Creates a user message from text.
    /// </summary>
    public static TrackedMessage CreateUserMessage(string content)
        => CreateUserMessage(new ChatMessage(ChatRole.User, content));

    /// <summary>
    /// Creates an assistant message.
    /// Included in requests, cache, memory, and summarization.
    /// </summary>
    public static TrackedMessage CreateAssistantMessage(ChatMessage message)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache | MessageBusinessType.Memory | MessageBusinessType.Resume,
            Message = message,
            Label = "Assistant"
        };

    /// <summary>
    /// Creates an assistant message from text.
    /// </summary>
    public static TrackedMessage CreateAssistantMessage(string content)
        => CreateAssistantMessage(new ChatMessage(ChatRole.Assistant, content));

    /// <summary>
    /// Creates a tool result message.
    /// Included in requests, cache, and summarization. Not in long-term memory.
    /// </summary>
    public static TrackedMessage CreateToolMessage(ChatMessage message)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache | MessageBusinessType.Resume,
            Message = message,
            Label = "Tool"
        };

    /// <summary>
    /// Creates a summary message (replaces summarized messages).
    /// Included in requests and cache. Not resumable (it IS the summary).
    /// </summary>
    public static TrackedMessage CreateSummaryMessage(string summary)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache,
            Message = new ChatMessage(ChatRole.System, $"[Previous conversation summary]\n{summary}"),
            Label = "Summary"
        };

    /// <summary>
    /// Creates a memory context message (loaded from storage).
    /// Included in requests only. Not cached/memorized (already in storage).
    /// </summary>
    public static TrackedMessage CreateMemoryContext(string memoryContent)
        => new()
        {
            BusinessType = MessageBusinessType.Message,
            Message = new ChatMessage(ChatRole.System, memoryContent),
            Label = "MemoryContext"
        };

    /// <summary>
    /// Creates a scene actor system message.
    /// Included in requests and cache. Not in memory (scene-specific context).
    /// </summary>
    public static TrackedMessage CreateSceneActorMessage(string sceneName, string actorName, string content)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache,
            Message = new ChatMessage(ChatRole.System, $"[Scene: {sceneName} - Actor: {actorName}]\n{content}"),
            Label = $"SceneActor:{sceneName}:{actorName}"
        };

    /// <summary>
    /// Creates an MCP context system message (resources/prompts from MCP server).
    /// Included in requests and cache. Not in memory (scene-specific context).
    /// </summary>
    public static TrackedMessage CreateMcpContextMessage(string sceneName, string content)
        => new()
        {
            BusinessType = MessageBusinessType.Message | MessageBusinessType.Cache,
            Message = new ChatMessage(ChatRole.System, content),
            Label = $"McpContext:{sceneName}"
        };

    /// <summary>
    /// Creates an execution checkpoint message that summarizes previous execution state.
    /// This helps the LLM understand what has already been done when resuming.
    /// Included in requests only (not cached - it's derived from cached state).
    /// </summary>
    public static TrackedMessage CreateExecutionCheckpoint(ExecutionState state)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("[Execution Checkpoint - Resuming from previous session]");
        builder.AppendLine();

        if (state.ExecutedSceneOrder.Count > 0)
        {
            builder.AppendLine($"Previously executed scenes ({state.ExecutedSceneOrder.Count}):");
            foreach (var sceneName in state.ExecutedSceneOrder)
            {
                builder.AppendLine($"  ✓ {sceneName}");

                // Add tool details if available
                if (state.ExecutedScenes.TryGetValue(sceneName, out var tools) && tools.Count > 0)
                {
                    foreach (var tool in tools)
                    {
                        builder.AppendLine($"      - Tool: {tool.ToolName}");
                    }
                }

                // Add result preview if available
                if (state.SceneResults.TryGetValue(sceneName, out var result) && !string.IsNullOrWhiteSpace(result))
                {
                    var preview = result.Length > 150 ? result[..150] + "..." : result;
                    builder.AppendLine($"      Result: {preview}");
                }
            }
            builder.AppendLine();
        }

        if (!string.IsNullOrEmpty(state.CurrentSceneName) && state.Phase == ExecutionPhase.ExecutingScene)
        {
            builder.AppendLine($"Currently executing scene: {state.CurrentSceneName}");
            builder.AppendLine();
        }

        builder.AppendLine($"Execution phase: {state.Phase}");
        builder.AppendLine($"Accumulated cost: {state.AccumulatedCost:F6}");
        builder.AppendLine($"State saved at: {state.SavedAt:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();
        builder.AppendLine("Continue from where we left off. Do not repeat already completed actions.");

        return new()
        {
            BusinessType = MessageBusinessType.Message, // Only sent to LLM, not cached (derived from state)
            Message = new ChatMessage(ChatRole.System, builder.ToString()),
            Label = "ExecutionCheckpoint"
        };
    }

    /// <summary>
    /// Is this message included in LLM requests?
    /// </summary>
    public bool IsActiveMessage => BusinessType.HasFlag(MessageBusinessType.Message);

    /// <summary>
    /// Should this message be cached?
    /// </summary>
    public bool ShouldCache => BusinessType.HasFlag(MessageBusinessType.Cache);

    /// <summary>
    /// Should this message be saved to memory?
    /// </summary>
    public bool ShouldSaveToMemory => BusinessType.HasFlag(MessageBusinessType.Memory);

    /// <summary>
    /// Should this message be summarized?
    /// </summary>
    public bool ShouldResume => BusinessType.HasFlag(MessageBusinessType.Resume);

    /// <summary>
    /// Removes Message and Resume flags (used after summarization).
    /// Message stays in history but is no longer sent to LLM.
    /// </summary>
    public void MarkAsSummarized()
    {
        BusinessType &= ~(MessageBusinessType.Message | MessageBusinessType.Resume);
    }
}
