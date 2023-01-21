### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Integration with MsSql and Repository Framework

     builder.Services.AddRepositoryInMsSql<Cat, Guid>(x =>
            {
                x.Schema = "repo";
                x.ConnectionString = configuration["ConnectionString:Database"];
            })
            .WithPrimaryKey(x => x.Id, x =>
            {
                x.ColumnName = "Key";
            })
            .WithColumn(x => x.Paws, x =>
            {
                x.ColumnName = "Zampe";
                x.IsNullable = true;
            })
            .AddBusinessBeforeInsert<CatBeforeInsertBusiness>()
            .AddBusinessBeforeInsert<CatBeforeInsertBusiness2>()

You found the IRepository<Cat, Guid> in DI to play with it.

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
