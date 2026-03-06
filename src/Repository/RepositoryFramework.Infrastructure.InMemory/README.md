# Rystem.RepositoryFramework.Infrastructure.InMemory

In-memory repository implementation for local development, functional tests, and load/reliability simulations.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.InMemory
```

## Quick start

```csharp
builder.Services.AddRepository<IperUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, "[a-z]{5,10}@gmail\\.com");
    });
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

## Populate collections and dictionaries

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(100, 8)
            .WithPattern(x => x.Groups!.First().Id, "[a-z]{4,5}")
            .WithPattern(x => x.Claims!.First().Value, "[a-z]{4,5}");
    });
});
```

## Populate interface properties with concrete implementations

```csharp
builder.Services.AddRepository<PopulationTest, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(100)
            .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
    });
});
```

## Simulate failures and latency

```csharp
builder.Services.AddRepository<Car, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.Settings.AddForRepositoryPattern(new MethodBehaviorSetting
        {
            ExceptionOdds = new List<ExceptionOdds>
            {
                new() { Exception = new Exception("Transient error"), Percentage = 10 },
                new() { Exception = new Exception("Timeout"), Percentage = 5 }
            },
            MillisecondsOfWait = new Range(100, 400)
        });
    });
});
```

## Notes

- Patterns are supported for primitive and common struct types.
- If you use random pre-population, call `WarmUpAsync()` during startup.
- You can combine this package with cache, API server, and business interceptors.
