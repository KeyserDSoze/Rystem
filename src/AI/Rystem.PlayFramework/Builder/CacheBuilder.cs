using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for configuring cache.
/// </summary>
public sealed class CacheBuilder
{
    private readonly PlayFrameworkBuilder _parent;

    internal CacheBuilder(PlayFrameworkBuilder parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Uses in-memory caching.
    /// </summary>
    public CacheBuilder WithMemory()
    {
        _parent.Services.AddMemoryCache();
        _parent.HasCustomCache = false;
        return this;
    }

    /// <summary>
    /// Uses distributed cache (must be registered separately).
    /// </summary>
    public CacheBuilder WithDistributed()
    {
        // Cache service will use IDistributedCache if available
        _parent.HasCustomCache = false;
        return this;
    }

    /// <summary>
    /// Uses custom cache implementation.
    /// </summary>
    public CacheBuilder WithCustomCache<TCache>() where TCache : class, IPlayFrameworkCache
    {
        _parent.Services.AddFactory<IPlayFrameworkCache, TCache>(_parent.Name, ServiceLifetime.Transient);
        _parent.HasCustomCache = true;
        return this;
    }

    /// <summary>
    /// Sets the default cache expiration duration.
    /// This is a server-side setting and cannot be overridden by clients.
    /// </summary>
    /// <param name="expiration">Cache expiration duration.</param>
    public CacheBuilder WithExpiration(TimeSpan expiration)
    {
        _parent.Settings.Cache.DefaultExpirationSeconds = (int)expiration.TotalSeconds;
        return this;
    }

    /// <summary>
    /// Sets the cache key prefix.
    /// </summary>
    /// <param name="prefix">Key prefix for all cache entries.</param>
    public CacheBuilder WithKeyPrefix(string prefix)
    {
        _parent.Settings.Cache.KeyPrefix = prefix;
        return this;
    }

    /// <summary>
    /// Configures cache settings.
    /// </summary>
    public CacheBuilder Configure(Action<CacheSettings> configure)
    {
        configure(_parent.Settings.Cache);
        return this;
    }

    /// <summary>
    /// Disables caching.
    /// </summary>
    public CacheBuilder Disable()
    {
        _parent.Settings.Cache.Enabled = false;
        return this;
    }
}
