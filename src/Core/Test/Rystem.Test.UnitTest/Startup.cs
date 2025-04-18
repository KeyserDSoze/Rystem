﻿using System;
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
        protected override bool HasTestHost => true;
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(ServiceController);
        protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("client", x =>
            {
                x.BaseAddress = new Uri("https://localhost:443");
            });
            return services;
        }
        protected override ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder applicationBuilder, IServiceProvider serviceProvider)
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
