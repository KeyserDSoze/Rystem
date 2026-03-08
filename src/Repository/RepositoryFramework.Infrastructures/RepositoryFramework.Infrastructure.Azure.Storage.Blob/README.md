# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob

`Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob` adds an Azure Blob Storage adapter for Repository Framework.

It stores one blob per repository entity and serializes the payload as `Entity<T, TKey>`, not as raw `T`.

This package is simple and flexible for key-based storage, but list queries are mostly client-side.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob
```

## Architecture

Each item is stored as:

- blob name: `{Prefix}{KeySettings<TKey>.AsString(key)}`
- blob content: JSON representation of `Entity<T, TKey>`

That means the key appears twice:

- in the blob name
- in the serialized payload

## Registration APIs

### Direct builder registration

Available on all three patterns:

- `WithBlobStorageAsync(...)`
- `WithBlobStorage(...)`

Supported for:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

The sync overloads are wrappers over the async versions.

### Connection service registration

Available overloads:

- `WithBlobStorage<T, TKey, TConnectionService>(...)`

Supported for repository, command, and query registrations.

This path is useful when the container or credential is selected per tenant or request.

## Direct builder example

This follows the test and sample usage style.

```csharp
builder.Services.AddRepository<Car, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        blobStorageBuilder.Settings.ContainerName = "cars";
        blobStorageBuilder.Settings.Prefix = "MyFolder/";
    });
});
```

## Connection service example

This mirrors the integration tests.

```csharp
builder.Services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage<AppUser, AppUserKey, BlobStorageConnectionService>(
        name: "blobstorage2");
});
```

Example connection service:

```csharp
internal sealed class BlobStorageConnectionService
    : IConnectionService<AppUser, AppUserKey, BlobContainerClientWrapper>
{
    public BlobContainerClientWrapper GetConnection(string entityName, string? factoryName = null)
        => new()
        {
            Client = new BlobContainerClient("<connection-string>", entityName.ToLower())
        };
}
```

## Configuration and defaults

`BlobStorageConnectionSettings` exposes:

| Property | Notes |
| --- | --- |
| `ConnectionString` | Used when present. If both this and `EndpointUri` are set, connection string wins. |
| `EndpointUri` | Used for managed identity mode. This points to a container endpoint. |
| `ManagedIdentityClientId` | Null means system-assigned identity. |
| `ContainerName` | Used only in connection-string mode. Defaults to `typeof(T).Name.ToLower()`. |
| `Prefix` | Prepended to every blob name and used as the listing prefix for queries. |
| `ClientOptions` | Passed to the Azure Blob SDK. |

Default lifetimes:

- direct builder registration: `Singleton`
- connection service registration: `Scoped`

## Managed identity note

In managed identity mode, the builder constructs the client with:

```csharp
new BlobContainerClient(Settings.EndpointUri, credential, Settings.ClientOptions)
```

So `EndpointUri` must already identify the container. `ContainerName` is not applied in that path.

## Lifecycle and provisioning

### Direct builder path

When you use `WithBlobStorageAsync(...)` or `WithBlobStorage(...)`, the package calls `CreateIfNotExistsAsync()` during registration.

That behavior is visible in `RepositoryFramework.UnitTest/Tests/Singularity/BlobStorageSingularityTest.cs`: registration succeeds before any explicit warm-up.

### Connection service path

When you use `WithBlobStorage<T, TKey, TConnectionService>(...)`, container creation is your connection service's responsibility.

### Bootstrap behavior

`BlobStorageRepository<T, TKey>.BootstrapAsync()` currently returns `true` and does nothing.

So like the Table Storage provider, the real provisioning behavior is tied to direct registration, not to `WarmUpAsync()`.

## CRUD behavior

- `GetAsync(key)` checks blob existence and then downloads/deserializes the payload
- `ExistAsync(key)` checks blob existence only
- `InsertAsync(key, value)` uploads without overwrite
- `UpdateAsync(key, value)` uploads with overwrite enabled
- `DeleteAsync(key)` deletes the blob directly

Practical consequence:

- insert is create-only
- update behaves like upsert

## Query behavior

`QueryAsync(...)` is a client-side scan.

The repository does this:

1. list blobs by `Prefix`
2. download each blob
3. deserialize each payload
4. pre-apply only the first `Where` expression as an in-memory predicate
5. apply the full Repository Framework filter pipeline in memory

So there is:

- no blob tag query support
- no metadata filtering
- no server-side ordering or paging
- no projection pushdown

## Query limitations to know about

- the first `Where` expression is compiled to `Func<T, bool>` and run locally
- `OrderBy`, `ThenBy`, `Skip`, `Top`, and paging all happen after values are materialized
- aggregate operations such as `Count`, `Sum`, `Min`, `Max`, and `Average` run in memory
- `BatchAsync(...)` is sequential and non-transactional

There is also an implementation detail worth knowing: query buffering uses `Dictionary<T, Entity<T, TKey>>`. If two blobs deserialize to values that compare equal under `EqualityComparer<T>.Default`, query enumeration can fail or collapse duplicates.

## Named registrations and factory behavior

The optional `name` parameter is a Repository Framework factory name.

In connection-service mode the repository calls:

```csharp
connectionService.GetConnection(typeof(T).Name, name)
```

So the connection service receives the CLR model name, not the configured `ContainerName`.

## CQRS examples

```csharp
await builder.Services.AddCommandAsync<Document, Guid>(async commandBuilder =>
{
    await commandBuilder.WithBlobStorageAsync(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });
});

await builder.Services.AddQueryAsync<Document, Guid>(async queryBuilder =>
{
    await queryBuilder.WithBlobStorageAsync(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        blobStorageBuilder.Settings.Prefix = "documents/";
    });
});
```

## When to use this package

Use it when you want:

- simple key-addressed storage in Azure Blob Storage
- easy handling of complex keys through `KeySettings<TKey>`
- a repository backend that is straightforward to inspect and reason about

Avoid it when you need efficient server-side querying across large datasets, because the current implementation scans and deserializes blobs on the client side.
