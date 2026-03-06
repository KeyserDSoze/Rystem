# Repository Framework

Repository Framework is a set of packages for Repository Pattern and CQRS with auto API generation, API clients, infrastructure providers, caching, migration tools, and UI components.

## Core package

- `Rystem.RepositoryFramework.Abstractions`

## Install core

```bash
dotnet add package Rystem.RepositoryFramework.Abstractions
```

## Typical setup

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
await app.Services.WarmUpAsync();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
app.Run();
```

## Package map

### Core

- [Rystem.RepositoryFramework.Abstractions](./RepositoryFramework.Abstractions/README.md)

### Infrastructures

- [Rystem.RepositoryFramework.Infrastructure.InMemory](./RepositoryFramework.Infrastructure.InMemory/README.md)
- [Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Table/README.md)
- [Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Cosmos.Sql/README.md)
- [Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Blob/README.md)
- [Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Dynamics.Dataverse/README.md)
- [Rystem.RepositoryFramework.Infrastructure.EntityFramework](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.EntityFramework/README.md)
- [Rystem.RepositoryFramework.Infrastructure.MsSql](./RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.MsSql/README.md)

### API

- [Rystem.RepositoryFramework.Api.Server](./RepositoryFramework.Api.Server/README.md)
- [Rystem.RepositoryFramework.Api.Client](./RepositoryFramework.Api.Client/README.md)
- [Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer](./RepositoryFramework.Api.Client.Authentication.BlazorServer/README.md)
- [Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm](./RepositoryFramework.Api.Client.Authentication.BlazorWasm/README.md)

### Cache

- [Rystem.RepositoryFramework.Cache](./RepositoryFramework.Cache/RepositoryFramework.Cache/README.md)
- [Rystem.RepositoryFramework.Cache.Azure.Storage.Blob](./RepositoryFramework.Cache/RepositoryFramework.Cache.Azure.Storage.Blob/README.md)

### Tools and UI

- [Rystem.RepositoryFramework.MigrationTools](./RepositoryFramework.MigrationTools/README.md)
- [Rystem.RepositoryFramework.Web.Components](./RepositoryFramework.Web/RepositoryFramework.Web.Components/README.md)
- [Rystem.RepositoryFramework.TypescriptGenerator](./RepositoryFramework.Tools.TypescriptGenerator/README.md)
- [rystem.repository.client](./repositoryframework.api.client.typescript/src/rystem/README.md)

## Notes

- Use consumer interfaces (`IRepository`, `ICommand`, `IQuery`) instead of pattern interfaces for dependency injection.
- All infrastructure providers can be combined with API server/client and cache decorators.
