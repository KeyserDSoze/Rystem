using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddService<TImplementation>(this IServiceCollection services,
           ServiceLifetime lifetime)
           where TImplementation : class
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddTransient<TImplementation>(),
               ServiceLifetime.Singleton => services.AddSingleton<TImplementation>(),
               _ => services.AddScoped<TImplementation>()
           };
        public static IServiceCollection AddService<TService, TImplementation>(this IServiceCollection services,
            ServiceLifetime lifetime)
            where TImplementation : class, TService
            where TService : class
            => lifetime switch
            {
                ServiceLifetime.Transient => services.AddTransient<TService, TImplementation>(),
                ServiceLifetime.Singleton => services.AddSingleton<TService, TImplementation>(),
                _ => services.AddScoped<TService, TImplementation>()
            };
        public static IServiceCollection AddService(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime)
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
               ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
               _ => services.AddScoped(serviceType, implementationType)
           };
        public static IServiceCollection TryAddService<TImplementation>(this IServiceCollection services,
           ServiceLifetime lifetime)
           where TImplementation : class
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    services.TryAddTransient<TImplementation>();
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<TImplementation>();
                    break;
                default:
                    services.TryAddScoped<TImplementation>();
                    break;
            }
            return services;
        }
        public static IServiceCollection TryAddService<TService, TImplementation>(this IServiceCollection services,
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
    }
}
