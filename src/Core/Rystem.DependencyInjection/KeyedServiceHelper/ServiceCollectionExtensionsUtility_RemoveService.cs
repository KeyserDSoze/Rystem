namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey)
            => services.RemoveKeyedService(typeof(TService), serviceKey);
        public static IServiceCollection RemoveKeyedService(
            this IServiceCollection services,
            Type serviceType,
            object? serviceKey)
        {
            if (services.HasKeyedService(serviceType, serviceKey, out var serviceDescriptor))
            {
                if (serviceDescriptor != null)
                    services.Remove(serviceDescriptor);
            }
            return services;
        }
    }
}
