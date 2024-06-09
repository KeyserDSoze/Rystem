using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Rystem.Test.XUnit
{
    public abstract class StartupHelper
    {
        protected abstract string? AppSettingsFileName { get; }
        protected abstract bool HasTestHost { get; }
        protected virtual ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration) => ValueTask.CompletedTask;
        protected virtual ValueTask ConfigureServerMiddlewaresAsync(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider) => ValueTask.CompletedTask;
        protected virtual bool AddHealthCheck => true;
        protected abstract Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration { get; }
        protected abstract Type? TypeToChooseTheRightAssemblyWithControllersToMap { get; }
        protected abstract IServiceCollection ConfigureCientServices(IServiceCollection services);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "It's necessary to have this method as a non-static method because the dependency injection package needs a non-static method.")]
        public void ConfigureHost(IHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureHostConfiguration(builder => { })
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder
                .AddJsonFile(AppSettingsFileName ?? "appsettings.json");
                if (TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration != null)
                    builder
                    .AddUserSecrets(TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration.Assembly);
                builder
                .AddEnvironmentVariables();
            });
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "It's necessary to have this method as a non-static method because the dependency injection package needs a non-static method.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Test purposes.")]
        public void ConfigureServices(IServiceCollection services, HostBuilderContext context)
        {
            if (HasTestHost)
            {
                var exception = HostTester.CreateHostServerAsync(context.Configuration,
                    TypeToChooseTheRightAssemblyWithControllersToMap,
                    ConfigureServerServicesAsync,
                    ConfigureServerMiddlewaresAsync,
                    AddHealthCheck).ToResult();
                if (exception != null)
                    throw exception;
                services.AddSingleton(context.Configuration);
                services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Instance);
            }
            ConfigureCientServices(services);
        }
    }
}
