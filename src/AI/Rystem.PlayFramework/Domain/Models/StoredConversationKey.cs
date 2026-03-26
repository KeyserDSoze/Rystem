namespace Rystem.PlayFramework;

/// <summary>
/// Composite key for <see cref="StoredConversation"/> storage.
/// <see cref="UserId"/> maps to the storage PartitionKey (for efficient per-user queries),
/// <see cref="ConversationKey"/> maps to the RowKey (unique within the user's partition).
/// </summary>
/// <param name="UserId">User identifier — used as PartitionKey in Azure Table Storage.</param>
/// <param name="ConversationKey">Unique conversation identifier — used as RowKey.</param>
public record StoredConversationKey(string? UserId, string ConversationKey);
