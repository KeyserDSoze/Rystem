using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static int ScanEntryAssembly<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, Assembly.GetEntryAssembly()!);
        public static int ScanEntryAssembly(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, Assembly.GetEntryAssembly()!);
        public static int ScanEntryAssembly(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, Assembly.GetEntryAssembly()!);
    }
}
