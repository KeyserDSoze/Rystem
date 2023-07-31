using RepositoryFramework;
using RepositoryFramework.Infrastructure.Azure.Cosmos.Sql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a default cosmos sql service for your repository pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="cosmosSqlBuilder">Settings for your Cosmos database.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IRepositoryBuilder<T, TKey>> WithCosmosSqlAsync<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<CosmosSqlRepository<T, TKey>,
                CosmosSqlRepositoryBuilder<T, TKey>,
                CosmosSqlClient>(
                options =>
                {
                    options.Services = builder.Services;
                    cosmosSqlBuilder.Invoke(options);
                }, name, ServiceLifetime.Singleton).NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default cosmos sql service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="cosmosSqlBuilder">Settings for your Cosmos database.</param>
        /// <param name="name">Factory name</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<ICommandBuilder<T, TKey>> WithCosmosSqlAsync<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<CosmosSqlRepository<T, TKey>,
                 CosmosSqlRepositoryBuilder<T, TKey>,
                 CosmosSqlClient>(
                 options =>
                 {
                     options.Services = builder.Services;
                     cosmosSqlBuilder.Invoke(options);
                 }, name, ServiceLifetime.Singleton).NoContext();
            return builder;
        }
        /// <summary>
        /// Add a default cosmos sql service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="cosmosSqlBuilder">Settings for your Cosmos database.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static async Task<IQueryBuilder<T, TKey>> WithCosmosSqlAsync<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
        {
            await builder.SetStorageAndBuildOptionsAsync<CosmosSqlRepository<T, TKey>,
                CosmosSqlRepositoryBuilder<T, TKey>,
                CosmosSqlClient>(
                options =>
                {
                    options.Services = builder.Services;
                    cosmosSqlBuilder.Invoke(options);
                }, name, ServiceLifetime.Singleton).NoContext();
            return builder;
        }
    }
}
