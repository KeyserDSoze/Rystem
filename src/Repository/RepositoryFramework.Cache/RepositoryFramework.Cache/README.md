# Rystem.RepositoryFramework.Cache

Cache decorators for Repository/CQRS integrations.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache
```

## In-memory cache

```csharp
builder.Services.AddRepository<Plant, int>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();

    repositoryBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(30);
        cacheOptions.Methods = RepositoryMethods.Get
            | RepositoryMethods.Insert
            | RepositoryMethods.Update
            | RepositoryMethods.Delete;
    });
});
```

## Distributed cache (IDistributedCache)

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "RepositoryFramework";
});

builder.Services.AddRepository<Country, CountryKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithRandomData(100, 5);
    });

    repositoryBuilder.WithDistributedCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(20);
        cacheOptions.Methods = RepositoryMethods.All;
    });
});
```

## Notes

- Same decorated interfaces are preserved: `IRepository<T, TKey>`, `ICommand<T, TKey>`, `IQuery<T, TKey>`.
- Command methods can invalidate/update cached query/get values depending on selected flags.
