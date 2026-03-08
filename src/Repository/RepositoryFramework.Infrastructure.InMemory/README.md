### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Infrastructure.InMemory

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Infrastructure.InMemory)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Infrastructure.InMemory)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Infrastructure.InMemory)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Infrastructure.InMemory)

In-memory storage for the Rystem repository ecosystem, designed for local development, sample apps, integration tests, random data population, and resilience simulations.

This package sits on top of `Rystem.RepositoryFramework.Abstractions` and is usually the easiest concrete provider to start with when you want repository behavior without a real external database.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.InMemory
```

The current package metadata in `src/Repository/RepositoryFramework.Infrastructure.InMemory/RepositoryFramework.Infrastructure.InMemory.csproj` is:

- package id: `Rystem.RepositoryFramework.Infrastructure.InMemory`
- version: `10.0.6`
- target framework: `net10.0`

---

## Package architecture

| Area | Purpose |
|---|---|
| `WithInMemory(...)` extensions | Register in-memory storage on repository, command, or query builders |
| `InMemoryStorage<T, TKey>` | Concrete storage implementation used by the registration extensions |
| `IRepositoryInMemoryBuilder<T, TKey>` | Builder used for seeding and behavior configuration |
| `RepositoryBehaviorSettings<T, TKey>` | Per-method latency and exception configuration |
| `MethodBehaviorSetting` / `ExceptionOdds` | Delay ranges and simulated failure probabilities |
| Warm-up population hooks | Deferred seeding from random generation, injected data, or JSON |

---

## What this package is good for

- fast tests that still go through repository contracts
- sample apps and demos that need realistic query/write behavior
- local development before wiring a real provider
- generating seeded datasets with `System.Population.Random`
- simulating latency and failures without external infrastructure

---

## Quick start

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

`WithInMemory()` is available on:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

`WarmUpAsync()` is only required when you use the population helpers, but many repository-based apps already call it during startup for consistency across providers.

---

## Registration API

All three overloads share the same signature and the same defaults.

```csharp
.WithInMemory(
    Action<IRepositoryInMemoryBuilder<T, TKey>>? inMemoryBuilder = null,
    string? name = null,
    ServiceLifetime lifetime = ServiceLifetime.Singleton)
```

| Parameter | Default | Description |
|---|---|---|
| `inMemoryBuilder` | `null` | Optional callback for seeding and behavior settings |
| `name` | `null` | Named factory key used by DI and warm-up resolution |
| `lifetime` | `Singleton` | DI lifetime for the registered storage service |

This provider uses `SetStorageAndBuildOptions(...)` from the abstractions layer under the hood.

---

## How the in-memory store behaves

`InMemoryStorage<T, TKey>` stores data in a static `ConcurrentDictionary<string, Entity<T, TKey>>` keyed by `KeySettings<TKey>.AsString(key)`.

That has a few practical consequences:

- data is shared per closed generic pair `T` and `TKey`
- inserts, updates, and reads are thread-safe at the dictionary level
- values are deep-cloned through JSON when written and when returned from `GetAsync` and `QueryAsync`
- the storage behaves more like a serialization boundary than a simple in-process reference cache

### Important note about named registrations

Named registrations help you resolve different DI entries, but they do not create isolated backing stores for the same `T` and `TKey` pair.

For example, these two registrations resolve separately through `IFactory<IRepository<User, string>>`, but they still share the same underlying static dictionary:

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(name: "default");
    repositoryBuilder.WithInMemory(name: "secondary");
});
```

So in this provider, `name` is primarily a DI-selection and warm-up-routing concept, not a data-partitioning mechanism.

---

## Seeding and warm-up

The in-memory builder exposes three seeding styles:

- `PopulateWithRandomData(...)`
- `PopulateWithDataInjection(...)`
- `PopulateWithJsonData(...)`

These methods do not insert data immediately during service registration. They register warm-up actions, and those actions run when `WarmUpAsync()` is called on the built service provider.

### Random data

`PopulateWithRandomData(...)` uses `System.Population.Random` and returns `IPopulationBuilder<Entity<T, TKey>>`, which is why the lambda examples target `x.Value` and `x.Key`.

