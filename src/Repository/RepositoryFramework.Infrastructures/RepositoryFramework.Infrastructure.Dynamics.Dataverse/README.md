### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Dataverse (Dynamics) and Repository Framework
Example from unit test with a business integration too.

     services.
        AddRepository<CalamityUniverseUser, string>(builder =>
        {
            builder.WithDataverse(dataverserBuilder =>
            {
                dataverserBuilder.Settings.Prefix = "repo_";
                dataverserBuilder.Settings.SolutionName = "TestAlessandro";
                if (configuration != null)
                    dataverserBuilder.Settings.SetConnection(configuration["ConnectionString:Dataverse:Environment"],
                        new(configuration["ConnectionString:Dataverse:ClientId"],
                        configuration["ConnectionString:Dataverse:ClientSecret"]));
            });
            builder
                .AddBusiness()
                .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness2>();
        });

You found the IRepository<CalamityUniverseUser, string> in DI to play with it.

## Configure database after build
You have to run a method after the service collection build during startup. This method creates your tables.

    var app = builder.Build();
    await app.Services.WarmUpAsync();

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your dataverse integration.
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
