namespace Rystem.PlayFramework;

/// <summary>
/// Scene execution state saved in cache to resume after client interaction.
/// Stored with ContinuationToken as key in distributed cache.
/// Cache TTL is handled by DistributedCacheEntryOptions.
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
    /// ID of the client interaction we're waiting for.
    /// </summary>
    public required string PendingInteractionId { get; init; }

    /// <summary>
    /// Original FunctionCallContent.CallId from the LLM response.
    /// Required to build a correct FunctionResultContent on resume.
    /// </summary>
    public required string OriginalCallId { get; init; }

    /// <summary>
    /// Original FunctionCallContent.Name (tool name) from the LLM response.
    /// Required to build a correct FunctionResultContent on resume.
    /// </summary>
    public required string OriginalToolName { get; init; }
}
