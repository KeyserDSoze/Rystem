namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
           TImplementation implementationInstance,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
            => services.AddEngineFactory<TService, TImplementation>(name, lifetime, implementationInstance, null, () => SendInError<TService, TImplementation>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
           TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsToBuild<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsToBuildAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TImplementation>(name ?? string.Empty));

    }
}
