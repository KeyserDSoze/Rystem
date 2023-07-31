namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection services,
           TService implementationInstance,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
            => services.AddEngineFactory<TService, TService>(name, lifetime, implementationInstance, null, () => SendInError<TService, TService>(name ?? string.Empty), null);
        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
           TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TOptions>
            where TOptions : class, new()
            => services.AddFactory<TService, TService, TOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TService>(name ?? string.Empty));
        public static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsToBuild<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TService>(name ?? string.Empty));
        public static Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsToBuildAsync<TBuiltOptions>, new()
            where TBuiltOptions : class
            => services.AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, lifetime, implementationInstance, null, () => SendInError<TService, TService>(name ?? string.Empty));

    }
}
