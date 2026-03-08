### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Abstractions

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Abstractions)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Abstractions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Abstractions)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Abstractions)

Core contracts, query primitives, registration builders, and runtime metadata for the Rystem repository ecosystem.

This is the foundation for the whole Repository Framework area documented in `src/Repository/README.md`.

It defines:

- repository, command, and query contracts
- DI registration builders for repository and CQRS setups
- key abstractions and key serialization rules
- query/filter primitives used by every storage provider
- business hooks and translation builders
- runtime metadata used by the API packages and diagnostics

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Abstractions
```

The current package metadata in `src/Repository/RepositoryFramework.Abstractions/RepositoryFramework.Abstractions.csproj` is:

- package id: `Rystem.RepositoryFramework.Abstractions`
- version: `10.0.6`
- target framework: `net10.0`

This package builds on top of `Rystem.DependencyInjection`.

---

## Package architecture

At a high level, this package is organized around these areas.

| Area | Purpose |
|---|---|
| Pattern interfaces | Low-level storage contracts such as `IRepositoryPattern<T, TKey>` |
| Consumer interfaces | DI-facing contracts such as `IRepository<T, TKey>` |
| Service registration builders | `AddRepository`, `AddCommand`, `AddQuery`, and their async variants |
| Query model | `QueryBuilder<T, TKey>`, `IFilterExpression`, paging, aggregate operations |
| Keys and state models | `IKey`, `IDefaultKey`, `Key<T1,...>`, `Entity<T, TKey>`, `State<T, TKey>` |
| Business and translation hooks | interceptors, examples, exposure rules, filter translation |
| Runtime metadata | `RepositoryFrameworkRegistry` and `RepositoryFrameworkService` |

---

## What this package provides

- Core storage contracts: `IRepositoryPattern<T, TKey>`, `ICommandPattern<T, TKey>`, `IQueryPattern<T, TKey>`
- Consumer contracts for DI: `IRepository<T, TKey>`, `ICommand<T, TKey>`, `IQuery<T, TKey>`
- Fluent registration builders: `AddRepository`, `AddCommand`, `AddQuery`, plus async variants
- Storage wiring primitives: `SetStorage`, `SetStorageWithOptions`, `SetStorageAndBuildOptions`, `SetStorageAndBuildOptionsAsync`, `SetStorageAndServiceConnection`
- Key helpers: `IKey`, `IDefaultKey`, `Key<T1>` through `Key<T1, T2, T3, T4, T5>`, and `KeySettings<TKey>`
- Query/filter model: `QueryBuilder<T, TKey>`, `FilterExpression`, `SerializableFilter`, metadata, paging, and aggregate operations
- Business hooks and translation builders for cross-cutting logic and model remapping
- Runtime registry data used by diagnostics and API exposure packages

---

## Mental model

This package does not ship a database provider by itself. Instead, it gives you a stable abstraction layer for three concerns:

1. Implement storage behavior behind repository contracts.
2. Register those implementations in DI, optionally with named factories.
3. Reuse the same query/filter/key model across in-memory, EF, blob, table, Cosmos, API client, and custom providers.

Most other packages in `src/Repository` are adapters built on top of these abstractions.

---

## Core interfaces

### Implementation interfaces

Implement one of these in your storage class.

```csharp
// Full repository (read + write)
public interface IRepositoryPattern<T, TKey> : ICommandPattern<T, TKey>, IQueryPattern<T, TKey>
    where TKey : notnull { }

// Write only
public interface ICommandPattern<T, TKey>
    where TKey : notnull
{
    Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default);
    Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default);
    Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default);
    IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
}

// Read only
public interface IQueryPattern<T, TKey>
    where TKey : notnull
{
    Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default);
    Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default);
    IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default);
    ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
}
```

Both query and command abstractions also inherit the non-generic bootstrap contract through their base interfaces, so storage implementations can participate in warm-up/startup bootstrap logic.

### Consumer interfaces

Inject these into application services instead of the pattern interfaces directly.

```csharp
IRepository<T, TKey>   // read + write + query extensions
ICommand<T, TKey>      // write only
IQuery<T, TKey>        // read only
```

The consumer interfaces are what the registration builders expose through DI and named factories.

---

## Basic registration

### Repository (read + write)

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
});
```

