# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table

`Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table` adds an Azure Table Storage adapter for Repository Framework.

This package is not a column-per-property mapper. Each repository item is stored as:

- `PartitionKey`
- `RowKey`
- Azure-managed `Timestamp`
- a single JSON `Value` payload containing the model

That design makes key-based lookups simple, but most query behavior stays client-side.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table
```

## Architecture

The storage implementation writes one internal `ITableEntity` per repository item.

- model data is serialized into one `Value` column
- `PartitionKey` and `RowKey` come from the configured table key reader
- `InsertAsync` delegates to `UpdateAsync`, so inserts behave like upserts

This package is best when Azure Table Storage is mainly your key-addressable backing store.

## Registration APIs

### Direct builder registration

Available on all three Repository Framework patterns:

- `WithTableStorageAsync(...)`
- `WithTableStorage(...)`

Supported for:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

The async overloads are the real implementations. The sync overloads just block on them.

### Connection service registration

Available overloads:

- `WithTableStorage<T, TKey, TConnectionService>(...)`
- `WithTableStorage<T, TKey, TConnectionService, TKeyReader>(...)`

These are useful when table clients are resolved per tenant or per request.

## Direct builder example

This mirrors the integration tests.

```csharp
await builder.Services.AddRepositoryAsync<AppUser, AppUserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        tableStorageBuilder.Settings.TableName = "appusers";

        tableStorageBuilder
            .WithTableStorageKeyReader<TableStorageKeyReader>()
            .WithPartitionKey(x => x.Id, x => x.Id)
            .WithRowKey(x => x.Username)
            .WithTimestamp(x => x.CreationTime);
    }, name: "tablestorage");
});
```

Example custom key reader from the tests:

```csharp
internal sealed class TableStorageKeyReader : ITableStorageKeyReader<AppUser, AppUserKey>
{
    public (string PartitionKey, string RowKey) Read(
        AppUserKey key,
        TableStorageSettings<AppUser, AppUserKey> settings)
        => (key.Id.ToString(), string.Empty);

