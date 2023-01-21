### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Integration with Entity Framework and Repository Framework

     builder.Sservices
        .AddRepositoryInEntityFramework<MappingUser, int, User, SampleContext>(
            x =>
            {
                x.DbSet = x => x.Users;
                x.References = x => x.Include(x => x.IdGruppos);
            })
        .Translate<User>()
            .With(x => x.Username, x => x.Nome)
            .With(x => x.Username, x => x.Cognome)
            .With(x => x.Email, x => x.IndirizzoElettronico)
            .With(x => x.Groups, x => x.IdGruppos)
            .With(x => x.Id, x => x.Identificativo)
            .WithKey(x => x, x => x.Identificativo)
        .Builder
        .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness>()
        .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness2>();

You found the IRepository<MappingUser, int> in DI to play with it.

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
