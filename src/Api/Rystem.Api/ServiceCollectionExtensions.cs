using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEndpoint<TService>(this IServiceCollection services,
            Action<ApiEndpointPolicyBuilder<TService>> builder,
            string? name = null)
        {
            var endpointsManager = services.TryAddSingletonAndGetService<EndpointsManager>();
            var value = new EndpointValue(typeof(TService))
            {
                FactoryName = name
            };
            endpointsManager.Endpoints.Add(value);
            var settings = new ApiEndpointPolicyBuilder<TService>(value);
            builder.Invoke(settings);
            return services;
        }
    }
}