### CQRS split

```csharp
builder.Services.AddCommand<AppUser, Guid>(commandBuilder =>
{
    commandBuilder.SetStorage<AppUserCommandStorage>();
});

builder.Services.AddQuery<AppUser, Guid>(queryBuilder =>
{
    queryBuilder.SetStorage<AppUserQueryStorage>();
});
```

### Async registration

Use async variants when the builder itself needs asynchronous setup.

```csharp
await builder.Services.AddRepositoryAsync<AppUser, Guid>(async repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
    await Task.CompletedTask;
});

await builder.Services.AddCommandAsync<AppUser, Guid>(async commandBuilder =>
{
    commandBuilder.SetStorage<AppUserCommandStorage>();
    await Task.CompletedTask;
});

await builder.Services.AddQueryAsync<AppUser, Guid>(async queryBuilder =>
{
    queryBuilder.SetStorage<AppUserQueryStorage>();
    await Task.CompletedTask;
});
```

---

## Storage registration primitives

All repository builders eventually flow through the storage registration methods in `RepositoryBaseBuilder`.

### `SetStorage<TStorage>()`

Use this when the storage can be resolved directly from DI.

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
});
```

### `SetStorageWithOptions<TStorage, TStorageOptions>()`

Use this when the storage consumes a simple options object implementing `IFactoryOptions`.

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorageWithOptions<AppUserStorage, AppUserStorageOptions>(options =>
    {
        options.ConnectionString = builder.Configuration["ConnectionStrings:Default"]!;
    });
});
```

### `SetStorageAndBuildOptions<TStorage, TStorageOptions, TConnection>()`

Use this when the builder must transform fluent configuration into a factory-backed connection/options object.

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorageAndBuildOptions<AppUserStorage, AppUserBuilder, AppUserConnection>(options =>
    {
        options.Name = "users";
    });
});
```

### `SetStorageAndBuildOptionsAsync<TStorage, TStorageOptions, TConnection>()`

Same as the previous one, but the options builder can perform asynchronous work.

```csharp
await builder.Services.AddRepositoryAsync<AppUser, Guid>(async repositoryBuilder =>
{
    await repositoryBuilder.SetStorageAndBuildOptionsAsync<AppUserStorage, AppUserBuilder, AppUserConnection>(options =>
    {
        options.Name = "users";
    });
});
```

### `SetStorageAndServiceConnection<TStorage, TConnectionService, TConnectionClient>()`

Use this when the actual connection or client is request-aware or tenant-aware.

```csharp
public sealed class TenantAwareConnectionService : IConnectionService<Order, Guid, DbConnection>
{
    private readonly ITenantResolver _tenantResolver;

    public TenantAwareConnectionService(ITenantResolver tenantResolver)
        => _tenantResolver = tenantResolver;

    public DbConnection GetConnection(string entityName, string? factoryName = null)
    {
        var connectionString = _tenantResolver.GetConnectionString(entityName, factoryName);
        return new SqlConnection(connectionString);
    }
}

builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorageAndServiceConnection<OrderStorage, TenantAwareConnectionService, DbConnection>();
});
```

### Lifetime and naming

- All `SetStorage*` methods default to `ServiceLifetime.Scoped`.
- Every `SetStorage*` overload accepts an optional `name` and `serviceLifetime`.
- The `name` is stored as the repository `FactoryName` in the runtime registry and is how named factories resolve a specific storage.

---

## Named registrations and factories

One model can be backed by multiple repositories or providers at the same time. The common pattern is to register each one with a `name`, then resolve it through `IFactory<TService>`.

This is heavily used in the integration tests and sample web API.

```csharp
builder.Services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<PrimaryAppUserStorage>(name: "primary");
    repositoryBuilder.SetStorage<ArchiveAppUserStorage>(name: "archive");
});
```

```csharp
public sealed class AppUserImportService
{
    private readonly IFactory<IRepository<AppUser, AppUserKey>> _repositoryFactory;

    public AppUserImportService(IFactory<IRepository<AppUser, AppUserKey>> repositoryFactory)
        => _repositoryFactory = repositoryFactory;

