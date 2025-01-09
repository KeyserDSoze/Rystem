using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static void SendInError<TService, TImplementation>(this IServiceCollection services
            AnyOf<string, Enum>? name)
            where TService : class
            where TImplementation : class, TService
        {
            var nameAsString = name?.AsString() ?? string.Empty;
            nameAsString = nameAsString.GetFactoryName<TService>();
            _ = services.HasKeyedService<TService, TImplementation>(name, out var serviceDescriptor);
            throw new ArgumentException($"Service {typeof(TImplementation).FullName} with name: '{nameAsString.Replace($"{typeof(TService).FullName}_", string.Empty)}' for your factory {typeof(TService).FullName} already exists in the form of {serviceDescriptor.ImplementationType?.FullName ?? serviceDescriptor.ImplementationInstance?.GetType().FullName ?? serviceDescriptor.ServiceType.FullName}");
        }
        private static void InformThatItsAlreadyInstalled(ref bool check)
        {
            check = false;
        }
        internal static string GetFactoryName<TService>(this string? name)
        {
            return $"Rystem.Factory.{typeof(TService).FullName}.{name ?? string.Empty}";
        }
        internal static string GetOptionsName<TService>(this string? name)
        {
            return $"Rystem.Factory.Options.{typeof(TService).FullName}.{name ?? string.Empty}";
        }
        private static IServiceCollection AddEngineFactoryWithoutGenerics(this IServiceCollection services,
            Type serviceType,
            Type? implementationType,
            AnyOf<string, Enum>? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory,
            Action? whenExists,
            bool fromDecoration,
            bool doNotRemoveExisting)
        {
            return Generics
                .WithStatic(typeof(ServiceCollectionExtensions), nameof(AddEngineFactory), serviceType, implementationType ?? serviceType)
                .Invoke(services, name!, canOverrideConfiguration, lifetime, implementationInstance!, implementationFactory!, whenExists!, fromDecoration, doNotRemoveExisting);
        }
        public static IServiceCollection AddEngineFactory<TService>(this IServiceCollection services)
            where TService : class
        {
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            return services;
        }
        public static IServiceCollection AddEngineFactory(this IServiceCollection services, Type serviceType)
        {
            var factoryInterfaceType = typeof(IFactory<>).MakeGenericType(serviceType);
            var factoryType = typeof(Factory<>).MakeGenericType(serviceType);
            services.TryAddTransient(factoryInterfaceType, factoryType);
            services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            return services;
        }
        private static IServiceCollection AddEngineFactory<TService, TImplementation>(this IServiceCollection services,
            AnyOf<string, Enum>? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory,
            Action? whenExists,
            bool fromDecoration,
            bool doNotRemoveExisting)
            where TService : class
            where TImplementation : class, TService
        {
            var nameAsString = name?.AsString();
            var factoryName = nameAsString.GetFactoryName<TService>();
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var serviceType = typeof(TService);
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            var existingServiceWithThatName = services.HasKeyedService<TService, TImplementation>(factoryName, out var serviceDescriptor);
            if (!existingServiceWithThatName || canOverrideConfiguration)
            {
                if (fromDecoration && !map.DecorationCount.ContainsKey(factoryName))
                    map.DecorationCount.Add(factoryName, new());
                services.AddKeyedServiceEngine(serviceType,
                    factoryName,
                    typeof(TService) != typeof(TImplementation) ? typeof(TImplementation) : typeof(TService),
                    implementationInstance,
                    implementationFactory,
                    lifetime,
                    canOverrideConfiguration,
                    doNotRemoveExisting);
                //todo: remember that we need to add this feature in the next release with .net 8 because we need to inject directly the last implementation of a service to retrieve without IFactory
                if (fromDecoration)
                    services.AddOrOverrideService(serviceProvider =>
                    {
                        return serviceProvider.GetRequiredService<IFactory<TService>>().Create(nameAsString)!;
                    }, lifetime);
            }
            else
                whenExists?.Invoke();
            return services;
        }
    }
}
