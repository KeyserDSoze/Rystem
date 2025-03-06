using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Storage.Blob;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default blob storage service for your repository pattern with a default connection service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IRepositoryBuilder<T, TKey> WithBlobStorage<T, TKey, TConnectionService>(
            this IRepositoryBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TConnectionService : class, IConnectionService<BlobContainerClientWrapper>
        {
            builder.SetStorageAndServiceConnection<BlobStorageRepository<T, TKey>, TConnectionService, BlobContainerClientWrapper>(
                name,
                serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a default blob storage service for your command pattern with a default connection service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ICommandBuilder<T, TKey> WithBlobStorage<T, TKey, TConnectionService>(
            this ICommandBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TConnectionService : class, IConnectionService<BlobContainerClientWrapper>
        {
            builder.SetStorageAndServiceConnection<BlobStorageRepository<T, TKey>, TConnectionService, BlobContainerClientWrapper>(
                name,
                serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a default blob storage service for your query pattern with a default connection service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IQueryBuilder<T, TKey> WithBlobStorage<T, TKey, TConnectionService>(
            this IQueryBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
            where TConnectionService : class, IConnectionService<BlobContainerClientWrapper>
        {
            builder.SetStorageAndServiceConnection<BlobStorageRepository<T, TKey>, TConnectionService, BlobContainerClientWrapper>(
                name,
                serviceLifetime);
            return builder;
        }
    }
}
