### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Migration Tools

You need to create a base model as a bridge for your migration. After that you can use the two repositories with repository pattern to help yourself with the migration from a old storage to a brand new storage.

### Sample with in memory integration (From UnitTest)
For instance you can create a repository (where the data will be migrated) and a migration source (where the data is)

    .AddRepository<MigrationUser, MigrationTo>()
    .AddMigrationSource<MigrationUser, MigrationFrom>(x => x.NumberOfConcurrentInserts = 2)

Now you may use the interface in DI

    IMigrationManager<MigrationUser> migrationService

and let the sorcery happen

    var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true);

### Sample with a State as TState and a string as TKey
    
    .AddRepository<IperMigrationUser, string, State, IperMigrationTo>()
    .AddMigrationSource<IperMigrationUser, string, State, IperMigrationFrom>(x => x.NumberOfConcurrentInserts = 2, x => x)

Now you may use the interface in DI

    IMigrationManager<MigrationUser, string, State<T>> migrationService

and let the sorcery happen

    var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true);

### Sample with a bool as TState by default and a string as TKey
    
    .AddRepository<IperMigrationUser, string, IperMigrationTo>()
    .AddMigrationSource<IperMigrationUser, string, IperMigrationFrom>(x => x.NumberOfConcurrentInserts = 2)

Now you may use the interface in DI

    IMigrationManager<MigrationUser, string> migrationService

and let the sorcery happen

    var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true);