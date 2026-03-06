# Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql

Azure Cosmos DB (SQL API) integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
```

## Quick start — connection string

```csharp
await builder.Services.AddRepositoryAsync<AppUser, AppUserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "app-database";
        // ContainerName defaults to the model type name when not set
        cosmosBuilder.Settings.ContainerName = "users";
        cosmosBuilder.WithId(x => x.Id);
    });
});
```

## Quick start — managed identity

```csharp
await builder.Services.AddRepositoryAsync<AppUser, AppUserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.EndpointUri = new Uri("https://<account>.documents.azure.com:443/");
        // Leave ManagedIdentityClientId null for system-assigned, or set for user-assigned:
        cosmosBuilder.Settings.ManagedIdentityClientId = "<client-id>";
        cosmosBuilder.Settings.DatabaseName = "app-database";
        cosmosBuilder.WithId(x => x.Id);
    });
});
```

## Sync variant

```csharp
builder.Services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithCosmosSql(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "app-database";
        cosmosBuilder.WithId(x => x.Id);
    });
});
```

## CQRS patterns

```csharp
// Command only
await builder.Services.AddCommandAsync<AppUser, AppUserKey>(async commandBuilder =>
    await commandBuilder.WithCosmosSqlAsync(b => { /* ... */ }));

// Query only
await builder.Services.AddQueryAsync<AppUser, AppUserKey>(async queryBuilder =>
    await queryBuilder.WithCosmosSqlAsync(b => { /* ... */ }));
```

## Custom key manager

Use `WithKeyManager<T>` when the key extraction logic is complex:

```csharp
cosmosBuilder.WithKeyManager<MyCustomKeyManager>();
// MyCustomKeyManager implements ICosmosSqlKeyManager<T, TKey>
```

For simple cases, prefer `WithId(x => x.Id)` which registers a default key manager.

## Throughput options

```csharp
cosmosBuilder.Settings.DatabaseOptions = new CosmosSettings
{
    ThroughputProperties = ThroughputProperties.CreateAutoscaleThroughput(4000)
};
cosmosBuilder.Settings.ContainerOptions = new CosmosSettings
{
    ThroughputProperties = ThroughputProperties.CreateManualThroughput(400)
};
```

## Settings reference

| Property | Description |
| --- | --- |
| `ConnectionString` | Cosmos DB connection string (mutually exclusive with `EndpointUri`) |
| `EndpointUri` | Cosmos DB endpoint for managed identity auth |
| `ManagedIdentityClientId` | User-assigned managed identity client ID (null = system-assigned) |
| `DatabaseName` | Name of the Cosmos database (created if not exists) |
| `ContainerName` | Container name — defaults to the model type name |
| `ClientOptions` | `CosmosClientOptions` passed to the SDK |
| `DatabaseOptions` | `CosmosSettings` (throughput/request options) for DB creation |
| `ContainerOptions` | `CosmosSettings` (throughput/request options) for container creation |

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
