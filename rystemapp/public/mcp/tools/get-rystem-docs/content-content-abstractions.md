# Rystem.Content.Abstractions

`Rystem.Content.Abstractions` contains the shared content API, the registration builder, and the cross-provider migration service.

It does not implement storage by itself. Real storage arrives through provider packages such as Blob, File Share, SharePoint, or InMemory.

## Installation

```bash
dotnet add package Rystem.Content.Abstractions
```

## What this package adds

This package defines:

- `IContentRepository`
- `IContentRepositoryBuilder`
- `ContentRepositoryOptions`
- `ContentRepositoryResult`
- `ContentRepositoryDownloadResult`
- `ContentInformationType`
- `IContentMigration`

At DI level, `AddContentRepository()` currently registers:

- `IContentMigration` as `Transient`

Provider registrations are added later through `With...Integration(...)` extension methods.

## Architecture

The core model is intentionally small:

- `IContentRepository` is the only storage contract
- `AddContentRepository()` returns an `IContentRepositoryBuilder`
- providers register named implementations through the shared factory system
- migrations resolve source and destination repositories from `IFactory<IContentRepository>`

In the current source tree and tests, named resolution is done with `IFactory<IContentRepository>`, not an `IContentRepositoryFactory` abstraction.

## Registration API

`IContentRepositoryBuilder` exposes three registration shapes:

| Method | Use when | Default lifetime |
| --- | --- | --- |
| `WithIntegration<TRepository>(name, lifetime)` | repository has no options object | `Transient` |
| `WithIntegration<TRepository, TOptions>(options, name, lifetime)` | repository needs synchronous options wiring | `Transient` |
| `WithIntegrationAsync<TRepository, TOptions, TConnection>(options, name, lifetime)` | repository setup is asynchronous and builds a connection wrapper | `Transient` |

The built-in Blob, File, and SharePoint providers sit on top of `WithIntegrationAsync(...)`. The InMemory provider uses `WithIntegration(...)` and overrides the lifetime to `Singleton`.

The provider-specific `WithBlobStorageIntegration(...)`, `WithFileStorageIntegration(...)`, `WithSharepointIntegration(...)`, and `WithInMemoryIntegration(...)` extensions are defined by the provider packages, not by this package itself.

## Minimal setup

```csharp
var repositories = services.AddContentRepository();

repositories.WithInMemoryIntegration("inmemory");
```

When you need a provider with async setup:

```csharp
var repositories = services.AddContentRepository();

await repositories.WithBlobStorageIntegrationAsync(options =>
{
    options.ContainerName = "supertest";
    options.ConnectionString = configuration["ConnectionString:Storage"];
}, "blobstorage");
```

## Consuming named repositories

The content tests resolve repositories like this:

```csharp
public sealed class ContentService
{
    private readonly IContentRepository _contentRepository;

    public ContentService(IFactory<IContentRepository> factory)
        => _contentRepository = factory.Create("blobstorage");
}
```

## `IContentRepository` contract

| Method | Return type | Purpose |
| --- | --- | --- |
| `ListAsync(prefix, downloadContent, informationRetrieve)` | `IAsyncEnumerable<ContentRepositoryDownloadResult>` | Enumerate files with optional prefix filtering |
| `DownloadAsync(path, informationRetrieve)` | `Task<ContentRepositoryDownloadResult?>` | Download bytes and optional metadata |
| `GetPropertiesAsync(path, informationRetrieve)` | `Task<ContentRepositoryResult?>` | Read metadata without downloading bytes |
| `UploadAsync(path, data, options, overwrite)` | `ValueTask<bool>` | Create or replace a file |
| `SetPropertiesAsync(path, options)` | `ValueTask<bool>` | Update headers, metadata, or tags |
| `DeleteAsync(path)` | `ValueTask<bool>` | Delete a file |
| `ExistAsync(path)` | `ValueTask<bool>` | Check whether a file exists |

Important caveat: `overwrite` is part of the shared interface, but not every provider enforces it the same way.

