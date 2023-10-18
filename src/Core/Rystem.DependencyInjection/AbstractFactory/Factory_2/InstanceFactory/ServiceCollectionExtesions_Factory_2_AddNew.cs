namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNewFactory<TService, TImplementation>(this IServiceCollection services,
           Func<IServiceProvider, object?, TService> implementationFactory,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddEngineFactory<TService, TImplementation>(name, false, lifetime, null, implementationFactory, () => services.SendInError<TService, TImplementation>(name ?? string.Empty), true, false);
        public static IServiceCollection AddNewFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, false, lifetime, null, implementationFactory, () => services.SendInError<TService, TImplementation>(name ?? string.Empty), true, false);
        public static IServiceCollection AddNewFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, implementationFactory, () => services.SendInError<TService, TImplementation>(name ?? string.Empty), true, false);
        public static Task<IServiceCollection> AddNewFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, implementationFactory, () => services.SendInError<TService, TImplementation>(name ?? string.Empty), true, false);

    }
}
