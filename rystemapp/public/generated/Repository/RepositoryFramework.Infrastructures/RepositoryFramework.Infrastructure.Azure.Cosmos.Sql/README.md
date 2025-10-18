### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Azure Cosmos Sql and Repository Framework
Example from unit test with a business integration too.

     await services
        .AddRepositoryAsync<AppUser, AppUserKey>(async builder =>
            {
                await builder.WithCosmosSqlAsync(x =>
                {
                    x.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
                    x.Settings.DatabaseName = "unittestdatabase";
                    x.WithId(x => new AppUserKey(x.Id));
                }).NoContext();
            builder
                .AddBusiness()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness2>();
        }).NoContext();

You found the IRepository<AppUser, AppUserKey> in DI to play with it.

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your cosmos sql integration.
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