## Shared models

### `ContentRepositoryOptions`

```csharp
var options = new ContentRepositoryOptions
{
    HttpHeaders = new ContentRepositoryHttpHeaders
    {
        ContentType = "image/png",
        CacheControl = "max-age=3600",
        ContentDisposition = "attachment; filename=image.png"
    },
    Metadata = new Dictionary<string, string>
    {
        ["author"] = "alice"
    },
    Tags = new Dictionary<string, string>
    {
        ["version"] = "1"
    }
};
```

### `ContentInformationType`

| Value | Meaning |
| --- | --- |
| `None` | no extra metadata |
| `HttpHeaders` | include content headers |
| `Metadata` | include metadata dictionary |
| `Tags` | include tags when supported |
| `All` | `HttpHeaders | Metadata | Tags` |

### Result models

| Type | Properties |
| --- | --- |
| `ContentRepositoryResult` | `Path`, `Uri`, `Options` |
| `ContentRepositoryDownloadResult` | everything in `ContentRepositoryResult` plus `Data` |

`Path` and `Uri` are provider-defined. Do not assume they have identical semantics across Blob, File Share, SharePoint, and InMemory.

## Migration service

`IContentMigration` copies content between named providers.

This example matches the usage style in `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Integrations/AllStorageTest.cs`.

```csharp
ContentMigrationResult result = await contentMigration.MigrateAsync(
    sourceName: "inmemory",
    destinationName: "filestorage",
    settings: options =>
    {
        options.Prefix = "Test/Folder1/";
        options.OverwriteIfExists = true;
        options.Predicate = item => item.Path?.Contains("fileName6") != true;
        options.ModifyDestinationPath = path => path.Replace("Folder2", "Folder3");
    });
```

Available settings:

| Property | Default | Meaning |
| --- | --- | --- |
| `Prefix` | `null` | restrict source enumeration |
| `Predicate` | `null` | skip items that do not match |
| `OverwriteIfExists` | `false` | pass overwrite intent to destination upload |
| `OnErrorContinue` | `true` | keep going after per-file errors |
| `ModifyDestinationPath` | `null` | rewrite the output path |

## Migration behavior notes

The current implementation is a straightforward copy loop:

- it resolves both repositories through `IFactory<IContentRepository>`
- it enumerates source items with `ListAsync(..., downloadContent: false, ContentInformationType.None)`
- it downloads each matched item with `ContentInformationType.All`
- it uploads the downloaded bytes plus options to the destination repository

Important caveats from the source:

- `NotMigratedPaths` exists on `ContentMigrationResult`, but the current implementation never fills it
- when an upload returns `false`, the result currently lands in `NotContentPaths`
- there is no provider-native server-side copy or batching logic

## Writing a custom provider

If you want your own backend, implement `IContentRepository` and register it through the builder.

```csharp
internal sealed class MyContentRepository : IContentRepository
{
    public IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(
        string? prefix = null,
        bool downloadContent = false,
        ContentInformationType informationRetrieve = ContentInformationType.None,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<ContentRepositoryDownloadResult?> DownloadAsync(
        string path,
        ContentInformationType informationRetrieve = ContentInformationType.None,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<ContentRepositoryResult?> GetPropertiesAsync(
        string path,
        ContentInformationType informationRetrieve = ContentInformationType.All,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask<bool> UploadAsync(
        string path,
        byte[] data,
        ContentRepositoryOptions? options = null,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask<bool> SetPropertiesAsync(
        string path,
        ContentRepositoryOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

Then register it:

```csharp
services
    .AddContentRepository()
    .WithIntegration<MyContentRepository>("custom");
```

## Related packages

- `Rystem.Content.Infrastructure.Storage.Blob`
- `Rystem.Content.Infrastructure.Storage.File`
- `Rystem.Content.Infrastructure.M365.Sharepoint`
- `Rystem.Content.Infrastructure.InMemory`

Use this package when you want the contract and migration layer; add a provider package when you want real storage.
