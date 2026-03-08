# Rystem.RepositoryFramework.Cache.Azure.Storage.Blob

`Rystem.RepositoryFramework.Cache.Azure.Storage.Blob` adds a Blob Storage-backed distributed cache adapter for Repository Framework decorators.

It does not cache entities directly inside your main repository storage. Instead, it registers a dedicated internal repository for `BlobStorageCacheModel` and uses Azure Blob Storage as the persistence layer for cache entries.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache.Azure.Storage.Blob
```

This package builds on top of:

- `Rystem.RepositoryFramework.Cache`
- `Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob`

## How it works

Calling `WithBlobStorageCache(...)` does two things:

1. registers an internal `IRepository<BlobStorageCacheModel, string>` backed by the Blob Storage infrastructure package
2. wires `BlobStorageCache<T, TKey>` as the distributed cache decorator for your repository, query, or command registration

Each cached value is stored as a blob containing:

- `Expiration` as a `DateTime`
- `Value` as serialized JSON

At read time the adapter:

1. checks whether the cache blob exists
2. downloads the blob
3. compares `Expiration` with `DateTime.UtcNow`
4. deserializes the stored JSON payload when still valid

If the entry is expired, the adapter simply returns a cache miss. The current implementation does not automatically delete the stale blob during that read.

## Basic repository example

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(storage =>
    {
        storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        storage.Settings.ContainerName = "users";
    });

    repositoryBuilder.WithBlobStorageCache(
        cacheStorage =>
        {
            cacheStorage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            cacheStorage.Settings.ContainerName = "user-cache";
            cacheStorage.Settings.Prefix = "cache/";
        },
        cache =>
        {
            cache.ExpiringTime = TimeSpan.FromMinutes(5);
            cache.Methods = RepositoryMethods.Get
                | RepositoryMethods.Query
                | RepositoryMethods.Exist;
        });
});
```

This keeps your domain data container separate from your cache container, which is usually the cleanest setup.

## CQRS registrations

The package exposes overloads for all three Repository Framework patterns.

### Repository

```csharp
builder.Services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(storage =>
    {
        storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    repositoryBuilder.WithBlobStorageCache(cacheStorage =>
    {
        cacheStorage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        cacheStorage.Settings.ContainerName = "document-cache";
    });
});
```

### Query only

```csharp
builder.Services.AddQuery<User, string>(queryBuilder =>
{
    queryBuilder.WithBlobStorage(storage =>
    {
        storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    queryBuilder.WithBlobStorageCache(
        cacheStorage =>
        {
            cacheStorage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            cacheStorage.Settings.ContainerName = "user-query-cache";
        },
        cache =>
        {
            cache.ExpiringTime = TimeSpan.FromMinutes(30);
            cache.Methods = RepositoryMethods.Get | RepositoryMethods.Query;
        });
});
```

### Command only

As with the base cache package, write caching on command-only registrations is meaningful only when `Methods` includes write flags.

```csharp
builder.Services.AddCommand<User, string>(commandBuilder =>
{
    commandBuilder.WithBlobStorage(storage =>
    {
        storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    commandBuilder.WithBlobStorageCache(
        cacheStorage =>
        {
            cacheStorage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            cacheStorage.Settings.ContainerName = "user-command-cache";
        },
        cache =>
        {
            cache.ExpiringTime = TimeSpan.FromMinutes(10);
            cache.Methods = RepositoryMethods.Insert
                | RepositoryMethods.Update
                | RepositoryMethods.Delete;
        });
});
```

## Two-level cache example

You can stack the standard in-memory decorator in front of Blob Storage.

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(storage =>
    {
        storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        storage.Settings.ContainerName = "users";
    });

    repositoryBuilder.WithInMemoryCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromSeconds(30);
        cache.Methods = RepositoryMethods.Get | RepositoryMethods.Query;
    });

    repositoryBuilder.WithBlobStorageCache(
        cacheStorage =>
        {
            cacheStorage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
            cacheStorage.Settings.ContainerName = "user-cache";
        },
        cache =>
        {
            cache.ExpiringTime = TimeSpan.FromMinutes(10);
            cache.Methods = RepositoryMethods.All;
        });
});
```

Read flow in that setup:

1. try in-memory cache
2. try Blob Storage distributed cache
3. hit the underlying repository
4. repopulate missed layers

## Blob-specific behavior

The Blob Storage cache adapter is intentionally small and inherits most behavior from the base cache package.

Important details from the source:

- cache values are serialized with `System.Text.Json`
- `SetAsync(...)` writes through `UpdateAsync(...)` on the internal blob repository
- because the blob repository uses `UploadAsync(..., overwrite: true)`, cache writes behave like upserts
- `DeleteAsync(...)` first checks whether the cache blob exists and then deletes it
- the cache repository key type is always `string`

## Naming and factory behavior

`WithBlobStorageCache(...)` accepts the same optional `name` parameter used across Repository Framework registrations.

That name is used for two different things:

- selecting the decorated repository/query/command registration
- naming the internal `IRepository<BlobStorageCacheModel, string>` factory registration created for the cache backend

This means named registrations are supported, but the same caveats from the base cache package still apply:

- `Get` and `Exist` cache keys include the factory name
- `Query` and `Operation` cache keys do not include the factory name in the current implementation

If you have multiple named registrations for the same model and they expose different data sources, avoid query caching or keep their cache backends physically separate.

## Cache options

This package uses `DistributedCacheOptions<T, TKey>` from `Rystem.RepositoryFramework.Cache`.

| Property | Default | Notes |
| --- | --- | --- |
| `Methods` | `Query | Get | Exist` | Enables read-through caching by default. |
| `ExpiringTime` | `365 * 365 days` | Set this explicitly for production workloads. |

Relevant flags:

- `Get` caches `GetAsync`
- `Exist` caches `ExistAsync`
- `Query` caches `QueryAsync` and `OperationAsync`
- `Insert`, `Update`, and `Delete` keep key-based cache entries in sync
- `All` enables every flag

As with the base cache package, write operations update `Get` and `Exist` entries for affected keys, but they do not invalidate previously cached query result sets.

## Choosing containers and prefixes

The cache storage registration uses the same Blob Storage builder as the main Azure Blob infrastructure package, so you can configure:

- `ConnectionString`
- `EndpointUri`
- `ManagedIdentityClientId`
- `ContainerName`
- `Prefix`
- `ClientOptions`

Using a dedicated container like `user-cache` is usually easier to inspect and rotate than mixing cache blobs with domain blobs.

## When to use this package

Use it when you want:

- a distributed cache layer without bringing in Redis or SQL Server cache infrastructure
- cache entries that can be shared across app instances through Azure Blob Storage
- a cache backend configured with the same Blob Storage connection model already used by Repository Framework storage packages

If you want the generic cache decorators without Blob Storage, start with `../RepositoryFramework.Cache/README.md`.
