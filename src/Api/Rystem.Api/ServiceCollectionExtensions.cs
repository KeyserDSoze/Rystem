using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceAsEndpoint<TService, TImplementation>(this IServiceCollection services,
            Action<ApiEndpointPolicyBuilder<TService>> builder,
            ServiceLifetime lifetime = ServiceLifetime.Transient,
            string? name = null)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddFactory<TService, TImplementation>(name, lifetime);
            return services.AddEndpoint(builder, name);
        }
        public static IServiceCollection AddEndpoint<TService>(this IServiceCollection services,
            Action<ApiEndpointPolicyBuilder<TService>> builder,
            string? name = null)
        {
            var value = new EndpointValue(typeof(TService))
            {
                FactoryName = name
            };
            EndpointsManager.Endpoints.Add(value);
            var settings = new ApiEndpointPolicyBuilder<TService>(value);
            builder.Invoke(settings);
            return services;
        }
    }
}
