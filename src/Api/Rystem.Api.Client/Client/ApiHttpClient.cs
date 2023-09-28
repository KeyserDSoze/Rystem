using System.Net.Http.Json;
using System.Reflection;

namespace Rystem.Api.Client
{
    internal sealed class ApiHttpClient<T> : DispatchProxyAsync where T : class
    {
        private static readonly string s_clientName = $"ApiHttpClient_{typeof(T).FullName}";
        private HttpClient? _httpClient;
        public ApiHttpClient() { }
        public void SetApiClientDispatchProxy(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(s_clientName);
        }
        public T CreateProxy()
        {
            var proxy = Create<T, ApiHttpClient<T>>() as ApiHttpClient<T>;
            proxy!._httpClient = _httpClient;
            return (proxy as T)!;
        }

        public override async Task InvokeAsync(MethodInfo method, object[] args)
        {
            await _httpClient!.GetAsync(args[0].ToString());
        }

        public override async Task<TResponse> InvokeAsyncT<TResponse>(MethodInfo method, object[] args)
        {
            var response = await _httpClient!.GetFromJsonAsync<TResponse>($"?id={args[0]}");
            return response!;
        }

        public override object Invoke(MethodInfo method, object[] args)
        {
            throw new NotImplementedException();
        }
    }
}
