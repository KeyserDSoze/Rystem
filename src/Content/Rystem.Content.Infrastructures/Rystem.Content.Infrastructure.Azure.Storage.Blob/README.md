# Rystem.Content.Infrastructure.Azure.Storage.Blob

[![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.Azure.Storage.Blob)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Azure.Storage.Blob)

Azure Blob Storage backend for [Rystem Content Framework](../Rystem.Content.Abstractions). Stores files as blobs inside an Azure Storage container, with optional path prefix, public access, and managed identity support.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Azure.Storage.Blob
```

---

## Registration

### Connection string

```csharp
await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "assets";
        x.Prefix        = "uploads/";   // optional path prefix prepended to every file path
        x.ConnectionString = configuration["Storage:ConnectionString"];
    }, "blob")
    .NoContext();
```

### Managed identity (passwordless)

```csharp
await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.EndpointUri              = new Uri("https://<account>.blob.core.windows.net");
        x.ManagedIdentityClientId  = "<user-assigned-mi-client-id>"; // omit for system-assigned
        x.ContainerName            = "assets";
    }, "blob")
    .NoContext();
```

### Synchronous variant

```csharp
services
    .AddContentRepository()
    .WithBlobStorageIntegration(x =>
    {
        x.ContainerName    = "assets";
        x.ConnectionString = configuration["Storage:ConnectionString"];
    }, "blob");
```

---

## `BlobStorageConnectionSettings` reference

| Property | Type | Description |
|----------|------|-------------|
| `ConnectionString` | `string?` | Azure Storage connection string |
| `EndpointUri` | `Uri?` | Blob service endpoint (used with managed identity) |
| `ManagedIdentityClientId` | `string?` | Client ID for user-assigned managed identity; omit for system-assigned |
| `ContainerName` | `string?` | Target blob container name |
| `Prefix` | `string?` | Path prefix prepended to every file path (e.g. `"uploads/"`) |
| `IsPublic` | `bool` | Set container public access level when created |
| `ClientOptions` | `BlobClientOptions?` | Advanced Azure SDK client options (retry policy, transport, etc.) |
| `UploadOptions` | `BlobUploadOptions?` | Advanced upload options (access tier, transfer options, etc.) |

---

## Usage

```csharp
public sealed class AssetService
{
    private readonly IContentRepository _repo;

    public AssetService(IContentRepositoryFactory factory)
        => _repo = factory.Create("blob");

    public async Task UploadImageAsync(string name, byte[] data)
    {
        await _repo.UploadAsync($"images/{name}", data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders { ContentType = "image/png" },
            Metadata    = new Dictionary<string, string> { { "source", "upload" } },
            Tags        = new Dictionary<string, string> { { "version", "1" } }
        });
    }

    public async Task<byte[]?> DownloadAsync(string name)
    {
        var result = await _repo.DownloadAsync($"images/{name}");
        return result?.Data;
    }

    public async Task<string?> GetPublicUriAsync(string name)
    {
        var props = await _repo.GetPropertiesAsync($"images/{name}", ContentInformationType.HttpHeaders);
        return props?.Uri;
    }

    public async Task ListAllAsync()
    {
        await foreach (var item in _repo.ListAsync(prefix: "images/", informationRetrieve: ContentInformationType.HttpHeaders))
            Console.WriteLine($"{item.Path}  {item.Options?.HttpHeaders?.ContentType}");
    }
}
```

---

## Notes

- **Container creation**: if the container does not exist it is created automatically on first use.
- **Prefix**: when set, all paths supplied to `IContentRepository` methods are transparently prefixed. For example, `UploadAsync("photo.png", …)` writes to `uploads/photo.png` inside the container.
- **Tags**: blob index tags require Storage account Gen2 and `Tags` in `ContentInformationType` to be retrieved. Not supported by Azure File Share or SharePoint backends.

    await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "supertest";
        x.Prefix = "site/";
        x.ConnectionString = configuration["ConnectionString:Storage"];
    },
    "blobstorage")
    .NoContext();

### How to use in a business class

    public class AllStorageTest
    {
        private readonly IContentRepositoryFactory _contentRepositoryFactory;
        private readonly Utility _utility;
        public AllStorageTest(IContentRepositoryFactory contentRepositoryFactory, Utility utility)
        {
            _contentRepositoryFactory = contentRepositoryFactory;
            _utility = utility;
        }
        
        public async Task ExecuteAsync()
        {
            var _contentRepository = _contentRepositoryFactory.Create("blobstorage");
            var file = await _utility.GetFileAsync();
            var name = "folder/file.png";
            var contentType = "images/png";
            var metadata = new Dictionary<string, string>()
            {
                { "name", "ale" }
            };
            var tags = new Dictionary<string, string>()
            {
                { "version", "1" }
            };
            var response = await _contentRepository.ExistAsync(name).NoContext();
            if (response)
            {
                await _contentRepository.DeleteAsync(name).NoContext();
                response = await _contentRepository.ExistAsync(name).NoContext();
            }
            Assert.False(response);
            response = await _contentRepository.UploadAsync(name, file.ToArray(), new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }, true).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.True(response);
            var options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.NotNull(options.Uri);
            foreach (var x in metadata)
            {
                Assert.Equal(x.Value, options.Options.Metadata[x.Key]);
            }
            foreach (var x in tags)
            {
                Assert.Equal(x.Value, options.Options.Tags[x.Key]);
            }
            Assert.Equal(contentType, options.Options.HttpHeaders.ContentType);
            metadata.Add("ale2", "single");
            response = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = metadata,
                Tags = tags
            }).NoContext();
            Assert.True(response);
            options = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All).NoContext();
            Assert.Equal("single", options.Options.Metadata["ale2"]);
            response = await _contentRepository.DeleteAsync(name).NoContext();
            Assert.True(response);
            response = await _contentRepository.ExistAsync(name).NoContext();
            Assert.False(response);
        }
    }
