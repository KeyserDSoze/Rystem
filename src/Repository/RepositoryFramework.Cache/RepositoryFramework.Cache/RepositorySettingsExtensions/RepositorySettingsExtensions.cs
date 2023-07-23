using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        private static void AddCacheManager<T, TKey>(
            this IServiceCollection services,
            PatternType patternType,
            CacheOptions<T, TKey> options)
            where TKey : notnull
        {
            if (patternType == PatternType.Repository)
                services
                    .AddDecoration<IRepository<T, TKey>, CachedRepository<T, TKey>>();
            else if (patternType == PatternType.Query)
                services
                    .AddDecoration<IQuery<T, TKey>, CachedQuery<T, TKey>>();
            else if (patternType == PatternType.Command && options.HasCommandPattern)
                services
                    .AddDecoration<ICommand<T, TKey>, CachedRepository<T, TKey>>();
        }
        /// <summary>
        /// Add cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithCache<T, TKey, TCache>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, ICache<T, TKey>
        {
            var defaultOptions = new CacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            builder.Services
                .RemoveServiceIfAlreadyInstalled<TCache>(true, typeof(ICache<T, TKey>))
                .AddService<ICache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            builder
                .Services
                .AddCacheManager(PatternType.Repository, defaultOptions);
            return builder;
        }
        /// <summary>
        /// Add distributed (for multi-instance environments) cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithDistributedCache<T, TKey, TCache>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
        {
            var defaultOptions = new DistributedCacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            builder.Services
                .RemoveServiceIfAlreadyInstalled<TCache>(true, typeof(IDistributedCache<T, TKey>))
                .AddService<IDistributedCache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            builder.Services.AddCacheManager(PatternType.Repository, defaultOptions);
            return builder;
        }
        /// <summary>
        /// Add cache to your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>RepositoryBusinessSettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> AddCacheForRepository<T, TKey>(this IServiceCollection services)
            where TKey : notnull
            => new(services, null);
    }
}
