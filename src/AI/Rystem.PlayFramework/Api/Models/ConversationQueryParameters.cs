namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Query parameters for listing conversations.
/// </summary>
public sealed class ConversationQueryParameters
{
    /// <summary>
    /// Search text in conversation messages (optional).
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Sort order by timestamp.
    /// </summary>
    public ConversationSortOrder OrderBy { get; set; } = ConversationSortOrder.TimestampDescending;

    /// <summary>
    /// Include public conversations (default: true).
    /// </summary>
    public bool IncludePublic { get; set; } = true;

    /// <summary>
    /// Include private conversations owned by current user (default: true).
    /// </summary>
    public bool IncludePrivate { get; set; } = true;

    /// <summary>
    /// Number of items to skip for pagination (default: 0).
    /// </summary>
    public int Skip { get; set; } = 0;

    /// <summary>
    /// Number of items to take for pagination (default: 50, max configured in settings).
    /// </summary>
    public int Take { get; set; } = 50;

    /// <summary>
    /// Include message contents (images, audio, PDFs, etc.) in responses (default: false).
    /// When false, contents are excluded to reduce payload size in list operations.
    /// </summary>
    public bool IncludeContents { get; set; } = false;
}

/// <summary>
/// Sort order for conversation list.
/// </summary>
public enum ConversationSortOrder
{
    /// <summary>
    /// Newest first (default).
    /// </summary>
    TimestampDescending = 0,

    /// <summary>
    /// Oldest first.
    /// </summary>
    TimestampAscending = 1
}
