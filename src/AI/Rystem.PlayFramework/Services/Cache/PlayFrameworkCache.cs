using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Rystem.PlayFramework;

/// <summary>
/// Unified cache service for PlayFramework conversations.
/// Uses IDistributedCache if available, falls back to IMemoryCache.
/// </summary>
internal sealed class PlayFrameworkCache : IPlayFrameworkCache
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache? _memoryCache;
    private readonly IJsonService _jsonService;
    private readonly PlayFrameworkSettings _settings;
    private readonly ILogger<PlayFrameworkCache> _logger;

    public PlayFrameworkCache(
        PlayFrameworkSettings settings,
        IJsonService jsonService,
        ILogger<PlayFrameworkCache> logger,
        IDistributedCache? distributedCache = null,
        IMemoryCache? memoryCache = null)
    {
        _settings = settings;
        _jsonService = jsonService;
        _logger = logger;
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
    }

    public async Task SaveAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.ConversationKey))
            return;

        var messagesToCache = context.GetMessagesForCache();
        if (messagesToCache.Count == 0)
            return;

        var cacheKey = $"conversation:{context.ConversationKey}";
        var expiration = _settings.Cache.CacheExpiration;

        if (_distributedCache != null)
        {
            var json = _jsonService.Serialize(messagesToCache);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _distributedCache.SetAsync(cacheKey, bytes, options, cancellationToken);
        }
        else if (_memoryCache != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            _memoryCache.Set(cacheKey, messagesToCache, options);
        }

        _logger.LogDebug("Saved {Count} messages to cache for conversation '{Key}'",
            messagesToCache.Count, context.ConversationKey);
    }

    public async Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.ConversationKey))
            return;

        var cacheKey = $"conversation:{context.ConversationKey}";
        List<TrackedMessage>? cachedMessages = null;

        if (_distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (bytes != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                cachedMessages = _jsonService.Deserialize<List<TrackedMessage>>(json);
            }
        }
        else if (_memoryCache != null)
        {
            _memoryCache.TryGetValue(cacheKey, out cachedMessages);
        }

        if (cachedMessages != null && cachedMessages.Count > 0)
        {
            context.RestoreFromCache(cachedMessages);
            _logger.LogDebug("Loaded {Count} messages from cache for conversation '{Key}'",
                cachedMessages.Count, context.ConversationKey);
        }
    }
}