```csharp
builder.Services.AddRepository<NonPlusSuperUser, NonPlusSuperUserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
    });
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

The two numeric parameters are:

- `numberOfElements`: how many root entities to generate
- `numberOfElementsWhenEnumerableIsFound`: how many items to generate for enumerable members

### Customizing random generation

Because random population returns an `IPopulationBuilder<Entity<T, TKey>>`, you can use the population features from the core ecosystem.

Real tests in the repository use helpers such as:

- `WithPattern(...)`
- `WithImplementation(...)`
- `WithValue(...)`
- `WithAutoIncrement(...)`

```csharp
repositoryBuilder.WithInMemory(inMemoryBuilder =>
{
    inMemoryBuilder
        .PopulateWithRandomData(90, 8)
        .WithAutoIncrement(x => x.Value!.Id, 0)
        .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
        .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.Value!.Inner!)
        .WithValue(x => x.Value!.Enabled, () => true);
});
```

### Inject from existing data

Use `PopulateWithDataInjection(...)` when you already have a list of entities.

```csharp
var users = new List<User>
{
    new() { Id = "alice", Name = "Alice" },
    new() { Id = "bob", Name = "Bob" }
};

builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithDataInjection(x => x.Id, users);
    });
});
```

The key selector should point to a property that can be read from each entity.

### Inject from JSON

Use `PopulateWithJsonData(...)` when the source is a JSON array of `T`.

```csharp
var json = """
[
  { "Id": "alice", "Name": "Alice" },
  { "Id": "bob", "Name": "Bob" }
]
""";

builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithJsonData(x => x.Id, json);
    });
});
```

If the JSON cannot be deserialized into `IEnumerable<T>`, the builder simply leaves the store unchanged.

### Warm-up behavior details

- `PopulateWithRandomData(...)`, `PopulateWithDataInjection(...)`, and `PopulateWithJsonData(...)` all seed through warm-up actions
- warm-up resolves the named repository/command/query registration when a `name` is provided
- plain `WithInMemory()` without population does not need warm-up to function, because `InMemoryStorage<T, TKey>.BootstrapAsync()` is currently a no-op

---

## CRUD and query usage

Once registered, the provider behaves like any other repository implementation from the consumer side.

```csharp
var result = await repository.InsertAsync(1, new Animal { Id = 1, Name = "Eagle" });
var item = await repository.GetAsync(1);
var exists = await repository.ExistAsync(1);

var page = await repository
    .Where(x => x.Id > 0)
    .OrderByDescending(x => x.Id)
    .PageAsync(1, 2);

var batch = repository.CreateBatchOperation();
for (var i = 0; i < 10; i++)
    batch.AddInsert(i, new Animal { Id = i, Name = i.ToString() });

await batch.ExecuteAsync().ToListAsync();
```

This exact style is covered by the in-memory all-methods tests in the repository.

---

## Behavior simulation

The provider can simulate latency and failure probability per repository method family through `RepositoryBehaviorSettings<T, TKey>`.

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.Settings.AddForCommandPattern(new MethodBehaviorSetting
        {
            MillisecondsOfWait = new Range(50, 200)
        });

        inMemoryBuilder.Settings.AddForQueryPattern(new MethodBehaviorSetting
        {
            MillisecondsOfWait = new Range(5, 20)
        });
    });
});
```

### Configuration methods

| Method | Intended scope |
|---|---|
| `AddForRepositoryPattern(setting)` | All methods through `RepositoryMethods.All` fallback |
| `AddForCommandPattern(setting)` | `Insert`, `Update`, `Delete`, `Batch` |
| `AddForQueryPattern(setting)` | `Get`, `Query`, `Exist`, `Operation` |
| `AddForInsert(setting)` | `Insert` |
| `AddForUpdate(setting)` | `Update` |
| `AddForDelete(setting)` | `Delete` |
| `AddForBatch(setting)` | `Batch` |
| `AddForGet(setting)` | `Get` |
| `AddForQuery(setting)` | `Query` |
| `AddForExist(setting)` | `Exist` |
| `AddForCount(setting)` | `Operation` |

