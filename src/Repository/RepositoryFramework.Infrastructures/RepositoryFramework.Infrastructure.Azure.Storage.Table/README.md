# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table

Azure Table Storage integration for Repository/CQRS services. The table is created automatically if it does not exist.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table
```

## Quick start — connection string

```csharp
await builder.Services.AddRepositoryAsync<User, UserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        // TableName defaults to the model type name
        tableStorageBuilder.Settings.TableName = "Users";
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id)
            .WithTimestamp(x => x.UpdatedAt);
    });
});
```

## Quick start — managed identity

```csharp
await builder.Services.AddRepositoryAsync<User, UserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.EndpointUri = new Uri("https://<account>.table.core.windows.net");
        // null = DefaultAzureCredential; set for user-assigned managed identity:
        tableStorageBuilder.Settings.ManagedIdentityClientId = "<client-id>";
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id);
    });
});
```

## Sync variant

```csharp
builder.Services.AddRepository<User, UserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithTableStorage(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id);
    });
});
```

## CQRS patterns

```csharp
// Command only
await builder.Services.AddCommandAsync<User, UserKey>(async commandBuilder =>
    await commandBuilder.WithTableStorageAsync(b => { /* ... */ }));

// Query only
await builder.Services.AddQueryAsync<User, UserKey>(async queryBuilder =>
    await queryBuilder.WithTableStorageAsync(b => { /* ... */ }));
```

## Key mapping API

| Method | Description |
| --- | --- |
| `WithPartitionKey(x => x.Prop, k => k.Prop)` | Maps a model property and a key property to PartitionKey |
| `WithRowKey(x => x.Prop, k => k.Prop)` | Maps a model property and a key property to RowKey |
| `WithRowKey(x => x.Prop)` | Maps only a model property to RowKey (no key expression needed) |
| `WithTimestamp(x => x.Prop)` | Maps a `DateTime` model property to the Azure Timestamp |
| `WithTableStorageKeyReader<T>()` | Registers a custom `ITableStorageKeyReader<T, TKey>` implementation |

## Settings reference

| Property | Description |
| --- | --- |
| `ConnectionString` | Storage connection string (mutually exclusive with `EndpointUri`) |
| `EndpointUri` | Table service endpoint for managed identity auth |
| `ManagedIdentityClientId` | User-assigned managed identity client ID (null = system-assigned) |
| `TableName` | Table name — defaults to the model type name |
| `ClientOptions` | `TableClientOptions` passed to the SDK |

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
