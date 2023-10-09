using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Rystem.Api
{
    public sealed class ApiClientChainRequest<T>
    {
        public Dictionary<string, ApiClientCreateRequestMethod> Methods { get; set; } = new();
    }
    public sealed class ApiClientCreateRequestMethod
    {
        public bool IsPost { get; set; }
        public string FixedPath { get; set; }
        public List<ApiClientCreateRequestParameterMethod> Parameters { get; set; } = new();
    }
    public sealed class ApiClientCreateRequestParameterMethod
    {
        public string Name { get; set; }
        public Action<ApiClientRequestBearer, object> Executor { get; set; }
        public Func<ApiClientRequestBearer, object, Task> ExecutorAsync { get; set; }
    }
    public sealed class ApiClientRequestBearer
    {
        public StringBuilder Path { get; set; }
        public StringBuilder Query { get; set; }
        public StringBuilder Cookie { get; set; }
        public HttpRequestMessage Request { get; set; }
    }
    public class ApiHttpClient<T> : DispatchProxyAsync where T : class
    {
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private ApiClientChainRequest<T> _requestChain;
        private HttpClient? _httpClient;
        public ApiHttpClient<T> SetApiClientDispatchProxy(IHttpClientFactory httpClientFactory,
            ApiClientChainRequest<T> requestChain)
        {
            _httpClient = httpClientFactory.CreateClient(s_clientName);
            _requestChain = requestChain;
            return this;
        }
        public T CreateProxy()
        {
            var proxy = Create<T, ApiHttpClient<T>>() as ApiHttpClient<T>;
            proxy!._httpClient = _httpClient;
            proxy!._requestChain = _requestChain;
            return (proxy as T)!;
        }
        private async Task<TResponse> CreateResponseAsync<TResponse>(MethodInfo method, object[] args, bool readResponse)
        {
            var currentMethod = _requestChain.Methods[method.Name];
            var request = new HttpRequestMessage(currentMethod.IsPost ? HttpMethod.Post : HttpMethod.Get, currentMethod.FixedPath);
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
            request.RequestUri = new Uri($"{context.Path}{context.Query}");
            if (context.Cookie.Length > 0)
                request.Headers.Add("cookie", context.Cookie.ToString());
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
            => CreateResponseAsync<object>(method, args, false);

        public override Task<TResponse> InvokeAsyncT<TResponse>(MethodInfo method, object[] args)
            => CreateResponseAsync<TResponse>(method, args, true);

        public override object Invoke(MethodInfo method, object[] args)
        {
            throw new NotImplementedException();
        }
    }
}