    public Task<AppUser?> GetFromArchiveAsync(AppUserKey key, CancellationToken cancellationToken = default)
    {
        var repository = _repositoryFactory.Create("archive")!;
        return repository.GetAsync(key, cancellationToken);
    }
}
```

The same pattern works for:

- `IFactory<IRepository<T, TKey>>`
- `IFactory<IQuery<T, TKey>>`
- `IFactory<ICommand<T, TKey>>`

This makes it easy to switch between environments, fallback providers, multi-tenant routing, cache layers, or migration tooling without changing the domain-facing contract.

---

## Keys

Key handling is more flexible than the old docs suggest. `KeySettings<TKey>` supports several strategies.

### Primitive and framework-native keys

These work out of the box:

- primitive numeric types
- `string`
- `Guid`
- `DateTime`
- `DateTimeOffset`
- `TimeSpan`
- `nint`
- `nuint`

```csharp
builder.Services.AddRepository<AppUser, Guid>(...);
builder.Services.AddRepository<Product, int>(...);
builder.Services.AddRepository<Session, string>(...);
```

### Composite keys with `Key<T1, T2, ...>`

You can use the built-in `Key<>` helpers for common composite-key scenarios.

```csharp
builder.Services.AddRepository<Order, Key<int, string>>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<OrderStorage>();
});

var key = new Key<int, string>(42, "region-eu");
var order = await repository.GetAsync(key);
```

### Custom keys with `IKey`

Implement `IKey` when you want full control over string serialization.

```csharp
public sealed class AppUserKey : IKey
{
    public string TenantId { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public static IKey Parse(string keyAsString)
    {
        var parts = keyAsString.Split('-', 2);
        return new AppUserKey { TenantId = parts[0], UserId = Guid.Parse(parts[1]) };
    }

    public string AsString() => $"{TenantId}-{UserId}";
}
```

### Property-based keys with `IDefaultKey`

Implement `IDefaultKey` when you want the framework to serialize all key properties using the default separator.

```csharp
public sealed class OrderKey : IDefaultKey
{
    public int OrderId { get; set; }
    public string Region { get; set; } = string.Empty;
}

builder.Services.AddDefaultSeparatorForDefaultKeyInterface("$$$");
```

### Plain POCO keys also work

If `TKey` is not primitive, not `IKey`, and not `IDefaultKey`, `KeySettings<TKey>` still supports it as long as the type exposes properties. In that case it falls back to JSON serialization.

This behavior is covered by the key tests and the class-key repository tests.

```csharp
public sealed class ClassAnimalKey
{
    public string Area { get; set; } = string.Empty;
    public int Id { get; set; }
    public Guid CorrelationId { get; set; }

    public ClassAnimalKey() { }

    public ClassAnimalKey(string area, int id, Guid correlationId)
        => (Area, Id, CorrelationId) = (area, id, correlationId);
}

builder.Services.AddRepository<ClassAnimal, ClassAnimalKey>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<ClassAnimalRepository>();
});
```

The practical rule is simple: if the key can be converted to and from a stable string representation, the abstraction layer can carry it through providers and APIs.

---

## Core models

### `State<T, TKey>`

All command methods return `State<T, TKey>`.

```csharp
public class State<T, TKey>
{
    public bool IsOk { get; set; }
    public Entity<T, TKey>? Entity { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public bool HasEntity { get; }
}
```

Useful factory helpers include:

```csharp
return State.Ok(value, key);
return State.NotOk(value, key);
return State.Default(isOk, value, key);

return await State.OkAsTask(value, key);
return await State.NotOkAsTask(value, key);
```

### `Entity<T, TKey>`

Query operations return `Entity<T, TKey>` so callers can keep both the key and the value.

```csharp
public class Entity<T, TKey>
{
    public TKey? Key { get; set; }
    public T? Value { get; set; }
    public bool HasValue { get; }
    public bool HasKey { get; }

