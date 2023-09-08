namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        internal static string GetDecoratedName<TService>(this string? name)
        {
            return $"Rystem.Decorated.{name ?? string.Empty}";
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
            var descriptor = services.GetDescriptor<TService>(factoryName);
            if (descriptor != null)
            {
                services.Remove(descriptor);
                descriptor = services
                    .GetServiceDescriptor<TService, TImplementation>(
                        decoratedName, descriptor.Lifetime, descriptor.KeyedImplementationInstance, descriptor.KeyedImplementationFactory);
                services.Add(descriptor);
                var check = true;
                services
                    .AddEngineFactory(
                        name,
                        true,
                        lifetime,
                        implementationInstance,
                        implementationFactory,
                        () => InformThatItsAlreadyInstalled(ref check));
                services.AddOrOverrideService<IDecoratedService<TService>, DecoratedService<TService>>(
                    serviceProvider =>
                {
                    var factory = serviceProvider.GetRequiredService<IFactory<TService>>();
                    return new DecoratedService<TService>(factory.Create(decoratedName));
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
