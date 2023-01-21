### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Cache example

    builder.Services
        .AddRepositoryInBlobStorage<User, string>(builder.Configuration["ConnectionString:Storage"])
        .WithInMemoryCache(x =>
        {
            x.RefreshTime = TimeSpan.FromSeconds(20);
            x.Methods = RepositoryMethod.All;
        })
        .WithBlobStorageCache(builder.Configuration["ConnectionString:Storage"], settings: x =>
        {
            x.RefreshTime = TimeSpan.FromSeconds(120);
            x.Methods = RepositoryMethod.All;
        });

### Usage
You always will find the same interface. For instance

    IRepository<User, string> repository

or if you added a query pattern or command pattern

    IQuery<User, string> query 
    ICommand<User, string> command