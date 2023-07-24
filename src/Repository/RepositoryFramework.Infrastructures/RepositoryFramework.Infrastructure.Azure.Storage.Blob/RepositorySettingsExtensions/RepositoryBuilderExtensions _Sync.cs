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
        /// <param name="connectionSettings">Settings for your blob storage.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBlobStorageBuilder<T, TKey> WithBlobStorage<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
            => builder.WithBlobStorageAsync(connectionSettings, name).ToResult();
        /// <summary>
        /// Add a default blob storage service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your blob storage.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBlobStorageBuilder<T, TKey> WithBlobStorage<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
            => builder.WithBlobStorageAsync(connectionSettings, name).ToResult();
        /// <summary>
        /// Add a default blob storage service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your blob storage.</param>
        /// <returns>IRepositoryBlobStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBlobStorageBuilder<T, TKey> WithBlobStorage<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<BlobStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
            => builder.WithBlobStorageAsync(connectionSettings, name).ToResult();
    }
}
