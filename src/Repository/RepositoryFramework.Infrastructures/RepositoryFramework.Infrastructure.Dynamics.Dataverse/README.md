### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Integration with Dataverse (Dynamics) and Repository Framework

     builder.Services.AddRepositoryDataverse<CalamityUniverseUser, string>(x =>
        {
            x.Prefix = "repo_";
            x.SolutionName = "TestAlessandro";
            x.Environment = configuration["ConnectionString:Dataverse:Environment"];
            x.ApplicationIdentity = new(configuration["ConnectionString:Dataverse:ClientId"],
                configuration["ConnectionString:Dataverse:ClientSecret"]);
        })
        .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness>()
        .AddBusinessBeforeInsert<CalamityUniverseUserBeforeInsertBusiness2>();

You found the IRepository<CalamityUniverseUser, string> in DI to play with it.

## Configure database after build
You have to run a method after the service collection build during startup. This method creates your tables.

    var app = builder.Build();
    await app.Services.WarmUpAsync();

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your dataverse integration.
You need only to add the AddApiFromRepositoryFramework and UseApiForRepositoryFramework

    builder.Services.AddApiFromRepositoryFramework(x =>
    {
        x.Name = "Repository Api";
        x.HasSwagger = true;
        x.HasDocumentation = true;
    });

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseApiForRepositoryFramework();
