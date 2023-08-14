using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        internal static Dictionary<Type, Dictionary<Type, bool>> ScannedTypes { get; } = new();
        public static IServiceCollection Scan<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
            => services.Scan(typeof(T), lifetime, assemblies);
        public static IServiceCollection Scan(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    services.AddScannedType(serviceType, type, lifetime);
                }
            }
            return
                services;
        }
        public static IServiceCollection Scan(
           this IServiceCollection services,
           ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.HasInterface(typeof(IScannable)))
                    {
                        var whatKindOfType = GetScannableInterfaceImplementation(type);
                        if (whatKindOfType != null)
                            services.AddScannedType(whatKindOfType, type, lifetime);
                    }
                }
            }
            return
                services;
        }
        private static void AddScannedType(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            if (!ScannedTypes.ContainsKey(serviceType))
                ScannedTypes.TryAdd(serviceType, new());
            var scannedTypes = ScannedTypes[serviceType];
            if (!scannedTypes.ContainsKey(implementationType))
            {
                if (!(implementationType.HasInterface(serviceType) || implementationType.IsTheSameTypeOrAFather(serviceType)))
                    throw new ArgumentException($"It's not possible to assign {implementationType.FullName} to {serviceType.FullName} during scan setup of services.");
                if (implementationType.HasInterface(typeof(ITransientScannable)))
                    lifetime = ServiceLifetime.Transient;
                else if (implementationType.HasInterface(typeof(IScopedScannable)))
                    lifetime = ServiceLifetime.Scoped;
                else if (implementationType.HasInterface(typeof(ISingletonScannable)))
                    lifetime = ServiceLifetime.Singleton;
                if (serviceType == implementationType)
                    services.AddService(serviceType, lifetime);
                else
                    services.AddService(serviceType, implementationType, lifetime);
                scannedTypes.TryAdd(implementationType, true);
            }
        }
        private static readonly Type s_objectType = typeof(object);
        private static Type? GetScannableInterfaceImplementation(Type? type)
        {
            if (type == s_objectType)
                return null;
            while (type != null && type != s_objectType)
            {
                var scannable = type.GetInterfaces().FirstOrDefault(
                    x => x.Name.StartsWith(nameof(IScannable)) && x.Name != nameof(IScannable));
                if (scannable != null)
                {
                    var genericType = scannable.GetGenericArguments()[0];
                    return genericType;
                }
                type = type.BaseType!;
            }
            return null;
        }
    }
}
