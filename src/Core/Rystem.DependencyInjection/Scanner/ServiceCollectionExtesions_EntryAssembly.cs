using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static ScanResult ScanEntryAssembly<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, Assembly.GetEntryAssembly()!);
        public static ScanResult ScanEntryAssembly(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, Assembly.GetEntryAssembly()!);
        public static ScanResult ScanEntryAssembly(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, Assembly.GetEntryAssembly()!);
    }
}
