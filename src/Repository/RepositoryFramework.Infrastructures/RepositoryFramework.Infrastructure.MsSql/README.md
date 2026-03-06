# Rystem.RepositoryFramework.Infrastructure.MsSql

Lightweight Microsoft SQL Server integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.MsSql
```

## Quick start

```csharp
builder.Services.AddRepository<Cat, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(msSqlBuilder =>
    {
        msSqlBuilder.ConnectionString = builder.Configuration["ConnectionStrings:Database"];
        msSqlBuilder.Schema = "repo";

        msSqlBuilder
            .WithPrimaryKey(x => x.Id, keyColumn =>
            {
                keyColumn.ColumnName = "Id";
            })
            .WithColumn(x => x.Paws, column =>
            {
                column.ColumnName = "Paws";
                column.IsNullable = true;
            });
    });
});
```

## Startup note

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

Call `WarmUpAsync()` to create/update SQL artifacts used by the integration.
