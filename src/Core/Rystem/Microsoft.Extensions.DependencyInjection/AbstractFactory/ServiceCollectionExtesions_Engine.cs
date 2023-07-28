using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static void SendInError<TService, TImplementation>(string name)
        {
            throw new ArgumentException($"Service {typeof(TImplementation).Name} with name: {name} for your factory {typeof(TService).Name} already exists.");
        }
        private static void InformThatItsAlreadyInstalled(ref bool check)
        {
            check = false;
        }
        private static IServiceCollection AddFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            Action? whenExists,
            Func<IServiceProvider, TService, TService>? addingBehaviorToFactory)
            where TService : class
            where TImplementation : class, TService
        {
            name ??= string.Empty;
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            var count = serviceType == implementationType ?
                services.Count(x => x.ImplementationType == serviceType)
                : services.Count(x => x.ImplementationType == implementationType);
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
                if (implementationFactory != null)
                {
                    services.AddService(serviceProvider => (TImplementation)implementationFactory.Invoke(serviceProvider), lifetime);
                }
                else if (implementationInstance != null)
                {
                    services.AddSingleton(implementationInstance);
                }
                else
                {
                    services.AddService<TImplementation>(lifetime);
                }
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
