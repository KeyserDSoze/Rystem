namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        internal static string GetDecoratedName<TService>(this string? name)
        {
            return $"Rystem.Decorated.{typeof(TService).FullName}.{name ?? string.Empty}";
        }
        private static IServiceCollection AddDecorationEngine<TService>(
           this IServiceCollection services,
            TService? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            string? name,
            ServiceLifetime lifetime)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TService>(implementationInstance, implementationFactory, name, lifetime);
        private static IServiceCollection AddDecorationEngine<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            string? name,
            ServiceLifetime lifetime)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
        {
            var factoryName = name.GetFactoryName<TService>();
            var decoratedName = name.GetDecoratedName<TService>();
            var descriptor = services.GetDescriptor<TService>(factoryName) ?? services.GetDescriptor<TService>(null);
            if (descriptor != null)
            {
                var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
                services.Remove(descriptor);
                map.Services.Remove(factoryName);
                services.AddEngineFactoryWithoutGenerics(typeof(TService),
                    descriptor.ImplementationType!,
                        decoratedName, true, descriptor.Lifetime,
                        (descriptor as KeyedServiceDescriptor)?.KeyedImplementationInstance ?? descriptor.ImplementationInstance,
                        (descriptor as KeyedServiceDescriptor)?.KeyedImplementationFactory ??
                            (descriptor.ImplementationFactory != null ?
                                (serviceProvider, key) => descriptor.ImplementationFactory(serviceProvider) : null),
                        null);
                var check = true;
                services
                    .AddEngineFactory<TService, TImplementation>(
                        name,
                        true,
                        lifetime,
                        implementationInstance,
                        implementationFactory,
                        () => InformThatItsAlreadyInstalled(ref check));
                services.AddOrOverrideService<IDecoratedService<TService>>(
                    serviceProvider =>
                {
                    var factory = serviceProvider.GetRequiredService<IFactory<TService>>();
                    return new DecoratedService<TService>(factory.CreateWithoutDecoration(name));
                }, lifetime);
                if (!check)
                    throw new ArgumentException($"Decorator name '{name}' is not installed correctly for service {typeof(TService).FullName}.");
            }
            else
                throw new ArgumentException($"Factory name '{name}' not found for service {typeof(TService).FullName}.");
            return services;
        }
    }
}
