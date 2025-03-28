﻿using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rystem.Api
{
    internal sealed class EndpointInformationProvider : IOperationFilter
    {
        private readonly EndpointsManager _endpointsManager;
        public EndpointInformationProvider(EndpointsManager endpointsManager)
        {
            _endpointsManager = endpointsManager;
        }
        private OpenApiString? GetExample(EndpointMethodParameterValue endpointMethodParameterValue)
        {
            if (endpointMethodParameterValue.Example != null)
                return new OpenApiString(endpointMethodParameterValue.IsPrimitive ? endpointMethodParameterValue.Example.ToString() : endpointMethodParameterValue.Example.ToJson(DefaultJsonSettings.ForEnum), false);
            return null;
        }
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var currentEndpoint = _endpointsManager.Endpoints.SelectMany(x => x.Methods).FirstOrDefault(x => x.Value.EndpointUri == context.ApiDescription.RelativePath);
            if (currentEndpoint.Value != null)
            {
                foreach (var parameter in currentEndpoint.Value.Parameters.Where(x => !x.IsCancellationToken))
                {
                    switch (parameter.Location)
                    {
                        case ApiParameterLocation.Query:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Query,
                                Required = parameter.IsRequired,
                                Example = GetExample(parameter)
                            });
                            break;
                        case ApiParameterLocation.Cookie:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Cookie,
                                Required = parameter.IsRequired,
                                Example = GetExample(parameter)
                            });
                            break;
                        case ApiParameterLocation.Header:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Header,
                                Required = parameter.IsRequired,
                                Example = GetExample(parameter)
                            });
                            break;
                        case ApiParameterLocation.Path:
                            operation.Parameters.Add(new OpenApiParameter
                            {
                                Name = parameter.Name,
                                In = ParameterLocation.Path,
                                Required = parameter.IsRequired,
                                Example = GetExample(parameter)
                            });
                            break;
                        case ApiParameterLocation.Body:
                            if (currentEndpoint.Value.IsMultipart)
                            {
                                var isStreamable = parameter.StreamType != StreamType.None || parameter.IsArrayOfBytes;
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
                                            },
                                            Example = GetExample(parameter)
                                        });
                                }
                                var content = operation.RequestBody.Content["multipart/form-data"];
                                if (parameter.IsRequired)
                                    content.Schema.Required.Add(parameter.Name);
                                content.Schema.Properties.Add(parameter.Name, new OpenApiSchema
                                {
                                    Type = isStreamable ? "string" : "object",
                                    Format = isStreamable ? "binary" : "application/json",
                                    Example = GetExample(parameter)
                                });
                                content.Encoding.Add(parameter.Name, new OpenApiEncoding
                                {
                                    ContentType = isStreamable ? parameter.ContentType : "application/json",
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
                                            parameter.IsArrayOfBytes ? "string" : "application/json", new OpenApiMediaType
                                            {
                                                Schema = new OpenApiSchema
                                                {
                                                    Type = parameter.IsArrayOfBytes ? "string" : "object",
                                                    Format = parameter.IsArrayOfBytes ? "string" : "application/json",
                                                    Description = parameter.Name,
                                                    Title = parameter.Name,
                                                    Example = GetExample(parameter)
                                                }
                                            }
                                        },
                                    }
                                };
                            }
                            break;
                    }
                }
            }
        }
    }
}
