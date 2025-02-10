using RepositoryFramework;
using RepositoryFramework.Infrastructure.MsSql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default MsSql service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">Builder for your repository.</param>
        /// <param name="sqlBuilder">Settings for your MsSql connection.</param>
        /// <param name="name">Factory name</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithMsSql<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
               Action<IMsSqlRepositoryBuilder<T, TKey>> sqlBuilder,
               string? name = null,
               ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<SqlRepository<T, TKey>, MsSqlRepositoryBuilder<T, TKey>, MsSqlOptions<T, TKey>>(
                sqlBuilder,
                name,
                lifetime);
            builder.Services.AddWarmUp(async serviceProvider =>
            {
                var repository = serviceProvider.GetRequiredService<IFactory<IRepositoryPattern<T, TKey>>>()!.CreateWithoutDecoration(name ?? string.Empty);
                if (repository is SqlRepository<T, TKey> sqlRepository)
                {
                    await sqlRepository.MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync();
                }
            });
            return builder;
        }
        /// <summary>
        /// Add a default MsSql service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">Builder for your repository.</param>
        /// <param name="sqlBuilder">Settings for your MsSql connection.</param>
        /// <param name="name">Factory name</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithMsSql<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
               Action<IMsSqlRepositoryBuilder<T, TKey>> sqlBuilder,
               string? name = null,
               ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<SqlRepository<T, TKey>, MsSqlRepositoryBuilder<T, TKey>, MsSqlOptions<T, TKey>>(
               sqlBuilder,
               name,
               lifetime);
            builder.Services.AddWarmUp(serviceProvider =>
                (serviceProvider.GetRequiredService<IFactory<ICommandPattern<T, TKey>>>()!.Create(name ?? string.Empty)
                    as SqlRepository<T, TKey>)!.MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync(
                    ));
            return builder;
        }
        /// <summary>
        /// Add a default MsSql service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">Builder for your repository.</param>
        /// <param name="sqlBuilder">Settings for your MsSql connection.</param>
        /// <param name="name">Factory name</param>
        /// <param name="lifetime">Service lifetime</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithMsSql<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
               Action<IMsSqlRepositoryBuilder<T, TKey>> sqlBuilder,
               string? name = null,
               ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<SqlRepository<T, TKey>, MsSqlRepositoryBuilder<T, TKey>, MsSqlOptions<T, TKey>>(
               sqlBuilder,
               name,
               lifetime);
            builder.Services.AddWarmUp(async serviceProvider =>
            {
                var queryPattern = serviceProvider.GetRequiredService<IFactory<IQueryPattern<T, TKey>>>()!.Create(name ?? string.Empty);
                if (queryPattern != null)
                    await queryPattern.BootstrapAsync();
            });
            return builder;
        }
    }
}
