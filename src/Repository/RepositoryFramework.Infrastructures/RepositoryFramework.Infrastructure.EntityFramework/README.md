# Rystem.RepositoryFramework.Infrastructure.EntityFramework

Entity Framework Core integration for Repository/CQRS services. Default service lifetime is `Scoped`.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
```

## Option A: same model for domain and database (3-generic overload)

When the domain model and the EF entity are the same type, use the 3-generic overload `WithEntityFramework<T, TKey, TContext>`. Property mappings are set up automatically with same-name resolution.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]));

builder.Services.AddRepository<User, int>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<User, int, AppDbContext>(efBuilder =>
    {
        efBuilder.DbSet = db => db.Users;
        efBuilder.References = q => q.Include(x => x.Groups);
    });
});
```

## Option B: separate domain and database models (4-generic overload)

When the domain model (`T`) differs from the EF entity (`TEntityModel`), use the 4-generic overload and chain `Translate<TEntityModel>()` to define property mappings.

```csharp
builder.Services.AddRepository<DomainUser, int>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<DomainUser, int, DbUser, AppDbContext>(efBuilder =>
    {
        efBuilder.DbSet = db => db.Users;
        efBuilder.References = q => q.Include(x => x.Groups);
    });

    repositoryBuilder.Translate<DbUser>()
        .With(x => x.Id, x => x.UserId)
        .With(x => x.Email, x => x.EmailAddress)
        .WithKey(x => x, x => x.UserId);
});
```

## CQRS patterns

```csharp
// Command only
builder.Services.AddCommand<User, int>(commandBuilder =>
{
    commandBuilder.WithEntityFramework<User, int, AppDbContext>(efBuilder =>
    {
        efBuilder.DbSet = db => db.Users;
    });
});

// Query only
builder.Services.AddQuery<User, int>(queryBuilder =>
{
    queryBuilder.WithEntityFramework<User, int, AppDbContext>(efBuilder =>
    {
        efBuilder.DbSet = db => db.Users;
        efBuilder.References = q => q.Include(x => x.Groups);
    });
});
```

## Options reference

| Property | Type | Description |
| --- | --- | --- |
| `DbSet` | `Func<TContext, DbSet<TEntityModel>>` | **Required.** Selects the `DbSet` from the context. |
| `References` | `Func<DbSet<TEntityModel>, IQueryable<TEntityModel>>?` | Optional. Add `Include(...)` calls for eager loading. |

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
