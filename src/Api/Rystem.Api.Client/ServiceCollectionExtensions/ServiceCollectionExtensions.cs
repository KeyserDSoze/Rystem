using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Rystem.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RystemApiServiceCollectionExtensions
    {
        private static string GetKey<T>() => $"ApiHttpClient_{typeof(T).FullName}";
        private static readonly string s_defaultKey = GetKey<object>();
        public static IServiceCollection ConfigurationHttpClientForEndpointApi<T>(this IServiceCollection services, Action<HttpClient>? settings)
            where T : class
        {
            var key = GetKey<T>();
            if (s_httpClientConfigurator.ContainsKey(key))
                s_httpClientConfigurator[key] = settings;
            else
                s_httpClientConfigurator[key] = settings;
            return services;
        }
        public static IServiceCollection ConfigurationHttpClientForApi(this IServiceCollection services, Action<HttpClient>? settings)
        {
            if (s_httpClientConfigurator.ContainsKey(s_defaultKey))
                s_httpClientConfigurator[s_defaultKey] = settings;
            else
                s_httpClientConfigurator[s_defaultKey] = settings;
            return services;
        }
        private static readonly Dictionary<string, Action<HttpClient>?> s_httpClientConfigurator = new();
        public static IServiceCollection AddClientsForEndpointApi(this IServiceCollection services)
        {
            var endpointsManager = services.TryAddKeyedSingletonAndGetService<EndpointsManager>(new EndpointsManager(), string.Empty);
            foreach (var endpoint in endpointsManager.Endpoints)
            {
                Generics.WithStatic(
                typeof(RystemApiServiceCollectionExtensions),
                nameof(PrivateUseEndpointApi),
                endpoint.Type).Invoke(services, endpoint);
            }
            return services;
        }
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValueForJson = new("application/json");
        private static readonly MediaTypeHeaderValue s_mediaTypeHeaderValueForText = new("application/text");
        private static IServiceCollection PrivateUseEndpointApi<T>(this IServiceCollection services, EndpointValue endpointValue)
            where T : class
        {
            var interfaceType = typeof(T);
            var key = GetKey<T>();
            if (s_httpClientConfigurator.ContainsKey(key))
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}", x => s_httpClientConfigurator[key]?.Invoke(x));
            else if (s_httpClientConfigurator.ContainsKey(s_defaultKey))
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}", x => s_httpClientConfigurator[s_defaultKey]?.Invoke(x));
            else
                services.AddHttpClient($"ApiHttpClient_{interfaceType.FullName}");
            var chainRequest = new ApiClientChainRequest<T>();
            services.AddFactory((serviceProvider, name) =>
            {
                var client = new ApiHttpClient<T>()
                                  .SetApiClientDispatchProxy(
                                      serviceProvider.GetRequiredService<IHttpClientFactory>(),
                                      chainRequest)
                                  .CreateProxy();
                return client;
            }, endpointValue.FactoryName, ServiceLifetime.Transient);
            foreach (var method in endpointValue.Methods)
            {
                var requestMethodCreator = new ApiClientCreateRequestMethod();
                //todo method with the same name
                chainRequest.Methods.Add(method.Value.Name!, requestMethodCreator);
                var endpointMethodValue = method.Value;
                var currentMethod = method.Value.Method;
                endpointMethodValue.EndpointUri = $"api/{endpointValue.EndpointName ?? interfaceType.Name}/{(!string.IsNullOrWhiteSpace(endpointValue.FactoryName) ? $"{endpointValue.FactoryName}/" : string.Empty)}{endpointMethodValue?.Name ?? method.Key}";
                requestMethodCreator.FixedPath = endpointMethodValue!.EndpointUri;
                var numberOfValueInPath = endpointMethodValue!.EndpointUri.Split('/').Length + 1;
                foreach (var parameter in method.Value.Parameters.OrderBy(x => x.Position))
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
                                    context.Cookie.Append($"{parameter.Name}={value}; ");
                                }
                            });
                            break;
                        case ApiParameterLocation.Header:
                            requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                            {
                                Executor = (context, value) =>
                                {
                                    context.Request.Headers.Add(parameter.Name, value?.ToString());
                                }
                            });
                            break;
                        case ApiParameterLocation.Path:
                            requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                            {
                                Executor = (context, value) =>
                                {
                                    context.Path.Append($"/{value}");
                                }
                            });
                            break;
                        case ApiParameterLocation.Body:
                            if (method.Value.IsMultipart)
                            {
                                var isStreamable = parameter.IsStream;
                                requestMethodCreator.Parameters.Add(new ApiClientCreateRequestParameterMethod
                                {
                                    Executor = (context, value) =>
                                    {
                                        if (!(value is null && !parameter.IsRequired))
                                        {
                                            if (context.Request.Content == null)
                                            {
                                                context.Request.Content = new MultipartFormDataContent();
                                            }
                                            if (context.Request.Content is MultipartFormDataContent multipart)
                                            {
                                                if (isStreamable && value is Stream stream)
                                                {
                                                    multipart.Add(new StreamContent(stream), parameter.Name);
                                                }
                                                else
                                                {
                                                    multipart.Add(new StringContent(parameter.IsPrimitive ? value?.ToString() : value.ToJson(),
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
                                        context.Request.Content = new StringContent(parameter.IsPrimitive ? value?.ToString() : value.ToJson());
                                    }
                                });
                            }
                            break;
                    }
                }
            }
            return services;
        }
    }
}
