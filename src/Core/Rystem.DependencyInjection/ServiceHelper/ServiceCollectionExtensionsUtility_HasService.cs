namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool HasService<TService>(
            this IServiceCollection services,
            out ServiceDescriptor? serviceDescriptor)
            where TService : class
        {
            serviceDescriptor = services.FirstOrDefault(x => !x.IsKeyedService && x.ServiceType == typeof(TService));
            return serviceDescriptor != null;
        }

        public static bool HasService<TService, TImplementation>(
            this IServiceCollection services,
            out ServiceDescriptor? serviceDescriptor)
            where TService : class
            where TImplementation : class, TService
        {
            serviceDescriptor = services.FirstOrDefault(x => !x.IsKeyedService && (x.ServiceType == typeof(TService)
                    && (x.ImplementationType == typeof(TImplementation)
                    || x.ImplementationInstance?.GetType() == typeof(TImplementation))));
            return serviceDescriptor != null;
        }
    }
}
