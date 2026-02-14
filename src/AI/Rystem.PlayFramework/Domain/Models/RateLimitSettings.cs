namespace Rystem.PlayFramework;

/// <summary>
/// Settings for rate limiting.
/// </summary>
public sealed class RateLimitSettings
{
    /// <summary>
    /// Enable or disable rate limiting.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Rate limiting algorithm strategy.
    /// </summary>
    public RateLimitingStrategy Strategy { get; set; } = RateLimitingStrategy.TokenBucket;

    /// <summary>
    /// Keys from metadata dictionary to group rate limits by.
    /// Multiple keys create a composite key (AND logic).
    /// Examples:
    /// - ["userId"] → limit per user
    /// - ["userId", "tenantId"] → limit per user+tenant
    /// - ["tenantId", "apiKey"] → limit per tenant+apiKey
    /// - [] or null → uses "global" key (all requests share same limit)
    /// </summary>
    public string[] GroupByKeys { get; set; } = [];

    #region Token Bucket Settings

    /// <summary>
    /// Token bucket capacity (maximum burst size).
    /// Default: 100 tokens.
    /// </summary>
    public int TokenBucketCapacity { get; set; } = 100;

    /// <summary>
    /// Token bucket refill rate (tokens per second).
    /// Default: 10 tokens/sec = 600 requests/min.
    /// </summary>
    public int TokenBucketRefillRate { get; set; } = 10;

    #endregion

    #region Fixed Window Settings

    /// <summary>
    /// Fixed window: maximum requests per interval.
    /// Default: 100 requests.
    /// </summary>
    public int FixedWindowMaxRequests { get; set; } = 100;

    /// <summary>
    /// Fixed window: time interval.
    /// Default: 1 minute.
    /// </summary>
    public TimeSpan FixedWindowInterval { get; set; } = TimeSpan.FromMinutes(1);

    #endregion

    #region Sliding Window Settings

    /// <summary>
    /// Sliding window: maximum requests per interval.
    /// Default: 100 requests.
    /// </summary>
    public int SlidingWindowMaxRequests { get; set; } = 100;

    /// <summary>
    /// Sliding window: time interval.
    /// Default: 1 minute.
    /// </summary>
    public TimeSpan SlidingWindowInterval { get; set; } = TimeSpan.FromMinutes(1);

    #endregion

    #region Concurrent Settings

    /// <summary>
    /// Maximum number of concurrent requests.
    /// Default: 10 concurrent requests.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    #endregion

    #region Behavior Settings

    /// <summary>
    /// Behavior when rate limit is exceeded.
    /// Default: Wait (blocks until limit resets).
    /// </summary>
    public RateLimitBehavior ExceededBehavior { get; set; } = RateLimitBehavior.Wait;

    /// <summary>
    /// Maximum time to wait when rate limit is exceeded (if behavior is Wait).
    /// If exceeded, throws RateLimitExceededException.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(30);

    #endregion

    #region Storage Settings

    /// <summary>
    /// Storage backend for rate limit state.
    /// Default: InMemory (automatically registered).
    /// For custom storage, set to Custom and register IRateLimitStorage implementation.
    /// </summary>
    public RateLimitStorage StorageType { get; set; } = RateLimitStorage.InMemory;

    #endregion
}
