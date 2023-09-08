using System.Reflection;
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
            Type implementationType,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory,
            Action? whenExists
            )
        {
            return Generics
                .WithStatic(typeof(ServiceCollectionExtensions), nameof(AddEngineFactory), serviceType, implementationType)
                .Invoke(services, name!, canOverrideConfiguration, lifetime, implementationInstance!, implementationFactory!, whenExists!);
        }
        private static IServiceCollection AddEngineFactory<TService, TImplementation>(this IServiceCollection services,
            string? name,
            bool canOverrideConfiguration,
            ServiceLifetime lifetime,
            TImplementation? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory,
            Action? whenExists)
            where TService : class
            where TImplementation : class, TService
        {
            name = name.GetFactoryName<TService>();
            services.TryAddTransient<IFactory<TService>, Factory<TService>>();
            var existingServiceWithThatName = services.HasKeyedService<TService, TImplementation>(name, out var serviceDescriptor);
            if (!existingServiceWithThatName || canOverrideConfiguration)
            {
                var id = 0;
                var reorder = false;
                if (Services.ContainsKey(name))
                {
                    id = Services[name].Skip;
                    reorder = true;
                }
                else
                {
                    id = Services.Count(x => x.Value.ServiceType == typeof(TService));
                    if (id > 0)
                        services.Remove(services.First(x => x.ServiceType == typeof(TService)));
                }
                services.AddKeyedServiceEngine(typeof(TService),
                    name,
                    typeof(TService) != typeof(TImplementation) ? typeof(TImplementation) : null,
                    implementationInstance,
                    implementationFactory,
                    lifetime,
                    canOverrideConfiguration,
                    id);
                if (reorder)
                {
                    foreach (var service in Services.Where(x => x.Value.ServiceType == typeof(TService)))
                    {
                        services.Remove(service.Value);
                    }
                    foreach (var service in Services.Where(x => x.Value.ServiceType == typeof(TService)).OrderBy(x => x.Value.Skip))
                    {
                        services.Add(service.Value);
                    }
                }
                //todo: remember that we need to add this feature in the next release with .net 8 because we need to inject directly the last implementation of a service to retrieve without IFactory

                services.AddService(serviceProvider => serviceProvider.GetRequiredService<IFactory<TService>>().Create(name)!, lifetime);
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
