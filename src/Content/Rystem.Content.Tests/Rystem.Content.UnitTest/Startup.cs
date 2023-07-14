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
                .WithBlobStorageIntegrationAsync(x =>
                {
                    x.ContainerName = "supertest";
                    x.Prefix = "site/";
                    x.ConnectionString = configuration["ConnectionString:Storage"];
                },
                "blobstorage")
                .ToResult()
                .WithInMemoryIntegration("inmemory")
                .WithSharepointIntegrationAsync(x =>
                {
                    x.TenantId = configuration["Sharepoint:TenantId"];
                    x.ClientId = configuration["Sharepoint:ClientId"];
                    x.ClientSecret = configuration["Sharepoint:ClientSecret"];
                    x.MapWithSiteNameAndDocumentLibraryName("TestNumberOne", "Foglione");
                    //x.MapOnlyDocumentLibraryName("Foglione");
                    //x.MapWithRootSiteAndDocumentLibraryName("Foglione");
                    //x.MapWithSiteIdAndDocumentLibraryId(configuration["Sharepoint:SiteId"],
                    //    configuration["Sharepoint:DocumentLibraryId"]);
                }, "sharepoint").ToResult();
        }
    }
}
