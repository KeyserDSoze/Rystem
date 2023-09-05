namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions>(
            this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                bool canOverrideConfiguration,
                ServiceLifetime lifetime,
                TImplementation? implementationInstance,
                Func<IServiceProvider, TService>? implementationFactory,
                Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceForFactoryWithOptions<TOptions>
                where TOptions : class, new()
        {
            var options = new TOptions();
            createOptions.Invoke(options);
            services.AddEngineFactory(
                name,
                canOverrideConfiguration,
                lifetime,
                implementationInstance,
                implementationFactory,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, serviceProvider => options)
                );
            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            Action? whenExists)
               where TService : class
               where TImplementation : class, TService, IServiceForFactoryWithOptions<TBuiltOptions>
               where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
               where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            services.AddFactory(builtOptions, name, ServiceLifetime.Singleton);
            services.AddEngineFactory(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions)
                );
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceForFactoryWithOptions<TBuiltOptions>
                where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
                where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            services.AddFactory(builtOptions, name, ServiceLifetime.Singleton);
            services.AddEngineFactory(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions)
                );
            return services;
        }
    }
}
