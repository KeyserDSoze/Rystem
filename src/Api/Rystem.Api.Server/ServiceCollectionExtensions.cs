using Rystem.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {

        public static IServiceCollection AddServerIntegrationForRystemApi(this IServiceCollection services,
            Action<EndpointOptions> options)
        {
            var endpointOptions = new EndpointOptions();
            options.Invoke(endpointOptions);
            services.AddSingleton(endpointOptions);
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<OpenApiEndpointTransformationProvider>();
            });
            if (endpointOptions.HasSwagger)
                services.AddSwaggerGen(c =>
                {
                    c.OperationFilter<EndpointInformationProvider>();
                });
            return services;
        }
    }
}
