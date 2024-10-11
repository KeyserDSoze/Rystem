# Migration Tools

You need to create a base model as a bridge for your migration. After that you can use the two repositories with repository pattern to help yourself with the migration from a old storage to a brand new storage.

## Sample with in memory integration (From UnitTest)
For instance you can create two repositories, one as source and one as target.
In the example we use an easy test integration with two in memory integrations.

    .AddRepository<SuperMigrationUser, string>(builder =>
    {
        builder.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(NumberOfItems);
        }, "source");
    })
    .AddRepository<SuperMigrationUser, string>(builder =>
    {
        builder.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(NumberOfItems);
        }, "target");
    })
        .AddMigrationManager<SuperMigrationUser, string>(settings =>
        {
            settings.SourceFactoryName = "source";
            settings.DestinationFactoryName = "target";
            settings.NumberOfConcurrentInserts = 10;
        })

Now you may use the interface in DI

    IMigrationManager<SuperMigrationUser, string> migrationService

and let the sorcery happens

    var migrationResult = await _migrationService.MigrateAsync(x => x.Id!, true);

## Parameters
| Name | Description |
| ------------------------- | ------------------------------ |
| Expression<Func<T, TKey>> navigationKey | Explain how to create the TKey from the TValue |
| bool checkIfExists = false | check existence on target before download from source |
| bool deleteEverythingBeforeStart = false | delete all items before starting the migration from target |