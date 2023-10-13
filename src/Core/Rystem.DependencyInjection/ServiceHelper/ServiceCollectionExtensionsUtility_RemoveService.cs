namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection RemoveService<TService>(
            this IServiceCollection services)
            => services.RemoveService(typeof(TService));
        public static IServiceCollection RemoveService(
            this IServiceCollection services,
            Type typeToRemove)
        {
            var currentServices = services.Where(x => x.ServiceType == typeToRemove && !x.IsKeyedService).ToList();
            foreach (var service in currentServices)
                services.Remove(service);
            return services;
        }
    }
}
