namespace Rystem.PlayFramework;

/// <summary>
/// Storage abstraction for rate limiting state.
/// </summary>
internal interface IRateLimitStorage
{
    #region Token Bucket Operations

    Task<TokenBucket> GetOrCreateBucketAsync(string key, int capacity, int refillRate);
    Task UpdateBucketAsync(string key, TokenBucket bucket);

    #endregion

    #region Window Counter Operations

    Task<List<DateTime>> GetRequestTimestampsAsync(string key);
    Task AddRequestTimestampAsync(string key, DateTime timestamp, TimeSpan windowDuration);
    Task<int> GetRequestCountAsync(string key);
    Task IncrementRequestCountAsync(string key, TimeSpan windowDuration);

    #endregion

    #region Concurrent Operations

    Task<int> GetConcurrentCountAsync(string key);
    Task IncrementConcurrentAsync(string key);
    Task DecrementConcurrentAsync(string key);

    #endregion
}
