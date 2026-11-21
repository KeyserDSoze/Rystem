# Rystem.Content.Infrastructure.Storage.Blob

This provider adds Azure Blob Storage support to the Content framework.

Repo note: the folder name is `Rystem.Content.Infrastructure.Azure.Storage.Blob`, but the current NuGet package id is `Rystem.Content.Infrastructure.Storage.Blob`.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Storage.Blob
```

## Architecture

The provider is a thin wrapper over `BlobContainerClient`.

- registration builds a single container client
- async registration creates the container immediately
- `Prefix` is prepended to every blob name
- `SetPropertiesAsync` maps to blob headers, metadata, and tags

The registration shape is defined in `src/Content/Rystem.Content.Infrastructures/Rystem.Content.Infrastructure.Azure.Storage.Blob/BuilderExtensions/ContentRepositoryBuilderExtensions.cs` and the runtime behavior lives in `BlobStorage/BlobStorageRepository.cs`.

## Registration API

| Method | Default lifetime | Notes |
| --- | --- | --- |
| `WithBlobStorageIntegrationAsync(options, name, serviceLifetime)` | `Transient` | preferred path because setup is async |
| `WithBlobStorageIntegration(options, name, serviceLifetime)` | `Transient` | sync wrapper over the async implementation |

## Example

This matches the unit-test startup in `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Startup.cs`.

```csharp
var repositories = builder.Services.AddContentRepository();

await repositories.WithBlobStorageIntegrationAsync(options =>
{
    options.ContainerName = "supertest";
    options.Prefix = "site/";
    options.ConnectionString = builder.Configuration["ConnectionString:Storage"];
    options.UploadOptions = new BlobUploadOptions
    {
        AccessTier = AccessTier.Cool
    };
}, "blobstorage");
```

Resolve and use it:

```csharp
public sealed class BlobAssetService
{
    private readonly IContentRepository _repository;

    public BlobAssetService(IFactory<IContentRepository> factory)
        => _repository = factory.Create("blobstorage");

    public ValueTask<bool> UploadAsync(string path, byte[] data)
        => _repository.UploadAsync(path, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = "image/png"
            },
            Metadata = new Dictionary<string, string>
            {
                ["source"] = "upload"
            },
            Tags = new Dictionary<string, string>
            {
                ["version"] = "1"
            }
        });
}
```

## Settings

`BlobStorageConnectionSettings` exposes:

| Property | Notes |
| --- | --- |
| `ConnectionString` | used when present |
| `EndpointUri` | used for managed identity mode |
| `ManagedIdentityClientId` | null means `DefaultAzureCredential`; otherwise `ManagedIdentityCredential` |
| `ContainerName` | used in connection-string mode |
| `Prefix` | prepended to every blob path |
| `IsPublic` | when `true`, registration sets public blob access on the container |
| `ClientOptions` | passed to `BlobContainerClient` |
| `UploadOptions` | reused by `UploadAsync(...)` |

## Managed identity note

In managed identity mode, the provider constructs the client as:

```csharp
new BlobContainerClient(settings.EndpointUri, credential, settings.ClientOptions)
```

So `EndpointUri` needs to point to the container itself, not just the storage account. In that path, `ContainerName` is ignored.

## Provider behavior

- `UploadAsync` writes the blob and then calls `SetPropertiesAsync(...)`
- `GetPropertiesAsync` reads blob properties and optionally tags
- `DownloadAsync` downloads content and can also include headers, metadata, and tags
- `ListAsync` can optionally download content for each blob

Native Blob features supported by this provider:

- HTTP headers
- metadata
- blob tags

## Important caveats

### Prefix handling is not fully uniform

- `ListAsync` strips the configured prefix from the returned `Path`
- `DownloadAsync` returns `Path = blobClient.Name`, which still includes the prefix
- `GetPropertiesAsync` does the same

So if you rely on `Path`, be aware that list results and direct point-lookups are not fully normalized.

### `overwrite` is weaker than it looks

`UploadAsync(path, data, options, overwrite)` accepts an `overwrite` flag, but the current implementation always calls `blobClient.UploadAsync(...)` with the configured upload options.

If you need strict create-only semantics when `overwrite == false`, enforce that in your application with `ExistAsync(...)` before uploading.

### `ContentInformationType.None` still returns an options object

For Blob and File providers, `informationRetrieve == None` returns an empty `ContentRepositoryOptions` instance rather than `null`.

## When to use this provider

Use it when you want:

- Azure Blob Storage as the backing store
- native blob metadata and tag support
- optional public container access
- simple path-prefix partitioning inside one container

Avoid assuming that all providers behave exactly like Blob Storage. The Content abstraction keeps the method names aligned, not every detail of path, URI, and overwrite semantics.
