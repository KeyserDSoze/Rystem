using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using System.Text.Json;

namespace Rystem.Api.Server
{
    public static class EndpointRouteBuilderExtensions
    {
        public static IEndpointRouteBuilder AddEndpointApi<T>(this IEndpointRouteBuilder app)
        {
            var interfaceType = typeof(T);
            foreach (var method in interfaceType.GetMethods())
            {
                if (method.GetParameters().Length > 0)
                {
                    List<ParameterInfo> primitives = new();
                    List<ParameterInfo> nonPrimitives = new();

                    foreach (var parameter in method.GetParameters())
                    {
                        if (parameter.ParameterType.IsPrimitive())
                        {
                            primitives.Add(parameter);
                        }
                        else
                            nonPrimitives.Add(parameter);
                    }
                    var isPost = nonPrimitives.Count > 0;
                    var isMultipart = nonPrimitives.Count > 1;
                    Func<HttpContext, object[]?> readParameters;
                    if (!isPost)
                    {
                        readParameters = (HttpContext context) =>
                        {
                            var parameters = new object[primitives.Count];
                            var counter = 0;
                            foreach (var parameter in primitives)
                            {
                                parameters[counter] = context.Request.Query[parameter.Name!].ToString().Cast(parameter.ParameterType);
                                counter++;
                            }
                            return parameters;
                        };
                        app.MapGet($"api/{interfaceType.Name}/{method.Name}", async (HttpContext context, [FromServices] T service) =>
                        {
                            var result = method.Invoke(service, readParameters(context));
                            if (result is Task task)
                                await task;
                            if (result is ValueTask valueTask)
                                await valueTask;
                            return ((dynamic)result!).Result;
                        }).AllowAnonymous();
                    }
                    else
                    {
                        readParameters = (HttpContext context) =>
                        {
                            var parameters = new object[primitives.Count];
                            var counter = 0;
                            foreach (var parameter in primitives)
                            {
                                parameters[counter] = context.Request.Query[parameter.Name!].Cast(parameter.ParameterType);
                                counter++;
                            }
                            foreach (var parameter in nonPrimitives)
                            {
                                var value = context.Request.Form.First(x => x.Key == parameter.Name);
                                parameters[counter] = value.Value.ToString().FromJson(parameter.ParameterType)!;
                                counter++;
                            }
                            return parameters;
                        };
                        app.MapPost($"api/{interfaceType.Name}/{method.Name}", async (HttpContext context, [FromServices] T service) =>
                        {
                            var result = method.Invoke(service, readParameters(context));
                            if (result is Task task)
                                await task;
                            if (result is ValueTask valueTask)
                                await valueTask;
                            return result;
                        }).AllowAnonymous();
                    }
                }
            }
            return app;
        }
    }
}
