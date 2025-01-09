namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        internal static string GetDecoratorName<TService>(this string? name, int index)
        {
            return $"Rystem.Decorator.{index}.{typeof(TService).FullName}.{name ?? string.Empty}";
        }
        private static IServiceCollection AddDecorationEngine<TService>(
           this IServiceCollection services,
            TService? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            AnyOf<string?, Enum>? name,
            ServiceLifetime lifetime)
            where TService : class, IDecoratorService<TService>
            => services.AddDecorationEngine<TService, TService>(implementationInstance, implementationFactory, name, lifetime);
        private static IServiceCollection AddDecorationEngine<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            AnyOf<string?, Enum>? name,
            ServiceLifetime lifetime)
           where TService : class
           where TImplementation : class, TService, IDecoratorService<TService>
        {
            var nameAsString = name.AsString();
            var factoryName = nameAsString.GetFactoryName<TService>();
            var descriptor = services.GetDescriptor<TService>(factoryName);
            //todo: controllare il primo factory se non c'è va installato come factory dal service descriptor che è null
            if (descriptor == null)
            {
                descriptor = services.GetDescriptor<TService>(null);
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                    services.AddEngineFactoryWithoutGenerics(typeof(TService),
                        descriptor.ImplementationType!,
                            name, true, descriptor.Lifetime,
                            descriptor.ImplementationInstance,
                            (descriptor.ImplementationFactory != null ?
                                (serviceProvider, key) => descriptor.ImplementationFactory(serviceProvider) : null),
                        null, true, false);
                }
            }

            if (descriptor != null)
            {
                var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
                map.DecorationCount[factoryName]++;
                var check = true;
                var decoratorName = nameAsString.GetDecoratorName<TService>(map.DecorationCount[factoryName]);
                services
                    .AddEngineFactory<TService, TImplementation>(
                        decoratorName,
                        true,
                        lifetime,
                        implementationInstance,
                        implementationFactory,
                        () => InformThatItsAlreadyInstalled(ref check),
                        false,
                        false);
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
