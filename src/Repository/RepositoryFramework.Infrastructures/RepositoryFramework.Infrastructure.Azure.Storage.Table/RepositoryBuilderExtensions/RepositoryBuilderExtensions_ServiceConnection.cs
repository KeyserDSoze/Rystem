using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Storage.Table;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default table storage service for your repository pattern with connection service integration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IRepositoryBuilder<T, TKey> WithTableStorage<T, TKey, TConnectionService>(
            this IRepositoryBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
             where TConnectionService : class, IConnectionService<TableClientWrapper<T, TKey>>
        {
            builder.SetStorageAndServiceConnection<TableStorageRepository<T, TKey>, TConnectionService, TableClientWrapper<T, TKey>>(name, serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a default table storage service for your command pattern with connection service integration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static ICommandBuilder<T, TKey> WithTableStorage<T, TKey, TConnectionService>(
            this ICommandBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
             where TConnectionService : class, IConnectionService<TableClientWrapper<T, TKey>>
        {
            builder.SetStorageAndServiceConnection<TableStorageRepository<T, TKey>, TConnectionService, TableClientWrapper<T, TKey>>(name, serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a default table storage service for your query pattern with connection service integration.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TConnectionService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="name"></param>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public static IQueryBuilder<T, TKey> WithTableStorage<T, TKey, TConnectionService>(
            this IQueryBuilder<T, TKey> builder,
            string? name = null,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
             where TConnectionService : class, IConnectionService<TableClientWrapper<T, TKey>>
        {
            builder.SetStorageAndServiceConnection<TableStorageRepository<T, TKey>, TConnectionService, TableClientWrapper<T, TKey>>(name, serviceLifetime);
            return builder;
        }
    }
}
