namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddEngineFactory<TService, TImplementation>(name, false, lifetime, null, null, () => SendInError<TService, TImplementation>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceForFactoryWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, false, lifetime, null, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceForFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceForFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));

    }
}
