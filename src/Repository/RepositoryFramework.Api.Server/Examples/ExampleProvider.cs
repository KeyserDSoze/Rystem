using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RepositoryFramework.Api.Server.Examples
{
    internal sealed class ExampleProvider : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var relativePath = context.ApiDescription.RelativePath;
            var request = EndpointRouteMap.ApiMap.Apis.SelectMany(x => x.Requests).FirstOrDefault(x => x.Uri == relativePath || x.StreamUri == relativePath);
            if (request != null)
            {
                SetRequestBodyExampleForOperation(operation, request);
                SetResponseBodyExampleForOperation(operation, request);
            }
        }

        public void SetRequestBodyExampleForOperation(
            OpenApiOperation operation,
            RequestApiMap request)
        {
            var firstOpenApiExample = new OpenApiString(request.Sample.RequestBody.ToJson(), false);
            var jsonExample = new Lazy<IOpenApiAny>(() => firstOpenApiExample);

            if (request.Sample.RequestQuery != null)
                foreach (var parameter in request.Sample.RequestQuery)
                {
                    var queryParameter = new OpenApiString(parameter.Value);
                    var oldParameter = operation.Parameters.FirstOrDefault(x => x.Name == parameter.Key);
                    if (oldParameter != null)
                        oldParameter.Example = queryParameter;
                    else
                    {
                        operation.Parameters.Add(new()
                        {
                            AllowEmptyValue = false,
                            Description = parameter.Key,
                            Example = queryParameter
                        });
                    }
                }
            if (operation.RequestBody?.Content != null)
                foreach (var content in operation.RequestBody.Content)
                {
                    content.Value.Example = jsonExample.Value;
                }
        }
        public void SetResponseBodyExampleForOperation(
            OpenApiOperation operation,
            RequestApiMap request)
        {
            var key = "200";
            var response = operation.Responses.FirstOrDefault(r => r.Key == key);

            if (response.Equals(default(KeyValuePair<string, OpenApiResponse>)) || response.Value == null)
            {
                return;
            }
            var firstOpenApiExample = new OpenApiString(request.Sample.Response.ToJson(), false);
            var jsonExample = new Lazy<IOpenApiAny>(() => firstOpenApiExample);
            var mediaType = new OpenApiMediaType()
            {
                Example = jsonExample.Value,
            };
            if (response.Value.Content.Count == 0)
                response.Value.Content.Add(key, mediaType);
            else
                foreach (var content in response.Value.Content)
                {
                    content.Value.Example = jsonExample.Value;
                }
        }
    }
}
