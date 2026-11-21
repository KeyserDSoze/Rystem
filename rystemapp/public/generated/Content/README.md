# Rystem Content Framework

`Rystem Content Framework` is a thin file-storage abstraction over multiple backends.

You write against `IContentRepository`, register one or more named integrations, and optionally move content between providers with `IContentMigration`.

## Packages

| Package | Purpose |
| --- | --- |
| [`Rystem.Content.Abstractions`](./Rystem.Content.Abstractions/README.md) | Core contracts, DI builder, and migration service |
| [`Rystem.Content.Infrastructure.Storage.Blob`](./Rystem.Content.Infrastructures/Rystem.Content.Infrastructure.Azure.Storage.Blob/README.md) | Azure Blob Storage provider |
| [`Rystem.Content.Infrastructure.Storage.File`](./Rystem.Content.Infrastructures/Rystem.Content.Infrastructure.Azure.Storage.File/README.md) | Azure File Share provider |
| [`Rystem.Content.Infrastructure.M365.Sharepoint`](./Rystem.Content.Infrastructures/Rystem.Content.Infrastructure.M365.Sharepoint/README.md) | SharePoint Online provider |
| [`Rystem.Content.Infrastructure.InMemory`](./Rystem.Content.Infrastructures/Rystem.Content.Infrastructure.InMemory/README.md) | In-memory test and local-dev provider |

Note: the Blob and File provider folders still include `Azure.Storage` in their repo path, but the current NuGet package ids are `Rystem.Content.Infrastructure.Storage.Blob` and `Rystem.Content.Infrastructure.Storage.File`.

## Architecture

The framework is intentionally small:

1. call `AddContentRepository()`
2. register one or more integrations with `With...Integration(...)`
3. resolve a backend by name with `IFactory<IContentRepository>`
4. use the same content API across providers
5. optionally copy content between backends with `IContentMigration`

The shared API is:

- `UploadAsync`
- `DownloadAsync`
- `GetPropertiesAsync`
- `ListAsync`
- `SetPropertiesAsync`
- `ExistAsync`
- `DeleteAsync`

## Quick start

This mirrors the registration shape used in `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Startup.cs`.

The `WithBlobStorageIntegrationAsync(...)` extension comes from the Blob provider package, not from `Rystem.Content.Abstractions` alone.

```csharp
var repositories = builder.Services.AddContentRepository();

await repositories.WithBlobStorageIntegrationAsync(options =>
{
    options.ContainerName = "supertest";
    options.Prefix = "site/";
    options.ConnectionString = builder.Configuration["ConnectionString:Storage"];
}, "blobstorage");

repositories.WithInMemoryIntegration("inmemory");
```

Resolve the backend the same way the integration tests do:

```csharp
public sealed class AssetService
{
    private readonly IContentRepository _repository;

    public AssetService(IFactory<IContentRepository> factory)
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
            }
        });
}
```

## Provider capability differences

The abstraction is shared, but provider behavior is not fully normalized.

| Provider | Headers | Metadata | Tags | Important caveat |
| --- | --- | --- | --- | --- |
| Blob | Native | Native | Native | `overwrite` handling is weaker than the signature suggests |
| File Share | Native | Native | No | `overwrite` is effectively ignored and listing is directory-based |
| SharePoint | Emulated | Emulated | Emulated | options are serialized into `DriveItem.Description`, not mapped to native fields |
| InMemory | In-memory | In-memory | In-memory | good for tests, but not a fidelity model for remote providers |

Also keep in mind:

- `Path` and `Uri` are provider-defined and are not identical across backends
- missing-file behavior is provider-specific
- `ContentInformationType.Tags` is not available everywhere
- list depth and prefix behavior differ by provider

## Migration

`IContentMigration` is registered by `AddContentRepository()` and copies content across named providers.

```csharp
ContentMigrationResult result = await migration.MigrateAsync(
    sourceName: "blobstorage",
    destinationName: "sharepoint",
    settings: options =>
    {
        options.Prefix = "documents/";
        options.OverwriteIfExists = true;
        options.Predicate = item => item.Path?.EndsWith(".pdf") == true;
        options.ModifyDestinationPath = path => path.Replace("documents/", "archive/");
    });
```

The migration implementation is a simple copy loop:

- source enumeration uses `ListAsync(prefix, downloadContent: false, ContentInformationType.None)`
- each matched item is downloaded with `ContentInformationType.All`
- destination writes use `UploadAsync(...)`

So migration is convenient, but it is not a provider-native bulk-copy pipeline.

## Grounded by tests

The most useful reference files for this area are:

- `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Startup.cs`
- `src/Content/Rystem.Content.Tests/Rystem.Content.UnitTest/Integrations/AllStorageTest.cs`
- `src/Content/Rystem.Content.Abstractions/Interfaces/IContentRepository.cs`
- `src/Content/Rystem.Content.Abstractions/Migrations/ContentMigration.cs`

Start with `Rystem.Content.Abstractions` for the shared contract, then pick the provider README that matches your storage backend.
