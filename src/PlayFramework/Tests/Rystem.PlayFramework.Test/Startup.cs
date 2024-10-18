using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Test.Api;
using Rystem.Test.XUnit;

namespace Rystem.PlayFramework.Test
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.test.json";

        protected override bool HasTestHost => true;

        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);

        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(CountryController);

        protected override IServiceCollection ConfigureClientServices(IServiceCollection services)
        {
            services.AddHttpClient("client", x =>
            {
                x.BaseAddress = new Uri("http://localhost");
            });
            return services;
        }
        protected override ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
        {
            services.AddServices(configuration);
            return ValueTask.CompletedTask;
        }
        protected override ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
        {
            applicationBuilder.UseMiddlewares();
            return ValueTask.CompletedTask;
        }
    }
}
