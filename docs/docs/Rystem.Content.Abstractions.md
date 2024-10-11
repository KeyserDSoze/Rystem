# Content Repository Abstractions
You may use this library to help the integration with your business and your several storage repositories.

# Dependency injection

    services
        .AddContentRepository()
        .WithIntegration<SimpleIntegration>("example", ServiceLifetime.Singleton);

with integration class

    internal sealed class SimpleIntegration : IContentRepository
    {
        public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ContentRepositoryDownloadResult?> DownloadAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ContentRepositoryResult?> GetPropertiesAsync(string path, ContentInformationType informationRetrieve = ContentInformationType.All, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(string? prefix = null, bool downloadContent = false, ContentInformationType informationRetrieve = ContentInformationType.None, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void SetName(string name)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> SetPropertiesAsync(string path, ContentRepositoryOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> UploadAsync(string path, byte[] data, ContentRepositoryOptions? options = null, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

# How to use
If you have only one integration installed at once, you may inject directly

    public sealed class SimpleBusiness
    {
        private readonly IContentRepository _contentRepository;

        public SimpleBusiness(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }
    }

## In case of multiple integrations you have to use the factory service

DI

    services
        .AddContentRepository()
        .WithIntegration<SimpleIntegration>("example", ServiceLifetime.Singleton);
        .WithIntegration<SimpleIntegration2>("example2", ServiceLifetime.Singleton);

in Business class to use the first integration

    public sealed class SimpleBusiness
    {
        private readonly IContentRepository _contentRepository;

        public SimpleBusiness(IContentRepositoryFactory contentRepositoryFactory)
        {
            _contentRepository = contentRepositoryFactory.Create("example");
        }
    }

in Business class to use the second integration

    public sealed class SimpleBusiness
    {
        private readonly IContentRepository _contentRepository;

        public SimpleBusiness(IContentRepositoryFactory contentRepositoryFactory)
        {
            _contentRepository = contentRepositoryFactory.Create("example2");
        }
    }

# Migration tool
You can migrate from two different sources. For instance from a blob storage to a sharepoint site document library.

Setup in DI

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
        }, "sharepoint")
        .ToResult();

Usage

    var result = await _contentMigration.MigrateAsync("blobstorage", "sharepoint",
        settings =>
        {
            settings.OverwriteIfExists = true;
            settings.Prefix = prefix;
            settings.Predicate = (x) =>
            {
                return x.Path?.Contains("fileName6") != true;
            };
            settings.ModifyDestinationPath = x =>
            {
                return x.Replace("Folder2", "Folder3");
            };
        }).NoContext();