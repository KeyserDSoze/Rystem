using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    public class RystemServiceDescriptor : ServiceDescriptor
    {
        public RystemServiceDescriptor(
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
            : base(serviceType, implementationType, lifetime)
        {
        }

        public RystemServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            ServiceLifetime lifetime)
            : base(serviceType, implementationType, lifetime)
        {
            ServiceKey = serviceKey;
        }

        public RystemServiceDescriptor(
            Type serviceType,
            object instance)
            : base(serviceType, instance)
        {
        }

        public RystemServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            object instance)
            : base(serviceType, instance)
        {
            ServiceKey = serviceKey;
        }

        public RystemServiceDescriptor(
            Type serviceType,
            Func<IServiceProvider, object> factory,
            ServiceLifetime lifetime)
            : base(serviceType, factory, lifetime)
        {
        }

        public RystemServiceDescriptor(
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> factory,
            ServiceLifetime lifetime)
            : base(serviceType, sp => factory(sp, null), lifetime)
        {
            ServiceKey = serviceKey;
        }

        public object? ServiceKey { get; }
        public bool IsKeyedService => ServiceKey != null;
        public object? KeyedImplementationInstance => ImplementationInstance;
        public Func<IServiceProvider, object?, object>? KeyedImplementationFactory => ImplementationFactory == null ? null : (serviceProvider, key) => ImplementationFactory(serviceProvider);
        public int Id { get; init; }
    }
    public static class ServiceProviderExtensions
    {
        public static T GetKeyedService<T>(this IServiceProvider serviceProvider, string name)
            where T : class
        {
            var services = serviceProvider.GetServices<T>();
            return default;
        }
    }
    public static partial class ServiceCollectionExtensions
    {
        private static readonly Dictionary<object, RystemServiceDescriptor> s_services;
        private static IServiceCollection AddKeyedServiceEngine(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type? implementationType,
            object? instance,
            Func<IServiceProvider, object?, object>? instanceFactory,
            ServiceLifetime lifetime,
            bool? canOverride,
            int id = 0)
        {
            if (serviceKey == null)
                throw new ArgumentNullException(nameof(serviceKey));
            if (s_services.ContainsKey(serviceKey))
            {
                if (canOverride == true)
                {
                    if (s_services.Remove(serviceKey, out var value))
                        services.Remove(value);
                }
                else if (canOverride == false)
                    throw new ArgumentException($"{serviceKey} already installed.");
                else
                    return services;
            }
            RystemServiceDescriptor descriptor;
            if (instance != null)
                descriptor = new RystemServiceDescriptor(serviceType, serviceKey, instance) { Id = id };
            else if (instanceFactory != null)
                descriptor = new RystemServiceDescriptor(serviceType, serviceKey, (services, key) => instanceFactory(services, key), lifetime) { Id = id };
            else
            {
                if (implementationType == null)
                    descriptor = new RystemServiceDescriptor(serviceType, serviceKey) { Id = id };
                else
                    descriptor = new RystemServiceDescriptor(serviceType, serviceKey, implementationType) { Id = id };
            }
            s_services.Add(serviceKey, descriptor);
            services.Add(descriptor);
            return services;
        }
        public static IServiceCollection AddKeyedSingleton<TService>(this IServiceCollection services,
            object? serviceKey,
            TService implementation)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, implementation, null, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, implementationFactory, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton<TService, TImplementation>(this IServiceCollection services,
           object? serviceKey,
           Func<IServiceProvider, object?, TImplementation> implementationFactory)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton<TService, TImplementation>(this IServiceCollection services,
           object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedSingleton<TService>(this IServiceCollection services,
           object? serviceKey,
           Func<IServiceProvider, object?, TService> implementationFactory)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Singleton, false);
        public static IServiceCollection AddKeyedTransient<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedTransient<TService>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedTransient<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedTransient<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedTransient(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, null, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedTransient(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Transient, false);
        public static IServiceCollection AddKeyedScoped<TService>(this IServiceCollection services,
          object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Scoped, false);
        public static IServiceCollection AddKeyedScoped<TService>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Scoped, false);
        public static IServiceCollection AddKeyedScoped<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Scoped, false);
        public static IServiceCollection AddKeyedScoped<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Scoped, false);
        public static IServiceCollection AddKeyedScoped(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, null, ServiceLifetime.Scoped, false);
        public static IServiceCollection AddKeyedScoped(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Scoped, false);




        public static IServiceCollection TryAddKeyedSingleton<TService>(this IServiceCollection services,
            object? serviceKey,
            TService implementation)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, implementation, null, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Func<IServiceProvider, object?, object> implementationFactory)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, implementationFactory, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton<TService, TImplementation>(this IServiceCollection services,
           object? serviceKey,
           Func<IServiceProvider, object?, TImplementation> implementationFactory)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton<TService, TImplementation>(this IServiceCollection services,
           object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedSingleton<TService>(this IServiceCollection services,
           object? serviceKey,
           Func<IServiceProvider, object?, TService> implementationFactory)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Singleton, true);
        public static IServiceCollection TryAddKeyedTransient<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient<TService>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
             Func<IServiceProvider, object?, object> implementationFactory)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, implementationFactory, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, null, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedTransient(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Transient, true);
        public static IServiceCollection TryAddKeyedScoped<TService>(this IServiceCollection services,
          object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, null, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped<TService>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, null, null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey,
             Func<IServiceProvider, object?, TService> implementationFactory)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, (serviceProvider, name) => implementationFactory.Invoke(serviceProvider, name)!, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
             Func<IServiceProvider, object?, object> implementationFactory)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, implementationFactory, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
        => services.AddKeyedServiceEngine(typeof(TService), serviceKey, typeof(TImplementation), null, null, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, null, null, null, ServiceLifetime.Scoped, true);
        public static IServiceCollection TryAddKeyedScoped(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
        => services.AddKeyedServiceEngine(serviceType, serviceKey, implementationType, null, null, ServiceLifetime.Scoped, true);

        public static RystemServiceDescriptor? GetDescriptor<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.GetDrescriptorEngine(typeof(TService), serviceKey, null);
        public static RystemServiceDescriptor? GetDescriptor<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
            where TService : class
            where TImplementation : class, TService
            => services.GetDrescriptorEngine(typeof(TService), serviceKey, typeof(TImplementation));
        public static RystemServiceDescriptor? GetDescriptor(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
            => services.GetDrescriptorEngine(serviceType, serviceKey, null);
        public static RystemServiceDescriptor? GetDescriptor(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
            => services.GetDrescriptorEngine(serviceType, serviceKey, implementationType);
        private static RystemServiceDescriptor? GetDrescriptorEngine(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type? implementationType)
        {
            var serviceDescriptor = services
                .Where(x => x is RystemServiceDescriptor)
                .Select(x => x as RystemServiceDescriptor).FirstOrDefault(
                    x => x.IsKeyedService
                    && x.ServiceKey == serviceKey
                    && x.ServiceType == serviceType
                    && (implementationType == null || x.ImplementationType == implementationType
                    || x.ImplementationInstance?.GetType() == implementationType));
            return serviceDescriptor;
        }
    }
}
