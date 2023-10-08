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
            if (currentEndpoint.Value != null)
            {
                var nonPrimitivesCount = currentEndpoint.Value.Parameters.Count(x => x.Location == ApiParameterLocation.Body);
                var isMultipart = nonPrimitivesCount > 1;
                foreach (var parameter in currentEndpoint.Value.Parameters)
                {
                    switch (parameter.Location)
                    {
                        case ApiParameterLocation.Query:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Query,
                                Required = parameter.IsRequired
                            });
                            break;
                        case ApiParameterLocation.Cookie:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Cookie,
                                Required = parameter.IsRequired
                            });
                            break;
                        case ApiParameterLocation.Header:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Header,
                                Required = parameter.IsRequired
                            });
                            break;
                        case ApiParameterLocation.Path:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Path,
                                Required = parameter.IsRequired
                            });
                            break;
                        case ApiParameterLocation.Body:
                            if (isMultipart)
                            {
                                if (operation.RequestBody == null)
                                {
                                    operation.RequestBody = new OpenApiRequestBody
                                    {
                                        Required = true,
                                        Content = new Dictionary<string, OpenApiMediaType>()
                                    };
                                    operation.RequestBody.Content.Add(
                                        "multipart/form-data", new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Type = "object"
                                            }
                                        });
                                }
                                var content = operation.RequestBody.Content["multipart/form-data"];
                                if (parameter.IsRequired)
                                    content.Schema.Required.Add(parameter.Name);
                                content.Schema.Properties.Add(parameter.Name, new OpenApiSchema
                                {
                                    Type = parameter.IsStream ? "string" : "object",
                                    Format = parameter.IsStream ? "binary" : "application/json"
                                });
                                content.Encoding.Add(parameter.Name, new OpenApiEncoding
                                {
                                    ContentType = parameter.IsStream ? parameter.ContentType : "application/json",
                                    Style = ParameterStyle.Form
                                });
                            }
                            else
                            {
                                operation.RequestBody = new OpenApiRequestBody
                                {
                                    Required = parameter.IsRequired,
                                    Content = new Dictionary<string, OpenApiMediaType>()
                                    {
                                        {
                                            "application/json", new OpenApiMediaType
                                            {
                                                Schema = new OpenApiSchema
                                                {
                                                    Type = "string",
                                                    Format = "string",
                                                    Description = parameter.Name,
                                                    Title = parameter.Name,
                                                }
                                            }
                                        },
                                    }
                                };
                            }
                            break;
                    }
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
}
