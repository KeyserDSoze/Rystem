namespace Rystem.PlayFramework;

/// <summary>
/// Unified cache service for PlayFramework conversations.
/// Saves and loads conversation history and execution state from cache using ConversationKey.
/// </summary>
public interface IPlayFrameworkCache
{
    /// <summary>
    /// Saves cacheable messages and execution state from context to cache.
    /// Uses context.ConversationKey as the cache key.
    /// Equivalent to SaveAsync(context, ExecutionPhase.Completed, null).
    /// </summary>
    Task SaveAsync(SceneContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads cached messages and execution state, adding them to context.
    /// Uses context.ConversationKey as the cache key.
    /// Also restores ExecutedScenes, ExecutedTools, SceneResults, and adds ExecutionCheckpoint message.
    /// </summary>
    Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default);
}
