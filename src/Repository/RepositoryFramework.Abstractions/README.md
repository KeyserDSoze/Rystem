# Rystem.RepositoryFramework.Abstractions

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Abstractions)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Abstractions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Abstractions)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Abstractions)

Core contracts and dependency-injection extensions for the Repository Pattern and CQRS in Rystem.

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Abstractions
```

---

## What this package provides

- Core implementation interfaces: `IRepositoryPattern<T, TKey>`, `ICommandPattern<T, TKey>`, `IQueryPattern<T, TKey>`
- Consumer interfaces for injection: `IRepository<T, TKey>`, `ICommand<T, TKey>`, `IQuery<T, TKey>`
- Key helpers: `IKey`, `IDefaultKey`, `Key<T1>` ... `Key<T1,T2,T3,T4,T5>`
- DI extensions: `AddRepository`, `AddCommand`, `AddQuery`, and their async variants
- Fluent query builder: `QueryBuilder<T, TKey>` with filter, sort, paging, aggregation
- Business hooks: `Before`/`After` interceptors for every repository operation
- Translation mapper: map domain models to storage models in query expressions

---

## Core interfaces

### Implementation interfaces

Implement one of these in your storage class:

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

### Consumer interfaces

Inject these into your services — never inject the pattern interfaces directly:

```csharp
IRepository<T, TKey>   // IRepositoryPattern<T, TKey> + query helpers + command helpers
ICommand<T, TKey>      // ICommandPattern<T, TKey>
IQuery<T, TKey>        // IQueryPattern<T, TKey>
```

---

## DI Setup

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

### Async setup

Use async variants when storage setup requires I/O (e.g. loading connection strings):

```csharp
await builder.Services.AddRepositoryAsync<AppUser, Guid>(async repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
    await Task.CompletedTask;
});

// Also available:
await builder.Services.AddCommandAsync<AppUser, Guid>(...);
await builder.Services.AddQueryAsync<AppUser, Guid>(...);
```

---

## Keys

### Primitive keys

Any non-null primitive works directly:

```csharp
builder.Services.AddRepository<AppUser, Guid>(...);
builder.Services.AddRepository<Product, int>(...);
builder.Services.AddRepository<Session, string>(...);
```

### Composite keys with `Key<T1, T2, ...>`

Up to 5 generic parameters:

```csharp
// Two-part composite key
builder.Services.AddRepository<Order, Key<int, string>>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<OrderStorage>();
});

// Usage
var key = new Key<int, string>(42, "region-eu");
await repository.GetAsync(key);
```

### Custom key with `IKey`

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

### `IDefaultKey` — property-based composite key

Implement `IDefaultKey` on a class that has properties. The framework serializes all properties using `"|||"` as separator by default:

```csharp
public sealed class OrderKey : IDefaultKey
{
    public int OrderId { get; set; }
    public string Region { get; set; } = string.Empty;
}

