# Rystem.RepositoryFramework.MigrationTools

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.MigrationTools)](https://www.nuget.org/packages/Rystem.RepositoryFramework.MigrationTools)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Migrates data between two repository integrations (source → destination) using `IMigrationManager<T, TKey>`. Works with any combination of registered repository, command, or query services identified by factory name.

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.MigrationTools
```

---

## Quick Start

```csharp
builder.Services
    .AddRepository<User, string>(repo =>
        repo.WithInMemory(x => x.PopulateWithRandomData(1000), name: "source"))
    .AddRepository<User, string>(repo =>
        repo.WithInMemory(name: "target"))
    .AddMigrationManager<User, string>(options =>
    {
        options.SourceFactoryName = "source";
        options.DestinationFactoryName = "target";
        options.NumberOfConcurrentInserts = 10;
    });

var app = builder.Build();
await app.Services.WarmUpAsync();
```

Then inject `IMigrationManager<User, string>` wherever you need to run the migration:

```csharp
public class MigrationRunner(IMigrationManager<User, string> migration)
{
    public Task<bool> RunAsync(CancellationToken ct)
        => migration.MigrateAsync(u => u.Id!, cancellationToken: ct);
}
```

---

## `AddMigrationManager<T, TKey>()`

Registers `IMigrationManager<T, TKey>` in DI.

```csharp
services.AddMigrationManager<T, TKey>(
    options => { ... },
    name: null);   // optional factory name
```

### `MigrationOptions<T, TKey>` Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SourceFactoryName` | `string` | — | **Required.** Factory name of the source repository (registered via `AddRepository` / `AddQuery` with the same name). |
| `DestinationFactoryName` | `string` | — | **Required.** Factory name of the destination repository (registered via `AddRepository` / `AddCommand` with the same name). |
| `NumberOfConcurrentInserts` | `int` | `10` | Maximum number of insert operations executed in parallel during migration. |

> `SourceFactoryName` and `DestinationFactoryName` must be different — registering the same name for both throws at startup.

---

## `MigrateAsync()`

```csharp
Task<bool> MigrateAsync(
    Expression<Func<T, TKey>> navigationKey,
    bool checkIfExists = false,
    bool deleteEverythingBeforeStart = false,
    CancellationToken cancellationToken = default)
```

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `navigationKey` | `Expression<Func<T, TKey>>` | — | **Required.** Selects the key property from the entity value (e.g. `u => u.Id`). Used to derive the key when inserting into the destination. |
| `checkIfExists` | `bool` | `false` | When `true`, checks whether each entity already exists in the destination before inserting. Skips entities that already exist. Requires destination to be a full `Repository` (not `Command`-only). |
| `deleteEverythingBeforeStart` | `bool` | `false` | When `true`, deletes all existing data in the destination before migration begins. Requires destination to be a full `Repository`. |
| `cancellationToken` | `CancellationToken` | `default` | Cancels the migration mid-flight. Returns `false` if cancelled. |

Returns `true` when migration completes, `false` if cancelled.

---

## Source and Destination Requirements

| Feature used | Source requirement | Destination requirement |
|---|---|---|
| Basic migration | `IQuery` or `IRepository` | `ICommand` or `IRepository` |
| `checkIfExists = true` | `IQuery` or `IRepository` | `IRepository` (needs `Exist`) |
| `deleteEverythingBeforeStart = true` | `IQuery` or `IRepository` | `IRepository` (needs `Query` + `Delete`) |

If a constraint is not met, `MigrateAsync` throws `ArgumentException` at runtime with a descriptive message.

---

## Migration Across Different Storage Backends

The most common use case is migrating from one storage technology to another:

```csharp
builder.Services
    // Source: existing SQL database
    .AddRepository<Product, Guid>(repo =>
        repo.WithEntityFramework<AppDbContext>(
            x => x.Products, name: "sql"))
    // Destination: new blob / cosmos / etc.
    .AddRepository<Product, Guid>(repo =>
        repo.WithInMemory(name: "inmemory"))
    .AddMigrationManager<Product, Guid>(options =>
    {
        options.SourceFactoryName = "sql";
        options.DestinationFactoryName = "inmemory";
        options.NumberOfConcurrentInserts = 20;
    });
```

---

## Full Example with All Options

```csharp
// Registration
builder.Services
    .AddRepository<Order, long>(repo =>
        repo.WithInMemory(x => x.PopulateWithRandomData(500), name: "old"))
    .AddRepository<Order, long>(repo =>
        repo.WithInMemory(name: "new"))
    .AddMigrationManager<Order, long>(options =>
    {
        options.SourceFactoryName = "old";
        options.DestinationFactoryName = "new";
        options.NumberOfConcurrentInserts = 25;
    });

// Execution
public class OrderMigration(IMigrationManager<Order, long> migration)
{
    public async Task<bool> RunAsync(CancellationToken ct)
    {
        return await migration.MigrateAsync(
            navigationKey: o => o.Id,
            checkIfExists: true,               // skip already-migrated records
            deleteEverythingBeforeStart: false, // keep destination data
            cancellationToken: ct);
    }
}
```

---

## Related Packages

| Package | Description |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Core interfaces and `AddRepository` registration |
| `Rystem.RepositoryFramework.Infrastructure.InMemory` | In-memory storage (useful for migration testing) |
