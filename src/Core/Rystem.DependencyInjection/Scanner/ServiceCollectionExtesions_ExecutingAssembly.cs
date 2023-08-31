using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static ScanResult ScanExecutingAssembly<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, Assembly.GetExecutingAssembly());
        public static ScanResult ScanExecutingAssembly(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, Assembly.GetExecutingAssembly());
        public static ScanResult ScanExecutingAssembly(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, Assembly.GetExecutingAssembly());
    }
}
