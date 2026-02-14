using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for configuring rate limiting.
/// </summary>
public sealed class RateLimitBuilder
{
    private readonly IServiceCollection _services;
    private readonly AnyOf<string?, Enum>? _factoryName;
    private RateLimitSettings _settings = new() { Enabled = true };

    internal RateLimitBuilder(IServiceCollection services, AnyOf<string?, Enum>? factoryName)
    {
        _services = services;
        _factoryName = factoryName;
    }

    /// <summary>
    /// Group rate limits by specified metadata keys.
    /// Multiple keys are combined with AND logic to create a composite key.
    /// </summary>
    /// <param name="keys">Metadata keys to group by (e.g., "userId", "tenantId", "region")</param>
    /// <example>
    /// <code>
    /// // Single key: limit per user
    /// .GroupBy("userId")
    /// 
    /// // Multiple keys: limit per user+tenant
    /// .GroupBy("userId", "tenantId")
    /// 
    /// // Three keys: limit per tenant+region+priority
    /// .GroupBy("tenantId", "region", "priority")
    /// </code>
    /// </example>
    public RateLimitBuilder GroupBy(params string[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            throw new ArgumentException("At least one grouping key must be specified. Use GroupBy(\"userId\") or GroupBy(\"tenantId\", \"userId\").", nameof(keys));
        }

        _settings.GroupByKeys = keys;
        return this;
    }

    /// <summary>
    /// Use token bucket algorithm for rate limiting.
    /// Tokens refill continuously at a fixed rate with burst capability.
    /// </summary>
    /// <param name="capacity">Maximum burst size (tokens available immediately)</param>
    /// <param name="refillRate">Tokens added per second</param>
    /// <example>
    /// <code>
    /// // 10 requests/sec with burst up to 100
    /// .TokenBucket(capacity: 100, refillRate: 10)
    /// </code>
    /// </example>
    public RateLimitBuilder TokenBucket(int capacity, int refillRate)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));
        if (refillRate <= 0)
            throw new ArgumentException("Refill rate must be greater than 0", nameof(refillRate));

        _settings.Strategy = RateLimitingStrategy.TokenBucket;
        _settings.TokenBucketCapacity = capacity;
        _settings.TokenBucketRefillRate = refillRate;
        return this;
    }

    /// <summary>
    /// Use sliding window algorithm for rate limiting.
    /// More accurate than fixed window, avoids boundary issues.
    /// </summary>
    /// <param name="maxRequests">Maximum requests per interval</param>
    /// <param name="interval">Time interval</param>
    /// <example>
    /// <code>
    /// // 100 requests per hour
    /// .SlidingWindow(maxRequests: 100, TimeSpan.FromHours(1))
    /// </code>
    /// </example>
    public RateLimitBuilder SlidingWindow(int maxRequests, TimeSpan interval)
    {
        if (maxRequests <= 0)
            throw new ArgumentException("Max requests must be greater than 0", nameof(maxRequests));
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero", nameof(interval));

        _settings.Strategy = RateLimitingStrategy.SlidingWindow;
        _settings.SlidingWindowMaxRequests = maxRequests;
        _settings.SlidingWindowInterval = interval;
        return this;
    }

    /// <summary>
    /// Use fixed window counter for rate limiting.
    /// Simple but can allow 2X requests at window boundaries.
    /// </summary>
    /// <param name="maxRequests">Maximum requests per interval</param>
    /// <param name="interval">Time interval</param>
    /// <example>
    /// <code>
    /// // 1000 requests per minute
    /// .FixedWindow(maxRequests: 1000, TimeSpan.FromMinutes(1))
    /// </code>
    /// </example>
    public RateLimitBuilder FixedWindow(int maxRequests, TimeSpan interval)
    {
        if (maxRequests <= 0)
            throw new ArgumentException("Max requests must be greater than 0", nameof(maxRequests));
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero", nameof(interval));

        _settings.Strategy = RateLimitingStrategy.FixedWindow;
        _settings.FixedWindowMaxRequests = maxRequests;
        _settings.FixedWindowInterval = interval;
        return this;
    }

    /// <summary>
    /// Use concurrent request limiter.
    /// Limits the number of simultaneous requests.
    /// </summary>
    /// <param name="maxConcurrent">Maximum concurrent requests</param>
    /// <example>
    /// <code>
    /// // Max 5 simultaneous requests
    /// .Concurrent(maxConcurrent: 5)
    /// </code>
    /// </example>
    public RateLimitBuilder Concurrent(int maxConcurrent)
    {
        if (maxConcurrent <= 0)
            throw new ArgumentException("Max concurrent must be greater than 0", nameof(maxConcurrent));

        _settings.Strategy = RateLimitingStrategy.Concurrent;
        _settings.MaxConcurrentRequests = maxConcurrent;
        return this;
    }

    /// <summary>
    /// Wait (with timeout) when rate limit is exceeded.
    /// Request blocks until tokens/slots are available.
    /// </summary>
    /// <param name="maxWait">Maximum time to wait before throwing exception</param>
    /// <example>
    /// <code>
    /// // Wait up to 30 seconds
    /// .WaitOnExceeded(TimeSpan.FromSeconds(30))
    /// </code>
    /// </example>
    public RateLimitBuilder WaitOnExceeded(TimeSpan maxWait)
    {
        if (maxWait <= TimeSpan.Zero)
            throw new ArgumentException("Max wait time must be greater than zero", nameof(maxWait));

        _settings.ExceededBehavior = RateLimitBehavior.Wait;
        _settings.MaxWaitTime = maxWait;
        return this;
    }

    /// <summary>
    /// Reject immediately when rate limit is exceeded.
    /// Throws RateLimitExceededException. Client must retry later.
    /// </summary>
    /// <example>
    /// <code>
    /// .RejectOnExceeded()
    /// </code>
    /// </example>
    public RateLimitBuilder RejectOnExceeded()
    {
        _settings.ExceededBehavior = RateLimitBehavior.Reject;
        return this;
    }

    /// <summary>
    /// Use custom storage implementation for rate limit state.
    /// Must register IRateLimitStorage implementation separately.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;IRateLimitStorage, RedisRateLimitStorage&gt;();
    /// builder.WithCustomStorage()
    /// </code>
    /// </example>
    public RateLimitBuilder WithCustomStorage()
    {
        _settings.StorageType = RateLimitStorage.Custom;
        return this;
    }

    internal RateLimitSettings Build() => _settings;
}
