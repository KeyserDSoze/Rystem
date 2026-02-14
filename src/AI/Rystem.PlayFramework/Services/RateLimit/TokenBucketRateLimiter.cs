using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rystem.PlayFramework;

/// <summary>
/// Token bucket rate limiter implementation.
/// Tokens refill continuously at a fixed rate with burst capability.
/// </summary>
internal sealed class TokenBucketRateLimiter : IRateLimiter, IFactoryName
{
    private readonly IServiceProvider _serviceProvider;
    private RateLimitSettings _settings;
    private readonly IFactory<PlayFrameworkSettings> _settingsFactory;
    private readonly IFactory<IRateLimitStorage>? _rateLimitStorageFactory;
    private IRateLimitStorage _storage;
    private readonly ILogger<TokenBucketRateLimiter> _logger;

    public TokenBucketRateLimiter(
        IServiceProvider serviceProvider,
        IFactory<PlayFrameworkSettings> settingsFactory,
        ILogger<TokenBucketRateLimiter> logger,
        IFactory<IRateLimitStorage>? rateLimitStorageFactory = null)
    {
        _serviceProvider = serviceProvider;
        _settingsFactory = settingsFactory;
        _rateLimitStorageFactory = rateLimitStorageFactory;
        _logger = logger;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _storage = _rateLimitStorageFactory?.Create(name) ?? _serviceProvider.GetRequiredService<IRateLimitStorage>();
        _settings = _settingsFactory.Create(name)?.RateLimiting ?? new();
    }

    public async Task<RateLimitCheckResult> CheckAndWaitAsync(
        string key,
        int cost = 1,
        CancellationToken cancellationToken = default)
    {
        var bucket = await _storage.GetOrCreateBucketAsync(
            key,
            _settings.TokenBucketCapacity,
            _settings.TokenBucketRefillRate);

        // Refill tokens based on elapsed time
        var now = DateTime.UtcNow;
        var elapsed = (now - bucket.LastRefillTime).TotalSeconds;
        var tokensToAdd = (int)(elapsed * _settings.TokenBucketRefillRate);

        if (tokensToAdd > 0)
        {
            bucket.Tokens = Math.Min(bucket.Tokens + tokensToAdd, _settings.TokenBucketCapacity);
            bucket.LastRefillTime = now;
            await _storage.UpdateBucketAsync(key, bucket);
        }

        // Check if enough tokens
        if (bucket.Tokens >= cost)
        {
            bucket.Tokens -= cost;
            await _storage.UpdateBucketAsync(key, bucket);

            _logger.LogDebug("Rate limit check passed for key: {Key}. Remaining tokens: {Remaining}/{Capacity}",
                key, bucket.Tokens, _settings.TokenBucketCapacity);

            return new RateLimitCheckResult
            {
                IsAllowed = true,
                RemainingTokens = bucket.Tokens,
                ResetTime = now.AddSeconds((double)(_settings.TokenBucketCapacity - bucket.Tokens) / _settings.TokenBucketRefillRate),
                Key = key
            };
        }

        // Not enough tokens - calculate wait time
        var tokensNeeded = cost - bucket.Tokens;
        var timeToWait = TimeSpan.FromSeconds((double)tokensNeeded / _settings.TokenBucketRefillRate);

        _logger.LogWarning("Rate limit exceeded for key: {Key}. Need {TokensNeeded} more tokens. Wait time: {WaitTime}s",
            key, tokensNeeded, timeToWait.TotalSeconds);

        // Reject if configured or wait time exceeds max
        if (_settings.ExceededBehavior == RateLimitBehavior.Reject || timeToWait > _settings.MaxWaitTime)
        {
            _logger.LogWarning("Rejecting request for key: {Key} (Behavior: {Behavior}, WaitTime: {WaitTime} > MaxWait: {MaxWait})",
                key, _settings.ExceededBehavior, timeToWait, _settings.MaxWaitTime);

            throw new RateLimitExceededException($"Rate limit exceeded for key '{key}'. Retry after {timeToWait.TotalSeconds:F1}s")
            {
                RetryAfter = timeToWait,
                Key = key
            };
        }

        // Wait for tokens
        _logger.LogInformation("Waiting {WaitTime}s for rate limit to reset (key: {Key})", timeToWait.TotalSeconds, key);
        await Task.Delay(timeToWait, cancellationToken);

        // Retry
        return await CheckAndWaitAsync(key, cost, cancellationToken);
    }

    public Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        // Token bucket doesn't require release (tokens refill automatically)
        return Task.CompletedTask;
    }

    public async Task<RateLimitStats> GetStatsAsync(string key, CancellationToken cancellationToken = default)
    {
        var bucket = await _storage.GetOrCreateBucketAsync(
            key,
            _settings.TokenBucketCapacity,
            _settings.TokenBucketRefillRate);

        // Calculate reset time
        var tokensToRefill = _settings.TokenBucketCapacity - bucket.Tokens;
        var secondsToReset = (double)tokensToRefill / _settings.TokenBucketRefillRate;
        var resetTime = bucket.LastRefillTime.AddSeconds(secondsToReset);

        return new RateLimitStats
        {
            CurrentUsage = _settings.TokenBucketCapacity - bucket.Tokens,
            Limit = _settings.TokenBucketCapacity,
            ResetTime = resetTime,
            RefreshInterval = TimeSpan.FromSeconds(1.0 / _settings.TokenBucketRefillRate),
            Key = key
        };
    }
}
