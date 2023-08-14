namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        public static int ScanCurrentDomain<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime)
            => services.Scan(typeof(T), lifetime, AppDomain.CurrentDomain.GetAssemblies());
        public static int ScanCurrentDomain(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime)
            => services.Scan(serviceType, lifetime, AppDomain.CurrentDomain.GetAssemblies());
        public static int ScanCurrentDomain(
           this IServiceCollection services,
           ServiceLifetime lifetime)
            => services.Scan(lifetime, AppDomain.CurrentDomain.GetAssemblies());
    }
}