// Change separator if needed (call before AddRepository):
builder.Services.AddDefaultSeparatorForDefaultKeyInterface("$$$");
```

---

## Models

### `State<T, TKey>`

Returned by all command operations:

```csharp
public class State<T, TKey>
{
    public bool IsOk { get; set; }
    public Entity<T, TKey>? Entity { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public bool HasEntity { get; }

    // Implicit conversions
    // bool state = state;       // true if IsOk
    // int  code  = state;       // Code ?? 0
}
```

Static factory helpers:

```csharp
return State.Ok(value, key);
return State.NotOk(value, key);
return State.Default(isOk, value, key);

// Async variants
return await State.OkAsTask(value, key);
return await State.NotOkAsTask(value, key);
```

### `Entity<T, TKey>`

Wrapper returned by query operations:

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

### `BatchOperations<T, TKey>`

Fluent builder for atomic multi-operation batches:

```csharp
var batch = new BatchOperations<AppUser, Guid>()
    .AddInsert(Guid.NewGuid(), new AppUser { Name = "Alice" })
    .AddUpdate(existingId, updatedUser)
    .AddDelete(oldId);

await foreach (var result in command.BatchAsync(batch))
{
    // result.Command — CommandType.Insert | Update | Delete
    // result.Key     — TKey
    // result.State   — State<T, TKey>
}
```

Or use the extension fluent builder:

```csharp
await command.CreateBatchOperation()
    .AddInsert(key1, value1)
    .AddDelete(key2)
    .ExecuteAsync();
```

---

## Fluent Query Builder

`IQuery<T, TKey>` and `IRepository<T, TKey>` expose extension methods that return a `QueryBuilder<T, TKey>`:

```csharp
// Injected
private readonly IQuery<Product, int> _query;

// Filtering
var results = await _query
    .Where(x => x.Price > 10)
    .OrderByDescending(x => x.Price)
    .Take(20)
    .Skip(40)
    .ToListAsync();

// Results are List<Entity<Product, int>> — use .Value to get the entity
foreach (var entity in results)
    Console.WriteLine(entity.Value!.Name);

// Without key wrapper
List<Product> products = await _query
    .Where(x => x.IsActive)
    .ToListAsEntityAsync();
```

### Available `QueryBuilder<T, TKey>` methods

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
| `GroupByAsync<TProperty>(expr)` | Group results |
| `AnyAsync(expr?)` | Returns `bool` |
| `FirstOrDefaultAsync(expr?)` | Returns `Entity<T,TKey>?` |
| `FirstAsync(expr?)` | Returns `Entity<T,TKey>` (throws if empty) |
| `FirstOrDefaultByKeyAsync(expr?)` | Filter by key, return first |
| `FirstByKeyAsync(expr?)` | Filter by key, return first (throws if empty) |
| `PageAsync(page, pageSize)` | Returns `Page<T,TKey>` with total count and pages |
| `ToListAsync()` | Returns `List<Entity<T,TKey>>` |
| `ToListAsEntityAsync()` | Returns `List<T>` (no key) |
| `QueryAsync()` | Returns `IAsyncEnumerable<Entity<T,TKey>>` |
| `QueryAsEntityAsync()` | Returns `IAsyncEnumerable<T>` (no key) |
| `CountAsync()` | Returns `long` |
| `SumAsync<TProperty>(expr)` | Aggregate sum |
| `AverageAsync<TProperty>(expr)` | Aggregate average |
| `MaxAsync<TProperty>(expr)` | Aggregate max |
| `MinAsync<TProperty>(expr)` | Aggregate min |
| `OperationAsync<TProperty>(operation)` | Custom aggregate operation |
| `AddMetadata(key, value)` | Attach custom metadata to the filter expression |

### Pagination

```csharp
Page<Product, int> page = await _query
    .Where(x => x.IsActive)
    .OrderBy(x => x.Name)
    .PageAsync(page: 1, pageSize: 20);

// page.Items     — List<Entity<Product, int>>
// page.TotalCount — long
// page.Pages     — long (total page count)
```

---

## Business Hooks

Business interceptors run before or after each repository operation. They are chained by `Priority` (lower value runs first; same priority overrides the previous):

### Register inline with the builder

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

### Register independently (CQRS or after-the-fact)

```csharp
builder.Services
    .AddBusinessForRepository<AppUser, Guid>()
    .AddBusinessBeforeUpdate<AppUserUpdateValidation>()
    .AddBusinessAfterUpdate<AppUserUpdateAudit>();
```

### Auto-scan all business classes in assemblies

```csharp
builder.Services.ScanBusinessForRepositoryFramework();
// or specific assemblies:
builder.Services.ScanBusinessForRepositoryFramework(typeof(MyAssemblyMarker).Assembly);
```

### All available hooks

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

### Implementing a business hook

```csharp
public sealed class AppUserValidation : IRepositoryBusinessBeforeInsert<AppUser, Guid>
{
    public int Priority => 1;

    public Task<State<AppUser, Guid>> BeforeInsertAsync(
        Entity<AppUser, Guid> entity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entity.Value?.Email))
            return State.NotOkAsTask<AppUser, Guid>(entity.Value!, entity.Key!.Value);

        return State.OkAsTask(entity);
    }
}
```

---

## Translation Mapper

Map your domain model to a different storage model (e.g. EF entity, DTO) while keeping query expressions translatable:

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();

    repositoryBuilder
        .Translate<AppUserEntity>()
        .With(x => x.Id,       x => x.ExternalId)
        .With(x => x.Username, x => x.DisplayName)
        .With(x => x.Email,    x => x.EmailAddress)
        .WithKey(x => x,       x => x.PrimaryKey);     // key mapping
});
```

**With same property names** (when domain model and storage model share property names):

```csharp
repositoryBuilder
    .Translate<AppUserEntity>()
    .WithSamePorpertiesName();
```

**Chain multiple translations:**

```csharp
repositoryBuilder
    .Translate<AppUserEntity>()
    .With(x => x.Id, x => x.ExternalId)
    .AndTranslate<AppUserCache>()
    .With(x => x.Id, x => x.CacheId);
```

---

## Exposing / Hiding Methods for API

Control which repository operations are exposed when using `Rystem.RepositoryFramework.Api.Server`:

```csharp
builder.Services.AddRepository<AppUser, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();

    // Expose all (default)
    repositoryBuilder.SetExposable(RepositoryMethods.All);

    // Expose only reads
    repositoryBuilder.SetOnlyQueryExposable();

    // Expose only writes
    repositoryBuilder.SetOnlyCommandExposable();

    // Expose specific methods
    repositoryBuilder.SetExposable(RepositoryMethods.Get | RepositoryMethods.Query);

    // Hide completely
    repositoryBuilder.SetNotExposable();
});
```

`RepositoryMethods` flags:

