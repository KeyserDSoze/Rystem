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
        /// <param name="dataverseBuilder">Settings for your dataverse.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithDataverse<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<IDataverseRepositoryBuilder<T, TKey>> dataverseBuilder,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<IQuery<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    ServiceLifetime.Singleton);
            return builder;
        }
        /// <summary>
        /// Add a default dataverse service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="dataverseBuilder">Settings for your dataverse.</param>
        /// <param name="name">Factory name</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithDataverse<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<IDataverseRepositoryBuilder<T, TKey>> dataverseBuilder,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<IQuery<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    ServiceLifetime.Singleton);
            return builder;
        }
        /// <summary>
        /// Add a default dataverse service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="dataverseBuilder">Settings for your dataverse.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IRepositoryDataverseBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithDataverse<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<IDataverseRepositoryBuilder<T, TKey>> dataverseBuilder,
            string? name = null)
            where TKey : notnull
        {
            builder
                .Services
                .AddWarmUp(serviceProvider => DataverseCreateTableOrMergeNewColumnsInExistingTableAsync(
                    serviceProvider.GetService<IFactory<IQuery<T, TKey>>>()!.Create(name ?? string.Empty) as DataverseRepository<T, TKey>));
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    ServiceLifetime.Singleton);
            return builder;
        }
    }
}
