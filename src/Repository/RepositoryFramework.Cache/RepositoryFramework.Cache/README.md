# Rystem.RepositoryFramework.Cache

Cache decorators for Repository/CQRS services. The cache layer wraps the original implementation as a decorator — the same `IRepository<T,TKey>`, `ICommand<T,TKey>`, and `IQuery<T,TKey>` interfaces are injected as usual.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache
```

## In-memory cache

Uses `IMemoryCache` (auto-registered). Suitable for single-instance applications.

```csharp
builder.Services.AddRepository<Plant, int>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();

    repositoryBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(30);
        // Which methods are cached. Default: Query | Get | Exist
        cacheOptions.Methods = RepositoryMethods.Get
            | RepositoryMethods.Query
            | RepositoryMethods.Exist;
    });
});
```

CQRS variants:

```csharp
// Command only
builder.Services.AddCommand<Plant, int>(commandBuilder =>
{
    commandBuilder.WithInMemory();
    commandBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromMinutes(1);
        cacheOptions.Methods = RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete;
    });
});

// Query only
builder.Services.AddQuery<Plant, int>(queryBuilder =>
{
    queryBuilder.WithInMemory();
    queryBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromMinutes(5);
    });
});
```

## Distributed cache (`IDistributedCache`)

Uses any `IDistributedCache` already registered in DI (Redis, SQL Server, etc.). Suitable for multi-instance environments.

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RepositoryFramework";
});

builder.Services.AddRepository<Country, CountryKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();

    repositoryBuilder.WithDistributedCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(20);
        cacheOptions.Methods = RepositoryMethods.All;
    });
});
```

## Custom cache implementation

Implement `ICache<T, TKey>` to plug in any custom cache backend and register it with `WithCache<T, TKey, TCache>`:

```csharp
public class MyCache<T, TKey> : ICache<T, TKey> where TKey : notnull
{
    public Task<CacheResponse<TValue>> RetrieveAsync<TValue>(string key, CancellationToken cancellationToken = default) { ... }
    public Task<bool> SetAsync<TValue>(string key, TValue value, CacheOptions<T, TKey> options, CancellationToken? cancellationToken = null) { ... }
    public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default) { ... }
}

builder.Services.AddRepository<Plant, int>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
    repositoryBuilder.WithCache<Plant, int, MyCache<Plant, int>>(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromMinutes(5);
    });
});
```

For distributed custom implementations, implement `IDistributedCache<T, TKey>` (extends `ICache<T, TKey>`) and use `WithDistributedCache<T, TKey, TCache>`.

## `CacheOptions` reference

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ExpiringTime` | `TimeSpan` | — | TTL for each cache entry. |
| `Methods` | `RepositoryMethods` | `Query \| Get \| Exist` | Which repository methods participate in caching. |

### `RepositoryMethods` flags

| Flag | Effect |
| --- | --- |
| `Get` | Caches `GetAsync` results; serves from cache on hit. |
| `Query` | Caches `QueryAsync` results; serves from cache on hit. |
| `Exist` | Caches `ExistAsync` results; serves from cache on hit. |
| `Insert` | After insert, updates/adds the entry in cache. |
| `Update` | After update, refreshes the entry in cache. |
| `Delete` | After delete, removes the entry from cache. |
| `All` | Enables all of the above. |

> When write methods (`Insert`, `Update`, `Delete`, or `All`) are included, the `ICommand` decorator is also registered.
