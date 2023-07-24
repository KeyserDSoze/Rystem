using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositoryBuilderExtensions
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
        private static TRepositoryBuilder WithCache<T, TKey, TCache, TRepositoryPattern, TRepositoryBuilder>(
          this IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder> builder,
          Action<CacheOptions<T, TKey>>? options = null,
          ServiceLifetime lifetime = ServiceLifetime.Singleton)
           where TKey : notnull
           where TCache : class, ICache<T, TKey>
           where TRepositoryPattern : class
           where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        {
            var defaultOptions = new CacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            builder.Services
                .RemoveService(typeof(ICache<T, TKey>))
                .AddService<ICache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            builder
                .Services
                .AddCacheManager(PatternType.Repository, defaultOptions);
            return (TRepositoryBuilder)builder;
        }
        /// <summary>
        /// Add cache mechanism for your Repository pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithCache<T, TKey, TCache>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, ICache<T, TKey>
        {
            return builder
                .WithCache<T, TKey, TCache, IRepositoryPattern<T, TKey>, IRepositoryBuilder<T, TKey>>(options, lifetime);
        }
        /// <summary>
        /// Add cache mechanism for your command pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithCache<T, TKey, TCache>(
           this ICommandBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, ICache<T, TKey>
        {
            return builder
                .WithCache<T, TKey, TCache, ICommandPattern<T, TKey>, ICommandBuilder<T, TKey>>(options, lifetime);
        }
        /// <summary>
        /// Add cache mechanism for your query pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithCache<T, TKey, TCache>(
           this IQueryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, ICache<T, TKey>
        {
            return builder
                .WithCache<T, TKey, TCache, IQueryPattern<T, TKey>, IQueryBuilder<T, TKey>>(options, lifetime);
        }
        /// <summary>
        /// Add distributed (for multi-instance environments) cache mechanism for your Repository pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithDistributedCache<T, TKey, TCache>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
        {
            return builder
                .WithDistributedCache<T, TKey, TCache, IRepositoryPattern<T, TKey>, IRepositoryBuilder<T, TKey>>(options, lifetime);
        }
        /// <summary>
        /// Add distributed (for multi-instance environments) cache mechanism for your command pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>ICommandBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static ICommandBuilder<T, TKey> WithDistributedCache<T, TKey, TCache>(
           this ICommandBuilder<T, TKey> builder,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
        {
            return builder
                .WithDistributedCache<T, TKey, TCache, ICommandPattern<T, TKey>, ICommandBuilder<T, TKey>>(options, lifetime);
        }
        /// <summary>
        /// Add distributed (for multi-instance environments) cache mechanism for your query pattern. 
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <typeparam name="TCache">Implementation of your cache.</typeparam>
        /// <param name="builder">IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IQueryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IQueryBuilder<T, TKey> WithDistributedCache<T, TKey, TCache>(
           this IQueryBuilder<T, TKey> builder,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
        {
            return builder
                .WithDistributedCache<T, TKey, TCache, IQueryPattern<T, TKey>, IQueryBuilder<T, TKey>>(options, lifetime);
        }
        public static TRepositoryBuilder WithDistributedCache<T, TKey, TCache, TRepositoryPattern, TRepositoryBuilder>(
          this IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder> builder,
           Action<DistributedCacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            where TCache : class, IDistributedCache<T, TKey>
            where TRepositoryPattern : class
            where TRepositoryBuilder : IRepositoryBaseBuilder<T, TKey, TRepositoryPattern, TRepositoryBuilder>
        {
            var defaultOptions = new DistributedCacheOptions<T, TKey>();
            options?.Invoke(defaultOptions);
            builder.Services
                .RemoveService(typeof(IDistributedCache<T, TKey>))
                .AddService<IDistributedCache<T, TKey>, TCache>(lifetime)
                .AddSingleton(defaultOptions);
            builder.Services.AddCacheManager(PatternType.Repository, defaultOptions);
            return (TRepositoryBuilder)builder;
        }
    }
}
