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
                if (currentService.ImplementationFactory != null)
                    services.TryAddService(typeof(TService), currentService.ImplementationFactory, currentService.Lifetime);
                else if (currentService.ImplementationType != null)
                    services.TryAddService(typeof(TService), currentService.ImplementationType, currentService.Lifetime);

                var implementationType = currentService.ImplementationType;
                if (Factory<TService>.Map.TryGetValue(name, out var factory))
                {
                    factory.DecoratorType = typeof(TImplementation);
                    implementationType = factory.ImplementationType;
                }
                else if (currentService.ImplementationType != null)
                {
                    var decoratedImplementation = currentService.ImplementationType!;
                    var decoratedLifetime = currentService.Lifetime;
                    services.TryAddService(decoratedImplementation, decoratedLifetime);
                    services.AddOrOverrideService(serviceProvider =>
                    {
                        var service = (TService)serviceProvider.GetRequiredService<TImplementation>();
                        if (service is IDecoratorService<TService> decorator)
                        {
                            decorator.DecoratedService = (TService)serviceProvider.GetRequiredService(decoratedImplementation);
                        }
                        return service;
                    }, lifetime);
                }
                if (implementationType != null)
                {
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
