using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.TestApi.Models;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class RuntimeServiceProviderTest
    {
        private readonly HttpClient _httpClient;
        public RuntimeServiceProviderTest(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("client");
        }
        [Fact]
        public async Task AddOneServiceAtRuntimeAsync()
        {
            var response = await _httpClient.GetAsync("Service/Get");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<ServiceWrapper>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.Null(jsonContent.AddedService);
            var response2 = await _httpClient.GetAsync("Service/Get");
            var responseAsString2 = await response2.Content.ReadAsStringAsync();
            var jsonContent2 = responseAsString2.FromJson<ServiceWrapper>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.NotNull(jsonContent2.AddedService);
            Assert.Equal(jsonContent.SingletonService.Id, jsonContent2.SingletonService.Id);
            Assert.Equal(jsonContent.Singleton2Service.Id, jsonContent2.Singleton2Service.Id);
            Assert.NotEqual(jsonContent.ScopedService.Id, jsonContent2.ScopedService.Id);
            Assert.NotEqual(jsonContent.Scoped2Service.Id, jsonContent2.Scoped2Service.Id);
            Assert.NotEqual(jsonContent.Transient2Service.Id, jsonContent2.Transient2Service.Id);
            Assert.NotEqual(jsonContent.TransientService.Id, jsonContent2.TransientService.Id);
        }
        [Fact]
        public async Task AddFactoryAtRuntimeWithoutRebuildAsync()
        {
            var response = await _httpClient.GetAsync("Service/FactoryWithoutRebuild");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<bool>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.True(jsonContent);
        }
        [Fact]
        public async Task AddFactoryAtRuntimeAsync()
        {
            var response = await _httpClient.GetAsync("Service/Factory?name=xx");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<bool>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.True(jsonContent);
            var response2 = await _httpClient.GetAsync("Service/Factory?name=xxy");
            var responseAsString2 = await response2.Content.ReadAsStringAsync();
            var jsonContent2 = responseAsString2.FromJson<bool>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.True(jsonContent);
        }
        [Theory]
        [InlineData(100, false)]
        [InlineData(1_000, false)]
        [InlineData(20, false)]
        [InlineData(20, true)]
        [InlineData(100, true)]
        [InlineData(1_000, true)]
        public async Task MultipleRebuildAtRuntimeAsync(int max, bool withParallel)
        {
            var response = await _httpClient.GetAsync($"Service/MultipleRebuild?max={max}&withParallel={withParallel}");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<int>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.Equal(max, jsonContent);
        }
        [Theory]
        [InlineData(100, false)]
        [InlineData(1_000, false)]
        [InlineData(20, false)]
        [InlineData(20, true)]
        [InlineData(100, true)]
        [InlineData(1_000, true)]
        public async Task MultipleFactoryAtRuntimeAsync(int max, bool withParallel)
        {
            var response = await _httpClient.GetAsync($"Service/MultipleFactory?max={max}&withParallel={withParallel}");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<int>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.Equal(max, jsonContent);
        }
        [Theory]
        [InlineData(100, false)]
        [InlineData(20, false)]
        [InlineData(20, true)]
        [InlineData(100, true)]
        [InlineData(1_000, true)]
        public async Task MultipleFactoryWithMultipleServicesAsync(int max, bool withParallel)
        {
            var response = await _httpClient.GetAsync($"Service/MultipleFactoryWithMultipleServices?max={max}&withParallel={withParallel}");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<int>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.Equal(max, jsonContent);
        }
    }
}
