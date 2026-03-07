# Repository Framework

Repository Framework is the repository and CQRS area of the Rystem ecosystem.

It is not one single package. It is a family of packages built around a shared abstraction layer and then extended with:

- storage providers
- auto-generated API endpoints
- API clients
- cache decorators
- migration helpers
- web and TypeScript tooling

If you are starting from scratch, the first package to read is [`Rystem.RepositoryFramework.Abstractions`](./RepositoryFramework.Abstractions/README.md).

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Start Here

Install the core package first:

```bash
dotnet add package Rystem.RepositoryFramework.Abstractions
```

That package gives you:

- `IRepository<T, TKey>`, `IQuery<T, TKey>`, `ICommand<T, TKey>`
- repository and CQRS registration methods
- query builder extensions
- key abstractions
- business hooks
- repository registry metadata

Everything else in the repository area builds on top of that foundation.

## Architecture

At a high level, the repository area is layered like this.

| Layer | Purpose |
|---|---|
| Abstractions | Core repository, command, query, key, builder, query-builder, and business-hook contracts |
| Infrastructures | Concrete storage implementations such as in-memory, EF, SQL, Azure, Cosmos, Dataverse |
| API | Server-side endpoint generation and matching clients |
| Cache | Repository decorators and cache providers |
| Tools | Migration helpers and TypeScript generation |
| Web | Blazor UI helpers for repository-oriented frontends |

A typical flow is:

1. register a repository through the abstractions package
2. choose one infrastructure backend
3. optionally expose that repository through `RepositoryFramework.Api.Server`
4. optionally add cache, migration, web, or client layers around it

## Typical Setup

This is the simplest real shape of a setup, taken from the same concepts used across the repository test projects:

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

builder.Services.AddApiFromRepositoryFramework()
    .WithPath("api");

var app = builder.Build();
await app.Services.WarmUpAsync();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

You can see larger real-world setups in:

- `src/Repository/RepositoryFramework.Test/RepositoryFramework.WebApi/Program.cs`
- `src/Repository/RepositoryFramework.Test/RepositoryFramework.UnitTest/Tests/AllIntegration/AllIntegrationTest.cs`

The Web API sample combines repositories, in-memory infrastructure, API generation, authorization scanning, examples, and business hooks in one host.

## How to Navigate the Packages

If your goal is to...

- build repositories and CQRS services -> start with [`Rystem.RepositoryFramework.Abstractions`](./RepositoryFramework.Abstractions/README.md)
- store data in memory for tests or demos -> go to [`Rystem.RepositoryFramework.Infrastructure.InMemory`](./RepositoryFramework.Infrastructure.InMemory/README.md)
- expose repositories as HTTP endpoints -> go to [`Rystem.RepositoryFramework.Api.Server`](./RepositoryFramework.Api.Server/README.md)
- consume generated repository APIs from clients -> go to [`Rystem.RepositoryFramework.Api.Client`](./RepositoryFramework.Api.Client/README.md)
- add repository caching -> go to [`Rystem.RepositoryFramework.Cache`](./RepositoryFramework.Cache/RepositoryFramework.Cache/README.md)
- migrate data between sources -> go to [`Rystem.RepositoryFramework.MigrationTools`](./RepositoryFramework.MigrationTools/README.md)

## Package Map

### Core

- [`Rystem.RepositoryFramework.Abstractions`](./RepositoryFramework.Abstractions/README.md)

### Infrastructures

- [`Rystem.RepositoryFramework.Infrastructure.InMemory`](./RepositoryFramework.Infrastructure.InMemory/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Table/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Cosmos.Sql/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Blob/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Dynamics.Dataverse/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.EntityFramework`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.EntityFramework/README.md)
- [`Rystem.RepositoryFramework.Infrastructure.MsSql`](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.MsSql/README.md)

### API

- [`Rystem.RepositoryFramework.Api.Server`](./RepositoryFramework.Api.Server/README.md)
- [`Rystem.RepositoryFramework.Api.Client`](./RepositoryFramework.Api.Client/README.md)
- [`Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer`](./RepositoryFramework.Api.Client.Authentication.BlazorServer/README.md)
- [`Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm`](./RepositoryFramework.Api.Client.Authentication.BlazorWasm/README.md)
- [`rystem.repository.client`](./repositoryframework.api.client.typescript/src/rystem/README.md)

### Cache

- [`Rystem.RepositoryFramework.Cache`](./RepositoryFramework.Cache/RepositoryFramework.Cache/README.md)
- [`Rystem.RepositoryFramework.Cache.Azure.Storage.Blob`](./RepositoryFramework.Cache/RepositoryFramework.Cache.Azure.Storage.Blob/README.md)

### Tools and UI

- [`Rystem.RepositoryFramework.MigrationTools`](./RepositoryFramework.MigrationTools/README.md)
- [`Rystem.RepositoryFramework.Web.Components`](./RepositoryFramework.Web/RepositoryFramework.Web.Components/README.md)
- [`Rystem.RepositoryFramework.TypescriptGenerator`](./RepositoryFramework.Tools.TypescriptGenerator/README.md)

## Notes

- For DI, use the consumer interfaces such as `IRepository<T, TKey>`, `IQuery<T, TKey>`, and `ICommand<T, TKey>` rather than injecting the low-level pattern interfaces directly.
- The repository area is designed to be composable: the same model registration can be combined with infrastructure providers, API exposure, caching, client generation, and migration tooling.
- Many examples in the repository test projects register several backends for the same model under different factory names, which is why the abstractions layer integrates so deeply with the Rystem factory system.
