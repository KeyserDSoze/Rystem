using System.Reflection;
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
        /// <param name="builder">Builder for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddRepository<T, TKey>(this IServiceCollection services,
          Action<IRepositoryBuilder<T, TKey>> builder)
          where TKey : notnull
        {
            var defaultSettings = new RepositoryFrameworkBuilder<T, TKey>(services);
            builder.Invoke(defaultSettings);
            defaultSettings.AfterBuild?.Invoke();
            defaultSettings.AfterBuildAsync?.Invoke().ToResult();
            return services;
        }
        /// <summary>
        /// Add command storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="builder">Settings for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddCommand<T, TKey>(this IServiceCollection services,
              Action<ICommandBuilder<T, TKey>> builder)
            where TKey : notnull
        {
            var defaultSettings = new CommandFrameworkBuilder<T, TKey>(services);
            builder?.Invoke(defaultSettings);
            defaultSettings.AfterBuild?.Invoke();
            defaultSettings.AfterBuildAsync?.Invoke().ToResult();
            return services;
        }
        /// <summary>
        /// Add query storage
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <param name="builder">Settings for your repository.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddQuery<T, TKey>(this IServiceCollection services,
              Action<IQueryBuilder<T, TKey>> builder)
            where TKey : notnull
        {
            var defaultSettings = new QueryFrameworkBuilder<T, TKey>(services);
            builder?.Invoke(defaultSettings);
            defaultSettings.AfterBuild?.Invoke();
            defaultSettings.AfterBuildAsync?.Invoke().ToResult();
            return services;
        }
        /// <summary>
        /// Add business to your repository or CQRS pattern.
        /// </summary>
        /// <typeparam name="T">Model used for your repository.</typeparam>
        /// <typeparam name="TKey">Key to manage your data from repository.</typeparam>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>RepositoryBusinessBuilder<<typeparamref name="T"/>, <typeparamref name="TKey"/>></returns>
        public static RepositoryBusinessBuilder<T, TKey> AddBusinessForRepository<T, TKey>(this IServiceCollection services)
            where TKey : notnull
            => new(services, null);
        /// <summary>
        /// Add all business classes to your repository or CQRS pattern.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection ScanBusinessForRepositoryFramework(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            var types = new List<Type>()
            {
                typeof(IRepositoryBusinessBeforeBatch<,>),
                typeof(IRepositoryBusinessBeforeInsert<,>),
                typeof(IRepositoryBusinessBeforeUpdate<,>),
                typeof(IRepositoryBusinessBeforeExist<,>),
                typeof(IRepositoryBusinessBeforeGet<,>),
                typeof(IRepositoryBusinessBeforeDelete<,>),
                typeof(IRepositoryBusinessBeforeOperation<,>),
                typeof(IRepositoryBusinessBeforeQuery<,>),
                typeof(IRepositoryBusinessAfterBatch<,>),
                typeof(IRepositoryBusinessAfterInsert<,>),
                typeof(IRepositoryBusinessAfterUpdate<,>),
                typeof(IRepositoryBusinessAfterExist<,>),
                typeof(IRepositoryBusinessAfterGet<,>),
                typeof(IRepositoryBusinessAfterDelete<,>),
                typeof(IRepositoryBusinessAfterOperation<,>),
                typeof(IRepositoryBusinessAfterQuery<,>),
            };
            var registry = services
                .TryAddSingletonAndGetService<RepositoryFrameworkRegistry>();
            foreach (var service in registry.Services.Select(x => x.Value))
            {
                var genericArguments = service.InterfaceType.GetGenericArguments();
                foreach (var type in types)
                {
                    var serviceType = type.MakeGenericType(genericArguments);
                    if (services
                        .Scan(serviceType, service.ServiceLifetime, assemblies) > 0)
                    {
                        var repositoryBusinessManagerInterface = typeof(IRepositoryBusinessManager<,>).MakeGenericType(genericArguments);
                        var repositoryBusinessManagerImplementation = typeof(RepositoryBusinessManager<,>).MakeGenericType(genericArguments);
                        services
                            .TryAddService(repositoryBusinessManagerInterface, repositoryBusinessManagerImplementation, ServiceLifetime.Transient); ;
                    }
                }
            }
            return services;
        }
        /// <summary>
        /// Add all business classes to your repository or CQRS pattern from current domain.
        /// </summary>
        /// <param name="services">IServiceCollection.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection ScanBusinessForRepositoryFramework(this IServiceCollection services)
            => services.ScanBusinessForRepositoryFramework(AppDomain.CurrentDomain.GetAssemblies());
    }
}
