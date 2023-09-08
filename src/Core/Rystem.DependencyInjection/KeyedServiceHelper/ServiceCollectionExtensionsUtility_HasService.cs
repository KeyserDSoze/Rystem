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
            serviceDescriptor = services.GetDescriptor(typeof(TService), serviceKey);
            return serviceDescriptor != null;
        }

        public static bool HasKeyedService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey,
            out ServiceDescriptor? serviceDescriptor)
            where TService : class
            where TImplementation : class, TService
        {
            serviceDescriptor = services.GetDescriptor<TService>(serviceKey);
            return serviceDescriptor != null;
        }
        public static bool HasKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            out ServiceDescriptor? serviceDescriptor)
        {
            serviceDescriptor = services.GetDescriptor(serviceType, serviceKey);
            return serviceDescriptor != null;
        }

        public static bool HasKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey,
            Type implementationType,
            out ServiceDescriptor? serviceDescriptor)
        {
            serviceDescriptor = services
                .GetDescriptor(serviceType,
                serviceKey, implementationType);
            return serviceDescriptor != null;
        }
    }
}
