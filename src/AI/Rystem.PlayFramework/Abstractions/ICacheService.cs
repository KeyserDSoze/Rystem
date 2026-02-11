namespace Rystem.PlayFramework;

/// <summary>
/// Interface for caching conversation responses.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets cached responses by key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cached responses or null if not found.</returns>
    Task<List<AiSceneResponse>?> GetAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves responses to cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Responses to cache.</param>
    /// <param name="behavior">Cache behavior.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync(
        string key,
        List<AiSceneResponse> value,
        CacheBehavior behavior,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached responses by key.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
