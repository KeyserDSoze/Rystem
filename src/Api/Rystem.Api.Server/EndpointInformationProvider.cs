using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rystem.Api
{
    internal sealed class EndpointInformationProvider : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var currentEndpoint = EndpointsManager.Endpoints.SelectMany(x => x.Methods).FirstOrDefault(x => x.Value.EndpointUri == context.ApiDescription.RelativePath);
            foreach (var parameter in currentEndpoint.Value.Parameters)
            {
                context.ApiDescription.ParameterDescriptions.Add(new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription
                {
                    Name = parameter.Name,
                    IsRequired = true,
                    Type = parameter.Type,
                    ParameterDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ParameterDescriptor
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.Type,
                    },
                });
            }
        }
    }
}
