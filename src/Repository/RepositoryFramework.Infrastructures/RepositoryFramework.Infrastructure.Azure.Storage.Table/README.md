### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Integration with Azure TableStorage and Repository Framework

    builder.Services
        .AddRepositoryInTableStorage<User, string>(builder.Configuration["ConnectionString:Storage"]);

You found the IRepository<User, string> in DI to play with it.

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your tablestorage integration.
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
