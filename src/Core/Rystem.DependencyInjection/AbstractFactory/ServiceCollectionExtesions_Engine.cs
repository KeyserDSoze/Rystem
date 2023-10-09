using System;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        private static void SendInError<TService, TImplementation>(this IServiceCollection services, string name)
            where TService : class
            where TImplementation : class, TService
        {
            name = name.GetFactoryName<TService>();
            _ = services.HasKeyedService<TService, TImplementation>(name, out var serviceDescriptor);
            throw new ArgumentException($"Service {typeof(TImplementation).FullName} with name: '{name.Replace($"{typeof(TService).FullName}_", string.Empty)}' for your factory {typeof(TService).FullName} already exists in the form of {serviceDescriptor.ImplementationType?.FullName ?? serviceDescriptor.ImplementationInstance?.GetType().FullName ?? serviceDescriptor.ServiceType.FullName}");
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
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory,
            Action? whenExists,
            bool fromDecoration)
        {
            return Generics
                .WithStatic(typeof(ServiceCollectionExtensions), nameof(AddEngineFactory), serviceType, implementationType ?? serviceType)
                .Invoke(services, name!, canOverrideConfiguration, lifetime, implementationInstance!, implementationFactory!, whenExists!, fromDecoration);
        }
        private static IServiceCollection AddEngineFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory,
            Action? whenExists,
            bool fromDecoration)
            where TService : class
            where TImplementation : class, TService
        {
            var factoryName = name.GetFactoryName<TService>();
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var serviceType = typeof(TService);
            var map = services.TryAddSingletonAndGetService<ServiceFactoryMap>();
            var existingServiceWithThatName = services.HasKeyedService<TService, TImplementation>(factoryName, out var serviceDescriptor);
            if (!existingServiceWithThatName || canOverrideConfiguration)
            {
                if (fromDecoration && !map.DecorationCount.ContainsKey(factoryName))
                    map.DecorationCount.Add(factoryName, new());
                if (map.Services.TryGetValue(factoryName, out var keyedServiceDescriptor))
                {
                    map.Services.Remove(factoryName);
                    services.Remove(keyedServiceDescriptor);
                }
                services.AddKeyedServiceEngine(serviceType,
                    factoryName,
                    typeof(TService) != typeof(TImplementation) ? typeof(TImplementation) : typeof(TService),
                    implementationInstance,
                    implementationFactory,
                    lifetime,
                    canOverrideConfiguration);
                //todo: remember that we need to add this feature in the next release with .net 8 because we need to inject directly the last implementation of a service to retrieve without IFactory
                if (fromDecoration)
                    services.AddOrOverrideService(serviceProvider =>
                    {
                        return serviceProvider.GetRequiredService<IFactory<TService>>().Create(name)!;
                    }, lifetime);
            }
            else
                whenExists?.Invoke();
            return services;
        }
        public static bool HasFactory<TService>(
            this IServiceCollection services,
            string? name)
            where TService : class
            => services.HasKeyedService<TService>(name, out _);
        public static bool HasFactory(
            this IServiceCollection services,
            Type serviceType,
            string? name)
            => services.HasKeyedService(serviceType, name, out _);
    }
}
