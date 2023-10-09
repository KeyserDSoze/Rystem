namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static IServiceCollection AddKeyedServiceEngine(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type? implementationType,
            object? instance,
            Func<IServiceProvider, object?, object>? instanceFactory,
            ServiceLifetime lifetime,
            bool? canOverride,
            int id = 0)
        {
            if (serviceKey == null)
                throw new ArgumentNullException(nameof(serviceKey));
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            if (map.Services.ContainsKey(serviceKey))
            {
                if (canOverride == true)
                {
                    if (map.Services.Remove(serviceKey, out var value))
                        services.Remove(value);
                }
                else if (canOverride == false)
                    throw new ArgumentException($"{serviceKey} already installed.");
                else
                    return services;
            }
            ServiceDescriptor descriptor;
            if (instance != null)
                descriptor = new ServiceDescriptor(serviceType, serviceKey, instance);
            else if (instanceFactory != null)
                descriptor = new ServiceDescriptor(serviceType, serviceKey, (services, key) => instanceFactory(services, key), lifetime);
            else
            {
                if (implementationType == null)
                    descriptor = new ServiceDescriptor(serviceType, serviceKey, lifetime);
                else
                    descriptor = new ServiceDescriptor(serviceType, serviceKey, implementationType, lifetime);
            }
            map.Services.Add(serviceKey, descriptor);
            services.Add(descriptor);
            return services;
        }
        public static IServiceCollection AddKeyedService<TService>(this IServiceCollection services,
            object? serviceKey,
           ServiceLifetime lifetime)
           where TService : class
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddKeyedTransient<TService>(serviceKey),
               ServiceLifetime.Singleton => services.AddKeyedSingleton<TService>(serviceKey),
               _ => services.AddKeyedScoped<TService>(serviceKey)
           };
        public static IServiceCollection AddKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            ServiceLifetime lifetime)
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddKeyedTransient(serviceType, serviceKey),
               ServiceLifetime.Singleton => services.AddKeyedSingleton(serviceType, serviceKey),
               _ => services.AddKeyedScoped(serviceType, serviceKey)
           };
        public static IServiceCollection AddKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
           Func<IServiceProvider, object?, TService> implementationFactory,
          ServiceLifetime lifetime)
          where TService : class
          => lifetime switch
          {
              ServiceLifetime.Transient => services.AddKeyedTransient(serviceKey, implementationFactory),
              ServiceLifetime.Singleton => services.AddKeyedSingleton(serviceKey, implementationFactory),
              _ => services.AddKeyedScoped(serviceKey, implementationFactory)
          };
        public static IServiceCollection AddKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
            => lifetime switch
            {
                ServiceLifetime.Transient => services.AddKeyedTransient<TService, TImplementation>(serviceKey),
                ServiceLifetime.Singleton => services.AddKeyedSingleton<TService, TImplementation>(serviceKey),
                _ => services.AddKeyedScoped<TService, TImplementation>(serviceKey)
            };
        public static IServiceCollection AddKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType,
            ServiceLifetime lifetime)
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddKeyedTransient(serviceType, serviceKey, implementationType),
               ServiceLifetime.Singleton => services.AddKeyedSingleton(serviceType, serviceKey, implementationType),
               _ => services.AddKeyedScoped(serviceType, serviceKey, implementationType)
           };
        public static IServiceCollection AddKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
            => lifetime switch
            {
                ServiceLifetime.Transient => services.AddKeyedTransient<TService, TImplementation>(serviceKey, implementationFactory),
                ServiceLifetime.Singleton => services.AddKeyedSingleton<TService, TImplementation>(serviceKey, implementationFactory),
                _ => services.AddKeyedScoped<TService, TImplementation>(serviceKey, implementationFactory)
            };
    }
}
