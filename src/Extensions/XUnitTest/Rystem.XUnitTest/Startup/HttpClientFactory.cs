using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Whistleblowing.Tests
{
    internal sealed class HttpClientFactory : IHttpClientFactory
    {
        public static HttpClientFactory Instance { get; } = new HttpClientFactory();
        public IHost? Host { get; set; }
        public IConfiguration Configuration { get; set; } = null!;
        private HttpClient? _httpClient;
        private HttpClientFactory() { }
        public HttpClient CreateClient(string name)
            => _httpClient!;
        public HttpClient CreateServerAndClient()
        {
            var server = Host!.GetTestServer();
            _httpClient = server.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Origin", "http://example.com");
            _httpClient.DefaultRequestHeaders.Add("Access-Control-Request-Method", "POST");
            _httpClient.DefaultRequestHeaders.Add("Access-Control-Request-Headers", "X-Requested-With");
            return _httpClient;
        }
    }
}
