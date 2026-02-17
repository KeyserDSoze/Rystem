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
    /// Saves cacheable messages and execution state from context to cache with specific phase.
    /// Uses context.ConversationKey as the cache key.
    /// </summary>
    /// <param name="context">The scene context to save.</param>
    /// <param name="phase">The current execution phase.</param>
    /// <param name="currentSceneName">The currently executing scene name (if applicable).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(SceneContext context, ExecutionPhase phase, string? currentSceneName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads cached messages and execution state, adding them to context.
    /// Uses context.ConversationKey as the cache key.
    /// Also restores ExecutedScenes, ExecutedTools, SceneResults, and adds ExecutionCheckpoint message.
    /// </summary>
    Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default);
}
