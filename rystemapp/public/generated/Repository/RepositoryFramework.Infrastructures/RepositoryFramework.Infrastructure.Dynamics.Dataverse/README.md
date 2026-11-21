# Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse

`Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse` adds a Microsoft Dataverse adapter for Repository Framework.

It auto-provisions a Dataverse table for your model, but the storage model is much simpler than a typical Dataverse-first mapping: every repository property is stored as a string column, and most queries run in memory after fetch.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse
```

## Architecture

For each registered model, the package works with one Dataverse table.

- table logical name: `Prefix + TableName.ToLower()`
- primary attribute logical name: `Prefix + PrimaryKey.ToLower()`
- every model property becomes a Dataverse string attribute
- primitive values are stringified
- non-primitive values are JSON serialized

So this package uses Dataverse mainly as a managed record store, not as a strongly typed Dataverse-schema translator.

## Registration APIs

Available on all three patterns:

- `WithDataverse(...)`

Supported for:

- `IRepositoryBuilder<T, TKey>`
- `ICommandBuilder<T, TKey>`
- `IQueryBuilder<T, TKey>`

## Example

This matches the repository API tests.

```csharp
builder.Services.AddRepository<CalamityUniverseUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "repo_";
        dataverseBuilder.Settings.SolutionName = "Solution001";
        dataverseBuilder.Settings.TableName = "CalamityUniverseUser";

        dataverseBuilder.Settings.SetConnection(
            builder.Configuration["Dataverse:Environment"]!,
            new DataverseAppRegistrationAccount(
                builder.Configuration["Dataverse:ClientId"]!,
                builder.Configuration["Dataverse:ClientSecret"]!));
    });
});
```

Example model from the tests:

```csharp
public sealed class CalamityUniverseUser
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public int Port { get; set; }
    public bool IsAdmin { get; set; }
    public Guid GroupId { get; set; }
}
```

## Builder API

`IDataverseRepositoryBuilder<T, TKey>` exposes:

| Member | Purpose |
| --- | --- |
| `Settings` | Connection and Dataverse table settings |
| `WithColumn(expr, customPrefix)` | Override the prefix used for one property |

`DataverseOptions<T, TKey>` exposes:

| Property | Default | Notes |
| --- | --- | --- |
| `Prefix` | `new_` | Global prefix for the logical table and column names |
| `TableName` | `typeof(T).Name` | Display and schema base name |
| `PrimaryKey` | `Id` | Name of the Dataverse primary attribute used to store the repository key |
| `SolutionName` | `null` | Used when creating a new table |
| `Description` | `null` | Falls back to an auto-generated description |
| `Environment` | required | Set through `SetConnection(...)` |
| `ApplicationIdentity` | required | Set through `SetConnection(...)` |

## Connection configuration

Use:

```csharp
dataverseBuilder.Settings.SetConnection(
    environment,
    new DataverseAppRegistrationAccount(clientId, clientSecret));
```

Internally the package creates a `ServiceClient` with a client-secret connection string like:

```text
Url=https://{Environment}.dynamics.com;AuthType=ClientSecret;ClientId=...;ClientSecret=...;RequireNewInstance=true
```

This package currently documents and implements only the app-registration client-secret path.

## Warm-up and provisioning

`WithDataverse(...)` automatically registers a warm-up action.

At startup, `BootstrapAsync()`:

1. checks whether the Dataverse table exists
2. creates it if missing
3. checks every model property column
4. creates any missing columns as `StringAttributeMetadata`

You should still run:

```csharp
await app.Services.WarmUpAsync();
```

That is the path the repository expects for auto-provisioning.

## What gets provisioned

When the table is created:

- ownership type is `UserOwned`
- the primary Dataverse attribute is a string column with max length `100`

When model properties are created:

- primitive properties become string columns with max length `100`
- non-primitive properties become string columns with max length `2000`

So even if your .NET property is `int`, `bool`, `Guid`, or a complex object, the stored Dataverse attribute is still a string attribute.

## Key behavior

The repository key `TKey` is stored in the Dataverse primary attribute column.

- primitive `TKey` values are stored with `ToString()`
- non-primitive `TKey` values are stored as JSON

Important nuance:

- `PrimaryKey` config controls the Dataverse primary attribute name
- it does not automatically mean the same property on your model is the repository key source

For example, the tests register `CalamityUniverseUser` with `TKey = string`, while the Dataverse table still uses the default logical primary attribute based on `Id`.

## Query behavior

`QueryAsync(...)` is mostly client-side.

The repository issues a Dataverse query with:

- `TopCount = 100`
- the configured `ColumnSet`
- no translated Repository Framework filter expression

Then it:

1. materializes the returned Dataverse entities into models
2. rebuilds `Entity<T, TKey>` values
3. applies Repository Framework filters in memory

Practical consequence:

- `Where`, ordering, paging, and aggregates all operate on the fetched in-memory subset
- only the first 100 Dataverse rows are visible to `QueryAsync(...)`

## CRUD behavior

Key-based methods do use Dataverse-side filtering.

- `GetAsync`, `ExistAsync`, `UpdateAsync`, and `DeleteAsync` query by the logical primary key column
- `InsertAsync` creates a new Dataverse row directly

`UpdateAsync` first finds the existing Dataverse row and then updates it by Dataverse row id.

## Batch and aggregate behavior

- `BatchAsync(...)` is a sequential loop over insert/update/delete operations
- there is no Dataverse bulk pipeline or transaction support in this package
- aggregate operations such as `Count`, `Sum`, `Min`, `Max`, and `Average` run in memory after `QueryAsync(...)`

## Custom column prefix caveat

`WithColumn(x => x.Property, customPrefix: "...")` exists, but the current implementation is less reliable than the API suggests.

Important source-backed caveats:

- column existence checks during bootstrap primarily look for unprefixed or globally prefixed names
- `ColumnSet` is built before `WithColumn(...)` overrides the property prefix

So per-column custom prefixes may not behave consistently across provisioning and reads. If you need robust mappings, prefer a single global `Prefix` for the whole table.

## Solution behavior caveat

`SolutionName` is used when creating a new Dataverse table.

The current implementation does not perform a broader ongoing solution-management workflow for already existing tables or attributes.

## CQRS examples

```csharp
builder.Services.AddCommand<AccountModel, string>(commandBuilder =>
{
    commandBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "repo_";
        dataverseBuilder.Settings.SetConnection(
            builder.Configuration["Dataverse:Environment"]!,
            new DataverseAppRegistrationAccount(
                builder.Configuration["Dataverse:ClientId"]!,
                builder.Configuration["Dataverse:ClientSecret"]!));
    });
});

builder.Services.AddQuery<AccountModel, string>(queryBuilder =>
{
    queryBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "repo_";
        dataverseBuilder.Settings.SetConnection(
            builder.Configuration["Dataverse:Environment"]!,
            new DataverseAppRegistrationAccount(
                builder.Configuration["Dataverse:ClientId"]!,
                builder.Configuration["Dataverse:ClientSecret"]!));
    });
});
```

## When to use this package

Use it when you want:

- Dataverse-backed persistence behind Repository Framework abstractions
- automatic table and column provisioning at startup
- a simple storage layer where complex values can be serialized into Dataverse string attributes

Avoid it when you need deep Dataverse-native query translation or strongly typed Dataverse attribute mapping, because the current implementation is intentionally much thinner than that.
