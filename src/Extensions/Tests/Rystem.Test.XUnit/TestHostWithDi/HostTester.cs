using System.Net;
using System.Reflection;
using System.Threading.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rystem.Test.XUnit
{
    public static class HostTester
    {
        public static async Task<Exception?> CreateHostServerAsync(IConfiguration configuration,
            Type? applicationPartToAdd,
            Func<IServiceCollection, IConfiguration, ValueTask> configureServicesAsync,
            Func<IApplicationBuilder, IServiceProvider, ValueTask> configureMiddlewaresAsync,
            bool addHealthCheck = false)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLock();
            var locker = serviceCollection.BuildServiceProvider().CreateScope().ServiceProvider.GetService<ILock>();
            Exception? exception = null;
            if (TestHttpClientFactory.Instance.Host == null)
            {
                await locker!.ExecuteAsync(async () =>
                {
                    if (TestHttpClientFactory.Instance.Host == null)
                    {
                        var iAmWaiting = true;
                        TestHttpClientFactory.Instance.Configuration = configuration;
                        TestHttpClientFactory.Instance.Host = new HostBuilder()
                            .ConfigureWebHost(webHostBuilder =>
                            {
                                webHostBuilder
                                .UseTestServer()
                                .Configure(async (context, app) =>
                                {
                                    try
                                    {
                                        await configureMiddlewaresAsync(app, app.ApplicationServices);
                                        if (addHealthCheck)
                                        {
                                            app.UseEndpoints(endpoints =>
                                            {
                                                endpoints.MapHealthChecks("/healthz");
                                            });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        exception = ex;
                                    }
                                    iAmWaiting = false;
                                }).ConfigureServices(services =>
                                {
                                    services.AddSingleton(configuration);
                                    services.AddMvc()
                                        .AddApplicationPart(applicationPartToAdd?.Assembly ?? Assembly.GetExecutingAssembly())
                                        .AddControllersAsServices();
                                    if (addHealthCheck)
                                        services.AddHealthChecks();
                                    configureServicesAsync(services, configuration).ToResult();
                                });
                            }).Build();
                        await TestHttpClientFactory.Instance.Host!.StartAsync();
                        while (iAmWaiting)
                        {
                            await Task.Delay(100);
                        }
                        if (exception == null && addHealthCheck)
                        {
                            var client = TestHttpClientFactory.Instance.CreateServerAndClient();
                            var response = await client.GetAsync("/healthz");
                            if (response.StatusCode != HttpStatusCode.OK && response.Content.Headers.ContentType!.ToString() != "text/plain"
                                    && await response.Content.ReadAsStringAsync() != "Healthy")
                            {
                                exception = new Exception("Health check failed");
                            }
                        }
                    }
                });
            }
            return exception;
        }
    }
}
