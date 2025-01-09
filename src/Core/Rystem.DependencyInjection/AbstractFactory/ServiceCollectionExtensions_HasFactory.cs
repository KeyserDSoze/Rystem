namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool HasFactory<TService>(
            this IServiceCollection services,
            string? name)
            where TService : class
            => services.HasKeyedService<TService>(name, out _);
        public static bool HasFactory(
            this IServiceCollection services,
            Type serviceType,
            string? name)
            => services.HasKeyedService(serviceType, name, out _);
        public static bool HasFactory<TService>(
            this IServiceCollection services,
            Enum? name)
            where TService : class
            => services.HasKeyedService<TService>(name?.GetDisplayName(), out _);
        public static bool HasFactory(
            this IServiceCollection services,
            Type serviceType,
            Enum? name)
            => services.HasKeyedService(serviceType, name?.GetDisplayName(), out _);
    }
}
