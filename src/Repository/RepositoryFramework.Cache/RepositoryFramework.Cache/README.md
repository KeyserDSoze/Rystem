# Rystem.RepositoryFramework.Cache

`Rystem.RepositoryFramework.Cache` adds cache decorators on top of Repository Framework registrations. You keep resolving the same `IRepository<T, TKey>`, `IQuery<T, TKey>`, or `ICommand<T, TKey>` services, but the runtime pipeline first checks a cache layer and then falls back to the original storage implementation.

The package is intentionally thin: it does not replace your storage, it decorates it.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache
```

## How the cache layer is wired

The package registers one of these decorators:

- `CachedRepository<T, TKey>` for full repositories.
- `CachedQuery<T, TKey>` for query-only registrations.
- `CachedRepository<T, TKey>` for command-only registrations, but only when cache options include write methods.

That means caching is opt-in per registration and per method group.

```csharp
builder.Services.AddRepository<Country, CountryKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(builder =>
    {
        builder.PopulateWithRandomData(100, 100);
    });

    repositoryBuilder.WithInMemoryCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromSeconds(10);
        cache.Methods = RepositoryMethods.Get
            | RepositoryMethods.Query
            | RepositoryMethods.Exist;
    });
});
```

After registration, the resolved `IRepository<Country, CountryKey>` behaves like this:

1. try cache
2. fall back to the underlying repository when needed
3. write the result back to cache when the configured method allows it

## Supported cache backends

### `WithInMemoryCache(...)`

Uses the package-provided `InMemoryCache<T, TKey>` over `IMemoryCache`.

- Calls `AddMemoryCache()` automatically.
- Registers the cache service as `Singleton`.
- Good fit for single-instance apps or as the first level of a multi-layer cache.

```csharp
builder.Services.AddRepository<Plant, int>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();

    repositoryBuilder.WithInMemoryCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromMinutes(1);
        cache.Methods = RepositoryMethods.Query | RepositoryMethods.Get;
    });
});
```

### `WithDistributedCache(...)`

Wraps the `Microsoft.Extensions.Caching.Distributed.IDistributedCache` that is already in DI.

- The Repository Framework adapter serializes values as JSON.
- The default adapter sets `AbsoluteExpiration`, `AbsoluteExpirationRelativeToNow`, and `SlidingExpiration` to the same `ExpiringTime`.
- Registers the adapter as `Singleton` by default.

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RepositoryFramework";
});

builder.Services.AddRepository<Country, CountryKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(builder =>
    {
        builder.PopulateWithRandomData(100, 100);
    });

    repositoryBuilder.WithDistributedCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromSeconds(10);
        cache.Methods = RepositoryMethods.Query | RepositoryMethods.Get | RepositoryMethods.Exist;
    });
});
```

### `WithCache<T, TKey, TCache>(...)`

Use this when you want a custom in-process or custom remote cache implementation.

```csharp
public sealed class MyCache<T, TKey> : ICache<T, TKey>
    where TKey : notnull
{
    public Task<CacheResponse<TValue>> RetrieveAsync<TValue>(
        string key,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new CacheResponse<TValue>(false, default));

    public Task<bool> SetAsync<TValue>(
        string key,
        TValue value,
        CacheOptions<T, TKey> options,
        CancellationToken? cancellationToken = null)
        => Task.FromResult(true);

    public Task<bool> DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

builder.Services.AddRepository<Plant, int>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();

    repositoryBuilder.WithCache<Plant, int, MyCache<Plant, int>>(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromMinutes(5);
        cache.Methods = RepositoryMethods.All;
    });
});
```

### `WithDistributedCache<T, TKey, TCache>(...)`

Use this when your custom cache should be treated as a distributed cache layer.

`TCache` must implement `IDistributedCache<T, TKey>`, which extends `ICache<T, TKey>`.

## CQRS registrations

The cache package works on full repositories and on CQRS-only registrations.

### Query only

```csharp
builder.Services.AddQuery<Plant, int>(queryBuilder =>
{
    queryBuilder.WithInMemory();
    queryBuilder.WithInMemoryCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromMinutes(5);
    });
});
```

### Command only

For command-only registrations, the decorator is added only when you cache write methods.

```csharp
builder.Services.AddCommand<Plant, int>(commandBuilder =>
{
    commandBuilder.WithInMemory();
    commandBuilder.WithInMemoryCache(cache =>
    {
        cache.ExpiringTime = TimeSpan.FromMinutes(1);
        cache.Methods = RepositoryMethods.Insert
            | RepositoryMethods.Update
            | RepositoryMethods.Delete;
    });
});
```

