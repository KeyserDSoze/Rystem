using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Rystem.PlayFramework;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rystem.PlayFramework
{
    public class ActorsOpenAiFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters.Count > 0 || context.ApiDescription.ParameterDescriptions.Count > 0)
            {
                var relativePath = context.ApiDescription.RelativePath;
                if (relativePath == "api/ai/message")
                    return;
                var name = relativePath.Replace("/", "_");
                var jsonFunctionObject = new ToolNonPrimitiveProperty()
                {
                    Type = "object",
                    Description = operation.Description ?? relativePath,
                };
                var jsonFunction = new Tool
                {
                    Name = name,
                    Description = operation.Description ?? name,
                    Parameters = jsonFunctionObject
                };
                foreach (var scene in ScenesBuilderHelper.FunctionsForEachScene)
                {
                    foreach (var path in scene.Value.AvailableApiPath)
                    {
                        if (path.IsMatch(relativePath))
                        {
                            scene.Value.Functions.Add(x =>
                            {
                                x.AddTool(name, operation.Description ?? name, jsonFunction);
                            });
                            break;
                        }
                    }
                }
                ScenesBuilderHelper.HttpActions.Add(name, new());
                ScenesBuilderHelper.HttpCalls.Add(name, (httpBringer) =>
                {
                    httpBringer.Method = context.ApiDescription.HttpMethod!;
                    return ValueTask.CompletedTask;
                });
                foreach (var parameter in context.ApiDescription.ParameterDescriptions)
                {
                    var parameterName = parameter.Name ?? parameter.Type.Name;
                    ToolPropertyHelper.Add(parameterName, parameter.Type, jsonFunctionObject);
                    if (parameter.Source == BindingSource.Query)
                    {
                        ScenesBuilderHelper.HttpActions[name][parameterName] = (value, httpBringer) =>
                        {
                            if (httpBringer.Query is null)
                                httpBringer.Query = new();
                            httpBringer.Query.Append($"{parameterName}={value[parameterName]}&");
                            return ValueTask.CompletedTask;
                        };
                    }
                    else if (parameter.Source == BindingSource.Body)
                    {
                        ScenesBuilderHelper.HttpActions[name][parameterName] = (value, httpBringer) =>
                        {
                            httpBringer.BodyAsJson = value[parameterName];
                            return ValueTask.CompletedTask;
                        };
                    }
                }
            }
        }
    }
}
