### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Infrastructure.EntityFramework

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Infrastructure.EntityFramework)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Infrastructure.EntityFramework)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Infrastructure.EntityFramework)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Infrastructure.EntityFramework)

Entity Framework Core integration for the Rystem repository ecosystem.

This package gives you a reusable generic repository implementation backed by `DbContext`, plus fluent registration extensions for repository, command, and query patterns.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
```

The current package metadata in `src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.EntityFramework/RepositoryFramework.Infrastructure.EntityFramework.csproj` is:

- package id: `Rystem.RepositoryFramework.Infrastructure.EntityFramework`
- version: `10.0.6`
- target framework: `net10.0`
- main EF dependency: `Microsoft.EntityFrameworkCore` `10.0.3`

---

## Package architecture

| Area | Purpose |
|---|---|
| `WithEntityFramework(...)` extensions | Register the generic EF-backed repository on repository, command, or query builders |
| `EntityFrameworkRepository<T, TKey, TEntityModel, TContext>` | Concrete implementation that talks to `DbContext` and `DbSet` |
| `EntityFrameworkOptions<T, TKey, TEntityModel, TContext>` | Minimal provider options: `DbSet` and optional `References` |
| Translation builders | Map repository models and key shapes to EF entity models |
| Same-model overloads | Convenience overloads that auto-map same-name properties |

---

## Mental model

This package is intentionally thin.

You bring:

- the `DbContext`
- the EF entity model
- the repository model exposed to the rest of the app
- the key mapping

The package provides the generic implementation that converts repository calls into:

- `DbSet.AddAsync(...)`
- `DbSet.Update(...)`
- `DbSet.FindAsync(...)`
- translated LINQ queries over `IQueryable`
- aggregate operations like count, sum, average, min, and max

---

## Prerequisites

Before using `WithEntityFramework(...)`, register your context yourself.

```csharp
builder.Services.AddDbContext<SampleContext>(options =>
{
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]);
});
```

The EF provider does not create or configure the `DbContext` for you.

---

## The key rule: map the repository key explicitly

The generic EF repository depends on `RepositoryMapper<T, TKey, TEntityModel>` for key lookups and key extraction.

In practice, that means you should configure `WithKey(...)` for the EF entity key whenever you use this provider.

This is true even with the same-model overloads.

For scalar keys, the key-side expression is typically just `x => x`.

```csharp
.WithKey(x => x, x => x.Identificativo)
```

Without an explicit key mapping, methods like `ExistAsync`, `GetAsync`, and key retrieval from inserted/query results do not have enough information to work correctly.

---

## Same model setup

Use the 3-generic overload when the repository model and the EF entity model are the same type.

```csharp
builder.Services.AddDbContext<SampleContext>(options =>
{
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]);
});

builder.Services.AddRepository<User, int>(repositoryBuilder =>
{
    repositoryBuilder
        .WithEntityFramework<User, int, SampleContext>(efOptions =>
        {
            efOptions.DbSet = db => db.Users;
            efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
        })
        .WithKey(x => x, x => x.Identificativo);
});
```

What this overload does for you:

- registers the generic EF repository with `ServiceLifetime.Scoped` by default
- automatically calls `Translate<T>().WithSamePorpertiesName()` for same-name property mappings

What it does not do for you:

- it does not infer the repository key mapping automatically
- it does not register the `DbContext`

So even in the same-model path, `WithKey(...)` is the important final step.

---

## Separate repository and EF models

Use the 4-generic overload when your repository model differs from the EF entity model.

This is the pattern used in the repo tests for `MappingUser` mapped to the EF `User` entity.

```csharp
builder.Services.AddRepository<MappingUser, int>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<MappingUser, int, User, SampleContext>(efOptions =>
    {
        efOptions.DbSet = db => db.Users;
        efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
    });

    repositoryBuilder.Translate<User>()
        .With(x => x.Username, x => x.Nome)
        .With(x => x.Email, x => x.IndirizzoElettronico)
        .With(x => x.Id, x => x.Identificativo)
        .WithKey(x => x, x => x.Identificativo);
});
```

This is the right approach when:

- your domain model uses different property names
- your EF model is generated from an existing schema
- you want to hide database-only fields from the application layer

---

## `EntityFrameworkOptions` reference

`EntityFrameworkOptions<T, TKey, TEntityModel, TContext>` intentionally stays small.

| Property | Type | Required | Purpose |
|---|---|---|---|
| `DbSet` | `Func<TContext, DbSet<TEntityModel>>` | yes | Select the EF set to use |
| `References` | `Func<DbSet<TEntityModel>, IQueryable<TEntityModel>>?` | no | Apply `Include(...)` or other query shaping for reads |

### `DbSet`

This is mandatory.

```csharp
efOptions.DbSet = db => db.Users;
```

### `References`

Use this for eager loading on `GetAsync` and `QueryAsync`.

```csharp
efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
```

Important nuance: the generic repository uses `References` only for read enumeration paths:

- `GetAsync(...)`
- `QueryAsync(...)`

Other methods use the plain `DbSet`:

- `ExistAsync(...)`
- `DeleteAsync(...)`
- `OperationAsync(...)`

So `References` is for read graph shaping, not a global query policy.

---

## CQRS variants

The EF registration extension also exists for command-only and query-only setups.

```csharp
builder.Services.AddCommand<User, int>(commandBuilder =>
{
    commandBuilder
        .WithEntityFramework<User, int, SampleContext>(efOptions =>
        {
            efOptions.DbSet = db => db.Users;
        })
        .WithKey(x => x, x => x.Identificativo);
});

