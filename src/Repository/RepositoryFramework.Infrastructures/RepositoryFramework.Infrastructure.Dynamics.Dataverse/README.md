# Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse

Microsoft Dynamics Dataverse integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Dynamics.Dataverse
```

## Quick start

```csharp
builder.Services.AddRepository<AccountModel, string>(repositoryBuilder =>
{
    repositoryBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "repo_";
        dataverseBuilder.Settings.SolutionName = "MySolution";

        dataverseBuilder.Settings.SetConnection(
            environment: builder.Configuration["Dataverse:Environment"]!,
            identity: new DataverseAppRegistrationAccount(
                builder.Configuration["Dataverse:ClientId"]!,
                builder.Configuration["Dataverse:ClientSecret"]!));
    });
});
```

## Startup note

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

Call `WarmUpAsync()` to provision/check Dataverse metadata used by the repository.
