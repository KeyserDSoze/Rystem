namespace Rystem.PlayFramework;

/// <summary>
/// Unified cache service for PlayFramework conversations.
/// Saves and loads conversation history from cache using ConversationKey.
/// </summary>
public interface IPlayFrameworkCache
{
    /// <summary>
    /// Saves cacheable messages from context.ConversationHistory to cache.
    /// Uses context.ConversationKey as the cache key.
    /// </summary>
    Task SaveAsync(SceneContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads cached messages and adds them to context.ConversationHistory.
    /// Uses context.ConversationKey as the cache key.
    /// </summary>
    Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default);
}
