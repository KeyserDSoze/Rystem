using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework;

/// <summary>
/// Unified cache service for PlayFramework conversations.
/// Uses IDistributedCache if available, falls back to IMemoryCache.
/// Saves both conversation messages and execution state.
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

        var storedConversation = context.ToStoredConversation();
        var cacheKey = BuildCacheKey(context.ConversationKey);
        var expiration = _settings.Cache.CacheExpiration;
        var saveStart = DateTime.UtcNow;

        if (_distributedCache != null)
        {
            var json = _jsonService.Serialize(storedConversation);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _distributedCache.SetAsync(cacheKey, bytes, options, cancellationToken);

            var saveDuration = (DateTime.UtcNow - saveStart).TotalMilliseconds;
            PlayFrameworkMetrics.RecordCacheAccess(hit: true, cacheKey: cacheKey, durationMs: saveDuration);
            _logger.LogDebug("Saved {Count} messages + execution state (phase: {Phase}) to distributed cache for conversation '{Key}' in {Duration:F1}ms",
                storedConversation.Messages.Count, context.ExecutionPhase, context.ConversationKey, saveDuration);
        }
        else if (_memoryCache != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            // For memory cache, store directly
            _memoryCache.Set(cacheKey, storedConversation, options);

            var saveDuration = (DateTime.UtcNow - saveStart).TotalMilliseconds;
            PlayFrameworkMetrics.RecordCacheAccess(hit: true, cacheKey: cacheKey, durationMs: saveDuration);
            _logger.LogDebug("Saved {Count} messages + execution state (phase: {Phase}) to memory cache for conversation '{Key}' in {Duration:F1}ms",
                storedConversation.Messages.Count, context.ExecutionPhase, context.ConversationKey, saveDuration);
        }
    }

    public async Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.ConversationKey))
            return;

        var cacheKey = BuildCacheKey(context.ConversationKey);
        StoredConversation? storedConversation = null;
        var loadStart = DateTime.UtcNow;

        if (_distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (bytes != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                storedConversation = _jsonService.Deserialize<StoredConversation>(json);
            }
        }
        else if (_memoryCache != null)
        {
            _memoryCache.TryGetValue(cacheKey, out storedConversation);
        }

        var loadDuration = (DateTime.UtcNow - loadStart).TotalMilliseconds;
        var cacheHit = storedConversation != null;
        PlayFrameworkMetrics.RecordCacheAccess(hit: cacheHit, cacheKey: cacheKey, durationMs: loadDuration);

        if (storedConversation == null)
        {
            _logger.LogDebug("Cache miss for conversation '{Key}' in {Duration:F1}ms", context.ConversationKey, loadDuration);
            return;
        }

        // Restore conversation using unified method
        context.LoadFromStoredConversation(storedConversation);

        _logger.LogInformation(
            "Cache hit for conversation '{Key}' in {Duration:F1}ms - {Count} messages, Phase: {Phase}, UserId: {UserId}",
            context.ConversationKey,
            loadDuration,
            storedConversation.Messages.Count,
            storedConversation.ExecutionState?.Phase,
            storedConversation.UserId ?? "anonymous");
    }

    private string BuildCacheKey(string conversationKey)
    {
        var prefix = _settings.Cache.KeyPrefix;
        return string.IsNullOrEmpty(prefix)
            ? $"conversation:{conversationKey}"
            : $"{prefix}:conversation:{conversationKey}";
    }
}
