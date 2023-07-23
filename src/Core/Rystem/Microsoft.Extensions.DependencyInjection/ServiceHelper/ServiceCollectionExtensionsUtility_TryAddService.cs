using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection TryAddService<TService>(
            this IServiceCollection services,
           ServiceLifetime lifetime)
           where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient<TService>();
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<TService>();
                    break;
                default:
                    services.TryAddScoped<TService>();
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService(this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient(serviceType);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton(serviceType);
                    break;
                default:
                    services.TryAddScoped(serviceType);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService(this IServiceCollection services,
            Type serviceType,
            Func<IServiceProvider, object> implementationFactory,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient(serviceType, implementationFactory);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton(serviceType, implementationFactory);
                    break;
                default:
                    services.TryAddScoped(serviceType, implementationFactory);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
           ServiceLifetime lifetime)
           where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient(implementationFactory);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton(implementationFactory);
                    break;
                default:
                    services.TryAddScoped(implementationFactory);
                    break;
            }
            return services;
        }

        public static IServiceCollection TryAddService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TImplementation : class, TService
            where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient<TService, TImplementation>();
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<TService, TImplementation>();
                    break;
                default:
                    services.TryAddScoped<TService, TImplementation>();
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient(serviceType, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton(serviceType, implementationType);
                    break;
                default:
                    services.TryAddScoped(serviceType, implementationType);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory,
           ServiceLifetime lifetime)
           where TService : class
            where TImplementation : class, TService
        {
            if (!services.Any(x => x.ServiceType == typeof(TService)))
            {
                services.AddService<TService, TImplementation>(
                    implementationFactory,
                    lifetime);
            }
            return services;
        }
    }
}