builder.Services.AddQuery<User, int>(queryBuilder =>
{
    queryBuilder
        .WithEntityFramework<User, int, SampleContext>(efOptions =>
        {
            efOptions.DbSet = db => db.Users;
            efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
        })
        .WithKey(x => x, x => x.Identificativo);
});
```

Default lifetime is still `Scoped`, which is the natural choice for `DbContext`-based work.

---

## Query and aggregate behavior

The EF provider delegates query construction to the shared repository filter model from the abstractions package.

That means consumer code stays the same:

```csharp
var page = await repository
    .Where(x => x.Username.Contains("eku"))
    .OrderByDescending(x => x.Id)
    .PageAsync(1, 2);

var sum = await repository.SumAsync(x => x.Id);
var average = await repository.AverageAsync(x => x.Id);
var max = await repository.MaxAsync(x => x.Id);
var min = await repository.MinAsync(x => x.Id);
```

In the repo tests, these queries and aggregates are exercised against both the mapped-model and same-model EF registrations.

---

## Operational notes from the source

### Service lifetime

- `WithEntityFramework(...)` defaults to `ServiceLifetime.Scoped`
- this matches normal `DbContext` usage
- you can override the lifetime, but deviating from `Scoped` should be deliberate

### Inserts and updates

- `InsertAsync(...)` maps the repository model to the EF entity, calls `AddAsync(...)`, saves changes, then maps the stored entity back
- `UpdateAsync(...)` maps the repository model to a new EF entity instance and calls `DbSet.Update(...)`

### Deletes

`DeleteAsync(...)` uses:

```csharp
await _dbSet.FindAsync(new object[] { key }, cancellationToken)
```

So the generic provider is most natural when `TKey` matches the actual EF primary key shape.

If your repository key is a wrapper or a richer object around the EF key, a handwritten storage implementation may be a better fit.

### Batch operations

`BatchAsync(...)` loops over operations and delegates to `InsertAsync`, `UpdateAsync`, and `DeleteAsync` one by one.

That means:

- it is not transactional by itself
- it triggers `SaveChangesAsync(...)` per item through those inner operations
- it is convenient, but not optimized for large EF bulk workloads

### Bootstrap and warm-up

`EntityFrameworkRepository<T, TKey, TEntityModel, TContext>.BootstrapAsync()` currently returns `true` immediately.

So this package does not add any EF-specific warm-up behavior of its own. Calling `WarmUpAsync()` at app startup is still safe if your application already uses it across providers.

---

## When to use custom storage instead

The repo test infrastructure includes both generic EF usage and a handwritten EF-backed repository.

The custom storage path is used for `AppUser` with `AppUserKey`:

```csharp
services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
{
    repositoryBuilder.SetStorage<AppUserStorage>();
    repositoryBuilder.Translate<User>()
        .With(x => x.Id, x => x.Identificativo)
        .With(x => x.Username, x => x.Nome)
        .With(x => x.Email, x => x.IndirizzoElettronico);
});
```

That is a good option when:

- your key type does not align cleanly with EF primary key lookup
- you need storage-specific logic that the generic provider does not cover
- you want exact control over how entities are queried, inserted, updated, or deleted

Use the generic provider first when it fits; drop to custom storage when the model/key mismatch becomes awkward.

---

## Practical examples from the repo

### Same-model EF repository

The test infrastructure registers `User` directly with the same-model overload:

```csharp
services.AddRepository<User, int>(repositoryBuilder =>
{
    repositoryBuilder
        .WithEntityFramework<User, int, SampleContext>(efOptions =>
        {
            efOptions.DbSet = db => db.Users;
            efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
        })
        .WithKey(x => x, x => x.Identificativo);
});
```

### Mapped domain model over EF entity

The same test infrastructure also registers `MappingUser` over the same EF `User` table with explicit translation rules:

```csharp
services.AddRepository<MappingUser, int>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<MappingUser, int, User, SampleContext>(efOptions =>
    {
        efOptions.DbSet = db => db.Users;
        efOptions.References = dbSet => dbSet.Include(x => x.IdGruppos);
    });

    repositoryBuilder.Translate<User>()
        .With(x => x.Username, x => x.Nome)
        .With(x => x.Email, x => x.IndirizzoElettronico)
        .With(x => x.Id, x => x.Identificativo)
        .WithKey(x => x, x => x.Identificativo);
});
```

### Business hooks still compose normally

Because EF registration works through the same repository builders as every other provider, you can keep using business hooks and other abstractions-layer features.

```csharp
services.AddBusinessForRepository<User, int>()
    .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
    .AddBusinessBeforeInsert<UserBeforeInsertBusiness2>();
```

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Core contracts, registration builders, filters, and translation model |
| `Rystem.RepositoryFramework.Infrastructure.InMemory` | In-memory provider for tests and demos |
| `Rystem.RepositoryFramework.Api.Server` | Expose repositories as HTTP endpoints |

If you are continuing through the repository area, this package is the main relational-provider bridge after `src/Repository/RepositoryFramework.Abstractions/README.md`.
