namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool AddOrOverrideFactory<TService>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
        {
            var check = true;
            services.AddEngineFactory<TService, TService>(name, true, lifetime, null, implementationFactory, () => InformThatItsAlreadyInstalled(ref check), true);
            return check;
        }
        public static bool AddOrOverrideFactory<TService, TOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TOptions>
            where TOptions : class, IFactoryOptions, new()
        {
            var check = true;
            services.AddFactory<TService, TService, TOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => InformThatItsAlreadyInstalled(ref check), true);
            return check;
        }
        public static bool AddOrOverrideFactory<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            services.AddFactory<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => InformThatItsAlreadyInstalled(ref check), true);
            return check;
        }

        public static async Task<bool> AddOrOverrideFactoryAsync<TService, TOptions, TBuiltOptions>(this IServiceCollection services,
            Func<IServiceProvider, object?, TService> implementationFactory,
            Action<TOptions> createOptions,
            string? name = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TService : class, IServiceWithFactoryWithOptions<TBuiltOptions>
            where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
            where TBuiltOptions : class, IFactoryOptions
        {
            var check = true;
            await services
                .AddFactoryAsync<TService, TService, TOptions, TBuiltOptions>(createOptions, name, true, lifetime, null, implementationFactory, () => InformThatItsAlreadyInstalled(ref check), true);
            return check;
        }
    }
}
