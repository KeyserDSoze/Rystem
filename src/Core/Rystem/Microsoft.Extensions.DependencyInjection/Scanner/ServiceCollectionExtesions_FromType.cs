using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static IServiceCollection ScanFromType<T, TScanAssemblyRetriever>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, typeof(TScanAssemblyRetriever).Assembly);
        public static IServiceCollection ScanFromType(
            this IServiceCollection services,
            Type serviceType,
            Type scanAssemblyRetriever,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, scanAssemblyRetriever.Assembly);
        public static IServiceCollection ScanFromType<TScanAssemblyRetriever>(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, typeof(TScanAssemblyRetriever).Assembly);
    }
}
