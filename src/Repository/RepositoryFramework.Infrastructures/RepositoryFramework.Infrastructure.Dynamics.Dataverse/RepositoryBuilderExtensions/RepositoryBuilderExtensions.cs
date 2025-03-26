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
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            builder
                .Services
                    .AddWarmUp(async serviceProvider =>
                    {
                        var repository = serviceProvider.GetService<IFactory<IRepositoryPattern<T, TKey>>>()!.Create(name ?? string.Empty);
                        if (repository != null)
                            await repository.BootstrapAsync();
                    });
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    lifetime);
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
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            builder
                .Services
                    .AddWarmUp(async serviceProvider =>
                    {
                        var repository = serviceProvider.GetService<IFactory<ICommandPattern<T, TKey>>>()!.Create(name ?? string.Empty);
                        if (repository != null)
                            await repository.BootstrapAsync();
                    });
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    lifetime);
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
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
        {
            builder
                .Services
                    .AddWarmUp(async serviceProvider =>
                    {
                        var repository = serviceProvider.GetService<IFactory<IQueryPattern<T, TKey>>>()!.Create(name ?? string.Empty);
                        if (repository != null)
                            await repository.BootstrapAsync();
                    });
            builder.SetStorageAndBuildOptions<DataverseRepository<T, TKey>,
                DataverseRepositoryBuilder<T, TKey>,
                DataverseClientWrapper<T, TKey>>(
                    dataverseBuilder,
                    name,
                    lifetime);
            return builder;
        }
    }
}
