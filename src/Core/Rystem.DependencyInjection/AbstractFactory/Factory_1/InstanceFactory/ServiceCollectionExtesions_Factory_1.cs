namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection services,
           Func<IServiceProvider, object?, TService> implementationFactory,
           AnyOf<string, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
            => services.AddEngineFactory<TService, TService>(name, true, lifetime, null, implementationFactory, () => services.SendInError<TService, TService>(name), true, true);

        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TService, TOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => services.SendInError<TService, TService>(name), true, true);

        public static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => services.SendInError<TService, TService>(name), true, true);
        public static Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => services.SendInError<TService, TService>(name), true, true);
    }
}
