# Rystem.RepositoryFramework.Cache.Azure.Storage.Blob

Azure Blob Storage distributed cache provider for Repository/CQRS decorators.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Cache.Azure.Storage.Blob
```

## Quick start

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });

    repositoryBuilder.WithInMemoryCache(cacheOptions =>
    {
        cacheOptions.ExpiringTime = TimeSpan.FromSeconds(60);
        cacheOptions.Methods = RepositoryMethods.Get | RepositoryMethods.Insert | RepositoryMethods.Update | RepositoryMethods.Delete;
    });

    repositoryBuilder.WithBlobStorageCache(
        blobStorageOptions =>
        {
            blobStorageOptions.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        },
        cacheOptions =>
        {
            cacheOptions.ExpiringTime = TimeSpan.FromSeconds(120);
            cacheOptions.Methods = RepositoryMethods.All;
        });
});
```

## Usage

Use the same service contracts as usual:

- `IRepository<User, string>`
- `ICommand<User, string>`
- `IQuery<User, string>`
