namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static TService TryAddSingletonAndGetService<TService>(
            this IServiceCollection services,
            TService implementationInstance)
           where TService : class
        {
            if (!services.HasService<TService>(out var serviceDescriptor))
            {
                services.AddSingleton(implementationInstance);
                return implementationInstance;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddSingletonAndGetService<TService>(
            this IServiceCollection services)
           where TService : class, new()
        {
            if (!services.HasService<TService>(out var serviceDescriptor))
            {
                var service = new TService();
                services.AddSingleton(service);
                return service;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddSingletonAndGetService<TService, TImplementation>(
            this IServiceCollection services,
            TImplementation implementationInstance)
           where TService : class
            where TImplementation : class, TService
        {
            if (!services.HasService<TService>(out var serviceDescriptor))
            {
                services.AddSingleton<TService>(implementationInstance);
                return implementationInstance;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService TryAddSingletonAndGetService<TService, TImplementation>(
            this IServiceCollection services)
           where TService : class
            where TImplementation : class, TService, new()
        {
            if (!services.HasService<TService>(out var serviceDescriptor))
            {
                var service = new TImplementation();
                services.AddSingleton<TService>(service);
                return service;
            }
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
        public static TService? GetSingletonService<TService>(
            this IServiceCollection services)
           where TService : class
        {
            if (!services.HasService<TService>(out var serviceDescriptor))
                return default;
            return (TService)serviceDescriptor!.ImplementationInstance!;
        }
    }
}
