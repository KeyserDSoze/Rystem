using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace RepositoryFramework.UnitTest
{
    public class Startup
    {

    }
    internal static class DiUtility
    {
        public class ForUserSecrets { }
        public static IServiceCollection CreateDependencyInjectionWithConfiguration(out IConfiguration configuration)
        {
            var services = new ServiceCollection();
            configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
               .AddUserSecrets<ForUserSecrets>()
               .AddEnvironmentVariables()
               .Build();
            services.AddSingleton(configuration);
            return services;
        }
        public static IServiceProvider Finalize(this IServiceCollection services, out IServiceProvider serviceProvider)
            => serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    }
}
