namespace Rystem.PlayFramework;

/// <summary>
/// Rate limiter abstraction for controlling request rates.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Check if request is allowed and optionally wait for availability.
    /// </summary>
    /// <param name="key">Rate limit key (e.g., "userId:user123" or "tenantId:acme|region:eu-west")</param>
    /// <param name="cost">Cost of this request in tokens (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if request is allowed</returns>
    /// <exception cref="RateLimitExceededException">Thrown if rate limit exceeded and behavior is Reject</exception>
    Task<RateLimitCheckResult> CheckAndWaitAsync(
        string key,
        int cost = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a slot (for concurrent limiting only).
    /// Must be called after request completes when using Concurrent strategy.
    /// </summary>
    /// <param name="key">Rate limit key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReleaseAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current usage statistics for a key.
    /// </summary>
    /// <param name="key">Rate limit key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current statistics</returns>
    Task<RateLimitStats> GetStatsAsync(string key, CancellationToken cancellationToken = default);
}
