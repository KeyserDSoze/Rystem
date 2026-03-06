# Rystem.Content.Infrastructure.M365.Sharepoint

[![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.M365.Sharepoint)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.M365.Sharepoint)

SharePoint Online backend for [Rystem Content Framework](../Rystem.Content.Abstractions). Reads and writes files in a SharePoint document library using a Microsoft 365 app registration (client credentials flow).

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint
```

---

## Prerequisites — App Registration

1. Create an Azure AD app registration.
2. Under **API permissions** → **Microsoft Graph** → grant **Application** (not delegated) permission: `Files.ReadWrite.All` or `Sites.ReadWrite.All`.
3. Create a client secret and note the `TenantId`, `ClientId`, `ClientSecret`.

> To get the SharePoint site ID:  
> `GET https://<tenant>.sharepoint.com/sites/<site-url>/_api/site/id`

---

## Registration

```csharp
await services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId     = configuration["Sharepoint:TenantId"];
        x.ClientId     = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];

        // Choose ONE of the mapping methods below:
        x.MapWithSiteNameAndDocumentLibraryName("MySite", "Documents");
    }, "sharepoint")
    .NoContext();
```

### Synchronous variant

```csharp
services
    .AddContentRepository()
    .WithSharepointIntegration(x =>
    {
        x.TenantId     = configuration["Sharepoint:TenantId"];
        x.ClientId     = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithSiteNameAndDocumentLibraryName("MySite", "Documents");
    }, "sharepoint");
```

---

## `SharepointConnectionSettings` — site mapping methods

Call **exactly one** of these methods to specify which document library to use:

| Method | When to use |
|--------|-------------|
| `MapWithSiteNameAndDocumentLibraryName(siteName, libraryName)` | You know the site name and library name (most common) |
| `MapWithSiteIdAndDocumentLibraryId(siteId, libraryId)` | You have both GUID IDs (most specific) |
| `MapWithSiteIdAndDocumentLibraryName(siteId, libraryName)` | You have the site GUID but only the library name |
| `MapWithRootSiteAndDocumentLibraryName(libraryName)` | Target a library on the root SharePoint site |
| `MapOnlyDocumentLibraryId(libraryId)` | Target a library by GUID without specifying a site |
| `MapOnlyDocumentLibraryName(libraryName)` | Target a library by name without specifying a site |

### Other settings

| Property | Type | Description |
|----------|------|-------------|
| `TenantId` | `string?` | Azure AD tenant ID |
| `ClientId` | `string?` | App registration client ID |
| `ClientSecret` | `string?` | App registration client secret |

---

## Usage

```csharp
public sealed class SharepointFileSvc
{
    private readonly IContentRepository _repo;

    public SharepointFileSvc(IContentRepositoryFactory factory)
        => _repo = factory.Create("sharepoint");

    public async Task UploadAsync(string relativePath, byte[] data)
    {
        await _repo.UploadAsync(relativePath, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders { ContentType = "application/pdf" },
            Metadata    = new Dictionary<string, string> { { "department", "legal" } }
        });
    }

    public async Task<byte[]?> DownloadAsync(string relativePath)
    {
        var result = await _repo.DownloadAsync(relativePath);
        return result?.Data;
    }

    public async Task ListFolderAsync(string folderPath)
    {
        await foreach (var item in _repo.ListAsync(prefix: folderPath))
            Console.WriteLine($"{item.Path}  {item.Options?.HttpHeaders?.ContentType}");
    }
}
```

---

## Notes

- **Tags**: SharePoint does not support arbitrary blob index tags — the `Tags` property in `ContentRepositoryOptions` is ignored.
- **Metadata**: SharePoint column metadata is partially supported via the `Metadata` dictionary; only columns that already exist in the list schema are written.
- **Path format**: use forward-slash paths like `"Folder/SubFolder/file.pdf"`. The integration maps these to SharePoint folder paths automatically.
- **Throughput**: SharePoint throttles requests at tenant level. For bulk uploads consider adding retry logic or using the migration tool with `OnErrorContinue = true`.

    await services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithSiteNameAndDocumentLibraryName("TestNumberOne", "Foglione");
        //x.MapWithRootSiteAndDocumentLibraryName("Foglione");
        //x.MapWithSiteIdAndDocumentLibraryId(configuration["Sharepoint:SiteId"],
        //    configuration["Sharepoint:DocumentLibraryId"]);
    }, "sharepoint")
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
            var _contentRepository = _contentRepositoryFactory.Create("sharepoint");
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
