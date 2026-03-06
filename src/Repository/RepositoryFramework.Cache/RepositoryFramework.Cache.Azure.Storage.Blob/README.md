# Rystem.RepositoryFramework.Cache.Azure.Storage.Blob

Azure Blob Storage distributed cache provider for Repository/CQRS decorators. Each cache entry is stored as a JSON blob with an expiration timestamp. Internally registers an `IRepository<BlobStorageCacheModel, string>` backed by Blob Storage to persist cache entries.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache.Azure.Storage.Blob
```

## Quick start

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    repositoryBuilder.WithBlobStorageCache(
        blobStorageOptions =>
        {
            // Separate container for cache entries
            blobStorageOptions.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            blobStorageOptions.Settings.ContainerName = "user-cache";
        },
        cacheOptions =>
        {
            cacheOptions.ExpiringTime = TimeSpan.FromMinutes(5);
            cacheOptions.Methods = RepositoryMethods.All;
        });
});
```

## CQRS patterns

```csharp
// Command only
builder.Services.AddCommand<User, string>(commandBuilder =>
{
    commandBuilder.WithBlobStorage(b => {  b.Settings.ConnectionString = "..."; });
    commandBuilder.WithBlobStorageCache(
        b => { b.Settings.ConnectionString = "..."; },
        cacheOptions =>
        {
            cacheOptions.ExpiringTime = TimeSpan.FromMinutes(10);
            cacheOptions.Methods = RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete;
        });
});

// Query only
builder.Services.AddQuery<User, string>(queryBuilder =>
{
    queryBuilder.WithBlobStorage(b => { b.Settings.ConnectionString = "..."; });
    queryBuilder.WithBlobStorageCache(
        b => { b.Settings.ConnectionString = "..."; },
        cacheOptions =>
        {
            cacheOptions.ExpiringTime = TimeSpan.FromMinutes(30);
            cacheOptions.Methods = RepositoryMethods.Get | RepositoryMethods.Query;
        });
});
```

## Combining in-memory + Blob Storage cache (two-level)

Stack `WithInMemoryCache` before `WithBlobStorageCache` to get a fast local layer backed by a persistent distributed layer:

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(b =>
    {
        b.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    // L1: fast in-process cache
    repositoryBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(60);
        cacheOptions.Methods = RepositoryMethods.Get | RepositoryMethods.Query;
    });

    // L2: durable distributed cache
    repositoryBuilder.WithBlobStorageCache(
        blobStorageOptions =>
        {
            blobStorageOptions.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            blobStorageOptions.Settings.ContainerName = "user-cache";
        },
        cacheOptions =>
        {
            cacheOptions.ExpiringTime = TimeSpan.FromMinutes(10);
            cacheOptions.Methods = RepositoryMethods.All;
        });
});
```

## How it works

- `WithBlobStorageCache` registers a dedicated `IRepository<BlobStorageCacheModel, string>` using the provided Blob Storage settings. This repository stores cache blobs independently from the main entity storage.
- On `Get`/`Query`/`Exist`: checks blob existence and expiration; returns cached value if valid.
- On `Insert`/`Update`: writes a new blob with the serialized value and computed expiration (`DateTime.UtcNow + ExpiringTime`).
- On `Delete`: removes the blob from cache.

## `CacheOptions` reference

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ExpiringTime` | `TimeSpan` | — | TTL for each cache entry. |
| `Methods` | `RepositoryMethods` | `Query \| Get \| Exist` | Which repository methods participate in caching. |

See [Rystem.RepositoryFramework.Cache](../RepositoryFramework.Cache/README.md) for the full `RepositoryMethods` flags reference.
