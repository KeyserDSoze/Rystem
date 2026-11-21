# Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql

`Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql` adds a Cosmos DB SQL API adapter for Repository Framework.

It is a thin integration: one container per model, one Cosmos item per entity, and a fixed partition key strategy based on `/id`.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
```

## Architecture

At registration time the builder:

1. creates the Cosmos database if missing
2. creates the container if missing
3. uses `/id` as the container partition key path

At write time the repository builds an item like this:

- `id`: string form of `TKey` using `KeySettings<TKey>.Instance.AsString(key)`
- every public property from `T`

So the Cosmos item key and the Repository Framework key are tightly coupled.

## Registration APIs

Available on all three patterns:

- `WithCosmosSqlAsync(...)`
- `WithCosmosSql(...)`

Supported for:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

The sync overloads are wrappers over the async implementations.

There is no service-connection registration variant in this package.

## Example with a simple key

This mirrors the API tests.

```csharp
await builder.Services.AddRepositoryAsync<SuperUser, string>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "BigDatabase";
        cosmosBuilder.WithId(x => x.Email!);
    });
});
```

## Example with a custom key type

This follows the integration tests.

```csharp
await builder.Services.AddRepositoryAsync<AppUser, AppUserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "unittestdatabase";
        cosmosBuilder.WithId(x => new AppUserKey(x.Id));
    }, name: "cosmos");
});
```

## Builder API

`ICosmosSqlRepositoryBuilder<T, TKey>` exposes:

| Member | Purpose |
| --- | --- |
| `Settings` | Cosmos connection and provisioning settings |
| `WithId(expr)` | Registers a default key manager that reads `TKey` from a model instance |
| `WithKeyManager<T>()` | Registers a custom `ICosmosSqlKeyManager<T, TKey>` |

## Configuration and defaults

`CosmosSqlConnectionSettings` exposes:

| Property | Notes |
| --- | --- |
| `ConnectionString` | Used when present. If both this and `EndpointUri` are set, connection string wins. |
| `EndpointUri` | Used for managed identity mode. |
| `ManagedIdentityClientId` | Null means system-assigned identity. |
| `DatabaseName` | Required in practice. |
| `ContainerName` | Defaults to `typeof(T).Name`. |
| `ClientOptions` | Passed to `CosmosClient`. |
| `DatabaseOptions` | Used only during `CreateDatabaseIfNotExistsAsync(...)`. |
| `ContainerOptions` | Used only during `CreateContainerIfNotExistsAsync(...)`. |

Default lifetime:

- `Singleton`

## Managed identity note

Managed identity is used only when:

- `ConnectionString == null`
- and `EndpointUri != null`

If both are configured, the builder silently uses the connection string path.

## Provisioning and lifecycle

Database and container creation happen during registration, not during warm-up.

That means `WithCosmosSqlAsync(...)` eagerly performs the create-if-not-exists calls.

`CosmosSqlRepository<T, TKey>.BootstrapAsync()` currently returns `true` and does nothing.

## Key behavior

There are two separate key concerns in this package.

### 1. How the repository addresses Cosmos items

CRUD methods always use:

```csharp
KeySettings<TKey>.Instance.AsString(key)
```

That string becomes both:

- the Cosmos item `id`
- the partition key value

### 2. How query results rebuild `TKey`

`WithId(...)` and `WithKeyManager<T>()` are used when the repository has to reconstruct `TKey` from an entity returned by Cosmos.

Important caveat:

- the custom key manager's `Read(entity)` is used for query results
- but CRUD methods do not use the key manager's string conversion

So a custom `ICosmosSqlKeyManager<T, TKey>` cannot change the actual Cosmos `id` serialization strategy. That is still controlled by `KeySettings<TKey>`.

## Fixed partition key strategy

The container partition key path is always created as:

```csharp
PartitionKeyPath = "/id"
```

This package does not expose a builder API to use another partition key path.

## Query behavior

`QueryAsync(...)` partially uses Cosmos LINQ and partially falls back to local processing.

What is pushed to Cosmos:

- `Where`

What is applied locally after materializing results:

- `OrderBy`
- `OrderByDescending`
- `ThenBy`
- `ThenByDescending`
- `Skip`
- `Top`

So the flow is:

1. run the `Where` portion against Cosmos
2. materialize matching items into memory
3. apply ordering and paging locally

This is important for large datasets, because `QueryAsync(...)` is not server-side paging over ordered results.

## Aggregate behavior

`OperationAsync(...)` uses Cosmos LINQ aggregates over the queryable for:

- `Count`
- `Sum`
- `Max`
- `Min`
- `Average`

That makes aggregate behavior more efficient than `QueryAsync(...)` plus client-side aggregation, but it still follows the generic Repository Framework operation model.

## CRUD and batch behavior

- `InsertAsync` uses `CreateItemAsync(...)`
- `UpdateAsync` uses `UpsertItemAsync(...)`
- `DeleteAsync` deletes by `id` and partition key
- `ExistAsync` runs a parameterized SQL query on `id`
- `BatchAsync(...)` is a sequential loop over operations, not a Cosmos transactional batch

So the package does not currently use Cosmos transactional batch support or rollback semantics.

## CQRS examples

```csharp
await builder.Services.AddCommandAsync<AppUser, AppUserKey>(async commandBuilder =>
{
    await commandBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "app-database";
        cosmosBuilder.WithId(x => new AppUserKey(x.Id));
    });
});

await builder.Services.AddQueryAsync<AppUser, AppUserKey>(async queryBuilder =>
{
    await queryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "app-database";
        cosmosBuilder.WithId(x => new AppUserKey(x.Id));
    });
});
```

## When to use this package

Use it when you want:

- a straightforward Cosmos SQL repository adapter
- easy mapping from repository key to Cosmos `id`
- container provisioning handled by registration

Be careful when you need a custom partitioning strategy or fully server-side ordered paging, because the current implementation does not provide either.
