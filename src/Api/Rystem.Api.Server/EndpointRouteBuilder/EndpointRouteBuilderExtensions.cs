using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderRystemExtensions
    {
        public static IEndpointRouteBuilder UseEndpointApi(this IEndpointRouteBuilder builder)
        {
            if (builder is IApplicationBuilder app)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            var endpointsManager = builder.ServiceProvider.GetRequiredService<EndpointsManager>();
            foreach (var endpoint in endpointsManager.Endpoints)
            {
                Generics.WithStatic(
                typeof(EndpointRouteBuilderRystemExtensions),
                nameof(PrivateUseEndpointApi),
                endpoint.Type).Invoke(builder, endpoint);
            }
            return builder;
        }
        private sealed class RetrieverWrapper
        {
            public Func<HttpContext, Task<object>>? ExecutorAsync { get; set; }
            public Func<HttpContext, object>? Executor { get; set; }
        }
        private static IEndpointRouteBuilder PrivateUseEndpointApi<T>(this IEndpointRouteBuilder builder, EndpointValue endpointValue)
            where T : class
        {
            var interfaceType = typeof(T);
            foreach (var method in endpointValue.Methods)
            {
                var endpointMethodValue = method.Value;
                List<RetrieverWrapper> retrievers = new();
                var currentMethod = method.Value.Method;
                endpointMethodValue.EndpointUri = $"api/{(endpointValue.EndpointName ?? interfaceType.Name)}/{(!string.IsNullOrWhiteSpace(endpointValue.FactoryName) ? $"{endpointValue.FactoryName}/" : string.Empty)}{endpointMethodValue?.Name ?? method.Key}";
                var numberOfValueInPath = endpointMethodValue!.EndpointUri.Split('/').Length + 1;
                foreach (var parameter in method.Value.Parameters.Where(x => x.Location == ApiParameterLocation.Path).OrderBy(x => x.Position))
                {
                    endpointMethodValue.EndpointUri += $"/{{{parameter.Name}}}";
                }
                var currentPathParameter = 0;
                foreach (var parameter in method.Value.Parameters.OrderBy(x => x.Position))
                {
                    switch (parameter.Location)
                    {
                        case ApiParameterLocation.Query:
                            retrievers.Add(new RetrieverWrapper
                            {
                                Executor = context =>
                                {
                                    if (!parameter.IsRequired && !context.Request.Query.ContainsKey(parameter.Name))
                                        return default!;
                                    var value = context.Request.Query[parameter.Name!].ToString();
                                    var returnValue = parameter.IsPrimitive ? value.Cast(parameter.Type) : value.FromJson(parameter.Type)!;
                                    return returnValue;
                                }
                            });
                            break;
                        case ApiParameterLocation.Cookie:
                            retrievers.Add(new RetrieverWrapper
                            {
                                Executor = context =>
                                {
                                    if (!parameter.IsRequired && !context.Request.Cookies.ContainsKey(parameter.Name))
                                        return default!;
                                    var value = context.Request.Cookies[parameter.Name!]!.ToString();
                                    var returnValue = parameter.IsPrimitive ? value.Cast(parameter.Type) : value.FromJson(parameter.Type)!;
                                    return returnValue;
                                }
                            });
                            break;
                        case ApiParameterLocation.Header:
                            retrievers.Add(new RetrieverWrapper
                            {
                                Executor = context =>
                                {
                                    if (!parameter.IsRequired && !context.Request.Headers.ContainsKey(parameter.Name))
                                        return default!;
                                    var value = context.Request.Headers[parameter.Name!].ToString();
                                    var returnValue = parameter.IsPrimitive ? value.Cast(parameter.Type) : value.FromJson(parameter.Type)!;
                                    return returnValue;
                                }
                            });
                            break;
                        case ApiParameterLocation.Path:
                            var currentPathParameterValue = parameter.Position == -1 ? currentPathParameter : parameter.Position;
                            retrievers.Add(new RetrieverWrapper
                            {
                                Executor = context =>
                                {
                                    var values = context.Request.Path.Value?.Split('/');
                                    if (parameter.IsRequired && values?.Length < parameter.Position)
                                        return default!;
                                    var value = values![numberOfValueInPath + currentPathParameterValue];
                                    var returnValue = parameter.IsPrimitive ? value.Cast(parameter.Type) : value.FromJson(parameter.Type)!;
                                    return returnValue;
                                }
                            });
                            currentPathParameter++;
                            break;
                        case ApiParameterLocation.Body:
                            if (method.Value.IsMultipart)
                            {
                                var isStreamable = parameter.IsStream || parameter.IsSpecialStream;
                                if (isStreamable)
                                {
                                    retrievers.Add(new RetrieverWrapper
                                    {
                                        ExecutorAsync = async context =>
                                        {
                                            var value = context.Request.Form.Files.FirstOrDefault(x => x.Name == parameter.Name);
                                            if (value == null && !parameter.IsRequired)
                                                return default!;
                                            if (parameter.IsSpecialStream && value is IFormFile formFile)
                                                return formFile;
                                            else
                                            {
                                                var memoryStream = new MemoryStream();
                                                await value!.CopyToAsync(memoryStream).NoContext();
                                                memoryStream.Position = 0;
                                                return memoryStream;
                                            }
                                        }
                                    });
                                }
                                else
                                {
                                    retrievers.Add(new RetrieverWrapper
                                    {
                                        Executor = context =>
                                        {
                                            var value = context.Request.Form.FirstOrDefault(x => x.Key == parameter.Name);
                                            if (value.Equals(default) && !parameter.IsRequired)
                                                return default!;
                                            var body = value.Value.ToString();
                                            if (string.IsNullOrWhiteSpace(body) && !parameter.IsRequired)
                                                return default!;
                                            return parameter.IsPrimitive ? body.Cast(parameter.Type) : body.FromJson(parameter.Type)!;
                                        }
                                    });
                                }
                            }
                            else
                            {
                                retrievers.Add(new RetrieverWrapper
                                {
                                    ExecutorAsync = async context =>
                                    {
                                        var value = await context.Request.Body.ConvertToStringAsync();
                                        return parameter.IsPrimitive ? value.Cast(parameter.Type) : value.FromJson(parameter.Type)!;
                                    }
                                });
                            }
                            break;
                    }
                }

                if (!method.Value.IsPost)
                {
                    builder
                        .MapGet(endpointMethodValue.EndpointUri, async (HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory) =>
                        {
                            return await ExecuteAsync(context, service, factory);
                        })
                        .AddAuthorization(endpointMethodValue.Policies);
                }
                else
                {
                    builder.MapPost(endpointMethodValue.EndpointUri, async (HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory) =>
                    {
                        try
                        {
                            return await ExecuteAsync(context, service, factory);
                        }
                        catch (Exception ex)
                        {
                            var olaf = ex.Message;
                            return default;
                        }
                    })
                    .AddAuthorization(endpointMethodValue.Policies);
                }

                async Task<object> ExecuteAsync(HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory)
                {
                    if (factory != null)
                        service = factory.Create(endpointValue.FactoryName);
                    var result = currentMethod.Invoke(service, await ReadParametersAsync(context));
                    if (result is Task task)
                        await task;
                    if (result is ValueTask valueTask)
                        await valueTask;
                    return ((dynamic)result!).Result;
                }

                async Task<object[]> ReadParametersAsync(HttpContext context)
                {
                    var parameters = new object[retrievers.Count];
                    var counter = 0;
                    foreach (var retriever in retrievers)
                    {
                        if (retriever.Executor is not null)
                            parameters[counter] = retriever.Executor.Invoke(context);
                        else if (retriever.ExecutorAsync is not null)
                            parameters[counter] = await retriever.ExecutorAsync.Invoke(context);
                        counter++;
                    }
                    return parameters;
                }
            }
            return builder;
        }
        private static RouteHandlerBuilder AddAuthorization(this RouteHandlerBuilder router, string[]? policies)
        {
            if (policies == null)
                router.AllowAnonymous();
            else if (policies.Length == 0)
            {
                router.RequireAuthorization();
            }
            else
            {
                router.RequireAuthorization(policies);
            }
            return router;
        }
    }
}