If you configure only `Get`, `Query`, or `Exist` on a command registration, there is no command decorator to apply those settings.

## Two-level caching

The decorators support having both local and distributed cache layers on the same registration. The unit tests wire exactly that pattern with in-memory storage plus in-memory cache plus Redis-backed distributed cache.

```csharp
builder.Services.AddRepository<Country, CountryKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(builder =>
    {
        builder.PopulateWithRandomData(100, 100);
    });

    repositoryBuilder
        .WithInMemoryCache(cache =>
        {
            cache.ExpiringTime = TimeSpan.FromSeconds(10);
        })
        .WithDistributedCache(cache =>
        {
            cache.ExpiringTime = TimeSpan.FromSeconds(10);
        });
});
```

Read flow in that setup:

1. check `ICache<T, TKey>`
2. check `IDistributedCache<T, TKey>`
3. hit the underlying repository
4. backfill any cache layers that missed

## What is cached

### Read methods

- `GetAsync(key)` caches the returned model.
- `ExistAsync(key)` caches the returned `State<T, TKey>`.
- `QueryAsync(filter)` materializes the full async stream to a `List<Entity<T, TKey>>` and caches that list.
- `OperationAsync(...)` is also cached, and it uses the `Query` flag to decide whether caching is enabled.

Because query results are materialized before caching, this package is best for repeated reads of the same filter, not for preserving streaming behavior end-to-end.

### Write methods

- `InsertAsync` can refresh `Get` and `Exist` cache entries for the affected key.
- `UpdateAsync` can refresh `Get` and `Exist` cache entries for the affected key.
- `DeleteAsync` can remove `Get` and `Exist` cache entries for the affected key.
- `BatchAsync` updates or removes per-key `Get` and `Exist` entries as it iterates through batch results.

The package does not keep a registry of query filters, so write operations do not invalidate previously cached query results.

That behavior is visible in `RepositoryFramework.UnitTest/Tests/Cache/CacheTest.cs`: deleting all records from the underlying repository does not remove the cached query result, so the old list is served again until cache expiration.

## Cache key behavior

The decorators build their own cache keys.

- `Get` and `Exist` include method name, model type name, factory name, and serialized key.
- `Query` includes method name, model type name, and the filter key.
- `Operation` includes method name, operation name, model type name, and the filter key.

Practical consequence:

- named repository registrations are respected for `Get` and `Exist`
- query and operation caches are not partitioned by factory name in the current implementation

If you use multiple named registrations for the same model with different backends, document that behavior for your team before enabling query caching.

## Defaults and options

## `CacheOptions<T, TKey>`

| Property | Default | Notes |
| --- | --- | --- |
| `Methods` | `Query | Get | Exist` | Controls which operations participate in caching. |
| `ExpiringTime` | `365 days` | Used by the built-in in-memory cache and passed to custom caches. |

`HasCommandPattern` becomes `true` when `Methods` includes `Insert`, `Update`, `Delete`, or `All`.

## `DistributedCacheOptions<T, TKey>`

Inherits from `CacheOptions<T, TKey>`.

| Property | Default | Notes |
| --- | --- | --- |
| `Methods` | `Query | Get | Exist` | Same behavior as `CacheOptions<T, TKey>`. |
| `ExpiringTime` | `365 * 365 days` | Very large default; set it explicitly for real workloads. |

## `RepositoryMethods` flags in cache scenarios

| Flag | Effect |
| --- | --- |
| `Get` | Enables read-through caching for `GetAsync`. |
| `Exist` | Enables read-through caching for `ExistAsync`. |
| `Query` | Enables caching for `QueryAsync` and `OperationAsync`. |
| `Insert` | After insert, refreshes key-based cache entries. |
| `Update` | After update, refreshes key-based cache entries. |
| `Delete` | After delete, removes key-based cache entries. |
| `All` | Enables every flag. |

## Warm-up and lifecycle notes

The cache package does not add its own bootstrap workflow. Any warm-up still comes from the underlying repository registration, for example when an in-memory repository is populated through `PopulateWithRandomData(...)` and then activated via `WarmUpAsync()`.

## When to use this package

Use it when you want to:

- add read caching without changing your repository consumers
- stack in-memory and distributed cache decorators
- keep cache concerns outside your domain repositories
- experiment with caching policies per registration or per named factory

If you also need a Blob Storage-backed distributed cache provider, see `../RepositoryFramework.Cache.Azure.Storage.Blob/README.md`.
