using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                })
                .AddEndpoint<IColam>(endpointBuilder =>
                {
                    endpointBuilder.SetEndpointName("Comator");
                    endpointBuilder.SetMethodName(typeof(IColam).GetMethods().First(), "Cod");
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
                }, "Doma");
            return services;
        }
    }
}
