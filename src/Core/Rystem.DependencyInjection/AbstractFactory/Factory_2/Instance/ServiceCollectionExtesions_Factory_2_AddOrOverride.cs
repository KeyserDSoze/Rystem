namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool AddOrOverrideFactory<TService, TImplementation>(this IServiceCollection services,
           TImplementation implementationInstance,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService
        {
            var check = true;
            services.AddEngineFactory<TService, TImplementation>(name, true, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
        public static bool AddOrOverrideFactory<TService, TImplementation, TOptions>(this IServiceCollection services,
            TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
        {
            var check = true;
            services.AddFactory<TService, TImplementation, TOptions>(createOptions, name, true, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
        public static bool AddOrOverrideFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            services.AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }

        public static async Task<bool> AddOrOverrideFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            TImplementation implementationInstance,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class
            where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, implementationInstance, null, () => InformThatItsAlreadyInstalled(ref check), true, false);
            return check;
        }
    }
}
