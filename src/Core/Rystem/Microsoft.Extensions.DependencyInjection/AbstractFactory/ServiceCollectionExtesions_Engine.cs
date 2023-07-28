using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists,
            Func<IServiceProvider, TService, TService>? addingBehaviorToFactory)
            where TService : class
            where TImplementation : class, TService
        {
            name ??= string.Empty;
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            var count = services.Count(x => x.ImplementationType == typeof(TImplementation));
            if (map.Services.TryAdd(name, new()
            {
                ServiceFactory = ServiceFactory,
                Implementation = new()
                {
                    Type = typeof(TImplementation),
                    Index = count
                }
            }))
            {
                services.AddService<TImplementation>(lifetime);
                services.AddOrOverrideService(serviceProvider => ServiceFactory(serviceProvider, false), lifetime);
            }
            else
                whenExists?.Invoke();

            TService ServiceFactory(IServiceProvider serviceProvider, bool withoutDecoration)
            {
                var factory = map.Services[name];
                var service = GetService(factory.Implementation, null);

                if (!withoutDecoration && factory.Decorators != null)
                {
                    foreach (var decoratorType in factory.Decorators)
                    {
                        service = GetService(decoratorType,
                            decorator =>
                            {
                                if (decorator is IDecoratorService<TService> decorateWithService)
                                {
                                    decorateWithService.SetDecoratedService(service);
                                }
                            });
                    }
                }
                return service;

                TService GetService(FactoryServiceType implementationType, Action<TService>? afterCreation)
                {
                    var service = (TService)serviceProvider
                         .GetServices(implementationType.Type)
                         .Skip(implementationType.Index)
                         .First()!;
                    addingBehaviorToFactory?.Invoke(serviceProvider, service);
                    afterCreation?.Invoke(service);
                    if (service is IFactoryService factoryService)
                        factoryService.SetFactoryName(name);
                    foreach (var behavior in factory.FurtherBehaviors.Select(x => x.Value))
                        service = behavior.Invoke(serviceProvider, service);
                    return service;
                }
            }

            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions>(
            this IServiceCollection services,
                Action<TOptions> createOptions,
                string? name,
                ServiceLifetime lifetime,
                Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithOptions<TOptions>
                where TOptions : class, new()
        {
            var options = new TOptions();
            createOptions.Invoke(options);
            services.TryAddSingleton(options);
            services.AddFactory<TOptions>(name, ServiceLifetime.Singleton);
            services.AddFactory<TService, TImplementation>(
                name,
                lifetime,
                whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, serviceProvider => options)
                );
            return services;
        }
        private static IServiceCollection AddFactory<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
               where TService : class
               where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
               where TOptions : class, IServiceOptions<TBuiltOptions>, new()
               where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = options.Build();
            services.TryAddTransient(builtOptions);
            services.AddFactory<TService, TImplementation>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions)
                );
            return services;
        }
        private static async Task<IServiceCollection> AddFactoryAsync<TService, TImplementation, TOptions, TBuiltOptions>(this IServiceCollection services,
            Action<TOptions> createOptions,
            string? name,
            ServiceLifetime lifetime,
            Action? whenExists)
                where TService : class
                where TImplementation : class, TService, IServiceWithOptions<TBuiltOptions>
                where TOptions : class, IServiceOptionsAsync<TBuiltOptions>, new()
                where TBuiltOptions : class
        {
            TOptions options = new();
            createOptions.Invoke(options);
            var builtOptions = await options.BuildAsync();
            services.TryAddTransient(builtOptions);
            services.AddFactory<TService, TImplementation>(name, lifetime, whenExists,
                (serviceProvider, service) =>
                    AddOptionsToFactory(serviceProvider, service, builtOptions)
                );
            return services;
        }
        private static TService AddOptionsToFactory<TService, TOptions>(
            IServiceProvider serviceProvider,
            TService service,
            Func<IServiceProvider, TOptions>? optionsCreator)
             where TOptions : class
        {
            if (service is IServiceWithOptions<TOptions> serviceWithOptions)
            {
                serviceWithOptions.SetOptions(optionsCreator?.Invoke(serviceProvider)!);
            }
            else if (service is IServiceWithOptions serviceWithCustomOptions)
            {
                var dynamicServiceWithCustomOptions = (dynamic)serviceWithCustomOptions;
                dynamicServiceWithCustomOptions
                    .SetOptions(optionsCreator?.Invoke(serviceProvider)!);
            }
            return service;
        }
    }
}
