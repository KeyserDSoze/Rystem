namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static int ScanFromTypes<T, TScanAssemblyRetriever, TScanAssemblyRetriever2>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, typeof(TScanAssemblyRetriever).Assembly, typeof(TScanAssemblyRetriever2).Assembly);
        public static int ScanFromTypes(
            this IServiceCollection services,
            Type serviceType,
            Type scanAssemblyRetriever,
            Type scanAssemblyRetriever2,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, scanAssemblyRetriever.Assembly, scanAssemblyRetriever2.Assembly);
        public static int ScanFromTypes<TScanAssemblyRetriever, TScanAssemblyRetriever2>(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, typeof(TScanAssemblyRetriever).Assembly, typeof(TScanAssemblyRetriever2).Assembly);
    }
}
