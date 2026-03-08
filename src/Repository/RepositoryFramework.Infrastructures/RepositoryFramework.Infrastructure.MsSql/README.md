# Rystem.RepositoryFramework.Infrastructure.MsSql

`Rystem.RepositoryFramework.Infrastructure.MsSql` adds a lightweight SQL Server adapter for Repository Framework.

It reflects your model into one SQL table, but query translation is minimal: reads load rows from SQL and most Repository Framework filtering runs in memory.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.MsSql
```

## Architecture

The package maps one table per model type.

- schema defaults to `dbo`
- table name defaults to `typeof(T).Name`
- every public property becomes a column
- one property must be marked as the primary key
- non-primitive properties are stored as JSON strings in SQL

This makes the provider easy to configure, but it is not a full LINQ-to-SQL translation layer.

## Registration APIs

Available on all three patterns:

- `WithMsSql(...)`

Supported for:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

## Example

This mirrors the API tests.

```csharp
builder.Services.AddRepository<Cat, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(msSqlBuilder =>
    {
        msSqlBuilder.Schema = "repo";
        msSqlBuilder.ConnectionString = builder.Configuration["ConnectionStrings:Database"];

        msSqlBuilder
            .WithPrimaryKey(x => x.Id, column =>
            {
                column.ColumnName = "Key";
            })
            .WithColumn(x => x.Paws, column =>
            {
                column.ColumnName = "Zampe";
                column.IsNullable = true;
            });
    });
});
```

Example model from the tests:

```csharp
public sealed class Cat
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int? Something { get; set; }
    public IEnumerable<Room> Rooms { get; set; } = new List<Room>();
    public int Paws { get; set; }
}
```

`Rooms` is a non-primitive property, so it is stored as JSON in SQL.

## Builder API

`IMsSqlRepositoryBuilder<T, TKey>` exposes:

| Member | Purpose |
| --- | --- |
| `ConnectionString` | SQL Server connection string |
| `Schema` | Target schema, default `dbo` |
| `TableName` | Target table name, default `typeof(T).Name` |
| `WithPrimaryKey(expr, cfg)` | Mark one property as the SQL primary key |
| `WithColumn(expr, cfg)` | Customize another column |

`PropertyHelper<T>` exposes:

| Property | Notes |
| --- | --- |
| `ColumnName` | Override the SQL column name |
| `IsNullable` | Controls `NOT NULL` generation |
| `SqlType` | Override the inferred SQL type |
| `Dimension` | Override SQL type dimensions such as `(100)` or `(14,4)` |
| `IsAutomaticCreated` | Present in the API, but not currently used by the repository implementation |

## Type mapping

The package infers SQL types from CLR property types.

Examples from the current mapper:

- `Guid` -> `uniqueidentifier`
- `string` -> `varchar(100)`
- `DateTime` -> `datetime2`
- `decimal` -> `decimal(14,4)`
- non-primitive types -> `varchar(max)` with JSON payloads

Nullable CLR types become nullable columns unless you override them.

## Required configuration

In practice you need:

- `ConnectionString`
- one `WithPrimaryKey(...)` call

If no primary key is configured, `BootstrapAsync()` throws.

## Warm-up and provisioning

This package has startup hooks, but the current implementation is not as complete as the old README implied.

### What `BootstrapAsync()` actually does

`SqlRepository<T, TKey>.BootstrapAsync()`:

1. checks whether a primary key is configured
2. checks whether any columns exist for the table name
3. creates the schema if missing
4. creates the table if missing

### Important current limitation

Despite method names and earlier docs, the package does not currently merge new columns into an existing table.

The helper named `MsSqlCreateTableOrMergeNewColumnsInExistingTableAsync()` is empty.

### Registration-shape caveat

- repository and command registrations add warm-up hooks that call that empty helper
- query registrations add a warm-up hook that calls `BootstrapAsync()`

So auto-provisioning is currently more reliable for query registrations than for repository/command registrations.

If you depend on startup schema creation for repository or command registrations, verify it in your environment instead of assuming it happens.

## Query behavior

`QueryAsync(...)` runs this shape of SQL:

```sql
select [col1],[col2],... from [Schema].[TableName]
```

It does not translate Repository Framework filters to SQL `WHERE`, `ORDER BY`, `TOP`, or paging clauses.

Instead it:

1. reads rows from SQL
2. materializes models
3. applies Repository Framework filters in memory

Practical consequence:

- `Where`, ordering, paging, and aggregates are mostly client-side
- large tables will be scanned more than you might expect

## Aggregate behavior

Aggregate operations such as:

- `Count`
- `Sum`
- `Min`
- `Max`
- `Average`

materialize items through the repository query flow and then compute results in memory.

## CRUD behavior

- `GetAsync` and `ExistAsync` query by the configured primary key column
- primitive keys are stored with `ToString()`
- non-primitive keys are stored as JSON in the primary key column
- inserts and updates only send non-null property values as SQL parameters

Important consequence:

- the package cannot update a column to `NULL` through the current parameter-building logic

## Batch behavior

`BatchAsync(...)` is a sequential loop over insert, update, and delete calls.

There is:

- no SQL transaction wrapping the whole batch
- no bulk insert path
- no rollback behavior

## Other limitations to know about

- only one primary key column is supported
- there is no composite key mapping
- table existence checks use `INFORMATION_SCHEMA.COLUMNS` filtered only by table name, not by schema name
- method names suggest schema evolution support, but the implementation is currently create-only

## CQRS examples

```csharp
builder.Services.AddCommand<Cat, Guid>(commandBuilder =>
{
    commandBuilder.WithMsSql(msSqlBuilder =>
    {
        msSqlBuilder.ConnectionString = builder.Configuration["ConnectionStrings:Database"];
        msSqlBuilder.WithPrimaryKey(x => x.Id, column =>
        {
            column.ColumnName = "Key";
        });
    });
});

builder.Services.AddQuery<Cat, Guid>(queryBuilder =>
{
    queryBuilder.WithMsSql(msSqlBuilder =>
    {
        msSqlBuilder.ConnectionString = builder.Configuration["ConnectionStrings:Database"];
        msSqlBuilder.WithPrimaryKey(x => x.Id, column =>
        {
            column.ColumnName = "Key";
        });
    });
});
```

## When to use this package

Use it when you want:

- a simple reflection-based SQL Server backend
- explicit table and column naming control
- JSON storage for complex properties without extra mapping layers

Be careful if you expect server-side query translation or automatic schema evolution, because the current implementation is much thinner than that.
