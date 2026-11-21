# Rystem.Content.Infrastructure.Storage.File

This provider adds Azure File Share support to the Content framework.

Repo note: the folder name is `Rystem.Content.Infrastructure.Azure.Storage.File`, but the current NuGet package id is `Rystem.Content.Infrastructure.Storage.File`.

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Storage.File
```

## Architecture

The provider is a thin wrapper over `ShareClient` and `ShareFileClient`.

- async registration creates the share immediately
- if `Prefix` is configured, registration also creates the prefix directories
- uploads create missing intermediate directories on demand
- headers and metadata are mapped to Azure File Share APIs

The public registration extensions live in `BuilderExtensions/ContentRepositoryBuilderExtensions.cs`, while the runtime behavior is in `FileStorage/FileStorageRepository.cs`.

## Registration API

| Method | Default lifetime | Notes |
| --- | --- | --- |
| `WithFileStorageIntegrationAsync(options, name, serviceLifetime)` | `Transient` | preferred path because setup is async |
| `WithFileStorageIntegration(options, name, serviceLifetime)` | `Transient` | sync wrapper over the async implementation |

## Example

This matches the unit-test startup in `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Startup.cs`.

```csharp
var repositories = builder.Services.AddContentRepository();

await repositories.WithFileStorageIntegrationAsync(options =>
{
    options.ShareName = "supertest";
    options.Prefix = "site/";
    options.ConnectionString = builder.Configuration["ConnectionString:Storage"];
}, "filestorage");
```

Resolve and use it:

```csharp
public sealed class FileShareDocumentService
{
    private readonly IContentRepository _repository;

    public FileShareDocumentService(IFactory<IContentRepository> factory)
        => _repository = factory.Create("filestorage");

    public ValueTask<bool> SaveAsync(string path, byte[] data)
        => _repository.UploadAsync(path, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = "application/pdf"
            },
            Metadata = new Dictionary<string, string>
            {
                ["department"] = "legal"
            }
        });
}
```

## Settings

`FileStorageConnectionSettings` exposes:

| Property | Notes |
| --- | --- |
| `ConnectionString` | used when present |
| `EndpointUri` | used for managed identity mode |
| `ManagedIdentityClientId` | null means `DefaultAzureCredential`; otherwise `ManagedIdentityCredential` |
| `ShareName` | used in connection-string mode |
| `Prefix` | prepended to every logical path and pre-created as directories |
| `ClientOptions` | passed to `ShareClient` |
| `ClientCreateOptions` | used when creating the share |
| `Permissions` | passed to `SetAccessPolicyAsync(...)` |
| `Conditions` | passed to `SetAccessPolicyAsync(...)` |
| `IsPublic` | present on the settings type, but not used by the current implementation |

## Managed identity note

In managed identity mode, the provider constructs the client as:

```csharp
new ShareClient(settings.EndpointUri, credential, settings.ClientOptions)
```

So `EndpointUri` needs to point to the share itself. In that path, `ShareName` is ignored.

## Provider behavior

- `UploadAsync` creates intermediate directories if they do not exist
- `SetPropertiesAsync` maps HTTP headers and metadata
- `GetPropertiesAsync` reads headers and metadata
- `Tags` are not supported and stay `null`

Compared with Blob Storage, this provider is more directory-oriented and less metadata-rich.

## Important caveats

### Listing is not recursive

`ListAsync(...)` enumerates a single directory scope and skips directory entries. It does not recursively walk nested folders the way the SharePoint provider does.

### `overwrite` is effectively ignored

If the file already exists, `UploadAsync(...)` resizes it and uploads the new bytes regardless of the `overwrite` parameter.

If you need strict create-only behavior, check `ExistAsync(...)` first.

### Path and URI semantics are inconsistent

The current implementation returns different shapes depending on the method:

- `DownloadAsync` returns `Path = path` and `Uri = path`
- `GetPropertiesAsync` returns `Path = fileClient.Name`
- `ListAsync` returns `Uri = fileClient.Uri.ToString()` and a relative path

So treat `Path` and `Uri` as provider-defined output rather than a normalized contract.

## When to use this provider

Use it when you want:

- Azure File Share as the backing store
- directory-style organization
- native file headers and metadata
- automatic directory creation during upload

It is less appropriate when you need native tag support or Blob-like path semantics.
