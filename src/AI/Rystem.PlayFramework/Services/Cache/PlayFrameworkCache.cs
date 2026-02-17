using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Rystem.PlayFramework;

/// <summary>
/// Data structure for caching both messages and execution state.
/// </summary>
internal sealed class CachedConversation
{
    /// <summary>
    /// Cached messages.
    /// </summary>
    public List<CachedMessage> Messages { get; set; } = [];

    /// <summary>
    /// Execution state (scenes executed, tools used, etc.).
    /// </summary>
    public ExecutionState? ExecutionState { get; set; }
}

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
        await SaveAsync(context, ExecutionPhase.Completed, null, cancellationToken);
    }

    public async Task SaveAsync(SceneContext context, ExecutionPhase phase, string? currentSceneName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.ConversationKey))
            return;

        var messagesToCache = context.GetMessagesForCache();

        // Create cached conversation with both messages and state
        var cachedConversation = new CachedConversation
        {
            Messages = messagesToCache.Select(CachedMessage.FromTrackedMessage).ToList(),
            ExecutionState = context.CreateExecutionState(phase, currentSceneName)
        };

        var cacheKey = BuildCacheKey(context.ConversationKey);
        var expiration = _settings.Cache.CacheExpiration;

        if (_distributedCache != null)
        {
            var json = _jsonService.Serialize(cachedConversation);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _distributedCache.SetAsync(cacheKey, bytes, options, cancellationToken);

            _logger.LogDebug("Saved {Count} messages + execution state (phase: {Phase}) to distributed cache for conversation '{Key}'",
                messagesToCache.Count, phase, context.ConversationKey);
        }
        else if (_memoryCache != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            // For memory cache, store directly
            _memoryCache.Set(cacheKey, cachedConversation, options);

            _logger.LogDebug("Saved {Count} messages + execution state (phase: {Phase}) to memory cache for conversation '{Key}'",
                messagesToCache.Count, phase, context.ConversationKey);
        }
    }

    public async Task LoadAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.ConversationKey))
            return;

        var cacheKey = BuildCacheKey(context.ConversationKey);
        CachedConversation? cachedConversation = null;

        if (_distributedCache != null)
        {
            var bytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            if (bytes != null)
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                cachedConversation = _jsonService.Deserialize<CachedConversation>(json);
            }
        }
        else if (_memoryCache != null)
        {
            _memoryCache.TryGetValue(cacheKey, out cachedConversation);
        }

        if (cachedConversation == null)
        {
            _logger.LogDebug("No cached conversation found for key '{Key}'", context.ConversationKey);
            return;
        }

        // Restore messages
        if (cachedConversation.Messages.Count > 0)
        {
            var trackedMessages = cachedConversation.Messages
                .Select(c => c.ToTrackedMessage())
                .ToList();

            context.RestoreFromCache(trackedMessages);

            _logger.LogDebug("Loaded {Count} messages from cache for conversation '{Key}'",
                trackedMessages.Count, context.ConversationKey);
        }

        // Restore execution state
        if (cachedConversation.ExecutionState != null)
        {
            context.RestoreExecutionState(cachedConversation.ExecutionState);

            _logger.LogInformation(
                "Restored execution state for conversation '{Key}' - Phase: {Phase}, Scenes: {SceneCount}, Tools: {ToolCount}, Cost: {Cost:F6}",
                context.ConversationKey,
                cachedConversation.ExecutionState.Phase,
                cachedConversation.ExecutionState.ExecutedSceneOrder.Count,
                cachedConversation.ExecutionState.ExecutedTools.Count,
                cachedConversation.ExecutionState.AccumulatedCost);
        }
    }

    private string BuildCacheKey(string conversationKey)
    {
        var prefix = _settings.Cache.KeyPrefix;
        return string.IsNullOrEmpty(prefix)
            ? $"conversation:{conversationKey}"
            : $"{prefix}:conversation:{conversationKey}";
    }
}