    public State<T, TKey> ToOkState();
    public State<T, TKey> ToNotOkState();
}
```

### Batch models

Use `BatchOperations<T, TKey>` and `BatchResult<T, TKey>` when the provider supports grouped write operations.

```csharp
var batch = new BatchOperations<AppUser, Guid>()
    .AddInsert(Guid.NewGuid(), new AppUser { Name = "Alice" })
    .AddUpdate(existingId, updatedUser)
    .AddDelete(oldId);

await foreach (var result in command.BatchAsync(batch))
{
    // result.Command
    // result.Key
    // result.State
}
```

The command and repository consumers also expose the fluent helper:

```csharp
await repository.CreateBatchOperation()
    .AddInsert(key1, value1)
    .AddDelete(key2)
    .ExecuteAsync()
    .ToListAsync();
```

---

## Fluent query builder

`IQuery<T, TKey>` and `IRepository<T, TKey>` expose extension methods that create a `QueryBuilder<T, TKey>`.

```csharp
var items = await repository
    .Where(x => x.IsActive)
    .OrderByDescending(x => x.Price)
    .Skip(20)
    .Take(10)
    .ToListAsync();

foreach (var entity in items)
    Console.WriteLine(entity.Value!.Name);

List<Product> values = await repository
    .Where(x => x.IsActive)
    .ToListAsEntityAsync();
```

### Main query methods

| Method | Description |
|---|---|
| `Where(expr)` | Filter by entity predicate |
| `WhereKey(expr)` | Filter by key predicate |
| `Take(n)` | Limit results |
| `Skip(n)` | Offset results |
| `OrderBy(expr)` | Ascending sort |
| `OrderByDescending(expr)` | Descending sort |
| `ThenBy(expr)` | Secondary ascending sort |
| `ThenByDescending(expr)` | Secondary descending sort |
| `AnyAsync(expr?)` | Returns `bool` |
| `FirstOrDefaultAsync(expr?)` | Returns `Entity<T, TKey>?` |
| `FirstAsync(expr?)` | Returns `Entity<T, TKey>` |
| `FirstOrDefaultByKeyAsync(expr?)` | Returns first entity filtered on key |
| `FirstByKeyAsync(expr?)` | Returns first entity filtered on key |
| `PageAsync(page, pageSize)` | Returns `Page<T, TKey>` |
| `ToListAsync()` | Returns `List<Entity<T, TKey>>` |
| `ToListAsEntityAsync()` | Returns `List<T>` |
| `QueryAsync()` | Returns `IAsyncEnumerable<Entity<T, TKey>>` |
| `QueryAsEntityAsync()` | Returns `IAsyncEnumerable<T>` |
| `CountAsync()` | Aggregate count |
| `SumAsync(expr)` | Aggregate sum |
| `AverageAsync(expr)` | Aggregate average |
| `MaxAsync(expr)` | Aggregate max |
| `MinAsync(expr)` | Aggregate min |
| `OperationAsync(operation)` | Custom aggregate operation |
| `AddMetadata(key, value)` | Attach metadata to the filter |

### Important behavior notes

- `PageAsync` validates that `page >= 1` and `pageSize >= 1`.
- `PageAsync` calculates total pages using a separate `Count` operation.
- `GroupByAsync` is not translated into provider-native grouping. It first enumerates `QueryAsync()` and then groups client-side.

```csharp
Page<AppUser, AppUserKey> firstPage = await repository
    .Where(x => x.Id > 0)
    .OrderByDescending(x => x.Id)
    .PageAsync(page: 1, pageSize: 20);
```

---

## Filter model and custom storage integration

`IFilterExpression` is the portable query description that storage providers receive in `QueryAsync` and `OperationAsync`.

This is the bridge between LINQ-like consumer code and your provider implementation.

### What it can do

- carry ordered filter operations such as `Where`, `OrderBy`, `Skip`, and `Take`
- serialize itself through `Serialize()` into a `SerializableFilter`
- produce a stable cache/transport key through `ToKey()`
- apply the filter directly to in-memory collections or queryables
- expose filter metadata and parsed values through `GetFilters(...)`
- translate mapped expressions through `Translate(...)`

### Typical custom repository usage

For in-memory or adapter-style repositories, the simplest pattern is to let the filter apply itself.

```csharp
public async IAsyncEnumerable<Entity<AppUser, AppUserKey>> QueryAsync(
    IFilterExpression filter,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await Task.Yield();

    foreach (var item in filter.Apply(_items))
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return Entity.Default(item, new AppUserKey(item.Id));
    }
}
```

### Metadata and caching scenarios

`AddMetadata(key, value)` is useful when your provider needs extra information that is not part of the entity predicate itself, such as tenant, partition, read consistency, or a custom projection mode.

```csharp
var query = repository
    .Where(x => x.IsActive)
    .AddMetadata("tenant", "eu-west")
    .AddMetadata("mode", "snapshot");