```csharp
[Flags]
public enum RepositoryMethods
{
    None      = 0,
    Insert    = 1,
    Update    = 2,
    Delete    = 4,
    Batch     = 8,
    Exist     = 16,
    Get       = 32,
    Query     = 64,
    Operation = 128,
    Bootstrap = 256,
    All       = Insert | Update | Delete | Batch | Exist | Get | Query | Operation | Bootstrap
}
```

---

## Swagger / OpenAPI Examples

Provide sample data for generated API documentation:

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

## Bootstrap pattern

If your storage class needs to run initialisation logic on startup (create tables, seed data, warm up connections), implement `IBootstrapPattern`:

```csharp
public class AppUserStorage : IRepositoryPattern<AppUser, Guid>, IBootstrapPattern
{
    public async ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
    {
        // create schema, seed data, pre-warm connections …
        await EnsureTableExistsAsync(cancellationToken);
        return true; // return false to signal failure
    }

    // … other IRepositoryPattern methods
}
```

Bootstrap runs automatically during application startup. The `RepositoryMethods.Bootstrap` flag in `SetExposable` controls whether the bootstrap endpoint is exposed via the REST API.

---

## Dynamic connections (`IConnectionService`)

When the connection string (or client) depends on the request context — for example in multi-tenant apps — implement `IConnectionService<T, TKey, TConnectionClient>` and register it with `SetStorageAndServiceConnection`:

```csharp
// 1. Connection service: resolves the right client for the given entity type
public sealed class TenantAwareDbConnectionService : IConnectionService<Order, Guid, DbConnection>
{
    private readonly ITenantResolver _tenantResolver;

    public TenantAwareDbConnectionService(ITenantResolver tenantResolver)
        => _tenantResolver = tenantResolver;

    public DbConnection GetConnection(string entityName, string? factoryName = null)
    {
        var connString = _tenantResolver.GetConnectionString(entityName);
        return new SqlConnection(connString);
    }
}

// 2. Storage: receives the connection client via DI
public sealed class OrderStorage : IRepositoryPattern<Order, Guid>
{
    private readonly IConnectionService<Order, Guid, DbConnection> _connectionService;

    public OrderStorage(IConnectionService<Order, Guid, DbConnection> connectionService)
        => _connectionService = connectionService;

    public async Task<State<Order, Guid>> InsertAsync(Guid key, Order value, CancellationToken ct = default)
    {
        using var conn = _connectionService.GetConnection("Order");
        // … use conn
        return State.Ok(value, key);
    }
    // … other methods
}

// 3. Registration
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.SetStorageAndServiceConnection<
        OrderStorage,
        TenantAwareDbConnectionService,
        DbConnection>();
});
```

---

## Post-build callbacks

Run code immediately after the repository is registered (e.g. validate configuration, emit telemetry):

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

Both `AfterBuild` (synchronous) and `AfterBuildAsync` are called after the `SetStorage*` call completes.

---

## Runtime registry (`RepositoryFrameworkRegistry`)

`RepositoryFrameworkRegistry` is a singleton registered automatically. Inject it to enumerate every repository registered in the application:

```csharp
public class RepositoryDiagnosticsService
{
    private readonly RepositoryFrameworkRegistry _registry;

    public RepositoryDiagnosticsService(RepositoryFrameworkRegistry registry)
        => _registry = registry;

    public void PrintAll()
    {
        foreach (var (key, service) in _registry.Services)
        {
            Console.WriteLine(
                $"{service.ModelType.Name} ({service.Type}) " +
                $"→ {service.ImplementationType.Name} " +
                $"[{service.ExposedMethods}]");
        }
    }

    // Get services for a specific model type
    public IEnumerable<RepositoryFrameworkService> GetForModel<T>()
        => _registry.GetByModel(typeof(T));
}
```

### `RepositoryFrameworkService` properties

| Property | Type | Description |
|---|---|---|
| `ModelType` | `Type` | The domain model type (`T`) |
| `KeyType` | `Type` | The key type (`TKey`) |
| `InterfaceType` | `Type` | The consumer interface (`IRepository<T,TKey>`, etc.) |
| `ImplementationType` | `Type` | The concrete storage class |
| `ExposedMethods` | `RepositoryMethods` | Which operations are exposed via the REST API |
| `ServiceLifetime` | `ServiceLifetime` | DI lifetime |
| `Type` | `PatternType` | `Repository`, `Command`, or `Query` |
| `FactoryName` | `string` | Named factory key (empty = default) |
| `Policies` | `List<string>` | Authorization policy names (used by the Web UI) |

---

## Related Packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Infrastructure.InMemory` | In-memory storage for testing |
| `Rystem.RepositoryFramework.Infrastructure.EntityFramework` | EF Core storage |
| `Rystem.RepositoryFramework.Api.Server` | Auto-generate REST API from repositories |
| `Rystem.RepositoryFramework.Api.Client` | TypeScript / .NET client for repository API |
