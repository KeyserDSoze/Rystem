### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Azure TableStorage and Repository Framework
Example from unit test with a business integration too.
Here you may find the chance to use ToResult() in services to avoid the async method.

    services
        .AddRepositoryAsync<AppUser, AppUserKey>(async builder =>
        {
            await builder
                .WithTableStorageAsync(tableStorageBuilder =>
                {
                    tableStorageBuilder
                        .Settings.ConnectionString = configuration["ConnectionString:Storage"];
                    tableStorageBuilder
                        .WithTableStorageKeyReader<TableStorageKeyReader>()
                        .WithPartitionKey(x => x.Id, x => x.Id)
                        .WithRowKey(x => x.Username)
                        .WithTimestamp(x => x.CreationTime);
                });
        }).ToResult();

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

    app.UseApiFromRepositoryFramework()
        .WithNoAuthorization();
