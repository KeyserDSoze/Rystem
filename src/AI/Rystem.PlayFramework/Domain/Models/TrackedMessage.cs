using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework;

/// <summary>
/// Flags indicating message behavior and storage.
/// </summary>
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
    /// Always has Message flag (never removed). Not cached/memorized (regenerated each request).
    /// </summary>
    public static TrackedMessage CreateInitialContext(string content)
        => new()
        {
            BusinessType = MessageBusinessType.Message, // Always in requests, never removed
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
