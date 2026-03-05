### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with MsSql and Repository Framework
Example from unit test with a business integration too.

     services.
        AddRepository<Cat, Guid>(settings =>
        {
            settings
            .WithMsSql(builder =>
            {
                builder.Schema = "repo";
                builder.ConnectionString = configuration["ConnectionString:Database"];
                builder.WithPrimaryKey(x => x.Id, x =>
                    {
                        x.ColumnName = "Key";
                    })
                .WithColumn(x => x.Paws, x =>
                {
                    x.ColumnName = "Zampe";
                    x.IsNullable = true;
                });
            });
        });

You found the IRepository<Cat, Guid> in DI to play with it.

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
