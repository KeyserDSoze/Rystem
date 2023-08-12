using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RepositoryFramework.Api.Server.Examples
{
    /// <inheritdoc />
    /// <summary>
    /// Adds example requests to your controller endpoints.
    /// See: https://github.com/mattfrear/Swashbuckle.AspNetCore.Examples
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SwaggerExampleAttribute : Attribute
    {
        /// <inheritdoc />
        /// <summary>
        /// Add example data for a request
        /// </summary>
        /// <param name="request">The type passed to the request</param>
        /// <param name="response">A type that inherits from IExamplesProvider</param>
        public SwaggerExampleAttribute(Type request, Type response)
        {
            Request = request;
            Response = response;
        }
        public Type Request { get; }
        public Type Response { get; }
    }
    internal sealed class ExampleProvider : IOperationFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<SwaggerOptions> _options;
        public ExampleProvider(IServiceProvider serviceProvider, IOptions<SwaggerOptions> options)
        {
            _serviceProvider = serviceProvider;
            _options = options;
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var actionAttributes = GetControllerAndActionAttributes(context);

            foreach (var attr in actionAttributes)
            {
                //var example = default(attr.ExamplesProviderType);

                //SetRequestBodyExampleForOperation(
                //    operation,
                //    context.SchemaRepository,
                //    attr.RequestType,
                //    example);
            }
        }
        public static IEnumerable<object> GetControllerAndActionAttributes(OperationFilterContext context)
        {
            var tt = context.MethodInfo.ReflectedType;
            var aa = context.MethodInfo.ReturnType;
            var aa2 = context.MethodInfo.GetParameters();
            var q = context.MethodInfo.ReflectedType?.GetTypeInfo();
            var uu = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            var controllerAttributes = context.MethodInfo.ReflectedType?.GetTypeInfo().GetCustomAttributes<SwaggerExampleAttribute>();
            
            //if (context.MethodInfo != null)
            //{
            //    var controllerAttributes = context.MethodInfo.ReflectedType?.GetTypeInfo().GetCustomAttributes<T>();
            //    var actionAttributes = context.MethodInfo.GetCustomAttributes<T>();

            //    var result = new List<T>(actionAttributes);
            //    if (controllerAttributes != null)
            //    {
            //        result.AddRange(controllerAttributes);
            //    }

            //    return result;
            //}

            //if (context.ApiDescription.ActionDescriptor.EndpointMetadata != null)
            //{
            //    var endpointAttributes = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<T>();

            //    var result = new List<T>(endpointAttributes);
            //    return result;
            //}
            return Enumerable.Empty<object>();
        }
        public void SetRequestBodyExampleForOperation(
            OpenApiOperation operation,
            SchemaRepository schemaRepository,
            Type requestType,
            object example)
        {
            //if (example == null)
            //{
            //    return;
            //}

            //if (operation.RequestBody == null || operation.RequestBody.Content == null)
            //{
            //    return;
            //}

            //var examplesConverter = new ExamplesConverter(mvcOutputFormatter);

            //IOpenApiAny firstOpenApiExample;
            //var multiple = example as IEnumerable<ISwaggerExample<object>>;
            //if (multiple == null)
            //{
            //    firstOpenApiExample = SetSingleRequestExampleForOperation(operation, example, examplesConverter);
            //}
            //else
            //{
            //    firstOpenApiExample = SetMultipleRequestExamplesForOperation(operation, multiple, examplesConverter);
            //}

            //if (swaggerOptions.SerializeAsV2)
            //{
            //    // Swagger v2 doesn't have a request example on the path
            //    // Fallback to setting it on the object in the "definitions"

            //    string schemaDefinitionName = requestType.SchemaDefinitionName();
            //    if (schemaRepository.Schemas.ContainsKey(schemaDefinitionName))
            //    {
            //        var schemaDefinition = schemaRepository.Schemas[schemaDefinitionName];
            //        if (schemaDefinition.Example == null)
            //        {
            //            schemaDefinition.Example = firstOpenApiExample;
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Sets an example on the operation for all of the operation's content types
        /// </summary>
        /// <returns>The first example so that it can be reused on the definition for V2</returns>
        //private IOpenApiAny SetSingleRequestExampleForOperation(
        //    OpenApiOperation operation,
        //    object example,
        //    ExamplesConverter examplesConverter)
        //{
        //    var jsonExample = new Lazy<IOpenApiAny>(() => examplesConverter.SerializeExampleJson(example));
        //    var xmlExample = new Lazy<IOpenApiAny>(() => examplesConverter.SerializeExampleXml(example));

        //    foreach (var content in operation.RequestBody.Content)
        //    {
        //        if (content.Key.Contains("xml"))
        //        {
        //            content.Value.Example = xmlExample.Value;
        //        }
        //        else
        //        {
        //            content.Value.Example = jsonExample.Value;
        //        }
        //    }

        //    return operation.RequestBody.Content.FirstOrDefault().Value?.Example;
        //}
    }
}
