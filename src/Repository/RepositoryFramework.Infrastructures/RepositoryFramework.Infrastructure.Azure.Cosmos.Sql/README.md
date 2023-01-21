### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Integration with Azure Cosmos Sql and Repository Framework

    builder.Services
         .AddRepositoryInCosmosSql<User, string>(
            builder.Configuration["ConnectionString:CosmosSql"],
            "BigDatabase");

You found the IRepository<User, string> in DI to play with it.

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your cosmos sql integration.
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
