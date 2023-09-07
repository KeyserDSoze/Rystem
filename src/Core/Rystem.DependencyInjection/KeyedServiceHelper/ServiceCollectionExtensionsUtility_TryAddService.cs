using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection TryAddKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
           ServiceLifetime lifetime)
           where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient<TService>(serviceKey);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton<TService>(serviceKey);
                    break;
                default:
                    services.TryAddKeyedScoped<TService>(serviceKey);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceType, serviceKey);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceType, serviceKey);
                    break;
                default:
                    services.TryAddKeyedScoped(serviceType, serviceKey);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceType, serviceKey, implementationFactory);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceType, serviceKey, implementationFactory);
                    break;
                default:
                    services.TryAddKeyedScoped(serviceType, serviceKey, implementationFactory);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory,
           ServiceLifetime lifetime)
           where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceKey, implementationFactory);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceKey, implementationFactory);
                    break;
                default:
                    services.TryAddKeyedScoped(serviceKey, implementationFactory);
                    break;
            }
            return services;
        }

        public static IServiceCollection TryAddKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            ServiceLifetime lifetime)
            where TImplementation : class, TService
            where TService : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient<TService, TImplementation>(serviceKey);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton<TService, TImplementation>(serviceKey);
                    break;
                default:
                    services.TryAddKeyedScoped<TService, TImplementation>(serviceKey);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType,
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceType, serviceKey, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceType, serviceKey, implementationType);
                    break;
                default:
                    services.TryAddKeyedScoped(serviceType, serviceKey, implementationType);
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory,
           ServiceLifetime lifetime)
           where TService : class
            where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddKeyedTransient(serviceKey, implementationFactory);
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddKeyedSingleton(serviceKey, implementationFactory);
                    break;
                default:
                    services.TryAddKeyedScoped(serviceKey, implementationFactory);
                    break;
            }
            return services;
        }
    }
}
