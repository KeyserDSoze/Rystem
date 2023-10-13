namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static TService TryAddKeyedSingletonAndGetService<TService>(
            this IServiceCollection services,
            TService implementationInstance,
            object? serviceKey)
           where TService : class
        {
            if (!services.HasKeyedService<TService>(serviceKey, out var serviceDescriptor))
            {
                services.AddKeyedSingleton(serviceKey, implementationInstance);
                return implementationInstance;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddKeyedSingletonAndGetService<TService>(
            this IServiceCollection services,
            object? serviceKey)
           where TService : class, new()
        {
            if (!services.HasKeyedService<TService>(serviceKey, out var serviceDescriptor))
            {
                var service = new TService();
                services.AddKeyedSingleton(serviceKey, service);
                return service;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddKeyedSingletonAndGetService<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation implementationInstance,
            object? serviceKey)
           where TService : class
            where TImplementation : class, TService
        {
            if (!services.HasKeyedService<TService>(serviceKey, out var serviceDescriptor))
            {
                services.AddKeyedSingleton<TService>(implementationInstance);
                return implementationInstance;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddKeyedSingletonAndGetService<TService, TImplementation>(
            this IServiceCollection services,
            object? serviceKey)
           where TService : class
            where TImplementation : class, TService, new()
        {
            if (!services.HasKeyedService<TService>(serviceKey, out var serviceDescriptor))
            {
                var service = new TImplementation();
                services.AddKeyedSingleton<TService>(service);
                return service;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService? GetSingletonKeyedService<TService>(
            this IServiceCollection services,
            object? serviceKey)
           where TService : class
        {
            if (!services.HasKeyedService<TService>(serviceKey, out var serviceDescriptor))
                return default;
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
    }
}
