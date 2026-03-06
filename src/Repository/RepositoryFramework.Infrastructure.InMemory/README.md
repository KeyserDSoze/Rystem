# Rystem.RepositoryFramework.Infrastructure.InMemory

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Infrastructure.InMemory)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Infrastructure.InMemory)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

In-memory repository implementation for local development, functional tests, and reliability simulations. Supports random data generation, JSON/object seeding, latency simulation, and configurable exception injection per repository method.

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.InMemory
```

---

## Quick Start

```csharp
builder.Services.AddRepository<User, string>(repo =>
{
    repo.WithInMemory();
});

var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

`WithInMemory()` is available on `IRepositoryBuilder`, `ICommandBuilder`, and `IQueryBuilder`.

---

## `WithInMemory()` Signature

```csharp
.WithInMemory(
    Action<IRepositoryInMemoryBuilder<T, TKey>>? inMemoryBuilder = null,
    string? name = null,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
```

| Parameter | Default | Description |
|---|---|---|
| `inMemoryBuilder` | `null` | Optional configuration callback for seeding and behavior. |
| `name` | `null` | Factory name for named registrations. |
| `lifetime` | `Singleton` | DI lifetime of the storage instance. |

---

## Seeding Data

### Random Data

Generates `numberOfElements` random entities using `System.Population.Random`. Returns an `IPopulationBuilder` for fine-grained pattern configuration.

```csharp
repo.WithInMemory(x =>
{
    x.PopulateWithRandomData(
        numberOfElements: 120,
        numberOfElementsWhenEnumerableIsFound: 5);
});
```

> Always call `await app.Services.WarmUpAsync()` after `Build()` when using random or injected population.

### Regex Patterns on Properties

Chain `WithPattern` on the returned `IPopulationBuilder` to constrain random string generation:

```csharp
repo.WithInMemory(x =>
{
    x.PopulateWithRandomData(100, 5)
        .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
        .WithPattern(x => x.Value!.Username, @"[a-z]{4,8}");
});
```

`WithPattern` supports any property reachable via a lambda, including nested properties and collection elements accessed via `.First()`.

### Concrete Implementations for Interfaces

When your entity has interface-typed properties, specify the concrete type to instantiate:

```csharp
repo.WithInMemory(x =>
{
    x.PopulateWithRandomData(100)
        .WithImplementation<IInnerService, ConcreteInnerService>(x => x.Value!.Service!);
});
```

### Inject from an Enumerable

Seed from an existing in-memory collection:

```csharp
var users = new List<User>
{
    new() { Id = "alice", Name = "Alice" },
    new() { Id = "bob",   Name = "Bob" },
};

repo.WithInMemory(x =>
{
    x.PopulateWithDataInjection(u => u.Id, users);
});
```

### Inject from JSON

Seed from a JSON string (expects a JSON array of `T`):

```csharp
var json = File.ReadAllText("seed/users.json");

repo.WithInMemory(x =>
{
    x.PopulateWithJsonData(u => u.Id, json);
});
```

---

## Behavior Simulation

Use `inMemoryBuilder.Settings` to configure latency and exception injection. This is useful for resilience testing without a real external dependency.

### `RepositoryBehaviorSettings<T, TKey>` Methods

| Method | Scope |
|---|---|
| `AddForRepositoryPattern(setting)` | All methods |
| `AddForCommandPattern(setting)` | `Insert`, `Update`, `Delete`, `Batch` |
| `AddForQueryPattern(setting)` | `Get`, `Query`, `Exist`, `Operation` |
| `AddForInsert(setting)` | `Insert` only |
| `AddForUpdate(setting)` | `Update` only |
| `AddForDelete(setting)` | `Delete` only |
| `AddForBatch(setting)` | `Batch` only |
| `AddForGet(setting)` | `Get` only |
| `AddForQuery(setting)` | `Query` only |
| `AddForExist(setting)` | `Exist` only |
| `AddForCount(setting)` | `Operation` (count/aggregates) only |

### `MethodBehaviorSetting` Properties

| Property | Type | Description |
|---|---|---|
| `MillisecondsOfWait` | `Range` | Random delay (min..max ms) added to every call of that method. |
| `MillisecondsOfWaitWhenException` | `Range` | Additional delay (min..max ms) added when an exception is thrown. |
| `ExceptionOdds` | `List<ExceptionOdds>` | List of exceptions each with a probability percentage (0–100). Total must not exceed 100. |

### `ExceptionOdds` Properties

| Property | Type | Description |
|---|---|---|
| `Percentage` | `double` | Probability (0.000…1 – 100) that this exception is thrown on each call. |
| `Exception` | `Exception?` | The exception instance to throw. |

### Example — Global Latency + Transient Errors

```csharp
repo.WithInMemory(x =>
{
    x.Settings.AddForRepositoryPattern(new MethodBehaviorSetting
    {
        MillisecondsOfWait = new Range(50, 200),
        ExceptionOdds = new List<ExceptionOdds>
        {
            new() { Exception = new TimeoutException("Simulated timeout"), Percentage = 5 },
            new() { Exception = new IOException("Simulated I/O error"), Percentage = 3 },
        }
    });
});
```

### Example — Slow Writes Only

```csharp
repo.WithInMemory(x =>
{
    x.Settings.AddForCommandPattern(new MethodBehaviorSetting
    {
        MillisecondsOfWait = new Range(300, 800)
    });
});
```

### Example — Inject Failures on Delete

```csharp
repo.WithInMemory(x =>
{
    x.Settings.AddForDelete(new MethodBehaviorSetting
    {
        ExceptionOdds = new List<ExceptionOdds>
        {
            new() { Exception = new InvalidOperationException("Cannot delete"), Percentage = 20 }
        },
        MillisecondsOfWaitWhenException = new Range(100, 300)
    });
});
```

---

## CQRS — Command and Query Variants

```csharp
// Command-only
builder.Services.AddCommand<Order, Guid>(cmd =>
    cmd.WithInMemory());

// Query-only
builder.Services.AddQuery<Product, int>(qry =>
    qry.WithInMemory(x =>
        x.PopulateWithRandomData(50)));
```

---

## Named Instances (Factory Pattern)

Use `name` to register multiple in-memory stores for the same entity type:

```csharp
builder.Services.AddRepository<Cache, string>(repo =>
    repo.WithInMemory(name: "hot"));

builder.Services.AddRepository<Cache, string>(repo =>
    repo.WithInMemory(name: "cold"));
```

---

## Full Example

```csharp
builder.Services.AddRepository<IperUser, string>(repo =>
{
    repo.WithInMemory(x =>
    {
        x.PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
            .WithPattern(x => x.Value!.Groups!.First().Name, @"[a-z]{4,6}");

        x.Settings.AddForCommandPattern(new MethodBehaviorSetting
        {
            MillisecondsOfWait = new Range(10, 50),
            ExceptionOdds = new List<ExceptionOdds>
            {
                new() { Exception = new Exception("Transient write error"), Percentage = 2 }
            }
        });
    });
});

var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

---

## Related Packages

| Package | Description |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Core interfaces and `AddRepository` registration |
| `Rystem.RepositoryFramework.Api.Server` | Expose repositories as HTTP endpoints |
| `Rystem.RepositoryFramework.Api.Client` | .NET HTTP client for repository endpoints |
