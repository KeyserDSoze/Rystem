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
        /// <param name="blobStorageBuilder">Settings for your blob storage.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBuilder<T, TKey>> WithBlobStorageAsync<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<IBlobStorageRepositoryBuilder<T, TKey>> blobStorageBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>,
                BlobStorageRepositoryBuilder<T, TKey>,
                BlobContainerClientWrapper>(
                blobStorageBuilder,
                name,
                ServiceLifetime.Singleton)
                .NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default blob storage service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="blobStorageBuilder">Settings for your blob storage.</param>
        /// <param name="name">Factory name</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<ICommandBuilder<T, TKey>> WithBlobStorageAsync<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<IBlobStorageRepositoryBuilder<T, TKey>> blobStorageBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>,
               BlobStorageRepositoryBuilder<T, TKey>,
               BlobContainerClientWrapper>(
               blobStorageBuilder,
               name,
               ServiceLifetime.Singleton)
               .NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default blob storage service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="blobStorageBuilder">Settings for your blob storage.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IQueryBuilder<T, TKey>> WithBlobStorageAsync<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<IBlobStorageRepositoryBuilder<T, TKey>> blobStorageBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<BlobStorageRepository<T, TKey>,
               BlobStorageRepositoryBuilder<T, TKey>,
               BlobContainerClientWrapper>(
               blobStorageBuilder,
               name,
               ServiceLifetime.Singleton)
               .NoContext();
            return builder;
        }
    }
}
