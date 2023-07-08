using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Content;

namespace File.UnitTest
{
    public class Startup
    {
        public class ForUserSecrets { }
        public void ConfigureServices(IServiceCollection services)
        {
            var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.test.json")
               .AddUserSecrets<ForUserSecrets>()
               .AddEnvironmentVariables()
               .Build();
            services
                .AddSingleton<Utility>()
                .AddContentRepository()
                .WithBlobStorageIntegration(x =>
                {
                    x.ContainerName = "supertest";
                    x.Prefix = "site/";
                    x.ConnectionString = configuration["ConnectionString:Storage"];
                },
                "blobstorage")
                .WithInMemoryIntegration("inmemory");
        }
    }
}
