using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static ScanResult ScanDependencyContext<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            Func<Assembly, bool>? predicate = null)
            => services.Scan(typeof(T), lifetime, GetFromDependencyContext(predicate));
        public static ScanResult ScanDependencyContext(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime,
            Func<Assembly, bool>? predicate = null)
            => services.Scan(serviceType, lifetime, GetFromDependencyContext(predicate));
        public static ScanResult ScanDependencyContext(
           this IServiceCollection services,
           ServiceLifetime lifetime,
            Func<Assembly, bool>? predicate = null)
            => services.Scan(lifetime, GetFromDependencyContext(predicate));

        private static Assembly[] GetFromDependencyContext(Func<Assembly, bool>? predicate)
        {
            predicate ??= x => true;
            return DependencyContext.Default!.GetDefaultAssemblyNames()
                .Select(x => Assembly.Load(x))
                .Where(predicate)
                .ToArray();
        }
    }
}
