namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFactory(this IServiceCollection services,
            Type serviceType,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            services.AddEngineFactoryWithoutGenerics(serviceType, serviceType, name, true, lifetime, null, null, null, false, true);
            return services;
        }
        public static IServiceCollection AddFactory(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            services.AddEngineFactoryWithoutGenerics(serviceType, implementationType, name, true, lifetime, null, null, null, false, true);
            return services;
        }
        public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
           AnyOf<string, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddEngineFactory<TService, TImplementation>(name, true, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, true);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, true);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, true);
        public static Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, true);

    }
}
