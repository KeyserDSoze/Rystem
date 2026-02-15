namespace Rystem.PlayFramework;

/// <summary>
/// Scene execution state saved in cache to resume after client interaction.
/// Stored with ContinuationToken as key in distributed cache.
/// </summary>
internal sealed class SceneContinuation
{
    /// <summary>
    /// Conversation key for this execution.
    /// </summary>
    public required string ConversationKey { get; init; }

    /// <summary>
    /// Unique token (GUID) to identify this continuation.
    /// Client sends this back to resume execution.
    /// </summary>
    public required string ContinuationToken { get; init; }

    /// <summary>
    /// Name of the scene being executed.
    /// </summary>
    public required string SceneName { get; init; }

    /// <summary>
    /// Complete execution context serialized as JSON.
    /// Restored when client resumes with continuation token.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// ID of the client interaction we're waiting for.
    /// </summary>
    public required string PendingInteractionId { get; init; }

    /// <summary>
    /// When this continuation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this continuation expires in cache.
    /// If client doesn't resume before this, token is invalid.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
