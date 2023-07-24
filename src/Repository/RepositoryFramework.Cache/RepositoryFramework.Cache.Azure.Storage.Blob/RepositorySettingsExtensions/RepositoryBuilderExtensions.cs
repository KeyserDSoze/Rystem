using RepositoryFramework;
using RepositoryFramework.Cache;
using RepositoryFramework.Cache.Azure.Storage.Blob;
using RepositoryFramework.Infrastructure.Azure.Storage.Blob;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add Azure Blob Storage cache mechanism for your Repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your storage connection.</param>
        /// <param name="cacheOptions">Settings for your cache.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithBlobStorageCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
                Action<BlobStorageConnectionSettings> options,
                Action<DistributedCacheOptions<T, TKey>>? cacheOptions = null,
                string? name = null)
            where TKey : notnull
        {
            builder
                .WithBlobStorageAsync(options)
                .ToResult();
            return builder
                .WithDistributedCache<T, TKey, BlobStorageCache<T, TKey>>(cacheOptions, name, ServiceLifetime.Singleton);
        }
        /// <summary>
        /// Add Azure Blob Storage cache mechanism for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your storage connection.</param>
        /// <param name="cacheOptions">Settings for your cache.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithBlobStorageCache<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
                Action<BlobStorageConnectionSettings> options,
                Action<DistributedCacheOptions<T, TKey>>? cacheOptions = null,
                string? name = null)
            where TKey : notnull
        {
            builder
                .WithBlobStorageAsync(options)
                .ToResult();
            return builder
                .WithDistributedCache<T, TKey, BlobStorageCache<T, TKey>>(cacheOptions, name, ServiceLifetime.Singleton);
        }
        /// <summary>
        /// Add Azure Blob Storage cache mechanism for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your storage connection.</param>
        /// <param name="cacheOptions">Settings for your cache.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithBlobStorageCache<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
                Action<BlobStorageConnectionSettings> options,
                Action<DistributedCacheOptions<T, TKey>>? cacheOptions = null,
                string? name = null)
            where TKey : notnull
        {
            builder
                .WithBlobStorageAsync(options)
                .ToResult();
            return builder
                .WithDistributedCache<T, TKey, BlobStorageCache<T, TKey>>(cacheOptions, name, ServiceLifetime.Singleton);
        }
    }
}
