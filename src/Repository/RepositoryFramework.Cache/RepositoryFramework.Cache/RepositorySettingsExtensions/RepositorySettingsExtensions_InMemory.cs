using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add in memory cache mechanism for your Repository or Query (CQRS), 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <returns>IRepositoryBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithInMemoryCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null)
            where TKey : notnull
        {
            builder.Services.AddMemoryCache();
            return builder.WithCache<T, TKey, InMemoryCache<T, TKey>>(options, ServiceLifetime.Singleton);
        }
    }
}
