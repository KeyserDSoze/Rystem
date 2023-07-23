using RepositoryFramework;
using RepositoryFramework.Cache;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RepositorySettingsExtensions
    {
        /// <summary>
        /// Add IDistributedCache you installed in your DI for your Repository or Query (CQRS) cache mechanism, 
        /// injected directly in the IRepository<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// or IQuery<<typeparamref name="T"/>, <typeparamref name="TKey"/>> interface
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="builder">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <param name="lifetime">Service Lifetime.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static IRepositoryBuilder<T, TKey> WithDistributedCache<T, TKey>(
           this IRepositoryBuilder<T, TKey> builder,
           Action<CacheOptions<T, TKey>>? options = null,
           ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TKey : notnull
            => builder.WithDistributedCache<T, TKey, DistributedCache<T, TKey>>(options, lifetime);
    }
}
