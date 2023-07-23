namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrOverrideService<TService, TImplementation>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
           where TService : class
           where TImplementation : class, TService
            => services
                .RemoveService(typeof(TService))
                .AddService<TService, TImplementation>(lifetime);
        public static IServiceCollection AddOrOverrideService(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime)
            => services
                .RemoveService(serviceType)
                .AddService(serviceType, implementationType, lifetime);
        public static IServiceCollection AddOrOverrideService<TService, TImplementation>(
            this IServiceCollection services,
            Func<IServiceProvider, TImplementation> implementationFactory,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
            => services
                .RemoveService(typeof(TService))
                .AddService(implementationFactory, lifetime);
        public static IServiceCollection AddOrOverrideService<TService>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
           where TService : class
            => services
                .RemoveService(typeof(TService))
                .AddService<TService>(lifetime);
        public static IServiceCollection AddOrOverrideService(this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services
                .RemoveService(serviceType)
                .AddService(serviceType, lifetime);
        public static IServiceCollection AddOrOverrideService<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
            ServiceLifetime lifetime)
           where TService : class
            => services
                .RemoveService(typeof(TService))
                .AddService(implementationFactory, lifetime);
    }
}
