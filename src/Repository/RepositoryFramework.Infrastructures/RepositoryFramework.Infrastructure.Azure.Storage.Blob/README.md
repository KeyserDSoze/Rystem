# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob

Azure Blob Storage integration for Repository/CQRS services. Each entity is stored as a JSON blob. The container is created automatically if it does not exist.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob
```

## Quick start — connection string

```csharp
builder.Services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        // ContainerName defaults to the model type name (lowercased)
        blobStorageBuilder.Settings.ContainerName = "documents";
        blobStorageBuilder.Settings.Prefix = "docs/";
    });
});
```

## Quick start — managed identity

```csharp
builder.Services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.EndpointUri = new Uri("https://<account>.blob.core.windows.net/<container>");
        // null = DefaultAzureCredential (system-assigned); set for user-assigned:
        blobStorageBuilder.Settings.ManagedIdentityClientId = "<client-id>";
    });
});
```

## Async setup variant

```csharp
await builder.Services.AddRepositoryAsync<Document, Guid>(async repositoryBuilder =>
{
    await repositoryBuilder.WithBlobStorageAsync(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });
});
```

## CQRS patterns

```csharp
// Command only
builder.Services.AddCommand<Document, Guid>(commandBuilder =>
    commandBuilder.WithBlobStorage(b => { /* ... */ }));

// Query only
builder.Services.AddQuery<Document, Guid>(queryBuilder =>
    queryBuilder.WithBlobStorage(b => { /* ... */ }));
```

## Dynamic connection service

For per-request connection resolution (e.g. multi-tenancy) implement `IConnectionService<T, TKey, BlobContainerClientWrapper>` and register it:

```csharp
builder.Services.AddRepository<Document, Guid>(repositoryBuilder =>
    repositoryBuilder.WithBlobStorage<Document, Guid, MyConnectionService>());
```

## Settings reference

| Property | Description |
| --- | --- |
| `ConnectionString` | Storage connection string (mutually exclusive with `EndpointUri`) |
| `EndpointUri` | Container endpoint for managed identity auth |
| `ManagedIdentityClientId` | User-assigned managed identity client ID (null = system-assigned) |
| `ContainerName` | Container name — defaults to the model type name (lowercased) |
| `Prefix` | Optional blob prefix (e.g. `"folder/"`) prepended to all blob names |
| `ClientOptions` | `BlobClientOptions` passed to the SDK |

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
