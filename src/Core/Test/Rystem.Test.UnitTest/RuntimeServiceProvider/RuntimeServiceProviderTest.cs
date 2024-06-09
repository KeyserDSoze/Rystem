using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public sealed class ServiceWrapper
    {
        public SingletonService SingletonService { get; set; }
        public Singleton2Service Singleton2Service { get; set; }
        public ScopedService ScopedService { get; set; }
        public Scoped2Service Scoped2Service { get; set; }
        public TransientService TransientService { get; set; }
        public Transient2Service Transient2Service { get; set; }
        public AddedService? AddedService { get; set; }
    }
    public sealed class SingletonService : TestService
    {

    }
    public sealed class ScopedService : TestService
    {
    }
    public sealed class TransientService : TestService
    {
    }
    public sealed class Singleton2Service : TestService
    {

    }
    public sealed class Scoped2Service : TestService
    {
    }
    public sealed class Transient2Service : TestService
    {
    }
    public sealed class AddedService : TestService
    {
    }
    public abstract class TestService : IDisposable, IAsyncDisposable
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public void Dispose()
        {
            Console.WriteLine(nameof(Dispose));
        }

        public ValueTask DisposeAsync()
        {
            Console.WriteLine(nameof(DisposeAsync));
            return ValueTask.CompletedTask;
        }
    }
    public class RuntimeServiceProviderTest
    {
        private readonly HttpClient _httpClient;
        public RuntimeServiceProviderTest(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("client");
        }
        [Fact]
        public async Task AddOneServiceAtRuntime()
        {
            var response = await _httpClient.GetAsync("hello");
            var responseAsString = await response.Content.ReadAsStringAsync();
            var jsonContent = responseAsString.FromJson<ServiceWrapper>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Assert.Null(jsonContent.AddedService);
            await RuntimeServiceProvider.GetServiceCollection()
                .AddSingleton<AddedService>()
                .ReBuildAsync();
            var response2 = await _httpClient.GetAsync("hello");
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
    }
}
