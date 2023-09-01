using RepositoryFramework;
using RepositoryFramework.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add a Repository Client as IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path.
        /// The final url will be https://{domain}/{startingPath}/
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="apiBuilder">Builder for you api integration</param>
        /// <param name="name">Factory name</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithApiClient<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<IApiRepositoryBuilder<T, TKey>> apiBuilder,
           string? name = null,
           ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<RepositoryClient<T, TKey>, ApiRepositoryBuilder<T, TKey>, ApiClientSettings<T, TKey>>(options =>
            {
                options.Services = builder.Services;
                apiBuilder.Invoke(options);
            }, name, serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a Query Client as IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path.
        /// The final url will be https://{domain}/{startingPath}/
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="apiBuilder">Builder for you api integration</param>
        /// <param name="name">Factory name</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithApiClient<T, TKey>(
           this IQueryBuilder<T, TKey> builder,
           Action<IApiRepositoryBuilder<T, TKey>> apiBuilder,
           string? name = null,
           ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<RepositoryClient<T, TKey>, ApiRepositoryBuilder<T, TKey>, ApiClientSettings<T, TKey>>(options =>
            {
                options.Services = builder.Services;
                apiBuilder.Invoke(options);
            }, name, serviceLifetime);
            return builder;
        }
        /// <summary>
        /// Add a Command Client as ICommand<<typeparamref name="T"/>, <typeparamref name="TKey"/>> with a domain and a starting path
        /// The final url will be https://{domain}/{startingPath}/
        /// </summary>
        /// <typeparam name="T">Model used for your repository</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="apiBuilder">Builder for you api integration</param>
        /// <param name="name">Factory name</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithApiClient<T, TKey>(
           this ICommandBuilder<T, TKey> builder,
           Action<IApiRepositoryBuilder<T, TKey>> apiBuilder,
           string? name = null,
           ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<RepositoryClient<T, TKey>, ApiRepositoryBuilder<T, TKey>, ApiClientSettings<T, TKey>>(options =>
            {
                options.Services = builder.Services;
                apiBuilder.Invoke(options);
            }, name, serviceLifetime);
            return builder;
        }
    }
}
