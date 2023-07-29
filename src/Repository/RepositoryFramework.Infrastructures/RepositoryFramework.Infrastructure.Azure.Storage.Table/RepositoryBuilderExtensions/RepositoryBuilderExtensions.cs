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
        /// <param name="connectionSettings">Settings for your table storage.</param>
        /// <returns>IRepositoryTableStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryTableStorageBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<TableStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                TableStorageConnectionSettings,
                TableClientWrapper>(
                    options =>
                    {
                        connectionSettings.Invoke(options);
                        options.ModelType = typeof(T);
                    },
                    name,
                    ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryTableStorageBuilder<T, TKey>(builder.Services, name ?? string.Empty);
        }
        /// <summary>
        /// Add a default table storage service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your table storage.</param>
        /// <returns>IRepositoryTableStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryTableStorageBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<TableStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                TableStorageConnectionSettings,
                TableClientWrapper>(
                    options =>
                    {
                        connectionSettings.Invoke(options);
                        options.ModelType = typeof(T);
                    },
                    name,
                    ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryTableStorageBuilder<T, TKey>(builder.Services, name ?? string.Empty);
        }
        /// <summary>
        /// Add a default table storage service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your table storage.</param>
        /// <returns>IRepositoryTableStorageBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryTableStorageBuilder<T, TKey>> WithTableStorageAsync<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<TableStorageConnectionSettings> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<TableStorageRepository<T, TKey>,
                TableStorageConnectionSettings,
                TableClientWrapper>(
                    options =>
                    {
                        connectionSettings.Invoke(options);
                        options.ModelType = typeof(T);
                    },
                    name,
                    ServiceLifetime.Singleton)
                .NoContext();
            return new RepositoryTableStorageBuilder<T, TKey>(builder.Services, name ?? string.Empty);
        }
    }
}
