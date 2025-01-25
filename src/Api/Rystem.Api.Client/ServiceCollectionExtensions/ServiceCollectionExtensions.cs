using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Rystem.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RystemApiServiceCollectionExtensions
    {
        public static IServiceCollection AddClientsForAllEndpointsApi(this IServiceCollection services, Action<HttpClientBuilder> httpClientBuilder)
        {
            var httpClientSettings = new HttpClientBuilder();
            httpClientBuilder.Invoke(httpClientSettings);
            var endpointsManager = services.GetSingletonService<EndpointsManager>();
            if (endpointsManager != null)
                foreach (var endpoint in endpointsManager.Endpoints)
                {
                    Generics.WithStatic(
                    typeof(RystemApiServiceCollectionExtensions),
                    nameof(PrivateAddClientForEndpointApi),
                    endpoint.Type).Invoke(services, endpoint, httpClientSettings, endpointsManager);
                }
            return services;
        }
        public static IServiceCollection AddClientForEndpointApi<T>(this IServiceCollection services, Action<HttpClientBuilder> httpClientBuilder, AnyOf<string, Enum>? factoryName = null)
            where T : class
        {
            var httpClientSettings = new HttpClientBuilder();
            httpClientBuilder.Invoke(httpClientSettings);
            var endpointsManager = services.GetSingletonService<EndpointsManager>();
            if (endpointsManager != null)
                return services.PrivateAddClientForEndpointApi<T>(
                    endpointsManager.Endpoints.First(x => x.Type == typeof(T) && x.FactoryName == factoryName),
                    httpClientSettings,
                    endpointsManager);
            return services;
        }
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValueForJson = new("application/json");
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValueForText = new("application/text");
        private static IServiceCollection PrivateAddClientForEndpointApi<T>(this IServiceCollection services, EndpointValue endpointValue, HttpClientBuilder builder, EndpointsManager endpointsManager)
            where T : class
        {
            var interfaceType = typeof(T);
            var key = builder.GetKey<T>();
            if (builder.HttpClientConfigurator.ContainsKey(key))
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}", x => builder.HttpClientConfigurator[key]?.Invoke(x));
            else if (builder.HttpClientConfigurator.ContainsKey(builder.DefaultKey))
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}", x => builder.HttpClientConfigurator[builder.DefaultKey]?.Invoke(x));
            else
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}");
            var chainRequest = new ApiClientChainRequest<T>();
            services.AddFactory((serviceProvider, name) =>
            {
                var client = new ApiHttpClient<T>()
                                  .SetApiClientDispatchProxy(
                                      serviceProvider.GetRequiredService<IHttpClientFactory>(),
                                      serviceProvider.GetService<IFactory<IRequestEnhancer>>(),
                                      chainRequest)
                                  .CreateProxy();
                return client;
            }, endpointValue.FactoryName, ServiceLifetime.Transient);
            foreach (var method in endpointValue.Methods)
            {
                var requestMethodCreator = new ApiClientCreateRequestMethod();
                chainRequest.Methods.Add(method.Key, requestMethodCreator);
                var endpointMethodValue = method.Value;
                var currentMethod = method.Value.Method;
                endpointMethodValue.EndpointUri = $"{endpointsManager.BasePath}{endpointValue.EndpointName}/{(endpointValue.FactoryName != null ? $"{endpointValue.FactoryName}/" : string.Empty)}{endpointMethodValue?.Name}";
                requestMethodCreator.FixedPath = endpointMethodValue!.EndpointUri;
                var streamTypeResult = method.Value.Method.ReturnType.IsGenericType &&
                       (method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                       || method.Value.Method.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>)) ?
                   EndpointMethodParameterValue.IsThisTypeASpecialStream(method.Value.Method.ReturnType.GetGenericArguments().First()) :
                   EndpointMethodParameterValue.IsThisTypeASpecialStream(method.Value.Method.ReturnType);
                requestMethodCreator.ResultStreamType = streamTypeResult;
                if (streamTypeResult == StreamType.AspNet)
                    requestMethodCreator.ReturnType = currentMethod.ReturnType.Assembly.GetType("Microsoft.AspNetCore.Http.FormFile")!;
                var numberOfValueInPath = endpointMethodValue!.EndpointUri.Split('/').Length + 1;
                foreach (var parameter in method.Value.Parameters.OrderBy(x => x.Position))
                {
                    if (parameter.Type == typeof(CancellationToken))
                    {
                        requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod());
                    }
                    else
                    {
                        //todo non primitive query parameter and other in theory primitive parameter
                        //todo primitive parameter in multipart
                        switch (parameter.Location)
                        {
                            case ApiParameterLocation.Query:
                                requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                {
                                    Executor = (context, value) =>
                                    {
                                        context.Query ??= new();
                                        if (context.Query.Length == 0)
                                            context.Query.Append('?');
                                        else
                                            context.Query.Append('&');
                                        context.Query.Append($"{parameter.Name}={value}");
                                    }
                                });
                                break;
                            case ApiParameterLocation.Cookie:
                                requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                {
                                    Executor = (context, value) =>
                                    {
                                        context.Cookie ??= new();
                                        context.Cookie.Append($"{parameter.Name}={value}; ");
                                    }
                                });
                                break;
                            case ApiParameterLocation.Header:
                                requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                {
                                    Executor = (context, value) =>
                                    {
                                        context.Headers.Add(parameter.Name, value?.ToString());
                                    }
                                });
                                break;
                            case ApiParameterLocation.Path:
                                requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                {
                                    Executor = (context, value) =>
                                    {
                                        context.Path ??= new();
                                        context.Path.Append($"/{value}");
                                    }
                                });
                                break;
                            case ApiParameterLocation.Body:
                                requestMethodCreator.IsPost = true;
                                if (method.Value.IsMultipart)
                                {
                                    var isStreamable = parameter.StreamType != StreamType.None;
                                    requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                    {
                                        Executor = (context, value) =>
                                        {
                                            if (!(value is null && !parameter.IsRequired))
                                            {
                                                if (context.Content == null)
                                                {
                                                    context.Content = new MultipartFormDataContent($"----------{Guid.NewGuid()}");
                                                }
                                                if (context.Content is MultipartFormDataContent multipart)
                                                {
                                                    if (isStreamable && value == null)
                                                    {
                                                        multipart.Add(new StreamContent(s_emptyStream), parameter.Name, parameter.Name);
                                                    }
                                                    else if (isStreamable && value is Stream stream)
                                                    {
                                                        multipart.Add(new StreamContent(stream), parameter.Name, parameter.Name);
                                                    }
                                                    else if (parameter.StreamType == StreamType.AspNet || parameter.StreamType == StreamType.Rystem)
                                                    {
                                                        var dynamicStream = (dynamic)value!;
                                                        var streamContent = new StreamContent(dynamicStream.OpenReadStream());
                                                        Try.WithDefaultOnCatch(() =>
                                                        {
                                                            var contentType = dynamicStream.ContentType as string;
                                                            if (contentType != null)
                                                                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                                                        });
                                                        multipart.Add(streamContent, parameter.Name, dynamicStream.FileName);
                                                    }
                                                    else
                                                    {
                                                        multipart.Add(new StringContent(parameter.IsPrimitive ? value?.ToString() : value?.ToJson(DefaultJsonSettings.ForEnum),
                                                            parameter.IsPrimitive ? s_mediaTypeHeaderValueForText : s_mediaTypeHeaderValueForJson), parameter.Name);
                                                    }
                                                }
                                            }
                                        }
                                    });
                                }
                                else
                                {
                                    requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                    {
                                        Executor = (context, value) =>
                                        {
                                            context.Content = new StringContent(parameter.IsPrimitive ? value?.ToString() : value.ToJson(DefaultJsonSettings.ForEnum));
                                        }
                                    });
                                }
                                break;
                        }
                    }
                }
            }
            return services;
        }
        private static readonly MemoryStream s_emptyStream = new();
    }
}
