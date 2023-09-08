namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions>(
            this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                bool canOverrideConfiguration,
                ServiceLifetime lifetime,
                TImplementation? implementationInstance,
                Func<IServiceProvider, object?, TService>? implementationFactory,
                Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
                where TOptions : class, IFactoryOptions, new()
        {
            var options = new TOptions();
            createOptions.Invoke(options);
            var optionsName = name.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions>((serviceProvider, name) =>
            {
                return options;
            }, optionsName, ServiceLifetime.Singleton);
            services.AddEngineFactory(
                name,
                canOverrideConfiguration,
                lifetime,
                implementationInstance,
                implementationFactory,
                whenExists);
            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            Action? whenExists)
               where TService : class
               where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
               where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
               where TBuiltOptions : class, IFactoryOptions
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            var optionsName = name.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions>((serviceProvider, name) =>
            {
                return builtOptions.Invoke(serviceProvider);
            }, optionsName, ServiceLifetime.Transient);
            services.AddEngineFactory(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory,
                whenExists);
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
                where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
                where TBuiltOptions : class, IFactoryOptions
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            var optionsName = name.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions>((serviceProvider, name) =>
            {
                return builtOptions.Invoke(serviceProvider);
            }, optionsName, ServiceLifetime.Transient);
            services.AddEngineFactory(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory,
                whenExists
                );
            return services;
        }
    }
}