```

The metadata becomes part of the filter representation available to the storage layer through `IFilterExpression`.

---

## Business hooks

Business interceptors let you wrap every repository operation with validation, auditing, policy checks, or side effects.

### Register inline during repository setup

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();

    repositoryBuilder
        .AddBusiness()
        .AddBusinessBeforeInsert<AppUserValidation>()
        .AddBusinessAfterInsert<AppUserAudit>()
        .AddBusinessBeforeDelete<AppUserDeletionGuard>();
});
```

### Register independently

```csharp
builder.Services
    .AddBusinessForRepository<AppUser, Guid>()
    .AddBusinessBeforeUpdate<AppUserUpdateValidation>()
    .AddBusinessAfterUpdate<AppUserUpdateAudit>();
```

### Scan assemblies

```csharp
builder.Services.ScanBusinessForRepositoryFramework();
builder.Services.ScanBusinessForRepositoryFramework(typeof(MyAssemblyMarker).Assembly);
```

`ScanBusinessForRepositoryFramework()` inspects all before/after business interfaces for the repositories already registered in the framework registry.

### Available hook families

| Interface | Triggered |
|---|---|
| `IRepositoryBusinessBeforeInsert<T, TKey>` | Before `InsertAsync` |
| `IRepositoryBusinessAfterInsert<T, TKey>` | After `InsertAsync` |
| `IRepositoryBusinessBeforeUpdate<T, TKey>` | Before `UpdateAsync` |
| `IRepositoryBusinessAfterUpdate<T, TKey>` | After `UpdateAsync` |
| `IRepositoryBusinessBeforeDelete<T, TKey>` | Before `DeleteAsync` |
| `IRepositoryBusinessAfterDelete<T, TKey>` | After `DeleteAsync` |
| `IRepositoryBusinessBeforeBatch<T, TKey>` | Before `BatchAsync` |
| `IRepositoryBusinessAfterBatch<T, TKey>` | After `BatchAsync` |
| `IRepositoryBusinessBeforeGet<T, TKey>` | Before `GetAsync` |
| `IRepositoryBusinessAfterGet<T, TKey>` | After `GetAsync` |
| `IRepositoryBusinessBeforeExist<T, TKey>` | Before `ExistAsync` |
| `IRepositoryBusinessAfterExist<T, TKey>` | After `ExistAsync` |
| `IRepositoryBusinessBeforeQuery<T, TKey>` | Before `QueryAsync` |
| `IRepositoryBusinessAfterQuery<T, TKey>` | After `QueryAsync` |
| `IRepositoryBusinessBeforeOperation<T, TKey>` | Before `OperationAsync` |
| `IRepositoryBusinessAfterOperation<T, TKey>` | After `OperationAsync` |

Priority controls ordering. Lower values run first.

---

## Translation mapper

The translation builder is what lets consumer code keep writing filters against the domain model while the provider translates them to a different storage model.

This is covered by the translation tests.

```csharp
builder.Services.AddRepository<Translatable, string>(repositoryBuilder =>
{
    repositoryBuilder
        .SetStorage<TranslatableRepository>()
        .Translate<ToTranslateSomething>()
            .With(x => x.Foolish, x => x.Folle)
            .With(x => x.Id, x => x.IdccnlValidita)
            .With(x => x.CcnlId, x => x.Idccnl)
            .With(x => x.From, x => x.DataInizio)
            .With(x => x.To, x => x.DataFine)
        .AndTranslate<ToTranslateSomethingElse>()
            .With(x => x.Foolish, x => x.Folle)
            .With(x => x.Id, x => x.IdccnlValidita)
            .With(x => x.CcnlId, x => x.Idccnl);
});
```

