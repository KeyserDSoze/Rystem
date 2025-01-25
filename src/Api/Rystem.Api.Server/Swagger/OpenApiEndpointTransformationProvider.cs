using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Rystem.Api
{

    internal sealed class OpenApiEndpointTransformationProvider : IOpenApiDocumentTransformer
    {
        private readonly EndpointsManager _endpointsManager;
        public OpenApiEndpointTransformationProvider(EndpointsManager endpointsManager)
        {
            _endpointsManager = endpointsManager;
        }
        private const string MultipartFormData = "multipart/form-data";
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            if (document.Components == null)
                document.Components = new();
            foreach (var path in document.Paths)
            {
                var currentEndpoint = _endpointsManager.Endpoints.SelectMany(x => x.Methods).FirstOrDefault(x => x.Value.EndpointUri == path.Key.Trim('/'));
                if (currentEndpoint.Value != null)
                {
                    var apiItem = path.Value;
                    foreach (var parameter in currentEndpoint.Value.Parameters.Where(x => !x.IsCancellationToken))
                    {
                        try
                        {
                            if (!document.Components.Schemas.ContainsKey(parameter.FullName) && !parameter.IsPrimitive)
                            {
                                document.Components.Schemas.Add(parameter.FullName, parameter.Type.GenerateOpenApiSchema(parameter.Example));
                            }
                            OpenApiSchema? schema = null;
                            OpenApiSchema? schemaInBody = null;
                            OpenApiReference? reference = null;
                            if (document.Components.Schemas.ContainsKey(parameter.FullName))
                            {
                                reference = new OpenApiReference
                                {
                                    Id = parameter.FullName,
                                    Type = ReferenceType.Schema
                                };
                                schemaInBody = new OpenApiSchema
                                {
                                    Reference = reference,
                                    Example = document.Components.Schemas[parameter.FullName].Example
                                };
                            }
                            else
                            {
                                schema = parameter.Type.GenerateOpenApiSchema(parameter.Example);
                            }
                            switch (parameter.Location)
                            {
                                case ApiParameterLocation.Query:
                                    apiItem.Parameters.Add(new OpenApiParameter
                                    {
                                        Name = parameter.Name,
                                        In = ParameterLocation.Query,
                                        Required = parameter.IsRequired,
                                        Schema = schema,
                                        Reference = reference
                                    });
                                    break;
                                case ApiParameterLocation.Cookie:
                                    apiItem.Parameters.Add(new OpenApiParameter
                                    {
                                        Name = parameter.Name,
                                        In = ParameterLocation.Cookie,
                                        Required = parameter.IsRequired,
                                        Schema = schema,
                                        Reference = reference
                                    });
                                    break;
                                case ApiParameterLocation.Header:
                                    apiItem.Parameters.Add(new OpenApiParameter
                                    {
                                        Name = parameter.Name,
                                        In = ParameterLocation.Header,
                                        Required = parameter.IsRequired,
                                        AllowEmptyValue = parameter.IsNullable,
                                        Schema = schema,
                                        Reference = reference
                                    });
                                    break;
                                case ApiParameterLocation.Path:
                                    apiItem.Parameters.Add(new OpenApiParameter
                                    {
                                        Name = parameter.Name,
                                        In = ParameterLocation.Path,
                                        Required = parameter.IsRequired,
                                        Schema = schema,
                                        Reference = reference
                                    });
                                    break;
                                case ApiParameterLocation.Body:
                                    var operation = apiItem.Operations.Select(x => x.Value).FirstOrDefault();
                                    if (operation != null)
                                    {
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
                                            }
                                            if (!operation.RequestBody.Content.ContainsKey(MultipartFormData))
                                                operation.RequestBody.Content.Add(
                                                   MultipartFormData, new OpenApiMediaType
                                                   {
                                                       Schema = new OpenApiSchema
                                                       {
                                                           Type = "object",
                                                       },
                                                   });
                                            var content = operation.RequestBody.Content[MultipartFormData];
                                            if (parameter.IsRequired)
                                                content.Schema.Required.Add(parameter.Name);
                                            content.Schema.Properties.Add(parameter.Name, schemaInBody);
                                            //content.Encoding.Add(parameter.Name, new OpenApiEncoding
                                            //{
                                            //    ContentType = isStreamable ? parameter.ContentType : "application/json",
                                            //    Style = ParameterStyle.Form
                                            //});
                                        }
                                        else
                                        {
                                            operation.RequestBody = new OpenApiRequestBody
                                            {
                                                Required = parameter.IsRequired,
                                                Content = new Dictionary<string, OpenApiMediaType>()
                                                {
                                                    {
                                                        parameter.Type.GetContentType(), new OpenApiMediaType
                                                        {
                                                            Schema = schemaInBody
                                                        }
                                                    },
                                                }
                                            };
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            var olaf = ex.Message;
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
