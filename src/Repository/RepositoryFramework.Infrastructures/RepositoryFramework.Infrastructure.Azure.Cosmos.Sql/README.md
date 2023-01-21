### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Azure Cosmos Sql and Repository Framework
Example from unit test with a business integration too.

    services
        .AddRepository<SuperUser, string>(
        settings =>
        {
            settings.WithCosmosSql(x =>
            {
                x.ConnectionString = configuration["ConnectionString:CosmosSql"];
                x.DatabaseName = "BigDatabase";
            })
                .WithId(x => x.Email!);
            settings
                .AddBusiness()
                .AddBusinessBeforeInsert<SuperUserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<SuperUserBeforeInsertBusiness2>();
        });

You found the IRepository<SuperUser, string> in DI to play with it.

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

    app.UseApiForRepositoryFramework()
        .WithNoAuthorization();
