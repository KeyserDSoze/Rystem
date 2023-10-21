using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.Api
{
    public class ApiHttpClient<T> : DispatchProxyAsync where T : class
    {
        private static readonly string s_clientNameForAll = $"ApiHttpClient";
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private ApiClientChainRequest<T> _requestChain;
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
        private async Task<TResponse> InvokeHttpRequestAsync<TResponse>(MethodInfo method, object[] args, bool readResponse)
        {
            var currentMethod = _requestChain.Methods[method.GetSignature()];
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
                var value = await response.Content.ReadAsStringAsync();
                return value.FromJson<TResponse>()!;
            }
            else
                return default!;
        }
        public override Task InvokeAsync(MethodInfo method, object[] args)
            => InvokeHttpRequestAsync<object>(method, args, false);

        public override Task<TResponse> InvokeAsyncT<TResponse>(MethodInfo method, object[] args)
            => InvokeHttpRequestAsync<TResponse>(method, args, true);

        public override object Invoke(MethodInfo method, object[] args)
        {
            return InvokeHttpRequestAsync(method, args, true).ToResult();
        }
    }
}
