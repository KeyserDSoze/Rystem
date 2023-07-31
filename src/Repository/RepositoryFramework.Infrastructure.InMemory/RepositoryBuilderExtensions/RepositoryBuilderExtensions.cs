using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    public static class RepositoryBuilderExtensions
    {
        /// <summary>
        /// Add an in memory storage to your repository pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="inMemoryBuilder"></param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithInMemory<T, TKey>(
            this IRepositoryBuilder<T, TKey> builder,
            Action<IRepositoryInMemoryBuilder<T, TKey>>? inMemoryBuilder = null,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<InMemoryStorage<T, TKey>, RepositoryInMemoryBuilder<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    options.Services = builder.Services;
                    options.FactoryName = name ?? string.Empty;
                    inMemoryBuilder?.Invoke(options);
                },
                name,
                ServiceLifetime.Singleton);
            return builder;
        }
        /// <summary>
        /// Add an in memory storage to your command pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="inMemoryBuilder"></param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithInMemory<T, TKey>(
            this ICommandBuilder<T, TKey> builder,
            Action<IRepositoryInMemoryBuilder<T, TKey>>? inMemoryBuilder = null,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<InMemoryStorage<T, TKey>, RepositoryInMemoryBuilder<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    options.Services = builder.Services;
                    options.FactoryName = name ?? string.Empty;
                    inMemoryBuilder?.Invoke(options);
                },
                name,
                ServiceLifetime.Singleton);
            return builder;
        }
        /// <summary>
        /// Add an in memory storage to your query pattern.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="builder"></param>
        /// <param name="inMemoryBuilder"></param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithInMemory<T, TKey>(
            this IQueryBuilder<T, TKey> builder,
            Action<IRepositoryInMemoryBuilder<T, TKey>>? inMemoryBuilder = null,
            string? name = null)
            where TKey : notnull
        {
            builder.SetStorageAndBuildOptions<InMemoryStorage<T, TKey>, RepositoryInMemoryBuilder<T, TKey>, RepositoryBehaviorSettings<T, TKey>>(
                options =>
                {
                    options.Services = builder.Services;
                    options.FactoryName = name ?? string.Empty;
                    inMemoryBuilder?.Invoke(options);
                },
                name,
                ServiceLifetime.Singleton);
            return builder;
        }
    }
}
