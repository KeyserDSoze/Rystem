### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Azure BlobStorage and Repository Framework
Example from unit test with a business integration too.

    services
        .AddRepository<Car, Guid>(builder =>
        {
            builder.WithBlobStorage(builder =>
            {
                builder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
                builder.Settings.Prefix = "MyFolder/";
            });
        });
    services
        .AddBusinessForRepository<Car, Guid>()
            .AddBusinessBeforeInsert<CarBeforeInsertBusiness>()
            .AddBusinessBeforeInsert<CarBeforeInsertBusiness2>();

You found the IRepository<Car, Guid> in DI to play with it.

### Automated api with Rystem.RepositoryFramework.Api.Server package
With automated api, you may have the api implemented with your blobstorage integration.
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
