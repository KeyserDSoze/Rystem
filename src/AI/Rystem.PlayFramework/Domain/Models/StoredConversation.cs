namespace Rystem.PlayFramework;

/// <summary>
/// Serializable representation of a conversation for storage (cache, repository, file, etc.).
/// Contains messages, execution state, and authorization metadata.
/// </summary>
public sealed class StoredConversation
{
    /// <summary>
    /// Unique conversation identifier (used as storage key).
    /// </summary>
    public required string ConversationKey { get; init; }

    /// <summary>
    /// User who owns this conversation (null = anonymous/public).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Serializable messages.
    /// </summary>
    public required List<CachedMessage> Messages { get; init; }

    /// <summary>
    /// Execution state (scenes executed, tools used, etc.).
    /// </summary>
    public ExecutionState? ExecutionState { get; init; }

    /// <summary>
    /// Whether this conversation is private (requires userId match to access).
    /// Computed from UserId.
    /// </summary>
    public bool IsPublic { get; set; }
}
