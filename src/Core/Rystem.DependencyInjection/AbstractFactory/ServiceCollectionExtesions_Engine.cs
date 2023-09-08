using System.Reflection;

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
            return $"Rystem.Factory.{name ?? string.Empty}";
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
            Func<IServiceProvider, object>? implementationFactory,
            Action? whenExists
            )
        {
            return Generics
                .WithStatic(typeof(ServiceCollectionExtensions), nameof(AddEngineFactory), serviceType, implementationType)
                .Invoke(services, name!, canOverrideConfiguration, lifetime, implementationInstance!, implementationFactory!, whenExists!);
        }
        private static RystemServiceDescriptor GetServiceDescriptor<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object?, TService>? implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            RystemServiceDescriptor serviceDescriptor;
            if (implementationFactory != null)
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, implementationFactory, lifetime);
            }
            else if (implementationInstance != null)
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, implementationInstance);
            }
            else
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, typeof(TImplementation), lifetime);
            }
            return serviceDescriptor;
        }
        private static RystemServiceDescriptor GetServiceDescriptor<TService, TImplementation>(
            this IServiceCollection services,
            string name,
            ServiceLifetime lifetime,
            object? implementationInstance,
            Func<IServiceProvider, object?, object>? implementationFactory)
            where TService : class
            where TImplementation : class, TService
        {
            RystemServiceDescriptor serviceDescriptor;
            if (implementationFactory != null)
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, implementationFactory, lifetime);
            }
            else if (implementationInstance != null)
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, implementationInstance);
            }
            else
            {
                serviceDescriptor = new RystemServiceDescriptor(typeof(TService), name, typeof(TImplementation), lifetime);
            }
            return serviceDescriptor;
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
            var existingServiceWithThatName = services.HasKeyedService<TService, TImplementation>(name, out var serviceDescriptor);
            if (!existingServiceWithThatName || canOverrideConfiguration)
            {
                var id = 0;
                if (s_services.ContainsKey(name))
                {
                    id = s_services[name].Id;
                }
                else
                {
                    id = s_services.Count(x => x.Value.ServiceType == typeof(TService));
                }
                services.AddKeyedServiceEngine(typeof(TService),
                    name,
                    typeof(TService) != typeof(TImplementation) ? typeof(TImplementation) : null,
                    implementationInstance,
                    implementationFactory,
                    lifetime,
                    canOverrideConfiguration,
                    id);
                services.AddOrOverrideService(serviceProvider => serviceProvider.GetRequiredService<IFactory<TService>>().Create(name)!, lifetime);
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
