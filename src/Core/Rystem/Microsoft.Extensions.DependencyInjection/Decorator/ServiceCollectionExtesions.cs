namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection AddDecoration<TService, TImplementation>(
            this IServiceCollection services,
           string? name = null,
           ServiceLifetime lifetime = ServiceLifetime.Transient)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
        {
            name ??= string.Empty;
            services.TryAddService<TImplementation>(lifetime);
            var currentService = services.FirstOrDefault(x => x.ServiceType == typeof(TService));
            if (currentService != null)
            {
                var implementationType = currentService.ImplementationType;
                if (currentService.ImplementationFactory != null)
                {
                    implementationType ??= typeof(TService);
                    Try.WithDefaultOnCatch(() =>
                    {
                        var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
                        var returnedService = currentService.ImplementationFactory.Invoke(serviceProvider);
                        if (returnedService != null)
                            implementationType = returnedService.GetType();
                    });
                    services.TryAddService(implementationType, currentService.ImplementationFactory, currentService.Lifetime);
                }
                else if (currentService.ImplementationType != null)
                    services.TryAddService(typeof(TService), currentService.ImplementationType, currentService.Lifetime);
                else if (currentService.ImplementationInstance != null)
                {
                    services.TryAddService(currentService.ImplementationInstance, lifetime);
                    implementationType = currentService.ImplementationInstance.GetType();
                }

                if (Factory<TService>.Map.TryGetValue(name, out var factory))
                {
                    factory.DecoratorType = typeof(TImplementation);
                    implementationType = factory.ImplementationType;
                }
                if (implementationType != null)
                {
                    services.AddOrOverrideService(serviceProvider =>
                    {
                        var service = (TService)serviceProvider.GetRequiredService<TImplementation>();
                        if (service is IDecoratorService<TService> decorator)
                        {
                            decorator.DecoratedService = (TService)serviceProvider.GetRequiredService(implementationType);
                        }
                        return service;
                    }, lifetime);

                    services.TryAddService(
                        typeof(IDecoratedService<>).MakeGenericType(typeof(TService)),
                        typeof(DecoratedService<,>).MakeGenericType(typeof(TService), implementationType),
                    ServiceLifetime.Transient);
                    return services;
                }
                else
                    throw new ArgumentException($"Service {typeof(TService).Name} is not possible to decorate. Use decoration on service with only implementationType or implementationFactory.");
            }
            throw new ArgumentException($"Service {typeof(TService).Name} is not possible to decorate. Use decoration after a correct setup of service.");
        }
    }
}
