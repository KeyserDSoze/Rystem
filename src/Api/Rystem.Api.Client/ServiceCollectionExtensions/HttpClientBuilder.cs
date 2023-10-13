namespace Microsoft.Extensions.DependencyInjection
{
    public sealed class HttpClientBuilder
    {
        internal string GetKey<T>() => $"ApiHttpClient_{typeof(T).FullName}";
        internal string DefaultKey => GetKey<object>();
        public HttpClientBuilder ConfigurationHttpClientForEndpointApi<T>(Action<HttpClient>? settings)
            where T : class
        {
            var key = GetKey<T>();
            if (HttpClientConfigurator.ContainsKey(key))
                HttpClientConfigurator[key] = settings;
            else
                HttpClientConfigurator[key] = settings;
            return this;
        }
        public HttpClientBuilder ConfigurationHttpClientForApi(Action<HttpClient>? settings)
        {
            if (HttpClientConfigurator.ContainsKey(DefaultKey))
                HttpClientConfigurator[DefaultKey] = settings;
            else
                HttpClientConfigurator[DefaultKey] = settings;
            return this;
        }
        internal Dictionary<string, Action<HttpClient>?> HttpClientConfigurator { get; } = new();
    }
}
