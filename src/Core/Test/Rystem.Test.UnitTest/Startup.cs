using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

namespace Rystem.Test.UnitTest
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.test.json";
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;
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
            applicationBuilder.UseRuntimeServiceProvider();
            applicationBuilder.UseRouting();
            applicationBuilder.UseEndpoints(x =>
            {
                x.MapGet("hello", ([FromServices] SingletonService singletonService, [FromServices] Singleton2Service singleton2Service,
                [FromServices] ScopedService scopedService, [FromServices] Scoped2Service scoped2Service,
                [FromServices] TransientService transientService, [FromServices] Transient2Service transient2Service,
                [FromServices] IServiceProvider serviceProvider) =>
                {
                    return new ServiceWrapper
                    {
                        TransientService = transientService,
                        Transient2Service = transient2Service,
                        ScopedService = scopedService,
                        Scoped2Service = scoped2Service,
                        SingletonService = singletonService,
                        Singleton2Service = singleton2Service,
                        AddedService = serviceProvider.GetService<AddedService>()
                    };
                });
            });
            return ValueTask.CompletedTask;
        }
        protected override ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
        {
            services.AddRuntimeServiceProvider();
            services.AddSingleton<SingletonService>();
            services.AddSingleton<Singleton2Service>();
            services.AddScoped<ScopedService>();
            services.AddScoped<Scoped2Service>();
            services.AddTransient<TransientService>();
            services.AddTransient<Transient2Service>();
            return ValueTask.CompletedTask;
        }
    }
}
