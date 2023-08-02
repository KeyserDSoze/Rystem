using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static void SendInError<TService, TImplementation>(this IServiceCollection services, string name)
        {
            name = name.GetIntegrationName<TService>();
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            var descriptor = map.Services[name].Descriptor;
            throw new ArgumentException($"Service {typeof(TImplementation).FullName} with name: '{name.Replace($"{typeof(TService).FullName}_", string.Empty)}' for your factory {typeof(TService).FullName} already exists in the form of {descriptor.ImplementationType?.FullName ?? descriptor.ImplementationInstance?.GetType().FullName ?? descriptor.ServiceType.FullName}");
        }
        private static void InformThatItsAlreadyInstalled(ref bool check)
        {
            check = false;
        }
        internal static string GetIntegrationName<TService>(this string? name)
        {
            //var fullName = typeof(TService).FullName!;
            //if (name?.StartsWith(fullName) == true)
            //    return name;
            //return $"{fullName}_{name}";
            return name ?? string.Empty;
        }

        private static IServiceCollection AddEngineFactoryWithoutGenerics(this IServiceCollection services,
            Type serviceType,
            Type implementationType,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object>? implementationFactory,
            Action? whenExists,
            Func<IServiceProvider, object, object>? addingBehaviorToFactory
            )
        {
            return Generics
                .WithStatic(typeof(ServiceCollectionExtesions), nameof(ServiceCollectionExtesions.AddEngineFactory), serviceType, implementationType)
                .Invoke(services, name!, canOverrideConfiguration, lifetime, implementationInstance!, implementationFactory!, whenExists!, addingBehaviorToFactory!);
        }
        private static IServiceCollection AddEngineFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            Action? whenExists,
            Func<IServiceProvider, TService, TService>? addingBehaviorToFactory)
            where TService : class
            where TImplementation : class, TService
        {
            name = name.GetIntegrationName<TService>();
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            if (map.Services.TryGetValue(name, out var value) && canOverrideConfiguration)
            {
                var descriptor = value.Descriptor;
                var toRemove = services.Where(x => x.ServiceType == descriptor.ServiceType).ToList();
                if (descriptor.ImplementationType != null)
                    toRemove.AddRange(services.Where(x => x.ImplementationType == descriptor.ImplementationType));
                foreach (var service in toRemove)
                    services.Remove(service);
            }
            if (!map.Services.ContainsKey(name) || canOverrideConfiguration)
            {
                ServiceDescriptor? serviceDescriptor = null;
                if (implementationFactory != null)
                {
                    serviceDescriptor = new ServiceDescriptor(serviceType, implementationFactory, lifetime);
                }
                else if (implementationInstance != null)
                {
                    serviceDescriptor = new ServiceDescriptor(serviceType, implementationInstance);
                }
                else
                {
                    var suffix = $"{serviceType.Name}{typeof(TImplementation).Name}{name}{Guid.NewGuid():N}";
                    var (@interface, implementation) = services
                        .AddProxy(
                            serviceType,
                            implementationType,
                            $"RystemProxyService{suffix}Interface",
                            $"RystemProxyService{suffix}Concretization",
                            lifetime);
                    serviceDescriptor = new ServiceDescriptor(@interface, implementation, lifetime);
                }
                if (!map.Services.ContainsKey(name))
                    map.Services.TryAdd(name, new()
                    {
                        ServiceFactory = ServiceFactory,
                        Descriptor = serviceDescriptor,
                    });
                else
                {
                    map.Services[name].ServiceFactory = ServiceFactory;
                    map.Services[name].Descriptor = serviceDescriptor;
                }
                services.AddOrOverrideService(serviceProvider => ServiceFactory(serviceProvider, true), lifetime);
            }
            else
                whenExists?.Invoke();

            TService ServiceFactory(IServiceProvider serviceProvider, bool withDecoration)
            {
                var factory = map.Services[name];
                var service = GetService(factory.Descriptor, null);

                if (withDecoration && factory.Decorators != null)
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

                TService GetService(ServiceDescriptor serviceDescriptor, Action<TService>? afterCreation)
                {
                    var service = GetServiceFromServiceProvider();
                    TService GetServiceFromServiceProvider()
                    {
                        if (serviceDescriptor.ImplementationInstance != null)
                            return (TService)serviceDescriptor.ImplementationInstance;
                        else if (serviceDescriptor.ImplementationFactory != null)
                            return (TService)serviceDescriptor.ImplementationFactory(serviceProvider);
                        else
                        {
                            var proxyService = serviceProvider.GetRequiredService(serviceDescriptor.ServiceType);
                            if (proxyService is ProxyService<TService> proxyServiceProxy)
                                return proxyServiceProxy.Proxy;
                        }
                        return default!;
                    }

                    addingBehaviorToFactory?.Invoke(serviceProvider, service);
                    afterCreation?.Invoke(service);
                    if (service is IServiceForFactory factoryService)
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
            if (service is IServiceForFactoryWithOptions<TOptions> serviceWithOptions)
            {
                serviceWithOptions.SetOptions(optionsCreator?.Invoke(serviceProvider)!);
            }
            else if (service is IServiceForFactoryWithOptions serviceWithCustomOptions)
            {
                var dynamicServiceWithCustomOptions = (dynamic)serviceWithCustomOptions;
                dynamicServiceWithCustomOptions
                    .SetOptions(optionsCreator?.Invoke(serviceProvider)!);
            }
            return service;
        }
    }
}
