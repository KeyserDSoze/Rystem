# Rystem.RepositoryFramework.Api.Server

Automatically exposes registered Repository/CQRS services as HTTP endpoints.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Server
```

## Quick start

```csharp
builder.Services
    .AddRepository<SuperUser, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory();
    });

builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository API")
    .WithPath("api")
    .WithSwagger()
    .WithVersion("v1")
    .WithDocumentation()
    .WithDefaultCors("https://example.com");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
app.Run();
```

## Authorization options

### Default authorization for all endpoints

```csharp
app.UseApiFromRepositoryFramework().WithDefaultAuthorization();
```

### Custom policies

```csharp
app.UseApiFromRepositoryFramework()
    .SetPolicyForAll().With("NormalUser")
    .And()
    .SetPolicy(RepositoryMethods.Insert, RepositoryMethods.Update).With("SuperAdmin")
    .Build();
```

### Configure one specific repository type

```csharp
app.UseApiFromRepositoryFramework<WebApplication, SuperUser, string>()
    .SetPolicyForCommand().With("SuperAdmin")
    .Build();
```

## Notes

- Endpoints are generated from registered repository services.
- API generation respects exposed repository methods (`RepositoryMethods`).
- If you use in-memory pre-population or SQL schema bootstrap, run `WarmUpAsync()` before serving requests.

## Related packages

- `Rystem.RepositoryFramework.Abstractions`
- `Rystem.RepositoryFramework.Api.Client`