    public AppUserKey Read(
        AppUser entity,
        TableStorageSettings<AppUser, AppUserKey> settings)
        => new(entity.Id);
}
```

## Connection service example

This is the other pattern used in the integration tests.

```csharp
builder.Services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
{
    repositoryBuilder
        .WithTableStorage<AppUser, AppUserKey, TableStorageConnectionService, TableStorageKeyReader>(
            name: "tablestorage2");
});
```

The connection service returns a ready-to-use `TableClientWrapper<T, TKey>`.

```csharp
internal sealed class TableStorageConnectionService
    : IConnectionService<AppUser, AppUserKey, TableClientWrapper<AppUser, AppUserKey>>
{
    public TableClientWrapper<AppUser, AppUserKey> GetConnection(
        string entityName,
        string? factoryName = null)
    {
        return new TableClientWrapper<AppUser, AppUserKey>
        {
            Client = new TableClient("<connection-string>", entityName.ToLower()),
            Settings = new TableStorageSettings<AppUser, AppUserKey>
            {
                PartitionKey = "Id",
                RowKey = "Username",
                Timestamp = "CreationTime",
                PartitionKeyFromKeyFunction = x => x.Id.ToString(),
                PartitionKeyFunction = x => x.Id.ToString(),
                RowKeyFunction = x => x.Username,
                TimestampFunction = x => x.CreationTime
            }
        };
    }
}
```

## Configuration and defaults

`TableStorageConnectionSettings` exposes:

| Property | Notes |
| --- | --- |
| `ConnectionString` | Used when present. If both this and `EndpointUri` are set, connection string wins. |
| `EndpointUri` | Table service endpoint for managed identity mode. |
| `ManagedIdentityClientId` | Null means system-assigned identity. |
| `TableName` | Defaults to `typeof(T).Name`. |
| `ClientOptions` | Passed to Azure Tables SDK. |

Builder overload defaults:

- direct builder lifetime: `Singleton`
- connection service lifetime: `Scoped`

## Key mapping API

`ITableStorageRepositoryBuilder<T, TKey>` exposes:

| Method | What it configures |
| --- | --- |
| `WithPartitionKey(model, key)` | Model property and key-side extractor for `PartitionKey`. |
| `WithRowKey(model, key)` | Model property and key-side extractor for `RowKey`. |
| `WithRowKey(model)` | Only the entity-side row key extractor. |
| `WithTimestamp(model)` | The model property name used for timestamp-aware query translation. |
| `WithTableStorageKeyReader<T>()` | Custom `ITableStorageKeyReader<T, TKey>`. |

## Important key behavior

The default key reader reconstructs keys like this:

- for key-based operations it uses `PartitionKeyFromKeyFunction(key)` and `RowKeyFromKeyFunction(key)`
- for entity-to-key reconstruction it uses the configured entity-side key functions

That leads to an important caveat:

- `WithRowKey(x => x.Prop)` does not configure `RowKeyFromKeyFunction`
- it also does not populate the stored `RowKey` name used for query translation

So the one-argument `WithRowKey(...)` overload is not enough for many real key-based scenarios. In practice, when `TKey` is not directly representable by the partition key alone, prefer a custom `ITableStorageKeyReader<T, TKey>`.

## Lifecycle and provisioning

### Direct builder path

When you use `WithTableStorageAsync(...)` or `WithTableStorage(...)`, the package creates the table during registration by calling `CreateTableIfNotExistsAsync(...)`.

### Connection service path

When you use `WithTableStorage<T, TKey, TConnectionService>(...)`, table creation is your responsibility inside the connection service.

### Bootstrap behavior

`TableStorageRepository<T, TKey>.BootstrapAsync()` currently returns `true` and does nothing.

So for this package, provisioning is tied to direct registration, not to `WarmUpAsync()`.

## Query behavior

`QueryAsync(...)` supports Azure-side filtering only in a narrow set of cases.

What it tries to push down:

- only the first `Where` expression
- only comparisons involving mapped `PartitionKey`, `RowKey`, or `Timestamp`

Everything else is effectively local:

- filtering on normal model properties
- `OrderBy` / `ThenBy`
- paging semantics
- aggregate operations

The flow is:

1. optionally build a Table Storage filter string
2. enumerate rows from Azure Tables
3. deserialize JSON `Value` into models
4. apply Repository Framework filter operations in memory

## Query limitations to know about

- non-key model properties are never server-filtered because they live inside serialized JSON
- `Top` and `Skip` are partially applied during table enumeration before the final local filter/order phase
- aggregate methods such as `Count`, `Sum`, `Min`, `Max`, and `Average` materialize items and run in memory
- `BatchAsync(...)` is just a sequential loop, not an Azure Tables transactional batch

## CRUD semantics

- `InsertAsync` calls `UpdateAsync`, so it overwrites existing rows instead of failing on duplicates
- `UpdateAsync` uses `UpsertEntityAsync(..., TableUpdateMode.Replace)`
- `GetAsync` catches missing-row exceptions and returns `default`
- `DeleteAsync` directly calls `DeleteEntityAsync(...)`

If you need strict create-only behavior, build that check above the repository.

## Named registrations and factory behavior

The optional `name` parameter is a Repository Framework factory name, not a table name.

It is used to:

- resolve the correct repository registration
- resolve the matching key reader factory registration
- resolve the matching connection service factory registration

In connection-service mode, the repository calls:

```csharp
connectionService.GetConnection(typeof(T).Name, name)
```

So the service receives the CLR model name, not the configured `TableName`.

## CQRS examples

```csharp
await builder.Services.AddCommandAsync<User, UserKey>(async commandBuilder =>
{
    await commandBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id);
    });
});

await builder.Services.AddQueryAsync<User, UserKey>(async queryBuilder =>
{
    await queryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id);
    });
});
```

## When to use this package

Use it when you want:

- Azure Table Storage as a simple key-addressed repository backend
- custom partition/row key control
- a lightweight adapter where table rows hold JSON documents

Avoid it when you need rich server-side querying over many model properties, because that is not what the current implementation is optimized for.
