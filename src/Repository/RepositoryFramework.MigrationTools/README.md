### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Migration Tools

You need to create a base model as a bridge for your migration. After that you can use the two repositories with repository pattern to help yourself with the migration from a old storage to a brand new storage.

### Sample with in memory integration (From UnitTest)
For instance you can create a repository (where the data will be migrated) and a migration source (where the data is)

    .AddRepository<SuperMigrationUser, string, SuperMigrationTo>(settings =>
    {
        settings
            .AddMigrationSource<SuperMigrationUser, string, SuperMigrationFrom>(x => x.NumberOfConcurrentInserts = 2);
    })

Now you may use the interface in DI

    IMigrationManager<SuperMigrationUser, string> migrationService

and let the sorcery happens

    var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true);