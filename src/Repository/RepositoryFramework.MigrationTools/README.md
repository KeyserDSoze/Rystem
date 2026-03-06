# Rystem.RepositoryFramework.MigrationTools

Utilities to migrate data between two repository integrations (source and destination) using `IMigrationManager<T, TKey>`.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.MigrationTools
```

## Quick start

```csharp
builder.Services
    .AddRepository<SuperMigrationUser, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(inMemoryBuilder =>
        {
            inMemoryBuilder.PopulateWithRandomData(1000);
        }, "source");
    })
    .AddRepository<SuperMigrationUser, string>(repositoryBuilder =>
    {
        repositoryBuilder.WithInMemory(inMemoryBuilder =>
        {
            inMemoryBuilder.PopulateWithRandomData(0);
        }, "target");
    })
    .AddMigrationManager<SuperMigrationUser, string>(settings =>
    {
        settings.SourceFactoryName = "source";
        settings.DestinationFactoryName = "target";
        settings.NumberOfConcurrentInserts = 10;
    });
```

## Run migration

```csharp
public sealed class MigrationRunner(IMigrationManager<SuperMigrationUser, string> migrationManager)
{
    public async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        return await migrationManager.MigrateAsync(
            x => x.Id!,
            checkIfExists: true,
            deleteEverythingBeforeStart: false,
            cancellationToken: cancellationToken);
    }
}
```

## `MigrateAsync` parameters

| Parameter | Meaning |
| --- | --- |
| `Expression<Func<T, TKey>> navigationKey` | Maps destination key from model value |
| `bool checkIfExists` | Checks destination existence before insert/update |
| `bool deleteEverythingBeforeStart` | Deletes destination data before migration |
| `CancellationToken cancellationToken` | Cancels the migration flow |
