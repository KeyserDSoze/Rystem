using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rystem.PlayFramework
{
    public class ActorsOpenAiFilter : IOperationFilter
    {
        private static bool s_firstRequest = true;
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (s_firstRequest)
            {
                s_firstRequest = false;
                if (operation.Parameters.Count > 0 || context.ApiDescription.ParameterDescriptions.Count > 0)
                {
                    var relativePath = context.ApiDescription.RelativePath;
                    if (relativePath == "api/ai/message")
                        return;
                    var functionName = relativePath.Replace("/", "_");
                    var jsonFunctionObject = new ToolNonPrimitiveProperty();
                    var jsonFunction = new Tool
                    {
                        Name = functionName,
                        Description = operation.Description ?? functionName,
                        Parameters = jsonFunctionObject
                    };
                    var function = FunctionsHandler.Instance[functionName];
                    function.Chooser = x => x.AddTool(jsonFunction);
                    foreach (var scene in PlayHandler.Instance.ChooseRightPath(relativePath))
                    {
                        PlayHandler.Instance[scene].Functions.Add(functionName);
                    }
                    function.HttpRequest = new()
                    {
                        Uri = relativePath,
                        Call = (httpBringer) =>
                            {
                                httpBringer.Method = context.ApiDescription.HttpMethod!;
                                return ValueTask.CompletedTask;
                            }
                    };
                    foreach (var parameter in context.ApiDescription.ParameterDescriptions)
                    {
                        var parameterName = parameter.Name ?? parameter.Type.Name;
                        ToolPropertyHelper.Add(parameterName, parameter.Type, jsonFunctionObject);
                        if (parameter.Source == BindingSource.Query)
                        {
                            function.HttpRequest.Actions.Add(parameterName, (value, httpBringer) =>
                            {
                                if (httpBringer.Query is null)
                                    httpBringer.Query = new();
                                httpBringer.Query.Append($"{parameterName}={value[parameterName]}&");
                                return ValueTask.CompletedTask;
                            });
                        }
                        else if (parameter.Source == BindingSource.Body)
                        {
                            function.HttpRequest.Actions.Add(parameterName, (value, httpBringer) =>
                            {
                                httpBringer.BodyAsJson = value[parameterName];
                                return ValueTask.CompletedTask;
                            });
                        }
                    }
                }
            }
        }
    }
}
