namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiHttpClients(this IServiceCollection services)
        {
            return services;
        }
        public static IServiceCollection AddApiHttpClient<T>(this IServiceCollection services, Action<HttpClient>? settings)
            where T : class
        {
            services.AddHttpClient($"ApiHttpClient_{typeof(T).FullName}", x => settings?.Invoke(x));

            return services;
        }
    }
}
