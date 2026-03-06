# Rystem.Content.Abstractions

[![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Abstractions)](https://www.nuget.org/packages/Rystem.Content.Abstractions)

Core contracts, models, and migration tooling for the Rystem Content Framework. Provides the `IContentRepository` interface and DI wiring so your business code never depends on a specific storage backend.

## Installation

```bash
dotnet add package Rystem.Content.Abstractions
```

## What this package provides

- `IContentRepository` — unified file storage interface (upload, download, list, delete, exist, properties)
- `IContentRepositoryFactory` — named-instance factory when multiple backends are registered
- `IContentMigration` — copy/migrate files between any two registered backends
- `ContentRepositoryOptions` — metadata, tags, HTTP headers for uploaded files
- `ContentInformationType` — flags enum controlling which metadata is fetched

---

## DI registration

```csharp
services
    .AddContentRepository()
    .WithIntegration<MyCustomIntegration>("custom", ServiceLifetime.Singleton);
```

Built-in backends add their own `WithXxxIntegration` extension methods on top of `IContentRepositoryBuilder`.

### Custom integration

Implement `IContentRepository` and register it:

```csharp
internal sealed class MyStorageIntegration : IContentRepository
{
    public IAsyncEnumerable<ContentRepositoryDownloadResult> ListAsync(
        string? prefix = null,
        bool downloadContent = false,
        ContentInformationType informationRetrieve = ContentInformationType.None,
        CancellationToken cancellationToken = default)
    { /* … */ }

    public Task<ContentRepositoryDownloadResult?> DownloadAsync(
        string path,
        ContentInformationType informationRetrieve = ContentInformationType.None,
        CancellationToken cancellationToken = default)
    { /* … */ }

    public Task<ContentRepositoryResult?> GetPropertiesAsync(
        string path,
        ContentInformationType informationRetrieve = ContentInformationType.All,
        CancellationToken cancellationToken = default)
    { /* … */ }

    public ValueTask<bool> UploadAsync(
        string path,
        byte[] data,
        ContentRepositoryOptions? options = default,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    { /* … */ }

    public ValueTask<bool> SetPropertiesAsync(
        string path,
        ContentRepositoryOptions? options = default,
        CancellationToken cancellationToken = default)
    { /* … */ }

    public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    { /* … */ }

    public ValueTask<bool> ExistAsync(string path, CancellationToken cancellationToken = default)
    { /* … */ }
}
```

---

## `IContentRepository` method reference

| Method | Return | Description |
|--------|--------|-------------|
| `UploadAsync(path, data, options?, overwrite)` | `ValueTask<bool>` | Upload a file. Set `overwrite = false` to skip if already exists |
| `DownloadAsync(path, informationRetrieve?)` | `Task<ContentRepositoryDownloadResult?>` | Download file bytes + optional metadata |
| `ExistAsync(path)` | `ValueTask<bool>` | Check whether a file exists |
| `DeleteAsync(path)` | `ValueTask<bool>` | Delete a file |
| `GetPropertiesAsync(path, informationRetrieve?)` | `Task<ContentRepositoryResult?>` | Fetch URI, metadata, tags, headers (no bytes) |
| `SetPropertiesAsync(path, options?)` | `ValueTask<bool>` | Update metadata, tags, or HTTP headers |
| `ListAsync(prefix?, downloadContent?, informationRetrieve?)` | `IAsyncEnumerable<ContentRepositoryDownloadResult>` | Enumerate files, optionally filtering by path prefix |

---

## Models

### `ContentRepositoryOptions`

Passed to `UploadAsync` and `SetPropertiesAsync`:

```csharp
var options = new ContentRepositoryOptions
{
    HttpHeaders = new ContentRepositoryHttpHeaders
    {
        ContentType       = "image/png",
        CacheControl      = "max-age=3600",
        ContentEncoding   = "gzip",
        ContentLanguage   = "en",
        ContentDisposition = "attachment; filename=photo.png",
    },
    Metadata = new Dictionary<string, string> { { "author", "alice" } },
    Tags     = new Dictionary<string, string> { { "version", "1" } }
};
```

### `ContentRepositoryResult`

Returned by `GetPropertiesAsync`:

| Property | Type | Description |
|----------|------|-------------|
| `Uri` | `string?` | Public or SAS URL of the file |
| `Path` | `string?` | Storage path |
| `Options` | `ContentRepositoryOptions?` | Metadata, tags, HTTP headers |

### `ContentRepositoryDownloadResult`

Extends `ContentRepositoryResult`, adds:

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `byte[]?` | File content bytes |

### `ContentInformationType` flags

| Value | Description |
|-------|-------------|
| `None` | No extra metadata fetched (fastest) |
| `HttpHeaders` | Fetch Content-Type, Cache-Control, etc. |
| `Metadata` | Fetch key-value metadata dictionary |
| `Tags` | Fetch blob/file tags |
| `All` | Fetch everything (`HttpHeaders \| Metadata \| Tags`) |

---

## Injecting `IContentRepository`

### Single backend

When only one backend is registered you can inject `IContentRepository` directly:

```csharp
public sealed class DocumentService
{
    private readonly IContentRepository _repo;

    public DocumentService(IContentRepository repo)
        => _repo = repo;
}
```

### Multiple named backends

Use `IContentRepositoryFactory` to resolve by name:

```csharp
services
    .AddContentRepository()
    .WithIntegration<BlobBackend>("blob")
    .WithIntegration<SharepointBackend>("sharepoint");

// In constructor:
public DocumentService(IContentRepositoryFactory factory)
{
    var blob       = factory.Create("blob");
    var sharepoint = factory.Create("sharepoint");
}
```

---

## Migration tool

Copy or move files between any two registered backends:

```csharp
// Inject IContentMigration (registered automatically by AddContentRepository)
public sealed class StorageMigrator
{
    private readonly IContentMigration _migration;
    public StorageMigrator(IContentMigration migration) => _migration = migration;

    public async Task RunAsync()
    {
        ContentMigrationResult result = await _migration.MigrateAsync(
            sourceName:      "blob",
            destinationName: "sharepoint",
            settings: s =>
            {
                s.Prefix             = "documents/";        // only files under this prefix
                s.OverwriteIfExists  = true;
                s.OnErrorContinue    = true;                // skip errors instead of throwing
                s.Predicate          = x => x.Path?.EndsWith(".pdf") == true;  // custom filter
                s.ModifyDestinationPath = path => path.Replace("documents/", "Archive/"); // remap paths
            });

        Console.WriteLine($"Migrated:  {result.MigratedPaths.Count}");
        Console.WriteLine($"Blocked:   {result.BlockedByPredicatePaths.Count}");
        Console.WriteLine($"Errors:    {result.NotMigratedPathsForErrors.Count}");
    }
}
```

### `ContentMigrationSettings` reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Prefix` | `string?` | `null` | Filter source files to those whose path starts with this prefix |
| `Predicate` | `Func<ContentRepositoryDownloadResult, bool>?` | `null` | Additional per-file filter; return `false` to skip |
| `OverwriteIfExists` | `bool` | `false` | Overwrite destination file if it already exists |
| `OnErrorContinue` | `bool` | `true` | Continue to next file on error instead of throwing |
| `ModifyDestinationPath` | `Func<string, string>?` | `null` | Transform source path before writing to destination |

### `ContentMigrationResult` reference

| Property | Description |
|----------|-------------|
| `MigratedPaths` | Files successfully copied |
| `NotMigratedPaths` | Files skipped (already exist and `OverwriteIfExists = false`) |
| `NotContentPaths` | Entries that had no downloadable content |
| `BlockedByPredicatePaths` | Files excluded by `Predicate` |
| `NotMigratedPathsForErrors` | Files that failed with an exception |

---

## Related packages

| Package | Backend |
|---------|---------|
| `Rystem.Content.Infrastructure.Azure.Storage.Blob` | Azure Blob Storage |
| `Rystem.Content.Infrastructure.Azure.Storage.File` | Azure File Share |
| `Rystem.Content.Infrastructure.M365.Sharepoint` | SharePoint Online |
| `Rystem.Content.Infrastructure.InMemory` | In-memory (testing) |
You may use this library to help the integration with your business and your several storage repositories.

## Dependency injection

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

## How to use
If you have only one integration installed at once, you may inject directly

    public sealed class SimpleBusiness
    {
        private readonly IContentRepository _contentRepository;

        public SimpleBusiness(IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
        }
    }

### In case of multiple integrations you have to use the factory service

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

## Migration tool
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