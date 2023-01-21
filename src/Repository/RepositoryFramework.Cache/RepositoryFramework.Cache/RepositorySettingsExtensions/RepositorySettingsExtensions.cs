using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        private static void AddCacheManager<T, TKey>(
            this RepositorySettings<T, TKey> settings,
            CacheOptions<T, TKey> options)
            where TKey : notnull
        {
            if (settings.Type == PatternType.Repository)
                settings.Services
                    .Decorate<IRepository<T, TKey>, CachedRepository<T, TKey>>();
            else if (settings.Type == PatternType.Query)
                settings.Services
                    .Decorate<IQuery<T, TKey>, CachedQuery<T, TKey>>();
            else if (settings.Type == PatternType.Command && options.HasCommandPattern)
                settings.Services
                    .Decorate<ICommand<T, TKey>, CachedRepository<T, TKey>>();
        }
        /// <summary>
        /// Add cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="settings">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> WithCache<T, TKey, TCache>(
           this RepositorySettings<T, TKey> settings,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, ICache<T, TKey>
        {
            var defaultOptions = new CacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            settings.Services
                .RemoveServiceIfAlreadyInstalled<TCache>(typeof(ICache<T, TKey>))
                .AddService<ICache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            settings.AddCacheManager(defaultOptions);
            return settings;
        }
        /// <summary>
        /// Add distributed (for multi-instance environments) cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="settings">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> WithDistributedCache<T, TKey, TCache>(
           this RepositorySettings<T, TKey> settings,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
        {
            var defaultOptions = new DistributedCacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            settings.Services
                .RemoveServiceIfAlreadyInstalled<TCache>(typeof(IDistributedCache<T, TKey>))
                .AddService<IDistributedCache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            settings.AddCacheManager(defaultOptions);
            return settings;
        }
    }
}
