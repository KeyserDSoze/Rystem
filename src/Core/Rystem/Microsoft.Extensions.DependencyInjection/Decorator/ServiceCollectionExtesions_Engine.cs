namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        private static IServiceCollection AddDecorationEngine<TService>(
           this IServiceCollection services,
            TService? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            string? name,
            ServiceLifetime lifetime)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TService>(implementationInstance, implementationFactory, name, lifetime);
        private static IServiceCollection AddDecorationEngine<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation? implementationInstance,
            Func<IServiceProvider, TService>? implementationFactory,
            string? name,
            ServiceLifetime lifetime)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
        {
            name = name.GetIntegrationName<TService>();
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);
            var map = services.TryAddSingletonAndGetService<FactoryServices<TService>>();
            if (!map.Services.ContainsKey(name))
            {
                var currentService = services.FirstOrDefault(x => x.ServiceType == serviceType)
                    ?? throw new ArgumentException($"It's not possible to override a service not installed. There isn't another instance of {typeof(TService).Name} in your Service collection to decorate with {typeof(TImplementation).Name}.");
                services
                    .AddEngineFactoryWithoutGenerics(
                        serviceType,
                        currentService.ImplementationType ?? currentService.ServiceType,
                        name,
                        false,
                        lifetime,
                        currentService.ImplementationInstance,
                        currentService.ImplementationFactory,
                        null,
                        null);
            }

            if (map.Services.TryGetValue(name, out var factory))
            {
                factory.Decorators ??= new();
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
                    var decoratorName = $"{serviceType.Name}{implementationType.Name}{name}{Guid.NewGuid():N}Count{factory.Decorators.Count}";
                    var (@interface, implementation) = services
                       .AddProxy(
                            serviceType,
                            implementationType,
                           $"RystemProxyService{decoratorName}DecoratorInterface",
                           $"RystemProxyService{decoratorName}DecoratorConcretization",
                           lifetime);
                    serviceDescriptor = new ServiceDescriptor(@interface, implementation, lifetime);
                }
                factory.Decorators.Add(serviceDescriptor);
            }
            else
                throw new ArgumentException($"Something goes wrong during setup of your decoration. It's not possible to setup Factory integration for your decoration {typeof(TImplementation).Name} for Service {typeof(TService).Name}");

            services
                .AddOrOverrideService<IDecoratedService<TService>>(serviceProvider =>
                {
                    var factory = serviceProvider.GetRequiredService<IFactory<TService>>();
                    var service = factory.CreateWithoutDecoration(name);
                    if (service is IServiceForFactory factoryService)
                        factoryService.SetFactoryName(name ?? string.Empty);
                    return new DecoratedService<TService>(service);
                }, lifetime);

            return services;
        }
    }
}
