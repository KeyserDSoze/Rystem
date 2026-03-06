# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table

Azure Table Storage integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table
```

## Quick start

```csharp
builder.Services.AddRepository<User, UserKey>(repositoryBuilder =>
{
    repositoryBuilder.WithTableStorage(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        tableStorageBuilder
            .WithPartitionKey(x => x.TenantId, x => x.TenantId)
            .WithRowKey(x => x.Id, x => x.Id)
            .WithTimestamp(x => x.UpdatedAt);
    });
});
```

## Async setup variant

```csharp
await builder.Services.AddRepositoryAsync<User, UserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });
});
```

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
