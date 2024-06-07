using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rystem.Test.UnitTest
{
    public class RuntimeServiceProviderTest
    {
        public sealed class Service1
        {

        }
        public sealed class Service2
        {

        }
        [Fact]
        public async Task AddOneServiceAtRuntime()
        {
            WebApplication? app = null;
            ThreadPool.UnsafeQueueUserWorkItem(async _ =>
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddRuntimeServiceProvider();
                builder.Services.AddTransient<Service1>();
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                app = builder.Build();
                app.UseRuntimeServiceProvider();
                app.MapGet("hello", ([FromServices] Service1 service1, [FromServices] Service2 service2) =>
                {
                    return "k";
                });
                app.Run();
            }, null);
            while (app == null)
            {
                await Task.Delay(10);
            }
            await RuntimeServiceProvider.GetServiceCollection()
                .AddSingleton<Service2>().
                ReBuildAsync();
            var client = new HttpClient();
            var request = await client.GetAsync("http://localhost/hello");
            Assert.True(request.IsSuccessStatusCode);
            var response = await request.Content.ReadAsStringAsync();
            Assert.Equal("k", response);
            await app.DisposeAsync();
        }
    }
}
