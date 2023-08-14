using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static int ScanCallingAssembly<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, Assembly.GetCallingAssembly()!);
        public static int ScanCallingAssembly(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, Assembly.GetCallingAssembly()!);
        public static int ScanCallingAssembly(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, Assembly.GetCallingAssembly()!);
    }
}
