using RepositoryFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add repository framework
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddRepository<T, TKey>(this IServiceCollection services,
          Action<RepositorySettings<T, TKey>> settings)
          where TKey : notnull
        {
            var defaultSettings = new RepositorySettings<T, TKey>(services, PatternType.Repository);
            settings.Invoke(defaultSettings);
            return services;
        }
        /// <summary>
        /// Add repository framework with a storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddRepository<T, TKey, TStorage>(this IServiceCollection services,
              Action<RepositorySettings<T, TKey>>? settings = null,
              ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TKey : notnull
            where TStorage : class, IRepository<T, TKey>
            => services.AddRepository<T, TKey>(x =>
                {
                    settings?.Invoke(x);
                    x.SetStorage<TStorage>(serviceLifetime);
                });
        /// <summary>
        /// Add command storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddCommand<T, TKey>(this IServiceCollection services,
              Action<RepositorySettings<T, TKey>>? settings = null,
              ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TKey : notnull
        {
            var defaultSettings = new RepositorySettings<T, TKey>(services, PatternType.Command);
            settings?.Invoke(defaultSettings);
            return services;
        }
        /// <summary>
        /// Add command storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddCommand<T, TKey, TStorage>(this IServiceCollection services,
              Action<RepositorySettings<T, TKey>>? settings = null,
              ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TKey : notnull
            where TStorage : class, ICommand<T, TKey>
            => services.AddCommand<T, TKey>(x =>
            {
                settings?.Invoke(x);
                x.SetStorage<TStorage>(serviceLifetime);
            });
        /// <summary>
        /// Add query storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddQuery<T, TKey>(this IServiceCollection services,
              Action<RepositorySettings<T, TKey>>? settings = null,
              ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TKey : notnull
        {
            var defaultSettings = new RepositorySettings<T, TKey>(services, PatternType.Query);
            settings?.Invoke(defaultSettings);
            return services;
        }
        /// <summary>
        /// Add query storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="settings">Settings for your repository.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddQuery<T, TKey, TStorage>(this IServiceCollection services,
              Action<RepositorySettings<T, TKey>>? settings = null,
              ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
            where TKey : notnull
            where TStorage : class, IQuery<T, TKey>
            => services.AddQuery<T, TKey>(x =>
            {
                settings?.Invoke(x);
                x.SetStorage<TStorage>(serviceLifetime);
            });
        /// <summary>
        /// Add business to your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="serviceLifetime">Service Lifetime</param>
        /// <returns>RepositoryBusinessSettings<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositoryBusinessSettings<T, TKey> AddBusinessForRepository<T, TKey>(this IServiceCollection services,
            ServiceLifetime? serviceLifetime = null)
            where TKey : notnull
            => new(services, serviceLifetime);
    }
}
