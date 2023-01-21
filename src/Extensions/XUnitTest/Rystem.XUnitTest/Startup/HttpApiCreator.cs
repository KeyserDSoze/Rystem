using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Concurrent;
using Xunit;

namespace Whistleblowing.Tests
{
    internal static class HttpApiCreator
    {
        public static async Task<Exception> CreateHostServerAsync<T>()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLock();
            var locker = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider.GetService<ILock>();
            Exception? exception = null;
            if (HttpClientFactory.Instance.Host == null)
            {
                await locker!.ExecuteAsync(async () =>
                {
                    if (HttpClientFactory.Instance.Host == null)
                    {
                        var iAmWaiting = true;
                        var services = DiUtility.CreateDependencyInjectionWithConfiguration(out var configuration);
                        HttpClientFactory.Instance.Configuration = configuration;
                        HttpClientFactory.Instance.Host = new HostBuilder()
                            .ConfigureWebHost(webHostBuilder =>
                            {
                                webHostBuilder
                                .UseTestServer()
                                .Configure(app =>
                                {
                                    try
                                    {
#warning to finish
                                        //app.ConfigureMiddlewareAsync(app.ApplicationServices, false).ToResult();
                                    }
                                    catch (Exception ex)
                                    {
                                        exception = ex;
                                    }
                                    iAmWaiting = false;
                                }).ConfigureServices(services =>
                                {
                                    services.AddMvc()
                                        .AddApplicationPart(typeof(T).Assembly)
                                        .AddControllersAsServices();
                                    //services.AddServicesAsync(configuration).ToResult();
                                });
                            }).Build();
                        await HttpClientFactory.Instance.Host!.StartAsync();
                        while (iAmWaiting)
                        {
                            await Task.Delay(100);
                        }
                        Assert.Equal(string.Empty, exception?.Message ?? string.Empty);
                        var client = HttpClientFactory.Instance.CreateServerAndClient();

                        var response = await client.GetAsync("/healthz");

                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("text/plain", response.Content.Headers.ContentType!.ToString());
                        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
                    }
                });
            }
            return exception;
        }
    }
}
