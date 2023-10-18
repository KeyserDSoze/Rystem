namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
            => services.AddEngineFactory<TService, TService>(name, true, lifetime, null, null, () => services.SendInError<TService, TService>(name ?? string.Empty), true, true);
        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TService, TOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TService>(name ?? string.Empty), true, true);
        public static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TService>(name ?? string.Empty), true, true);
        public static Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, null, () => services.SendInError<TService, TService>(name ?? string.Empty), true, true);
    }
}
