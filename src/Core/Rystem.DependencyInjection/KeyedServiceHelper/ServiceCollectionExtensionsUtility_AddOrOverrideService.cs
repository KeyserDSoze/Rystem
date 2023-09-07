namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOrOverrideKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            ServiceLifetime lifetime)
           where TService : class
           where TImplementation : class, TService
            => services
                .RemoveService(typeof(TService))
                .AddKeyedService<TService, TImplementation>(serviceKey, lifetime);
        public static IServiceCollection AddOrOverrideKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType,
            ServiceLifetime lifetime)
            => services
                .RemoveKeyedService(serviceType, serviceKey)
                .AddKeyedService(serviceType, serviceKey, implementationType, lifetime);
        public static IServiceCollection AddOrOverrideKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TImplementation> implementationFactory,
            ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
            => services
                .RemoveKeyedService(typeof(TService), serviceKey)
                .AddKeyedService(serviceKey, implementationFactory, lifetime);
        public static IServiceCollection AddOrOverrideKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
            ServiceLifetime lifetime)
           where TService : class
            => services
                .RemoveKeyedService(typeof(TService), serviceKey)
                .AddKeyedService<TService>(serviceKey, lifetime);
        public static IServiceCollection AddOrOverrideKeyedService(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            ServiceLifetime lifetime)
            => services
                .RemoveKeyedService(serviceType, serviceKey)
                .AddKeyedService(serviceType, serviceKey, lifetime);
        public static IServiceCollection AddOrOverrideKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
            Func<IServiceProvider, object?, TService> implementationFactory,
            ServiceLifetime lifetime)
           where TService : class
            => services
                .RemoveKeyedService(typeof(TService), serviceKey)
                .AddKeyedService(serviceKey, implementationFactory, lifetime);
        public static IServiceCollection AddOrOverrideKeyedSingleton<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            TImplementation implementation)
           where TService : class
            where TImplementation : class, TService
            => services
                .RemoveKeyedService(typeof(TService), serviceKey)
                .AddKeyedSingleton<TService>(serviceKey, implementation);
        public static IServiceCollection AddOrOverrideKeyedSingleton<TService>(
            this IServiceCollection services,
            object? serviceKey,
            TService implementation)
           where TService : class
            => services
                .RemoveKeyedService(typeof(TService), serviceKey)
                .AddKeyedSingleton(serviceKey, implementation);
    }
}
