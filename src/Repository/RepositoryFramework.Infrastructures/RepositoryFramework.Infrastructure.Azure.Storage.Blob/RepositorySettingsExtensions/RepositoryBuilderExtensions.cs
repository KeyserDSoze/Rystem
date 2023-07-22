using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Storage.Blob;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default blob storage service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your Cosmos database.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBlobStorageBuilder<T, TKey>> WithBlobStorage<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>, BlobStorageConnectionSettings, BlobContainerClientWrapper>(
                options =>
                {
                    connectionSettings.Invoke(options);
                    options.ModelType = typeof(T);
                },
                name,
                ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryBlobStorageBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add a default blob storage service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your Cosmos database.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBlobStorageBuilder<T, TKey>> WithBlobStorage<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>, BlobStorageConnectionSettings, BlobContainerClientWrapper>(
                options =>
                {
                    connectionSettings.Invoke(options);
                    options.ModelType = typeof(T);
                },
                name,
                ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryBlobStorageBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add a default blob storage service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your Cosmos database.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBlobStorageBuilder<T, TKey>> WithBlobStorage<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>, BlobStorageConnectionSettings, BlobContainerClientWrapper>(
                options =>
                {
                    connectionSettings.Invoke(options);
                    options.ModelType = typeof(T);
                },
                name,
                ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryBlobStorageBuilder<T, TKey>(builder.Services);
        }
    }
}
