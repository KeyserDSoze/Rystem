# Repository Pattern Setup

> Setup Rystem Repository Framework for CQRS and Repository pattern implementations

## Description

This tool helps you configure and setup the Rystem Repository Framework in your .NET project. It provides guidance for implementing the Repository pattern and CQRS (Command Query Responsibility Segregation).

## Usage

Use this tool when you need to:
- Set up a new repository for your entities
- Configure multiple storage backends (Entity Framework, Cosmos DB, Azure Storage, etc.)
- Implement CQRS patterns in your application
- Add caching layers to your repositories

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Abstractions
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
```

## Basic Setup

```csharp
// Program.cs
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder
        .WithEntityFramework<ApplicationDbContext>(options =>
        {
            options.AddForSqlServer(builder.Configuration["ConnectionStrings:Default"]);
        });
});
```

## Advanced Configuration

```csharp
// With caching
builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder
        .WithEntityFramework<CatalogDbContext>()
        .WithCache(cache =>
        {
            cache.WithDistributedCache();
            cache.WithDefaultExpiration(TimeSpan.FromMinutes(5));
        });
});

// With CQRS
builder.Services.AddRepositoryFrameworkCQRS<Order, Guid>(cqrs =>
{
    cqrs.WithCommand(command =>
    {
        command.WithCosmosDb(/* config */);
    });
    cqrs.WithQuery(query =>
    {
        query.WithBlobStorage(/* config */);
    });
});
```

## Patterns

### Repository Pattern
Use for standard CRUD operations with a single storage backend and optional caching.

### CQRS Pattern
Use when you need different storage or optimization strategies for reads vs writes.

## See Also

- [Rystem.RepositoryFramework Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository)
- [Entity Framework Integration](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/Rystem.RepositoryFramework.Infrastructure.EntityFramework)
- [CQRS Implementation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/Rystem.RepositoryFramework.Abstractions)
