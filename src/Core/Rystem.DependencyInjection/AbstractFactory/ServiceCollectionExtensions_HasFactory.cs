namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static bool HasFactory<TService>(
            this IServiceCollection services,
            AnyOf<string?, Enum>? name)
            where TService : class
        {
            var nameAsString = name?.AsString();
            return services.HasKeyedService<TService>(nameAsString, out _);
        }

        public static bool HasFactory(
            this IServiceCollection services,
            Type serviceType,
            AnyOf<string?, Enum>? name)
        {
            var nameAsString = name?.AsString();
            return services.HasKeyedService(serviceType, nameAsString, out _);
        }
    }
}
