namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection services,
           TService implementationInstance,
           AnyOf<string, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
        {
            var nameAsString = name?.AsString();
            return services.AddEngineFactory<TService, TService>(nameAsString, true, lifetime, implementationInstance, null, () => services.SendInError<TService, TService>(nameAsString ?? string.Empty), true, true);
        }
        public static IServiceCollection AddFactory<TService, TOptions>(this IServiceCollection services,
           TService implementationInstance,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
        {
            var nameAsString = name?.AsString();
            return services.AddFactory<TService, TService, TOptions>(createOptions, nameAsString, true, lifetime, implementationInstance, null, () => services.SendInError<TService, TService>(nameAsString ?? string.Empty), true, true);
        }

        public static IServiceCollection AddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var nameAsString = name?.AsString();
            return services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, nameAsString, true, lifetime, implementationInstance, null, () => services.SendInError<TService, TService>(nameAsString ?? string.Empty), true, true);
        }

        public static Task<IServiceCollection> AddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var nameAsString = name?.AsString();
            return services.AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, nameAsString, true, lifetime, implementationInstance, null, () => services.SendInError<TService, TService>(nameAsString ?? string.Empty), true, true);
        }
    }
}
