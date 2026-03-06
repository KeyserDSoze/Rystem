# Rystem.RepositoryFramework.Infrastructure.MsSql

Lightweight Microsoft SQL Server integration for Repository/CQRS services. Tables are created (or updated with new columns) automatically via `WarmUpAsync()` at startup.

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
        // Schema defaults to "dbo"
        msSqlBuilder.Schema = "repo";
        // TableName defaults to the model type name
        msSqlBuilder.TableName = "Cats";

        msSqlBuilder
            .WithPrimaryKey(x => x.Id, col =>
            {
                col.ColumnName = "Id";
            })
            .WithColumn(x => x.Name, col =>
            {
                col.ColumnName = "Name";
                col.IsNullable = false;
            })
            .WithColumn(x => x.Paws, col =>
            {
                col.ColumnName = "Paws";
                col.IsNullable = true;
            });
    });
});
```

## CQRS patterns

```csharp
// Command only
builder.Services.AddCommand<Cat, Guid>(commandBuilder =>
    commandBuilder.WithMsSql(b => { /* ... */ }));

// Query only
builder.Services.AddQuery<Cat, Guid>(queryBuilder =>
    queryBuilder.WithMsSql(b => { /* ... */ }));
```

## Builder reference

| Member | Type | Description |
| --- | --- | --- |
| `ConnectionString` | `string` | **Required.** SQL Server connection string. |
| `Schema` | `string` | Database schema (default `"dbo"`). |
| `TableName` | `string` | Table name — defaults to the model type name. |
| `WithPrimaryKey(expr, cfg)` | method | Designates a property as the primary key column. |
| `WithColumn(expr, cfg)` | method | Customises a column (name, nullability, etc.). |

### PropertyHelper options

```csharp
msSqlBuilder.WithColumn(x => x.Name, col =>
{
    col.ColumnName = "Name";      // override column name
    col.IsNullable = false;        // NOT NULL constraint
});
```

## Startup — WarmUp required

`WithMsSql` automatically registers a warm-up action that creates the table (if missing) or adds any new columns to an existing table. You **must** call `WarmUpAsync()` at application startup:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
await app.Services.WarmUpAsync();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
