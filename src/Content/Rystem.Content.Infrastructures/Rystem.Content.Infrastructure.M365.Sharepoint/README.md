# Rystem.Content.Infrastructure.M365.Sharepoint

This provider adds SharePoint Online support to the Content framework through Microsoft Graph.

It models one SharePoint document library as one `IContentRepository` registration.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint
```

## Authentication model

The current implementation uses Microsoft Graph with client credentials.

You need an app registration with application permissions such as:

- `Files.ReadWrite.All`
- or `Sites.ReadWrite.All`

And you need:

- `TenantId`
- `ClientId`
- `ClientSecret`

## Architecture

Async registration does real setup work:

- it creates a `GraphServiceClient`
- resolves the target site and document library ids
- when a site-scoped library name is configured and the library is missing, it creates the document library

The registration extensions live in `BuilderExtensions/ContentRepositoryBuilderExtensions.cs`, the mapping rules in `Options/SharepointConnectionSettings.cs`, and the runtime behavior in `SharepointRepository/SharepointRepository.cs` plus `SharepointServiceClient/SharepointServiceClientFactory.cs`.

## Registration API

| Method | Default lifetime | Notes |
| --- | --- | --- |
| `WithSharepointIntegrationAsync(options, name, serviceLifetime)` | `Transient` | preferred path because setup is async |
| `WithSharepointIntegration(options, name, serviceLifetime)` | `Transient` | sync wrapper over the async implementation |

## Mapping methods

Call exactly one of these on `SharepointConnectionSettings`:

| Method | Meaning |
| --- | --- |
| `MapWithSiteIdAndDocumentLibraryId(siteId, documentLibraryId)` | use exact ids |
| `MapWithSiteIdAndDocumentLibraryName(siteId, documentLibraryName)` | site id plus library name |
| `MapWithSiteNameAndDocumentLibraryName(siteName, documentLibraryName)` | find the site by name, then find or create the library |
| `MapWithRootSiteAndDocumentLibraryName(documentLibraryName)` | use the root site and a library name |
| `MapOnlyDocumentLibraryId(documentLibraryId)` | search by library id only |
| `MapOnlyDocumentLibraryName(documentLibraryName)` | search by library name only |

If your tenant has ambiguous site names, prefer the id-based methods.

## Example

This matches `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Startup.cs`.

```csharp
var repositories = builder.Services.AddContentRepository();

await repositories.WithSharepointIntegrationAsync(options =>
{
    options.TenantId = builder.Configuration["Sharepoint:TenantId"];
    options.ClientId = builder.Configuration["Sharepoint:ClientId"];
    options.ClientSecret = builder.Configuration["Sharepoint:ClientSecret"];
    options.MapWithSiteNameAndDocumentLibraryName(
        builder.Configuration["Sharepoint:SiteName"]!,
        builder.Configuration["Sharepoint:DocumentLibraryName"]!);
}, "sharepoint");
```

Resolve and use it:

```csharp
public sealed class SharepointContentService
{
    private readonly IContentRepository _repository;

    public SharepointContentService(IFactory<IContentRepository> factory)
        => _repository = factory.Create("sharepoint");

    public ValueTask<bool> UploadAsync(string path, byte[] data)
        => _repository.UploadAsync(path, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = "application/pdf"
            },
            Metadata = new Dictionary<string, string>
            {
                ["department"] = "legal"
            },
            Tags = new Dictionary<string, string>
            {
                ["version"] = "1"
            }
        });
}
```

## Provider behavior

- `UploadAsync` writes file content through `ItemWithPath(path).Content.PutAsync(...)`
- `ExistAsync` and `GetPropertiesAsync` query the file through Graph
- `ListAsync` recursively enumerates folders
- `DeleteAsync` returns `true` when the item does not exist

This provider is the most conceptually different from Blob and File Share because it does not map `ContentRepositoryOptions` to native SharePoint storage primitives.

## Important caveats

### Options are stored in `DriveItem.Description`

`SetPropertiesAsync(...)` serializes the whole `ContentRepositoryOptions` object into `DriveItem.Description`.

`GetPropertiesAsync(...)` reads that description back and deserializes it.

Practical consequence:

- headers are not written as real SharePoint HTTP response headers
- metadata is not mapped to list columns by this provider
- tags are not native SharePoint tags

They round-trip because the provider stores its own JSON payload in the description field.

### Site-name lookup is fuzzy

When you use `MapWithSiteNameAndDocumentLibraryName(...)`, the provider searches Graph sites and takes the first match.

If you have multiple similarly named sites, use the id-based mapping methods instead.

### Missing libraries can be created automatically

When a site is known and the requested document library name does not exist, the async setup path creates a document library for you.

### Listing is recursive

Unlike the File Share provider, `ListAsync(...)` descends into child folders.

## When to use this provider

Use it when you want:

- SharePoint document libraries behind the shared content API
- Microsoft 365 app-registration authentication
- recursive listing over folder structures
- convenient cross-provider migration targets

Be careful if you need native SharePoint field or header semantics, because this provider currently stores repository options in the item description instead.
