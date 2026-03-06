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
        // Column prefix — defaults to "new_"
        dataverseBuilder.Settings.Prefix = "repo_";
        // Table name — defaults to the model type name
        dataverseBuilder.Settings.TableName = "AccountModel";
        dataverseBuilder.Settings.SolutionName = "MySolution";
        dataverseBuilder.Settings.Description = "Account repository";
        // Primary key property name on the model — defaults to "Id"
        dataverseBuilder.Settings.PrimaryKey = "Id";

        dataverseBuilder.Settings.SetConnection(
            environment: builder.Configuration["Dataverse:Environment"]!,
            identity: new DataverseAppRegistrationAccount(
                builder.Configuration["Dataverse:ClientId"]!,
                builder.Configuration["Dataverse:ClientSecret"]!));
    });
});
```

## Custom column prefix

All columns share the global `Prefix` by default. To override the prefix for a specific property:

```csharp
dataverseBuilder.WithColumn(x => x.ExternalId, customPrefix: "ext_");
// Leave customPrefix null to keep the global prefix for that column
```

## CQRS patterns

```csharp
// Command only
builder.Services.AddCommand<AccountModel, string>(commandBuilder =>
    commandBuilder.WithDataverse(b => { /* ... */ }));

// Query only
builder.Services.AddQuery<AccountModel, string>(queryBuilder =>
    queryBuilder.WithDataverse(b => { /* ... */ }));
```

## Startup — WarmUp required

`WithDataverse` automatically registers a warm-up action that validates and provisions Dataverse metadata (table columns, solution membership). You **must** call `WarmUpAsync()` at application startup:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

## Settings reference

| Property | Type | Description |
| --- | --- | --- |
| `SetConnection(env, identity)` | method | Sets the Dataverse environment URL and app registration credentials |
| `Prefix` | `string` | Column/table logical name prefix (default `"new_"`) |
| `TableName` | `string` | Dataverse table name — defaults to the model type name |
| `SolutionName` | `string?` | Optional Dataverse solution to associate the table with |
| `Description` | `string?` | Optional description for the table |
| `PrimaryKey` | `string` | Property name used as primary key (default `"Id"`) |

### DataverseAppRegistrationAccount

```csharp
new DataverseAppRegistrationAccount(clientId: "...", clientSecret: "...");
```
