using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace Rystem.PlayFramework;

/// <summary>
/// Default cache service implementation using IMemoryCache or IDistributedCache.
/// </summary>
internal sealed class CacheService : ICacheService
{
    private readonly IMemoryCache? _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly PlayFrameworkSettings _settings;

    public CacheService(
        PlayFrameworkSettings settings,
        IMemoryCache? memoryCache = null,
        IDistributedCache? distributedCache = null)
    {
        _settings = settings;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    public async Task<List<AiSceneResponse>?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Cache.Enabled)
        {
            return null;
        }

        var fullKey = GetFullKey(key);

        // Try memory cache first
        if (_memoryCache != null && _memoryCache.TryGetValue(fullKey, out List<AiSceneResponse>? cached))
        {
            return cached;
        }

        // Try distributed cache
        if (_distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(fullKey, cancellationToken);
            if (bytes != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                var result = JsonSerializer.Deserialize<List<AiSceneResponse>>(json);
                
                // Populate memory cache for faster subsequent access
                if (result != null && _memoryCache != null)
                {
                    _memoryCache.Set(fullKey, result, GetCacheOptions());
                }
                
                return result;
            }
        }

        return null;
    }

    public async Task SetAsync(
        string key,
        List<AiSceneResponse> value,
        CacheBehavior behavior,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Cache.Enabled)
        {
            return;
        }

        var fullKey = GetFullKey(key);

        // Store in memory cache
        if (_memoryCache != null)
        {
            var options = GetCacheOptions(behavior);
            _memoryCache.Set(fullKey, value, options);
        }

        // Store in distributed cache
        if (_distributedCache != null)
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            
            var options = new DistributedCacheEntryOptions();
            
            if (behavior == CacheBehavior.Forever)
            {
                // No expiration
            }
            else if (behavior == CacheBehavior.Default && _settings.Cache.DefaultExpirationSeconds.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_settings.Cache.DefaultExpirationSeconds.Value);
            }
            
            await _distributedCache.SetAsync(fullKey, bytes, options, cancellationToken);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);

        _memoryCache?.Remove(fullKey);

        if (_distributedCache != null)
        {
            await _distributedCache.RemoveAsync(fullKey, cancellationToken);
        }
    }

    private string GetFullKey(string key) => $"{_settings.Cache.KeyPrefix}{key}";

    private MemoryCacheEntryOptions GetCacheOptions(CacheBehavior behavior = CacheBehavior.Default)
    {
        var options = new MemoryCacheEntryOptions();

        if (behavior == CacheBehavior.Forever)
        {
            // No expiration
        }
        else if (behavior == CacheBehavior.Default && _settings.Cache.DefaultExpirationSeconds.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_settings.Cache.DefaultExpirationSeconds.Value);
        }

        return options;
    }
}
