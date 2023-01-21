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
        /// <param name="settings">IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></param>
        /// <param name="options">Settings for your cache.</param>
        /// <returns>IRepositorySettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositorySettings<T, TKey> WithInMemoryCache<T, TKey>(
           this RepositorySettings<T, TKey> settings,
           Action<CacheOptions<T, TKey>>? options = null)
            where TKey : notnull
        {
            settings.Services.AddMemoryCache();
            return settings.WithCache<T, TKey, InMemoryCache<T, TKey>>(options, ServiceLifetime.Singleton);
        }
    }
}
