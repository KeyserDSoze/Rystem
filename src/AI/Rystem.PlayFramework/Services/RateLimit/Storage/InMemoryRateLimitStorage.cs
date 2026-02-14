using System.Collections.Concurrent;

namespace Rystem.PlayFramework;

/// <summary>
/// In-memory storage for rate limiting state.
/// Thread-safe but not distributed. Lost on restart.
/// </summary>
internal sealed class InMemoryRateLimitStorage : IRateLimitStorage
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _timestamps = new();
    private readonly ConcurrentDictionary<string, int> _counters = new();
    private readonly ConcurrentDictionary<string, int> _concurrent = new();

    #region Token Bucket Operations

    public Task<TokenBucket> GetOrCreateBucketAsync(string key, int capacity, int refillRate)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket
        {
            Tokens = capacity,
            Capacity = capacity,
            RefillRate = refillRate,
            LastRefillTime = DateTime.UtcNow
        });

        return Task.FromResult(bucket);
    }

    public Task UpdateBucketAsync(string key, TokenBucket bucket)
    {
        _buckets[key] = bucket;
        return Task.CompletedTask;
    }

    #endregion

    #region Window Counter Operations

    public Task<List<DateTime>> GetRequestTimestampsAsync(string key)
    {
        if (!_timestamps.TryGetValue(key, out var timestamps))
        {
            timestamps = new List<DateTime>();
            _timestamps[key] = timestamps;
        }

        // Clean up old timestamps (older than 24 hours)
        var cutoff = DateTime.UtcNow.AddHours(-24);
        timestamps.RemoveAll(t => t < cutoff);

        return Task.FromResult(timestamps);
    }

    public Task AddRequestTimestampAsync(string key, DateTime timestamp, TimeSpan windowDuration)
    {
        if (!_timestamps.TryGetValue(key, out var timestamps))
        {
            timestamps = new List<DateTime>();
            _timestamps[key] = timestamps;
        }

        // Add new timestamp
        lock (timestamps)
        {
            timestamps.Add(timestamp);

            // Clean up old timestamps outside window
            var cutoff = DateTime.UtcNow - windowDuration;
            timestamps.RemoveAll(t => t < cutoff);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetRequestCountAsync(string key)
    {
        _counters.TryGetValue(key, out var count);
        return Task.FromResult(count);
    }

    public Task IncrementRequestCountAsync(string key, TimeSpan windowDuration)
    {
        _counters.AddOrUpdate(key, 1, (_, count) => count + 1);

        // Schedule cleanup after window duration
        _ = Task.Delay(windowDuration).ContinueWith(_ =>
        {
            _counters.AddOrUpdate(key, 0, (_, count) => Math.Max(0, count - 1));
        });

        return Task.CompletedTask;
    }

    #endregion

    #region Concurrent Operations

    public Task<int> GetConcurrentCountAsync(string key)
    {
        _concurrent.TryGetValue(key, out var count);
        return Task.FromResult(count);
    }

    public Task IncrementConcurrentAsync(string key)
    {
        _concurrent.AddOrUpdate(key, 1, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task DecrementConcurrentAsync(string key)
    {
        _concurrent.AddOrUpdate(key, 0, (_, count) => Math.Max(0, count - 1));
        return Task.CompletedTask;
    }

    #endregion
}
