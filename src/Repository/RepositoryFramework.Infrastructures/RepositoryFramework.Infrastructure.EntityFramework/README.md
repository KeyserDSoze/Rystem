# Rystem.RepositoryFramework.Infrastructure.EntityFramework

Entity Framework Core integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
```

## Option A: same model for domain and database

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration["ConnectionStrings:Database"]);
});

builder.Services.AddRepository<User, int>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<User, int, AppDbContext>(efBuilder =>
    {
        efBuilder.DbSet = db => db.Users;
        efBuilder.References = q => q.Include(x => x.Groups);
    });
});
```

## Option B: separate domain and database models

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

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
