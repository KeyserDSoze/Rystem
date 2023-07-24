using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add in memory and distributed cache mechanism for your Repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="inMemoryOptions">Settings for your cache.</param>
        /// <param name="distributedOptions">Settings for your cache.</param>
        /// <param name="inMemoryLifetime">Service Lifetime.</param>
        /// <param name="distributedLifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithInMemoryAndDistributedCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? inMemoryOptions = null,
           Action<CacheOptions<T, TKey>>? distributedOptions = null,
           ServiceLifetime inMemoryLifetime = ServiceLifetime.Singleton,
           ServiceLifetime distributedLifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder
                .WithCache<T, TKey, InMemoryCache<T, TKey>>(inMemoryOptions, inMemoryLifetime)
                .WithDistributedCache(distributedOptions, distributedLifetime);
        /// <summary>
        /// Add in memory and distributed cache mechanism for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="inMemoryOptions">Settings for your cache.</param>
        /// <param name="distributedOptions">Settings for your cache.</param>
        /// <param name="inMemoryLifetime">Service Lifetime.</param>
        /// <param name="distributedLifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithInMemoryAndDistributedCache<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? inMemoryOptions = null,
           Action<CacheOptions<T, TKey>>? distributedOptions = null,
           ServiceLifetime inMemoryLifetime = ServiceLifetime.Singleton,
           ServiceLifetime distributedLifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder
                .WithCache<T, TKey, InMemoryCache<T, TKey>>(inMemoryOptions, inMemoryLifetime)
                .WithDistributedCache(distributedOptions, distributedLifetime);
        /// <summary>
        /// Add in memory and distributed cache mechanism for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="inMemoryOptions">Settings for your cache.</param>
        /// <param name="distributedOptions">Settings for your cache.</param>
        /// <param name="inMemoryLifetime">Service Lifetime.</param>
        /// <param name="distributedLifetime">Service Lifetime.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithInMemoryAndDistributedCache<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? inMemoryOptions = null,
           Action<CacheOptions<T, TKey>>? distributedOptions = null,
           ServiceLifetime inMemoryLifetime = ServiceLifetime.Singleton,
           ServiceLifetime distributedLifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder
                .WithCache<T, TKey, InMemoryCache<T, TKey>>(inMemoryOptions, inMemoryLifetime)
                .WithDistributedCache(distributedOptions, distributedLifetime);
    }
}
