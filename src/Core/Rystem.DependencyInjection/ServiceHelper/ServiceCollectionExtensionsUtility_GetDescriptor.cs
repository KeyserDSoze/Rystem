namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static ServiceDescriptor? GetDescriptor<TService>(this IServiceCollection services,
            object? serviceKey)
            => services.GetDrescriptorEngine(typeof(TService), serviceKey, null);
        public static ServiceDescriptor? GetDescriptor<TService, TImplementation>(this IServiceCollection services,
            object? serviceKey)
            where TService : class
            where TImplementation : class, TService
            => services.GetDrescriptorEngine(typeof(TService), serviceKey, typeof(TImplementation));
        public static ServiceDescriptor? GetDescriptor(this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
            => services.GetDrescriptorEngine(serviceType, serviceKey, null);
        public static ServiceDescriptor? GetDescriptor(this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType)
            => services.GetDrescriptorEngine(serviceType, serviceKey, implementationType);
        private static ServiceDescriptor? GetDrescriptorEngine(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type? implementationType)
        {
            if (serviceKey != null)
            {
                var serviceDescriptor = services
                    .Where(x => x.IsKeyedService)
                    .FirstOrDefault(x => serviceKey == null || x!.ServiceKey?.Equals(serviceKey) == true);
                return serviceDescriptor;
            }
            else
            {
                var serviceDescriptor = services
                    .Where(x => !x.IsKeyedService)
                    .FirstOrDefault(
                        x => x!.ServiceType == serviceType
                        && (implementationType == null || x.ImplementationType == implementationType
                        || x.ImplementationInstance?.GetType() == implementationType));
                return serviceDescriptor;
            }
        }
    }
}
