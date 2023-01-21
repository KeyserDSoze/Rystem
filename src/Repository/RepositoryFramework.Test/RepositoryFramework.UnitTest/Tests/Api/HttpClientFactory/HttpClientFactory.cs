using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace RepositoryFramework.UnitTest.Tests.Api
{
    internal sealed class HttpClientFactory : IHttpClientFactory
    {
        public static HttpClientFactory Instance { get; } = new HttpClientFactory();
        public IHost? Host { get; set; }
        public IServiceProvider? ServiceProvider { get; set; }
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
