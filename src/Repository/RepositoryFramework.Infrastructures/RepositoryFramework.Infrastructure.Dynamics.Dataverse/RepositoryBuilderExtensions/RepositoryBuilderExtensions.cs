using Microsoft.Extensions.DependencyInjection.Extensions;
using RepositoryFramework;
using RepositoryFramework.Infrastructure.Dynamics.Dataverse;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default dataverse service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your dataverse.</param>
        /// <returns>IRepositoryDataverseBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryDataverseBuilder<T, TKey> WithDataverse<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<DataverseOptions<T, TKey>> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<IRepository<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseOptions<T, TKey>,
                DataverseClientWrapper>(
                    connectionSettings,
                    name,
                    ServiceLifetime.Singleton);
            builder
                .Services
                .TryAddSingleton(DataverseOptions<T, TKey>.Instance);
            return new RepositoryDataverseBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add a default dataverse service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your dataverse.</param>
        /// <returns>IRepositoryDataverseBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryDataverseBuilder<T, TKey> WithDataverse<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<DataverseOptions<T, TKey>> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<ICommand<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseOptions<T, TKey>,
                DataverseClientWrapper>(
                    connectionSettings,
                    name,
                    ServiceLifetime.Singleton);
            builder
                .Services
                .TryAddSingleton(DataverseOptions<T, TKey>.Instance);
            return new RepositoryDataverseBuilder<T, TKey>(builder.Services);
        }
        /// <summary>
        /// Add a default dataverse service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="connectionSettings">Settings for your dataverse.</param>
        /// <returns>IRepositoryDataverseBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryDataverseBuilder<T, TKey> WithDataverse<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<DataverseOptions<T, TKey>> connectionSettings,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<IQuery<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseOptions<T, TKey>,
                DataverseClientWrapper>(
                    connectionSettings,
                    name,
                    ServiceLifetime.Singleton);
            builder
                .Services
                .TryAddSingleton(DataverseOptions<T, TKey>.Instance);
            return new RepositoryDataverseBuilder<T, TKey>(builder.Services);
        }
    }
}
