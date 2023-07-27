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
        /// <param name="options">Settings for your MsSql connection.</param>
        /// <returns>IRepositoryMsSqlBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryMsSqlBuilder<T, TKey> WithMsSql<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
               Action<MsSqlOptions<T, TKey>> options,
               string? name = null,
               ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            var sqlBuilder = new RepositoryMsSqlBuilder<T, TKey>(builder.Services);
            builder.SetStorageWithOptions<SqlRepository<T, TKey>, MsSqlOptions<T, TKey>>(
                settings =>
                {
                    options.Invoke(settings);
                    settings.RefreshColumnNames();
                    foreach (var action in sqlBuilder.ActionsToDoDuringSettingsSetup)
                        action.Invoke(settings);
                },
                name,
                serviceLifetime);
            builder.Services.AddWarmUp(serviceProvider =>
                (serviceProvider.GetRequiredService<IFactory<IRepository<T, TKey>>>()!.Create(name ?? string.Empty)
                    as SqlRepository<T, TKey>)!.MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync(
                    ));
            return sqlBuilder;
        }
        /// <summary>
        /// Add a default MsSql service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">Builder for your repository.</param>
        /// <param name="options">Settings for your MsSql connection.</param>
        /// <returns>IRepositoryMsSqlBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryMsSqlBuilder<T, TKey> WithMsSql<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
               Action<MsSqlOptions<T, TKey>> options,
               string? name = null,
               ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            var sqlBuilder = new RepositoryMsSqlBuilder<T, TKey>(builder.Services);
            builder.SetStorageWithOptions<SqlRepository<T, TKey>, MsSqlOptions<T, TKey>>(
                settings =>
                {
                    options.Invoke(settings);
                    settings.RefreshColumnNames();
                    foreach (var action in sqlBuilder.ActionsToDoDuringSettingsSetup)
                        action.Invoke(settings);
                },
                name,
                serviceLifetime);
            builder.Services.AddWarmUp(serviceProvider =>
                (serviceProvider.GetRequiredService<IFactory<ICommand<T, TKey>>>()!.Create(name ?? string.Empty)
                    as SqlRepository<T, TKey>)!.MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync(
                    ));
            return sqlBuilder;
        }
        /// <summary>
        /// Add a default MsSql service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">Builder for your repository.</param>
        /// <param name="options">Settings for your MsSql connection.</param>
        /// <returns>IRepositoryMsSqlBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryMsSqlBuilder<T, TKey> WithMsSql<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
               Action<MsSqlOptions<T, TKey>> options,
               string? name = null,
               ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            var sqlBuilder = new RepositoryMsSqlBuilder<T, TKey>(builder.Services);
            builder.SetStorageWithOptions<SqlRepository<T, TKey>, MsSqlOptions<T, TKey>>(
                settings =>
                {
                    options.Invoke(settings);
                    settings.RefreshColumnNames();
                    foreach (var action in sqlBuilder.ActionsToDoDuringSettingsSetup)
                        action.Invoke(settings);
                },
                name,
                serviceLifetime);
            builder.Services.AddWarmUp(serviceProvider =>
                (serviceProvider.GetRequiredService<IFactory<IQuery<T, TKey>>>()!.Create(name ?? string.Empty)
                    as SqlRepository<T, TKey>)!.MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync(
                    ));
            return sqlBuilder;
        }
    }
}
