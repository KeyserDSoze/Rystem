using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public CacheBuilder WithCustomCache<TCache>() where TCache : class, ICacheService
    {
        _parent.Services.AddScoped<ICacheService, TCache>();
        _parent.HasCustomCache = true;
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
