# Rystem.RepositoryFramework.MigrationTools

`Rystem.RepositoryFramework.MigrationTools` adds `IMigrationManager<T, TKey>` for moving data from one Repository Framework registration to another.

It is designed around named registrations: one source factory and one destination factory. The source must be readable, the destination must be writable.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.MigrationTools
```

## Architecture

The migration manager resolves services through Repository Framework factories.

- source: `IQuery<T, TKey>` when available, otherwise `IRepository<T, TKey>`
- destination: `ICommand<T, TKey>` when available, otherwise `IRepository<T, TKey>`
- destination-as-repository: optional, used only for existence checks and destination cleanup

This lets you migrate across:

- repository to repository
- query to command
- query to repository
- repository to command

as long as the named registrations exist.

## Basic example

```csharp
builder.Services
    .AddRepository<User, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(builder =>
        {
            builder.PopulateWithRandomData(1000);
        }, name: "source");
    })
    .AddRepository<User, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(name: "target");
    })
    .AddMigrationManager<User, string>(options =>
    {
        options.SourceFactoryName = "source";
        options.DestinationFactoryName = "target";
        options.NumberOfConcurrentInserts = 10;
    });

var serviceProvider = builder.Services.Finalize();
await serviceProvider.WarmUpAsync();
```

Then inject the manager and run the migration:

```csharp
public sealed class MigrationRunner(IMigrationManager<User, string> migrationManager)
{
    public Task<bool> RunAsync(CancellationToken cancellationToken)
        => migrationManager.MigrateAsync(x => x.Id!, cancellationToken: cancellationToken);
}
```

## Registering `IMigrationManager<T, TKey>`

Use `AddMigrationManager<T, TKey>(...)`.

```csharp
builder.Services.AddMigrationManager<User, string>(options =>
{
    options.SourceFactoryName = "source";
    options.DestinationFactoryName = "target";
    options.NumberOfConcurrentInserts = 20;
});
```

### `MigrationOptions<T, TKey>`

| Property | Default | Notes |
| --- | --- | --- |
| `SourceFactoryName` | required | Must match a named `IQuery<T, TKey>` or `IRepository<T, TKey>` registration. |
| `DestinationFactoryName` | required | Must match a named `ICommand<T, TKey>` or `IRepository<T, TKey>` registration. |
| `NumberOfConcurrentInserts` | `10` | Upper bound used by the migration loop before it waits for the current batch of insert tasks. |

Startup validation performed by `AddMigrationManager(...)`:

- `SourceFactoryName` and `DestinationFactoryName` are normalized to empty strings when null
- source and destination names must be different, otherwise registration throws

## `MigrateAsync(...)`

```csharp
Task<bool> MigrateAsync(
    Expression<Func<T, TKey>> navigationKey,
    bool checkIfExists = false,
    bool deleteEverythingBeforeStart = false,
    CancellationToken cancellationToken = default)
```

### What the current implementation does

1. resolve the configured source and destination factories
2. optionally clear the destination first
3. read the entire source using `QueryAsync(IFilterExpression.Empty)`
4. materialize the full result set into memory with `ToListAsync()`
5. insert entities into the destination in batches of concurrent tasks

This package is therefore a straightforward bulk copy helper, not a streaming migration pipeline.

## Source and destination requirements

| Scenario | Source requirement | Destination requirement |
| --- | --- | --- |
| basic migration | `IQuery<T, TKey>` or `IRepository<T, TKey>` | `ICommand<T, TKey>` or `IRepository<T, TKey>` |
| `checkIfExists = true` | same as above | `IRepository<T, TKey>` |
| `deleteEverythingBeforeStart = true` | same as above | `IRepository<T, TKey>` |

When a requirement is not met, the manager throws an `ArgumentException` with a descriptive message.

## Examples by scenario

### Migrate from one named repository to another

```csharp
builder.Services
    .AddRepository<Order, long>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(builder =>
        {
            builder.PopulateWithRandomData(500);
        }, name: "old");
    })
    .AddRepository<Order, long>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(name: "new");
    })
    .AddMigrationManager<Order, long>(options =>
    {
        options.SourceFactoryName = "old";
        options.DestinationFactoryName = "new";
        options.NumberOfConcurrentInserts = 25;
    });
```

### Migrate between different storage backends

```csharp
builder.Services
    .AddRepository<Product, Guid>(repositoryBuilder =>
    {
        repositoryBuilder.WithEntityFramework<AppDbContext>(x => x.Products, name: "sql");
    })
    .AddRepository<Product, Guid>(repositoryBuilder =>
    {
        repositoryBuilder.WithBlobStorage(storage =>
        {
            storage.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        }, name: "blob");
    })
    .AddMigrationManager<Product, Guid>(options =>
    {
        options.SourceFactoryName = "sql";
        options.DestinationFactoryName = "blob";
        options.NumberOfConcurrentInserts = 20;
    });
```

### Skip items that already exist

```csharp
await migrationManager.MigrateAsync(
    navigationKey: x => x.Id,
    checkIfExists: true,
    cancellationToken: cancellationToken);
```

When `checkIfExists` is enabled, the manager calls `ExistAsync(entity.Key!)` on the destination repository before inserting.

### Clear destination before migrating

```csharp
await migrationManager.MigrateAsync(
    navigationKey: x => x.Id,
    deleteEverythingBeforeStart: true,
    cancellationToken: cancellationToken);
```

When `deleteEverythingBeforeStart` is enabled, the manager loads the destination with `ToListAsync()` and deletes items one by one with `DeleteAsync(...)` before starting inserts.

## Important behavior notes from the source

These details matter when you use the package in production-like migrations.

### `navigationKey` is currently not used

`MigrateAsync(...)` accepts `Expression<Func<T, TKey>> navigationKey`, but the current implementation inserts using `entity.Key!` from the source query result and never evaluates the expression.

Practical consequence:

- the migration currently depends on the source repository returning correct `Entity<T, TKey>.Key` values
- changing `navigationKey` does not change the destination key in the current implementation

### Source data is fully materialized first

The migration manager executes:

```csharp
await _source.QueryAsync(IFilterExpression.Empty, cancellationToken: cancellationToken)
    .ToListAsync();
```

That means:

- the whole source dataset is loaded before inserts begin
- memory usage grows with the full migration size
- there is no built-in pagination or chunked source enumeration

### Destination cleanup is sequential

When `deleteEverythingBeforeStart` is `true`, destination deletion happens one item at a time.

### Final insert batch caveat

The current implementation waits only when the in-flight task count becomes greater than `NumberOfConcurrentInserts`, and it does not explicitly await the final partial batch before returning `true`.

For small or non-divisible workloads, that means the method can report completion before the last queued inserts have definitely finished.

If you rely on strict end-of-migration completion semantics, verify the destination after the call or patch the implementation before using it for critical data moves.

## Cancellation behavior

The migration loop checks `cancellationToken.IsCancellationRequested` before queueing each new insert. When cancellation is observed there, the method returns `false`.

Already-started insert tasks are not rolled back.

## Warm-up note

The migration package does not bootstrap repositories for you. If your source or destination uses warm-up driven infrastructure, such as seeded in-memory repositories, run `WarmUpAsync()` before starting the migration.

## When to use this package

Use it when you want:

- a simple named-source to named-destination migration helper
- a way to copy data between Repository Framework integrations without writing one-off glue code
- a migration utility for test environments, storage switches, or one-time backfills

For very large datasets or strict completion guarantees, treat the current package as a starting point and review the implementation in `RepositoryFramework.MigrationTools/Managers/MigrationManager.cs` before depending on it operationally.