### `MethodBehaviorSetting`

| Property | Type | Meaning |
|---|---|---|
| `MillisecondsOfWait` | `Range` | Inclusive random delay added before the operation |
| `MillisecondsOfWaitWhenException` | `Range` | Additional delay added on the simulated-failure path |
| `ExceptionOdds` | `List<ExceptionOdds>` | Candidate exceptions and their probabilities |

### `ExceptionOdds`

| Property | Type | Meaning |
|---|---|---|
| `Percentage` | `double` | Probability from `> 0` to `100` |
| `Exception` | `Exception?` | Exception object associated with that probability |

### Validation rules

Before the options are finalized, the builder validates the configured percentages:

- each percentage must be greater than `0` and less than or equal to `100`
- the total of one list cannot exceed `100`

Invalid configurations throw during options building, not later at query time.

---

## Important behavior differences by method

The provider does not simulate failures in the exact same way for every method.

### Read-style methods

These methods throw the configured simulated exception when the probability path is hit:

- `GetAsync`
- `QueryAsync`
- `OperationAsync`

### Command-style methods

These methods do not throw the configured exception. Instead, the simulated failure path returns a failed `State<T, TKey>`:

- `InsertAsync`
- `UpdateAsync`
- `DeleteAsync`
- `ExistAsync`

So for writes and `ExistAsync`, you should check `state.IsOk` rather than expect an exception.

### Batch behavior

`BatchAsync` executes insert/update/delete operations one by one and yields a result for each operation. It is not transactional and does not roll back previous operations.

Also, as currently implemented, `BatchAsync` does not read the dedicated `RepositoryMethods.Batch` behavior setting directly. It inherits behavior from the individual insert/update/delete calls it performs.

That means `AddForBatch(...)` exists in the options API, but it is not consumed by `InMemoryStorage<T, TKey>` today.

---

## CQRS variants

The same extension exists for command-only and query-only registrations.

```csharp
builder.Services.AddCommand<Order, Guid>(commandBuilder =>
{
    commandBuilder.WithInMemory();
});

builder.Services.AddQuery<Product, int>(queryBuilder =>
{
    queryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithRandomData(50, 3);
    });
});
```

Because the concrete in-memory storage implements the full repository contract internally, warm-up seeding still works even when the public DI surface is query-only or command-only.

---

## Practical examples from the repo

### Sample web API seed data

The sample API under `src/Repository/RepositoryFramework.Test/RepositoryFramework.WebApi/Program.cs` registers repositories like this:

```csharp
builder.Services.AddRepository<SuperUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
    });
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

### Test-focused latency simulation

The population tests configure different delays for reads and writes:

```csharp
repositoryBuilder.WithInMemory(inMemoryBuilder =>
{
    inMemoryBuilder.Settings.AddForCommandPattern(new MethodBehaviorSetting
    {
        MillisecondsOfWait = new Range(100, 250)
    });

    inMemoryBuilder.Settings.AddForQueryPattern(new MethodBehaviorSetting
    {
        MillisecondsOfWait = new Range(10, 40)
    });
});
```

### Full exception distribution example

The exception tests configure a full distribution across several exceptions:

```csharp
repositoryBuilder.WithInMemory(inMemoryBuilder =>
{
    inMemoryBuilder.Settings.AddForRepositoryPattern(new MethodBehaviorSetting
    {
        ExceptionOdds = new List<ExceptionOdds>
        {
            new() { Exception = new Exception("Normal Exception"), Percentage = 10.352 },
            new() { Exception = new Exception("Big Exception"), Percentage = 49.1 },
            new() { Exception = new Exception("Great Exception"), Percentage = 40.548 }
        }
    });
});
```

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Core contracts, query model, and registration builders |
| `Rystem.RepositoryFramework.Api.Server` | Expose repositories as HTTP endpoints |
| `Rystem.RepositoryFramework.Api.Client` | Call repository APIs from .NET or TypeScript |

If you are continuing through the repository area, this is usually the next package to understand after `src/Repository/RepositoryFramework.Abstractions/README.md`.
