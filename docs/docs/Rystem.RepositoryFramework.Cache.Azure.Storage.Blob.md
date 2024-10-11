# Cache example

    .AddRepository<User, string>(repositoryBuilder =>
    {
        repositoryBuilder
        .WithBlobStorage(storageBuilder =>
        {
            storageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionString:Storage"];
        });
        repositoryBuilder
        .WithInMemoryCache(x =>
        {
            x.ExpiringTime = TimeSpan.FromSeconds(60);
            x.Methods = RepositoryMethods.Get | RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete;
        })
        .WithBlobStorageCache(
            x =>
            {
                x.Settings.ConnectionString = builder.Configuration["ConnectionString:Storage"];
            }
            , x =>
            {
                x.ExpiringTime = TimeSpan.FromSeconds(120);
                x.Methods = RepositoryMethods.All;
            });
    });

## Usage
You always will find the same interface. For instance

    IRepository<User, string> repository

or if you added a query pattern or command pattern

    IQuery<User, string> query 
    ICommand<User, string> command