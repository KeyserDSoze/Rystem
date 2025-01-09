namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNewFactory(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            services.AddEngineFactoryWithoutGenerics(serviceType, implementationType, name, false, lifetime, null, null, null, false, false);
            return services;
        }
        public static IServiceCollection AddNewFactory<TService, TImplementation>(this IServiceCollection services,
           AnyOf<string?, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddEngineFactory<TService, TImplementation>(name, false, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, false);
        public static IServiceCollection AddNewFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, false);
        public static IServiceCollection AddNewFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, false);
        public static Task<IServiceCollection> AddNewFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TImplementation>(name), true, false);

    }
}
