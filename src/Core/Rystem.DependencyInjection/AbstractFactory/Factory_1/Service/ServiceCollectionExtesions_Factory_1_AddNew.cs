namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNewFactory<TService>(this IServiceCollection services,
           AnyOf<string?, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
            => services.AddEngineFactory<TService, TService>(name, false, lifetime, null, null, () => services.SendInError<TService, TService>(name), true, false);
        public static IServiceCollection AddNewFactory<TService, TOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
            => services.AddFactory<TService, TService, TOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TService>(name), true, false);
        public static IServiceCollection AddNewFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TService>(name), true, false);
        public static Task<IServiceCollection> AddNewFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string?, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
            => services.AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, null, null, () => services.SendInError<TService, TService>(name), true, false);
    }
}
