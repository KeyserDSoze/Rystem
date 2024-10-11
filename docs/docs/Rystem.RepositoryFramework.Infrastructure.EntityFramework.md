# Integration with Entity Framework and Repository Framework
Example from unit test with a business integration too.

     services.AddDbContext<SampleContext>(options =>
            {
                options.UseSqlServer(configuration["ConnectionString:Database"]);
            }, ServiceLifetime.Scoped);

    services
        .AddRepository<MappingUser, int>(builder =>
        {
            builder.WithEntityFramework<MappingUser, int, User, SampleContext>(
                t =>
                {
                    t.DbSet = x => x.Users;
                    t.References = x => x.Include(x => x.IdGruppos);
                });
            builder.Translate<User>()
                .With(x => x.Username, x => x.Nome)
                .With(x => x.Username, x => x.Cognome)
                .With(x => x.Email, x => x.IndirizzoElettronico)
                .With(x => x.Groups, x => x.IdGruppos)
                .With(x => x.Id, x => x.Identificativo)
                .WithKey(x => x, x => x.Identificativo);
            builder
                .AddBusiness()
                    .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness>()
                    .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness2>();
        });

You found the IRepository<MappingUser, int> in DI to play with it.

## Automated api with Rystem.RepositoryFramework.Api.Server package
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