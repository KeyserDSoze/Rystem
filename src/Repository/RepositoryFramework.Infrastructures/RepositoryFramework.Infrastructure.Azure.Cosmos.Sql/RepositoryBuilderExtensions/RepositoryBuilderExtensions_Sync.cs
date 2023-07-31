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
        public static IRepositoryBuilder<T, TKey> WithCosmosSql<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
            => builder.WithCosmosSqlAsync(cosmosSqlBuilder, name).ToResult();
        /// <summary>
        /// Add a default cosmos sql service for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="cosmosSqlBuilder">Settings for your Cosmos database.</param>
        /// <param name="name">Factory name</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithCosmosSql<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
            => builder.WithCosmosSqlAsync(cosmosSqlBuilder, name).ToResult();
        /// <summary>
        /// Add a default cosmos sql service for your query pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="cosmosSqlBuilder">Settings for your Cosmos database.</param>
        /// <param name="name">Factory name</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithCosmosSql<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
            Action<ICosmosSqlRepositoryBuilder<T, TKey>> cosmosSqlBuilder,
            string? name = null)
            where TKey : notnull
            => builder.WithCosmosSqlAsync(cosmosSqlBuilder, name).ToResult();
    }
}
