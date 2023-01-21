using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add in memory and distributed cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="inMemoryOptions">Settings for your cache.</param>
        /// <param name="distributedOptions">Settings for your cache.</param>
        /// <param name="inMemoryLifetime">Service Lifetime.</param>
        /// <param name="distributedLifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> WithInMemoryAndDistributedCache<T, TKey>(
           this RepositorySettings<T, TKey> settings,
           Action<CacheOptions<T, TKey>>? inMemoryOptions = null,
           Action<CacheOptions<T, TKey>>? distributedOptions = null,
           ServiceLifetime inMemoryLifetime = ServiceLifetime.Singleton,
           ServiceLifetime distributedLifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => settings
                .WithCache<T, TKey, InMemoryCache<T, TKey>>(inMemoryOptions, inMemoryLifetime)
                .WithDistributedCache(distributedOptions, distributedLifetime);
    }
}
