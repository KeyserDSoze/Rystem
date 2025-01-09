namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool TryAddFactory<TService>(this IServiceCollection services,
           TService implementationInstance,
           AnyOf<string, Enum>? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
        {
            var check = true;
            var nameAsString = name?.AsString();
            services.AddEngineFactory<TService, TService>(nameAsString, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
        public static bool TryAddFactory<TService, TOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
        {
            var check = true;
            var nameAsString = name?.AsString();
            services.AddFactory<TService, TService, TOptions>(createOptions, nameAsString, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
        public static bool TryAddFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            var nameAsString = name?.AsString();
            services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, nameAsString, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }

        public static async Task<bool> TryAddFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            TService implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, false, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
    }
}
