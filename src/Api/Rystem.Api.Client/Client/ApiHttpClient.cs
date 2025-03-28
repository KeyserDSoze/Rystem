﻿using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api
{
    public class ApiHttpClient<T> : DispatchProxy where T : class
    {
        private static readonly string s_clientNameForAll = $"ApiHttpClient";
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private static readonly Dictionary<string, MethodInfoWrapper> s_methods = new();
        private static readonly MethodInfo s_dispatchProxyInvokeMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeSync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeTMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeTSync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeTAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeTAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeValueAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeValueAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeValueTAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeValueTAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeEnumerableAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeEnumerableAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private sealed class MethodInfoWrapper
        {
            public MethodInfo Method { get; init; }
            public int Cancellation { get; init; }
        }
        private static bool IsSpecificGenericType(Type toVerify, Type type)
        {
            var current = type;
            while (current != null)
            {
                if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == toVerify)
                    return true;
                current = current.GetTypeInfo().BaseType;
            }
            return false;
        }
        private ApiClientChainRequest<T>? _requestChain;
        private HttpClient? _httpClient;
        private IEnumerable<IRequestEnhancer>? _requestEnhancers;
        private IEnumerable<IRequestEnhancer>? _requestForAllEnhancers;
        public ApiHttpClient<T> SetApiClientDispatchProxy(IHttpClientFactory httpClientFactory,
            IFactory<IRequestEnhancer>? factory,
            ApiClientChainRequest<T> requestChain)
        {
            _httpClient = httpClientFactory.CreateClient(s_clientName);
            _requestChain = requestChain;
            _requestEnhancers = factory?.CreateAll(s_clientName);
            _requestForAllEnhancers = factory?.CreateAll(s_clientNameForAll);
            return this;
        }
        public T CreateProxy()
        {
            var proxy = Create<T, ApiHttpClient<T>>() as ApiHttpClient<T>;
            proxy!._httpClient = _httpClient;
            proxy!._requestChain = _requestChain;
            proxy!._requestEnhancers = _requestEnhancers;
            proxy!._requestForAllEnhancers = _requestForAllEnhancers;
            return (proxy as T)!;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method == null)
                return default;
            if (_requestChain == null)
                return default!;
            var signature = method.ToSignature();
            var currentMethod = _requestChain.Methods[signature];
            if (!s_methods.ContainsKey(signature))
            {
                MethodInfo methodInfo;
                if (method.ReturnType == typeof(Task))
                {
                    methodInfo = s_dispatchProxyInvokeAsyncMethod;
                }
                else if (method.ReturnType == typeof(ValueTask))
                {
                    methodInfo = s_dispatchProxyInvokeValueAsyncMethod;
                }
                else if (IsSpecificGenericType(typeof(Task<>), method.ReturnType))
                {
                    var returnTypes = method.ReturnType.GetGenericArguments();
                    methodInfo = s_dispatchProxyInvokeTAsyncMethod.MakeGenericMethod(returnTypes);
                }
                else if (IsSpecificGenericType(typeof(ValueTask<>), method.ReturnType))
                {
                    var returnTypes = method.ReturnType.GetGenericArguments();
                    methodInfo = s_dispatchProxyInvokeValueTAsyncMethod.MakeGenericMethod(returnTypes);
                }
                else if (IsSpecificGenericType(typeof(IAsyncEnumerable<>), method.ReturnType))
                {
                    var returnTypes = method.ReturnType.GetGenericArguments();
                    methodInfo = s_dispatchProxyInvokeEnumerableAsyncMethod.MakeGenericMethod(returnTypes);
                }
                else if (method.ReturnType != typeof(void))
                {
                    methodInfo = s_dispatchProxyInvokeTMethod.MakeGenericMethod(method.ReturnType);
                }
                else
                {
                    methodInfo = s_dispatchProxyInvokeMethod;
                }
                s_methods.TryAdd(signature, new MethodInfoWrapper
                {
                    Method = methodInfo,
                    Cancellation = GetParameterWithCancellationToken()
                });

                int GetParameterWithCancellationToken()
                {
                    var parameters = method!.GetParameters();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType == typeof(CancellationToken))
                            return i;
                    }
                    return -1;
                }
            }
            s_methods.TryGetValue(signature, out var invokerWrapper);
            var cancellationWrapped = invokerWrapper!.Cancellation >= 0 ? args![invokerWrapper.Cancellation] : new CancellationToken();
            var arguments = new object[4] { currentMethod, args!, method.ReturnType, cancellationWrapped! };
            var response = invokerWrapper.Method!.Invoke(this, arguments)!;
            return response;
        }
        private async ValueTask<HttpRequestMessage> CalculateRequestAsync(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
        {
            var context = new ApiClientRequestBearer();
            var parameterCounter = 0;
            if (args != null && args.Length > 0)
            {
                foreach (var chain in currentMethod.Parameters)
                {
                    chain.Executor?.Invoke(context, args[parameterCounter]);
                    parameterCounter++;
                }
            }
            var request = new HttpRequestMessage(currentMethod.IsPost ? HttpMethod.Post : HttpMethod.Get, $"{currentMethod.FixedPath}{context.Path}{context.Query}");
            if (context.Cookie?.Length > 0)
                request.Headers.Add("cookie", context.Cookie.ToString());
            if (context.Headers?.Count > 0)
                foreach (var header in context.Headers)
                    request.Headers.Add(header.Key, header.Value);
            if (context.Content != null)
                request.Content = context.Content;
            if (_requestForAllEnhancers != null)
                foreach (var enhancer in _requestForAllEnhancers)
                    await enhancer.EnhanceAsync(request, cancellationToken);
            if (_requestEnhancers != null)
                foreach (var enhancer in _requestEnhancers)
                    await enhancer.EnhanceAsync(request, cancellationToken);
            return request;
        }
        private async ValueTask<TResponse> InvokeHttpRequestAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, bool readResponse, Type returnType, CancellationToken cancellationToken)
        {
            var request = await CalculateRequestAsync(currentMethod, args, returnType, cancellationToken);
            var response = await _httpClient!.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (readResponse)
            {
                if (currentMethod.ResultStreamType == StreamType.None)
                {
                    var value = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.FromJson<TResponse>()!;
                    else
                        return default!;
                }
                else
                {
                    var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var readStream = GetRightStream(currentMethod, stream, response);
                    if (readStream != null)
                        return (TResponse)readStream;
                }
            }
            return default!;
        }
        private async Task InvokeAsync(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => await InvokeHttpRequestAsync<object>(currentMethod, args, false, returnType, cancellationToken);
        private async Task<TResponse> InvokeTAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => await InvokeHttpRequestAsync<TResponse>(currentMethod, args, true, returnType, cancellationToken);
        private async ValueTask InvokeValueAsync(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => await InvokeHttpRequestAsync<object>(currentMethod, args, false, returnType, cancellationToken);
        private ValueTask<TResponse> InvokeValueTAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => InvokeHttpRequestAsync<TResponse>(currentMethod, args, true, returnType, cancellationToken);
        private TResponse InvokeTSync<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => InvokeHttpRequest<TResponse>(currentMethod, args, true, returnType, cancellationToken);
        private void InvokeSync(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType, CancellationToken cancellationToken)
            => InvokeHttpRequest<object>(currentMethod, args, false, returnType, cancellationToken);
        private TResponse InvokeHttpRequest<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, bool readResponse, Type returnType, CancellationToken cancellationToken)
        {
            var request = CalculateRequestAsync(currentMethod, args, returnType, cancellationToken).ToResult();
            var response = _httpClient!.SendAsync(request, cancellationToken).ToResult();
            response.EnsureSuccessStatusCode();
            if (readResponse)
            {
                if (currentMethod.ResultStreamType == StreamType.None)
                {
                    var value = response.Content.ReadAsStringAsync(cancellationToken).ToResult();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value.FromJson<TResponse>()!;
                    else
                        return default!;
                }
                else
                {
                    var stream = response.Content.ReadAsStreamAsync(cancellationToken).ToResult();
                    var readStream = GetRightStream(currentMethod, stream, response);
                    if (readStream != null)
                        return (TResponse)readStream;
                }
            }
            return default!;
        }
        private static object? GetRightStream(ApiClientCreateRequestMethod currentMethod, Stream stream, HttpResponseMessage response)
        {
            if (currentMethod.ResultStreamType == StreamType.Default)
                return stream;
            else if (currentMethod.ResultStreamType == StreamType.Rystem)
            {
                var file = new HttpFile(stream, 0, stream.Length, response.Content.Headers.ContentDisposition.Name!, response.Content.Headers.ContentDisposition.FileName!)
                {
                    ContentType = response.Content.Headers.ContentType?.MediaType!,
                    ContentDisposition = response.Content.Headers.ContentDisposition.DispositionType
                };
                return file;
            }
            else if (currentMethod.ResultStreamType == StreamType.AspNet)
            {
                dynamic file = Activator.CreateInstance(currentMethod.ReturnType, new object[5] { stream, 0, stream.Length, response.Content.Headers.ContentDisposition.Name!, response.Content.Headers.ContentDisposition.FileName! })!;
                if (file != null)
                {
                    file.ContentType = response.Content.Headers.ContentType?.MediaType!;
                    file.ContentDisposition = response.Content.Headers.ContentDisposition.DispositionType;
                }
                return file;
            }
            return null;
        }
        private async IAsyncEnumerable<TResponse> InvokeEnumerableAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object?[]? args, Type returnType,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var request = await CalculateRequestAsync(currentMethod, args, returnType, cancellationToken);
            var response = await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).NoContext();
            var items = JsonSerializer.DeserializeAsyncEnumerable<TResponse>(stream, Constants.JsonSerializerOptions, cancellationToken);
            if (items != null)
                await foreach (var item in items)
                {
                    if (item != null)
                        yield return item;
                }
        }
    }
}
