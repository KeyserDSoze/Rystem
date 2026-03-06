# Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql

Azure Cosmos DB (SQL API) integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
```

## Quick start

```csharp
await builder.Services.AddRepositoryAsync<AppUser, AppUserKey>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "app-database";
        cosmosBuilder.WithId(x => x.Id);
    });
});
```

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```

## Notes

- Use `WithId(...)` to map model key to Cosmos `id`.
- You can use managed identity or connection string in `CosmosSqlConnectionSettings`.
