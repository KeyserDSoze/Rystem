namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
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
