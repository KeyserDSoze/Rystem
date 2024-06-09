using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.TestApi.Controllers;
using Rystem.Test.TestApi.Extensions;
using Rystem.Test.XUnit;

namespace Rystem.Test.UnitTest
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.test.json";
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(ServiceController);
        protected override IServiceCollection ConfigureCientServices(IServiceCollection services)
        {
            services.AddHttpClient("client", x =>
            {
                x.BaseAddress = new Uri("http://localhost");
            });
            return services;
        }
        protected override ValueTask ConfigureServerMiddlewaresAsync(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
        {
            applicationBuilder.UseTestApplication();
            return ValueTask.CompletedTask;
        }
        protected override ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
        {
            services.AddTestServices();
            return ValueTask.CompletedTask;
        }
    }
}
