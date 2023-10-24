using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api
{
    public class ApiHttpClient<T> : DispatchProxy where T : class
    {
        private static readonly string s_clientNameForAll = $"ApiHttpClient";
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
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
        private async ValueTask<TResponse> InvokeHttpRequestAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object[] args, bool readResponse)
        {
            var context = new ApiClientRequestBearer();
            var parameterCounter = 0;
            foreach (var chain in currentMethod.Parameters)
            {
                if (chain.Executor != null)
                    chain.Executor(context, args[parameterCounter]);
                else
                    await chain.ExecutorAsync(context, args[parameterCounter]);
                parameterCounter++;
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
                    await enhancer.EnhanceAsync(request);
            if (_requestEnhancers != null)
                foreach (var enhancer in _requestEnhancers)
                    await enhancer.EnhanceAsync(request);
            var response = await _httpClient!.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (readResponse)
            {
                //todo add the chance to return a stream or something similar which is not a json
                //todo add the IAsyncEnumerable
                var value = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.FromJson<TResponse>()!;
                else
                    return default!;
            }
            else
                return default!;
        }
        private async Task InvokeAsync(ApiClientCreateRequestMethod currentMethod, object[] args, Type returnType)
            => await InvokeHttpRequestAsync<object>(currentMethod, args, false);
        private async Task<TResponse> InvokeTAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object[] args, Type returnType)
            => await InvokeHttpRequestAsync<TResponse>(currentMethod, args, true);
        private async ValueTask InvokeValueAsync(ApiClientCreateRequestMethod currentMethod, object[] args, Type returnType)
            => await InvokeHttpRequestAsync<object>(currentMethod, args, false);
        private async ValueTask<TResponse> InvokeValueTAsync<TResponse>(ApiClientCreateRequestMethod currentMethod, object[] args, Type returnType)
            => await InvokeHttpRequestAsync<TResponse>(currentMethod, args, true);
        private static readonly MethodInfo s_dispatchProxyInvokeMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeSync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeTAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeTAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeValueAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeValueAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly MethodInfo s_dispatchProxyInvokeValueTAsyncMethod = typeof(ApiHttpClient<T>).GetMethod(nameof(InvokeValueTAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly Dictionary<string, MethodInfo> s_methods = new();
        protected override object Invoke(MethodInfo method, object[] args)
        {
            var signature = method.GetSignature();
            if (_requestChain == null)
                return default!;
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
                else if (IsGenericTask(method.ReturnType))
                {
                    var returnTypes = method.ReturnType.GetGenericArguments();
                    methodInfo = s_dispatchProxyInvokeTAsyncMethod.MakeGenericMethod(returnTypes);
                }
                else if (IsGenericValueTask(method.ReturnType))
                {
                    var returnTypes = method.ReturnType.GetGenericArguments();
                    methodInfo = s_dispatchProxyInvokeValueTAsyncMethod.MakeGenericMethod(returnTypes);
                }
                else
                {
                    methodInfo = s_dispatchProxyInvokeMethod;
                }
                s_methods.TryAdd(signature, methodInfo);
            }
            var arguments = new object[3] { currentMethod, args, method.ReturnType };
            s_methods.TryGetValue(signature, out var invoker);
            return invoker!.Invoke(this, arguments)!;
        }
        private object InvokeSync(ApiClientCreateRequestMethod currentMethod, object[] args, Type returnType)
        {
            var context = new ApiClientRequestBearer();
            var parameterCounter = 0;
            foreach (var chain in currentMethod.Parameters)
            {
                if (chain.Executor != null)
                    chain.Executor(context, args[parameterCounter]);
                else
                    chain.ExecutorAsync(context, args[parameterCounter]).ToResult();
                parameterCounter++;
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
                    enhancer.EnhanceAsync(request).ToResult();
            if (_requestEnhancers != null)
                foreach (var enhancer in _requestEnhancers)
                    enhancer.EnhanceAsync(request).ToResult();
            var response = _httpClient!.SendAsync(request).ToResult();
            response.EnsureSuccessStatusCode();
            var value = response.Content.ReadAsStringAsync().ToResult();
            if (returnType != typeof(void) && !string.IsNullOrWhiteSpace(value))
                return value.FromJson(returnType)!;
            else
                return default!;
        }
        private static bool IsGenericTask(Type type)
        {
            var current = type;
            while (current != null)
            {
                if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == typeof(Task<>))
                    return true;
                current = current.GetTypeInfo().BaseType;
            }
            return false;
        }
        private static bool IsGenericValueTask(Type type)
        {
            var current = type;
            while (current != null)
            {
                if (current.GetTypeInfo().IsGenericType && current.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    return true;
                current = current.GetTypeInfo().BaseType;
            }
            return false;
        }
    }
}
