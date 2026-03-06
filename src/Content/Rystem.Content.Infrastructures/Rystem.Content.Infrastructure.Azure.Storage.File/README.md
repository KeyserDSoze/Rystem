# Rystem.Content.Infrastructure.Azure.Storage.File

[![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.Azure.Storage.File)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Azure.Storage.File)

Azure File Share backend for [Rystem Content Framework](../Rystem.Content.Abstractions). Stores files in an Azure Storage file share — ideal for SMB-mounted shares, legacy lift-and-shift applications, or workloads that need directory-style file semantics.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Azure.Storage.File
```

---

## Registration

### Connection string

```csharp
await services
    .AddContentRepository()
    .WithFileStorageIntegrationAsync(x =>
    {
        x.ShareName        = "documents";
        x.Prefix           = "site/";    // optional path prefix
        x.ConnectionString = configuration["Storage:ConnectionString"];
    }, "filestorage")
    .NoContext();
```

### Managed identity (passwordless)

```csharp
await services
    .AddContentRepository()
    .WithFileStorageIntegrationAsync(x =>
    {
        x.EndpointUri             = new Uri("https://<account>.file.core.windows.net");
        x.ManagedIdentityClientId = "<user-assigned-mi-client-id>";
        x.ShareName               = "documents";
    }, "filestorage")
    .NoContext();
```

### Synchronous variant

```csharp
services
    .AddContentRepository()
    .WithFileStorageIntegration(x =>
    {
        x.ShareName        = "documents";
        x.ConnectionString = configuration["Storage:ConnectionString"];
    }, "filestorage");
```

---

## `FileStorageConnectionSettings` reference

| Property | Type | Description |
|----------|------|-------------|
| `ConnectionString` | `string?` | Azure Storage connection string |
| `EndpointUri` | `Uri?` | File service endpoint (used with managed identity) |
| `ManagedIdentityClientId` | `string?` | Client ID for user-assigned managed identity |
| `ShareName` | `string?` | Target file share name |
| `Prefix` | `string?` | Path prefix prepended to every file path |
| `IsPublic` | `bool` | Whether the share is created with public access |
| `ClientOptions` | `ShareClientOptions?` | Advanced Azure SDK client options |
| `ClientCreateOptions` | `ShareCreateOptions?` | Options applied when creating the share |
| `Permissions` | `List<ShareSignedIdentifier>?` | Stored access policies on the share |
| `Conditions` | `ShareFileRequestConditions?` | Conditional request headers (ETag, Last-Modified) |

---

## Usage

```csharp
public sealed class DocumentService
{
    private readonly IContentRepository _repo;

    public DocumentService(IContentRepositoryFactory factory)
        => _repo = factory.Create("filestorage");

    public async Task SaveAsync(string relativePath, byte[] data)
    {
        await _repo.UploadAsync(relativePath, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders { ContentType = "application/pdf" },
            Metadata    = new Dictionary<string, string> { { "department", "hr" } }
        });
    }

    public async Task<byte[]?> ReadAsync(string relativePath)
    {
        var result = await _repo.DownloadAsync(relativePath);
        return result?.Data;
    }

    public async Task ListFolderAsync(string folder)
    {
        await foreach (var item in _repo.ListAsync(prefix: folder))
            Console.WriteLine(item.Path);
    }
}
```

---

## Notes

- **Share creation**: if the file share does not exist it is created automatically on first use.
- **Tags**: Azure File Share does not support blob index tags — `Tags` in `ContentRepositoryOptions` is silently ignored.
- **Prefix**: works identically to the Blob backend — transparently prepended to all paths.
- **Comparison with Blob**: prefer Blob Storage for internet-facing CDN assets. Prefer File Share when you need SMB mounting or per-directory permissions.

    await services
        .AddContentRepository()
        .WithFileStorageIntegrationAsync(x =>
        {
            x.ShareName = "supertest";
            x.Prefix = "site/";
            x.ConnectionString = configuration["ConnectionString:Storage"];
        },
        "filestorage")
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
            var _contentRepository = _contentRepositoryFactory.Create("filestorage");
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
