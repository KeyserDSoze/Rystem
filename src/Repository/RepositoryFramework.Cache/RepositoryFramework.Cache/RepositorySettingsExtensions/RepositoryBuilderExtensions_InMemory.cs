using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add in memory cache mechanism for your Repository pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithInMemoryCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null)
            where TKey : notnull
        {
            builder.Services.AddMemoryCache();
            return builder.WithCache<T, TKey, InMemoryCache<T, TKey>>(options, name, ServiceLifetime.Singleton);
        }
        /// <summary>
        /// Add in memory cache mechanism for your command pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithInMemoryCache<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null)
            where TKey : notnull
        {
            builder.Services.AddMemoryCache();
            return builder.WithCache<T, TKey, InMemoryCache<T, TKey>>(options, name, ServiceLifetime.Singleton);
        }
        /// <summary>
        /// Add in memory cache mechanism for your query pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithInMemoryCache<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           string? name = null)
            where TKey : notnull
        {
            builder.Services.AddMemoryCache();
            return builder.WithCache<T, TKey, InMemoryCache<T, TKey>>(options, name, ServiceLifetime.Singleton);
        }
    }
}
