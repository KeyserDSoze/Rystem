using RepositoryFramework;
using RepositoryFramework.Cache;
using RepositoryFramework.Cache.Azure.Storage.Blob;
using RepositoryFramework.Infrastructure.Azure.Storage.Blob;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add Azure Blob Storage cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your storage connection.</param>
        /// <param name="cacheOptions">Settings for your cache.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> WithBlobStorageCache<T, TKey>(
           this RepositorySettings<T, TKey> settings,
                Action<BlobStorageConnectionSettings> options,
                Action<DistributedCacheOptions<T, TKey>>? cacheOptions = null)
            where TKey : notnull
        {
            settings
                .WithBlobStorage(options);
            return settings
                .WithDistributedCache<T, TKey, BlobStorageCache<T, TKey>>(cacheOptions, ServiceLifetime.Singleton);
        }
    }
}
