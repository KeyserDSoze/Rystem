namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool HasKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey,
            out ServiceDescriptor? serviceDescriptor)
            where TService : class
        {
            serviceDescriptor = services.FirstOrDefault(
                 x => x.IsKeyedService
                    && x.ServiceKey == serviceKey
                    && x.ServiceType == typeof(TService));
            return serviceDescriptor != null;
        }

        public static bool HasKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            out ServiceDescriptor? serviceDescriptor)
            where TService : class
            where TImplementation : class, TService
        {
            serviceDescriptor = services.FirstOrDefault(
                    x => x.IsKeyedService
                    && x.ServiceKey == serviceKey
                    && x.ServiceType == typeof(TService)
                    && (x.ImplementationType == typeof(TImplementation)
                    || x.ImplementationInstance?.GetType() == typeof(TImplementation)));
            return serviceDescriptor != null;
        }
        public static bool HasKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            out ServiceDescriptor? serviceDescriptor)
        {
            serviceDescriptor = services.FirstOrDefault(
                 x => x.IsKeyedService
                    && x.ServiceKey == serviceKey
                    && x.ServiceType == serviceType);
            return serviceDescriptor != null;
        }

        public static bool HasKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType,
            out ServiceDescriptor? serviceDescriptor)
        {
            serviceDescriptor = services.FirstOrDefault(
                    x => x.IsKeyedService
                    && x.ServiceKey == serviceKey
                    && x.ServiceType == serviceType
                    && (x.ImplementationType == implementationType
                    || x.ImplementationInstance?.GetType() == implementationType));
            return serviceDescriptor != null;
        }
    }
}
