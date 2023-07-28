namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
           Func<IServiceProvider, TService> implementationFactory,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddFactory<TService, TImplementation>(name, lifetime, null, implementationFactory, () => SendInError<TService, TImplementation>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, lifetime, null, implementationFactory, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptions<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, null, implementationFactory, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, null, implementationFactory, () => SendInError<TService, TImplementation>(name ?? string.Empty));

    }
}
