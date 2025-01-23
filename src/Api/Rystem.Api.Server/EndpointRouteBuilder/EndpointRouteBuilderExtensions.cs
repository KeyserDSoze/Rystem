using System.ProgrammingLanguage;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Rystem.Api;

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
                endpoint.Type).Invoke(builder, endpoint, endpointsManager);
            }
            return builder;
        }
        private static List<Type> s_typesToAvoid = new List<Type> { typeof(IHttpFile), typeof(IFormFile), typeof(CancellationToken) };
        private static List<Type> s_genericTypesToAvoid = new List<Type> { typeof(Task<>), typeof(ValueTask<>), typeof(IEnumerable<>),
        typeof(IList<>), typeof(IDictionary<,>), typeof(ICollection<>), typeof(IAsyncEnumerable<>)};
        public static void UseEndpointApiModels(this IEndpointRouteBuilder builder)
        {
            var languages = new List<ProgrammingLanguageType>() { ProgrammingLanguageType.Typescript };
            var endpointsManager = builder.ServiceProvider.GetRequiredService<EndpointsManager>();
            var types = new List<Type>();
            foreach (var endpoint in endpointsManager.Endpoints)
            {
                foreach (var method in endpoint.Methods)
                {
                    Add(method.Value.Method.ReturnType);
                    foreach (var possibleType in method.Value.Method.GetParameters().Select(p => p.ParameterType))
                        Add(possibleType);
                }
                void Add(Type current)
                {
                    if (s_typesToAvoid.Contains(current) || current.IsPrimitive())
                        return;
                    if (current.IsGenericType && s_genericTypesToAvoid.Contains(current.GetGenericTypeDefinition()))
                    {
                        foreach (var type in current.GetGenericArguments())
                            Add(type);
                    }
                    else if (!(current == typeof(ValueTask) || current == typeof(Task) || current == typeof(void)))
                    {
                        types.Add(current);
                    }
                }
            }
            types = types.Distinct(x => x).ToList();

            foreach (var language in languages)
            {
                var converted = types.ConvertAs(language);
                Try.WithDefaultOnCatch(() =>
                {
                    builder
                        .MapGet($"Business/Models/{language}", () =>
                        {
                            return Results.Text(converted.Text, contentType: converted.MimeType);
                        })
                        .WithTags($"Business-{language}");
                });
            }
        }
        private sealed class RetrieverWrapper
        {
            public Func<HttpContext, Task<object>>? ExecutorAsync { get; set; }
            public Func<HttpContext, object>? Executor { get; set; }
            public bool IsCancellationToken { get; set; }
        }
        private static readonly FieldInfo s_baseStreamFromIFormFile = typeof(FormFile).GetField("_baseStream", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo s_baseStreamOffsetFromIFormFile = typeof(FormFile).GetField("_baseStreamOffset", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static IEndpointRouteBuilder PrivateUseEndpointApi<T>(this IEndpointRouteBuilder builder, EndpointValue endpointValue, EndpointsManager endpointsManager)
            where T : class
        {
            var interfaceType = typeof(T);
            foreach (var method in endpointValue.Methods)
            {
                var endpointMethodValue = method.Value;
                List<RetrieverWrapper> retrievers = new();
                var currentMethod = method.Value.Method;
                endpointMethodValue.EndpointUri = $"{endpointValue?.BasePath ?? endpointsManager.BasePath}{endpointValue.EndpointName}/{(endpointValue.FactoryName != null ? $"{endpointValue.FactoryName.Match(x => x, x => x?.ToString())}/" : string.Empty)}{endpointMethodValue!.Name}";
                var numberOfValueInPath = endpointMethodValue!.EndpointUri.Split('/').Length + 1;
                foreach (var parameter in method.Value.Parameters.Where(x => x.Location == ApiParameterLocation.Path).OrderBy(x => x.Position))
                {
                    endpointMethodValue.EndpointUri += $"/{{{parameter.Name}}}";
                }
                var currentPathParameter = 0;
                foreach (var parameter in method.Value.Parameters.OrderBy(x => x.Position))
                {
                    if (parameter.Type == typeof(CancellationToken))
                    {
                        retrievers.Add(new RetrieverWrapper
                        {
                            IsCancellationToken = true
                        });
                    }
                    else
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
                                    var isStreamable = parameter.StreamType != StreamType.None;
                                    if (isStreamable)
                                    {
                                        retrievers.Add(new RetrieverWrapper
                                        {
                                            ExecutorAsync = async context =>
                                            {
                                                var value = context.Request.Form.Files.FirstOrDefault(x => x.Name == parameter.Name);
                                                if (value == null && !parameter.IsRequired)
                                                    return default!;
                                                if ((parameter.StreamType == StreamType.AspNet || parameter.StreamType == StreamType.Rystem)
                                                    && value is IFormFile formFile)
                                                {
                                                    if (parameter.StreamType == StreamType.AspNet)
                                                        return formFile;
                                                    else
                                                    {
                                                        var baseStreamAsObject = s_baseStreamFromIFormFile.GetValue(formFile);
                                                        var baseStream = baseStreamAsObject != null ? (Stream)baseStreamAsObject : new MemoryStream();
                                                        var offset = (long)s_baseStreamOffsetFromIFormFile.GetValue(formFile)!;
                                                        var file = new HttpFile(baseStream, offset, formFile.Length, formFile.Name, formFile.FileName);
                                                        if (formFile.Headers != null && formFile.Headers is IDictionary<string, StringValues> headers)
                                                        {
                                                            file.Headers ??= new();
                                                            foreach (var header in headers)
                                                            {
                                                                file.Headers.TryAdd(header.Key, header.Value.ToString());
                                                            }
                                                        }
                                                        return file;
                                                    }
                                                }
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
                }
                var withoutReturn = method.Value.Method.ReturnType == typeof(void) || method.Value.Method.ReturnType == typeof(Task) || method.Value.Method.ReturnType == typeof(ValueTask);
                var isGenericAsync = method.Value.Method.ReturnType.IsGenericType &&
                    (method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                    || method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

                var streamTypeResult = method.Value.Method.ReturnType.IsGenericType &&
                        (method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                        || method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)) ?
                    EndpointMethodParameterValue.IsThisTypeASpecialStream(method.Value.Method.ReturnType.GetGenericArguments().First()) :
                    EndpointMethodParameterValue.IsThisTypeASpecialStream(method.Value.Method.ReturnType);

                if (!method.Value.IsPost)
                {
                    builder
                        .MapGet(endpointMethodValue.EndpointUri, async (HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory, CancellationToken cancellationToken) =>
                        {
                            var response = await ExecuteAsync(context, service, factory, cancellationToken);
                            return CalculateResponse(response);
                        })
                        .AddAuthorization(endpointMethodValue.Policies);
                }
                else
                {
                    builder
                        .MapPost(endpointMethodValue.EndpointUri, async (HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory, CancellationToken cancellationToken) =>
                        {
                            var response = await ExecuteAsync(context, service, factory, cancellationToken);
                            return CalculateResponse(response);
                        })
                        .AddAuthorization(endpointMethodValue.Policies);
                }

                object CalculateResponse(object response)
                {
                    if (streamTypeResult == StreamType.AspNet && response is IFormFile formFile)
                        return Results.Stream(formFile.OpenReadStream(), formFile.ContentType, formFile.FileName);
                    else if (streamTypeResult == StreamType.Rystem && response is IHttpFile httpFile)
                        return Results.Stream(httpFile.OpenReadStream(), httpFile.ContentType, httpFile.FileName);
                    else if (streamTypeResult == StreamType.Default && response is Stream stream)
                        return Results.Stream(stream);
                    else
                        return response;
                }

                async Task<object> ExecuteAsync(HttpContext context, [FromServices] T? service, [FromServices] IFactory<T>? factory, CancellationToken cancellationToken)
                {
                    if (factory != null)
                        service = factory.Create(endpointValue.FactoryName);
                    var result = currentMethod.Invoke(service, await ReadParametersAsync(context, cancellationToken));
                    cancellationToken.ThrowIfCancellationRequested();
                    if (result is Task task)
                        await task;
                    if (result is ValueTask valueTask)
                        await valueTask;
                    if (withoutReturn)
                        return default!;
                    else if (result is not null)
                    {
                        var response = isGenericAsync ? ((dynamic)result!).Result : result;
                        if (response is not null)
                            return response;
                        else
                            return default!;
                    }
                    else
                        return default!;
                }

                async Task<object[]> ReadParametersAsync(HttpContext context, CancellationToken cancellationToken)
                {
                    var parameters = new object[retrievers.Count];
                    var counter = 0;
                    foreach (var retriever in retrievers)
                    {
                        if (retriever.IsCancellationToken)
                            parameters[counter] = cancellationToken;
                        else if (retriever.Executor is not null)
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
