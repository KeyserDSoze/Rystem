using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Rystem.Test.XUnit
{
    internal sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public static TestHttpClientFactory Instance { get; } = new TestHttpClientFactory();
        public IHost? Host { get; set; }
        public IConfiguration Configuration { get; set; } = null!;
        private HttpClient? _httpClient;
        private TestHttpClientFactory() { }
        public HttpClient CreateClient(string name)
            => CreateServerAndClient();
        public HttpClient CreateServerAndClient()
        {
            var server = Host!.GetTestServer();
            _httpClient = server.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://localhost");
            _httpClient.DefaultRequestHeaders.Add("Access-Control-Request-Method", "POST");
            _httpClient.DefaultRequestHeaders.Add("Access-Control-Request-Headers", "X-Requested-With");
            return _httpClient;
        }
    }
}
