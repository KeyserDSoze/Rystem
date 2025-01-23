using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api.Test.Domain
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBusiness(this IServiceCollection services)
        {
            services
                .ConfigureEndpoints(x =>
                {
                    x.BasePath = "rapi/";
                })
                .AddEndpoint<ISalubry>(endpointBuilder =>
                {
                    endpointBuilder.SetEndpointName("Salubriend");
                    endpointBuilder.SetMethodName(x => x.GetAsync, "Gimme");
                    endpointBuilder
                        .AddAuthorizationForAll("policy");
                })
                .AddEndpoint<IColam>(endpointBuilder =>
                {
                    endpointBuilder.SetEndpointName("Comator");
                    endpointBuilder.SetMethodName(typeof(IColam).GetMethods().First(), "Cod");
                })
                  .AddEndpoint<ITeamCalculator>(endpointBuilder =>
                  {
                      endpointBuilder.SetEndpointName("TeamCalculator");
                  })
                .AddEndpoint<ISalubry>(endpointBuilder =>
                {
                    endpointBuilder
                        .SetEndpointName("E")
                        .SetMethodName(x => x.GetAsync, "Ra")
                        .SetupParameter(x => x.GetAsync, "id", x =>
                        {
                            x.Location = ApiParameterLocation.Body;
                            x.Example = 56;
                        });
                }, "Doma")
                .AddEndpointWithFactory<IEmbeddingService>();
            return services;
        }
    }
}
