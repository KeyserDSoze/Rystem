using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Storage interface for conversation memory.
/// </summary>
public interface IMemoryStorage : IFactoryName
{
    /// <summary>
    /// Retrieves the memory for a conversation.
    /// </summary>
    /// <param name="conversationKey">Unique key for the conversation (from SceneRequestSettings.ConversationKey).</param>
    /// <param name="metadata">Request metadata for additional context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversation memory, or null if not found.</returns>
    Task<ConversationMemory?> GetAsync(
        string conversationKey,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the memory for a conversation.
    /// </summary>
    /// <param name="conversationKey">Unique key for the conversation (from SceneRequestSettings.ConversationKey).</param>
    /// <param name="memory">The conversation memory to save.</param>
    /// <param name="metadata">Request metadata for additional context.</param>
    /// <param name="settings">Request settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(
        string conversationKey,
        ConversationMemory memory,
        IReadOnlyDictionary<string, object>? metadata,
        SceneRequestSettings? settings,
        CancellationToken cancellationToken = default);
}
