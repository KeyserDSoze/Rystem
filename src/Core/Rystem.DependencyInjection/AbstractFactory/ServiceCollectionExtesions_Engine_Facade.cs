namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions>(
            this IServiceCollection services,
                Action<TOptions> createOptions,
                AnyOf<string, Enum>? name,
                bool canOverrideConfiguration,
                ServiceLifetime lifetime,
                TImplementation? implementationInstance,
                Func<IServiceProvider, object?, TService>? implementationFactory,
                Action? whenExists,
                bool fromDecoration,
                bool doNotRemoveExisting)
                where TService : class
                where TImplementation : class, TService, IServiceWithFactoryWithOptions<TOptions>
                where TOptions : class, IFactoryOptions, new()
        {
            var options = new TOptions();
            createOptions.Invoke(options);
            var nameAsString = name.AsString();
            var optionsName = nameAsString.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions, TOptions>((serviceProvider, name) =>
            {
                return options;
            }, optionsName, ServiceLifetime.Singleton);
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            map.OptionsSetter.TryAdd(optionsName, (service, opt) =>
            {
                if (opt is TOptions optionsForService && service is IServiceWithFactoryWithOptions<TOptions> serviceWithOptions)
                {
                    serviceWithOptions.SetOptions(optionsForService);
                }
            });
            services.AddEngineFactory<TService, TImplementation>(
                name,
                canOverrideConfiguration,
                lifetime,
                implementationInstance,
                implementationFactory != null ? (serviceProvider, name) => implementationFactory(serviceProvider, name) : null,
                whenExists,
                fromDecoration,
                doNotRemoveExisting);
            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            Action? whenExists,
            bool fromDecoration,
            bool doNotRemoveExisting)
               where TService : class
               where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
               where TOptions : class, IOptionsBuilder<TBuiltOptions>, new()
               where TBuiltOptions : class, IFactoryOptions
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            var nameAsString = name.AsString();
            var optionsName = nameAsString.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions, TBuiltOptions>((serviceProvider, name) =>
            {
                return builtOptions.Invoke(serviceProvider);
            }, optionsName, ServiceLifetime.Transient);
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            map.OptionsSetter.TryAdd(optionsName, (service, opt) =>
            {
                if (opt is TBuiltOptions optionsForService && service is IServiceWithFactoryWithOptions<TBuiltOptions> serviceWithOptions)
                {
                    serviceWithOptions.SetOptions(optionsForService);
                }
            });
            services.AddEngineFactory<TService, TImplementation>(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory != null ? (serviceProvider, name) => implementationFactory(serviceProvider, name) : null,
                whenExists,
                fromDecoration,
                doNotRemoveExisting);
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            AnyOf<string, Enum>? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            Action? whenExists,
            bool fromDecoration,
            bool doNotRemoveExisting)
                where TService : class
                where TImplementation : class, TService, IServiceWithFactoryWithOptions<TBuiltOptions>
                where TOptions : class, IOptionsBuilderAsync<TBuiltOptions>, new()
                where TBuiltOptions : class, IFactoryOptions
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            var nameAsString = name.AsString();
            var optionsName = nameAsString.GetOptionsName<TService>();
            services.AddOrOverrideFactory<IFactoryOptions, TBuiltOptions>((serviceProvider, name) =>
            {
                return builtOptions.Invoke(serviceProvider);
            }, optionsName, ServiceLifetime.Transient);
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            map.OptionsSetter.TryAdd(optionsName, (service, opt) =>
            {
                if (opt is TBuiltOptions optionsForService && service is IServiceWithFactoryWithOptions<TBuiltOptions> serviceWithOptions)
                {
                    serviceWithOptions.SetOptions(optionsForService);
                }
            });
            services.AddEngineFactory<TService, TImplementation>(name, canOverrideConfiguration, lifetime,
                implementationInstance,
                implementationFactory != null ? (serviceProvider, name) => implementationFactory(serviceProvider, name) : null,
                whenExists,
                fromDecoration,
                doNotRemoveExisting);
            return services;
        }
    }
}
