namespace Rystem.PlayFramework;

/// <summary>
/// Represents the persistent memory of a conversation.
/// Contains only the most important information extracted from conversation history.
/// </summary>
public sealed class ConversationMemory
{
    /// <summary>
    /// Summary of the conversation including key points and context.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Important facts extracted from the conversation.
    /// Example: { "userName": "John", "issue": "Payment problem", "resolved": false }
    /// </summary>
    public Dictionary<string, object> ImportantFacts { get; set; } = new();

    /// <summary>
    /// When this memory was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Number of conversations included in this memory.
    /// </summary>
    public int ConversationCount { get; set; }
}
