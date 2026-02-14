namespace Rystem.PlayFramework;

/// <summary>
/// Rate limiting algorithm strategy.
/// </summary>
public enum RateLimitingStrategy
{
    /// <summary>
    /// No rate limiting.
    /// </summary>
    None = 0,

    /// <summary>
    /// Token bucket algorithm: smooth rate with burst capability.
    /// Tokens refill continuously at a fixed rate.
    /// Best for: API rate limiting with burst tolerance.
    /// </summary>
    TokenBucket = 1,

    /// <summary>
    /// Fixed window counter: X requests per fixed time interval.
    /// Simple but can allow 2X requests at window boundaries.
    /// Best for: Simple hourly/daily limits.
    /// </summary>
    FixedWindow = 2,

    /// <summary>
    /// Sliding window counter: more accurate than fixed window.
    /// Uses weighted count based on current position in window.
    /// Best for: Accurate rate limiting without boundary issues.
    /// </summary>
    SlidingWindow = 3,

    /// <summary>
    /// Concurrent request limiter: max N simultaneous requests.
    /// Best for: Controlling resource usage and concurrency.
    /// </summary>
    Concurrent = 4
}