Common helpers include:

- `Translate<TTranslated>()`
- `With(domainProperty, translatedProperty)`
- `WithKey(...)`
- `WithSamePorpertiesName()`
- `AndTranslate<TTranslated>()`

This is especially useful for EF models, API DTOs, cache projections, or legacy schemas where names do not line up with the public domain model.

---

## API exposure and examples

These settings matter when the repository is later surfaced through `Rystem.RepositoryFramework.Api.Server`.

### Control exposed methods

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();

    repositoryBuilder.SetExposable(RepositoryMethods.All);
    repositoryBuilder.SetOnlyQueryExposable();
    repositoryBuilder.SetOnlyCommandExposable();
    repositoryBuilder.SetExposable(RepositoryMethods.Get | RepositoryMethods.Query);
    repositoryBuilder.SetNotExposable();
});
```

`RepositoryMethods` also includes `Bootstrap`, so the bootstrap endpoint can be included or hidden when you expose repositories as APIs.

### Provide OpenAPI examples

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
    repositoryBuilder.SetExamples(
        new AppUser { Name = "Alice", Email = "alice@example.com" },
        Guid.NewGuid());
});
```

---

## Bootstrap and warm-up lifecycle

If a storage implementation needs startup work such as schema creation, seeding, or client warm-up, implement the bootstrap contract.

```csharp
public sealed class AppUserStorage : IRepositoryPattern<AppUser, Guid>, IBootstrapPattern
{
    public async ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);
        return true;
    }

    // other IRepositoryPattern members omitted
}
```

Important: bootstrap does not run just because `AddRepository` was called.

In the real tests and sample web API, bootstrap is executed by calling `WarmUpAsync()` on the built service provider:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

That is the practical startup lifecycle to document and rely on.

---

## Post-build callbacks

You can run synchronous or asynchronous logic after the repository builder delegate finishes.

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();

    repositoryBuilder.AfterBuild = () =>
    {
        Console.WriteLine("AppUser repository registered.");
    };

    repositoryBuilder.AfterBuildAsync = async () =>
    {
        await ValidateSchemaAsync();
    };
});
```

The important detail is timing: `AfterBuild` and `AfterBuildAsync` run after the whole `AddRepository` or `AddCommand` or `AddQuery` builder delegate returns, not after each individual `SetStorage*` call.

---

## Runtime registry

`RepositoryFrameworkRegistry` is registered automatically as a singleton. It tracks every repository, command, and query configured through the framework.

```csharp
public sealed class RepositoryDiagnosticsService
{
    private readonly RepositoryFrameworkRegistry _registry;

    public RepositoryDiagnosticsService(RepositoryFrameworkRegistry registry)
        => _registry = registry;

    public void PrintAll()
    {
        foreach (var (_, service) in _registry.Services)
        {
            Console.WriteLine(
                $"{service.ModelType.Name} {service.Type} -> {service.ImplementationType.Name} [{service.FactoryName}]");
        }
    }
}
```

### `RepositoryFrameworkService` metadata

| Property | Description |
|---|---|
| `ModelType` | The domain model type |
| `KeyType` | The key type |
| `InterfaceType` | The DI-facing contract |
| `ImplementationType` | The storage implementation |
| `ExposedMethods` | API-visible methods |
| `ServiceLifetime` | DI lifetime used by registration |
| `Type` | `Repository`, `Command`, or `Query` |
| `FactoryName` | Named registration key, empty when default |
| `Policies` | Authorization policy names used by UI/API tooling |

This registry is what later packages use to expose repositories as APIs, create documentation, or inspect configured services.

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Infrastructure.InMemory` | In-memory storage and test-friendly provider |
| `Rystem.RepositoryFramework.Infrastructure.EntityFramework` | EF Core adapter |
| `Rystem.RepositoryFramework.Api.Server` | Auto-generate REST APIs from repository registrations |
| `Rystem.RepositoryFramework.Api.Client` | .NET and TypeScript client adapters |

If you are continuing through the repository area, this README is the conceptual base to read before the infrastructure packages.
