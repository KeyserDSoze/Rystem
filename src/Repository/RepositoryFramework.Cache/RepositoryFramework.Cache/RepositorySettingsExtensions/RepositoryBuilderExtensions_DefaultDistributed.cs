using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add IDistributedCache you installed in your DI for your Repository pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithDistributedCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder.WithDistributedCache<T, TKey, DistributedCache<T, TKey>>(options, name, lifetime);
        /// <summary>
        /// Add IDistributedCache you installed in your DI for your command pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithDistributedCache<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder.WithDistributedCache<T, TKey, DistributedCache<T, TKey>>(options, name, lifetime);
        /// <summary>
        /// Add IDistributedCache you installed in your DI for your query pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithDistributedCache<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder.WithDistributedCache<T, TKey, DistributedCache<T, TKey>>(options, name, lifetime);
    }
}
