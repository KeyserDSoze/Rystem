using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureEndpoints(this IServiceCollection services,
            Action<EndpointsManager> configurator)
        {
            var endpointsManager = new EndpointsManager();
            configurator.Invoke(endpointsManager);
            services.TryAddSingleton(endpointsManager);
            return services;
        }
        public static IServiceCollection AddEndpoint<TService>(this IServiceCollection services,
            Action<ApiEndpointBuilder<TService>> builder,
            AnyOf<string, Enum>? name = null)
        {
            var endpointsManager = services.TryAddSingletonAndGetService<EndpointsManager>();
            var value = new EndpointValue(typeof(TService))
            {
                FactoryName = name
            };
            endpointsManager.Endpoints.Add(value);
            var settings = new ApiEndpointBuilder<TService>(value, endpointsManager.RemoveAsyncSuffix);
            builder.Invoke(settings);
            return services;
        }
        public static IServiceCollection AddEndpointWithFactory<TService>(this IServiceCollection services,
            Action<ApiEndpointBuilder<TService>>? builder = null)
        {
            var endpointsManager = services.TryAddSingletonAndGetService<EndpointsManager>();
            var value = new EndpointValue(typeof(TService))
            {
                IsFactory = true
            };
            endpointsManager.Endpoints.Add(value);
            var settings = new ApiEndpointBuilder<TService>(value, endpointsManager.RemoveAsyncSuffix);
            builder?.Invoke(settings);
            return services;
        }
    }
}
