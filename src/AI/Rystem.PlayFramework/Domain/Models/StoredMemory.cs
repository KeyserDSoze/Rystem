namespace Rystem.PlayFramework;

/// <summary>
/// Persistent storage model for conversation memory, keyed by conversation or metadata-driven key.
/// Used by <see cref="RepositoryMemoryStorage"/> as the entity for <c>IRepository&lt;StoredMemory, string&gt;</c>.
/// </summary>
public sealed class StoredMemory
{
    /// <summary>
    /// Storage key (either conversationKey or a metadata-driven composite key such as "userId:john|sessionId:abc").
    /// </summary>
    public required string Key { get; init; }
    /// <summary>
    /// Summary of the conversation including key points and context.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Important facts extracted from the conversation.
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

    /// <summary>
    /// Converts this stored entity back to a <see cref="ConversationMemory"/>.
    /// </summary>
    public ConversationMemory ToConversationMemory() => new()
    {
        Summary = Summary,
        ImportantFacts = ImportantFacts,
        LastUpdated = LastUpdated,
        ConversationCount = ConversationCount
    };

    /// <summary>
    /// Creates a <see cref="StoredMemory"/> from a <see cref="ConversationMemory"/> and a storage key.
    /// </summary>
    public static StoredMemory FromConversationMemory(string key, ConversationMemory memory) => new()
    {
        Key = key,
        Summary = memory.Summary,
        ImportantFacts = memory.ImportantFacts,
        LastUpdated = memory.LastUpdated,
        ConversationCount = memory.ConversationCount
    };
}
