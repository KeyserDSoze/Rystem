namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddService<TService>(this IServiceCollection services,
           ServiceLifetime lifetime)
           where TService : class
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddTransient<TService>(),
               ServiceLifetime.Singleton => services.AddSingleton<TService>(),
               _ => services.AddScoped<TService>()
           };
        public static IServiceCollection AddService(this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
           => lifetime switch
           {
               ServiceLifetime.Transient => services.AddTransient(serviceType),
               ServiceLifetime.Singleton => services.AddSingleton(serviceType),
               _ => services.AddScoped(serviceType)
           };
        public static IServiceCollection AddService<TService>(this IServiceCollection services,
           Func<IServiceProvider, TService> implementationFactory,
          ServiceLifetime lifetime)
          where TService : class
          => lifetime switch
          {
              ServiceLifetime.Transient => services.AddTransient(implementationFactory),
              ServiceLifetime.Singleton => services.AddSingleton(implementationFactory),
              _ => services.AddScoped(implementationFactory)
          };
        public static IServiceCollection AddService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
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
        public static IServiceCollection AddService<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
            => lifetime switch
            {
                ServiceLifetime.Transient => services.AddTransient<TService, TImplementation>(implementationFactory),
                ServiceLifetime.Singleton => services.AddSingleton<TService, TImplementation>(implementationFactory),
                _ => services.AddScoped<TService, TImplementation>(implementationFactory)
            };
    }
}
