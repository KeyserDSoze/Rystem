### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Azure TableStorage and Repository Framework
Example from unit test with a business integration too.

    services
    .AddRepository<SuperCar, Guid>(settings =>
    {
        settings
            .WithTableStorage(x => x.ConnectionString = configuration["ConnectionString:Storage"])
            .WithPartitionKey(x => x.Id, x => x)
            .WithRowKey(x => x.Name)
            .WithTimestamp(x => x.Time)
            .WithTableStorageKeyReader<Car2KeyStorageReader>();
        settings
        .AddBusiness()
        .AddBusinessBeforeInsert<SuperCarBeforeInsertBusiness>()
        .AddBusinessBeforeInsert<SuperCarBeforeInsertBusiness2>();
    });

You found the IRepository<SuperCar, Guid> in DI to play with it.

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your tablestorage integration.
You need only to add the AddApiFromRepositoryFramework and UseApiForRepositoryFramework

     builder.Services.AddApiFromRepositoryFramework()
        .WithDescriptiveName("Repository Api")
        .WithPath(Path)
        .WithSwagger()
        .WithVersion(Version)
        .WithDocumentation()
        .WithDefaultCors("http://example.com");  

    var app = builder.Build();

    app.UseApiForRepositoryFramework()
        .WithNoAuthorization();
