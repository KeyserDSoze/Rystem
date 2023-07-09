using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                .WithInMemoryIntegration("inmemory")
                .WithSharepointIntegration(x =>
                {
                    x.TenantId = configuration["Sharepoint:TenantId"];
                    x.ClientId = configuration["Sharepoint:ClientId"];
                    x.ClientSecret = configuration["Sharepoint:ClientSecret"];
                    //x.WithoutPreconfiguredSite("SuperSito", "SuperDocumentLibrary");
                    x.WithPreconfiguredSite(configuration["Sharepoint:SiteId"],
                        configuration["Sharepoint:DocumentLibraryId"]);
                }, "sharepoint");
        }
    }
}
