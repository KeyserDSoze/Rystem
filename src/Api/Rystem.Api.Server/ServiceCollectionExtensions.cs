using Rystem.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServerIntegrationForRystemApi(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.OperationFilter<EndpointInformationProvider>();
            });
            return services;
        }
    }
}
