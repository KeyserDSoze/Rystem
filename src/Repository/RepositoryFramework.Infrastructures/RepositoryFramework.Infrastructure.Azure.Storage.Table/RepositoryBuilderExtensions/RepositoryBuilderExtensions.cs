using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Storage.Table;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default table storage service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="tableStorageBuilder">Settings for your table storage.</param>
        /// <param name="name">Factory name.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<ITableStorageRepositoryBuilder<T, TKey>> tableStorageBuilder,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                TableStorageRepositoryBuilder<T, TKey>,
                TableClientWrapper<T, TKey>>(
                    options =>
                    {
                        options.SetDefault(builder.Services, name);
                        tableStorageBuilder.Invoke(options);
                        options.Settings.ModelType = typeof(T);
                    },
                    name,
                    lifetime)
                .NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default table storage service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="tableStorageBuilder">Settings for your table storage.</param>
        /// <param name="name">Factory name.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<ICommandBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<ITableStorageRepositoryBuilder<T, TKey>> tableStorageBuilder,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                TableStorageRepositoryBuilder<T, TKey>,
                TableClientWrapper<T, TKey>>(
                    options =>
                    {
                        options.SetDefault(builder.Services, name);
                        tableStorageBuilder.Invoke(options);
                        options.Settings.ModelType = typeof(T);
                    },
                    name,
                    lifetime)
                .NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default table storage service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="tableStorageBuilder">Settings for your table storage.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IQueryBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<ITableStorageRepositoryBuilder<T, TKey>> tableStorageBuilder,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                 TableStorageRepositoryBuilder<T, TKey>,
                 TableClientWrapper<T, TKey>>(
                     options =>
                     {
                         options.SetDefault(builder.Services, name);
                         tableStorageBuilder.Invoke(options);
                         options.Settings.ModelType = typeof(T);
                     },
                     name,
                     lifetime)
                 .NoContext();
            return builder;
        }
    }
}
