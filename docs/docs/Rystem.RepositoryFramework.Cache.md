# Cache

## Examples
You can add a repository (with default blob integration for instance) and after attack an in memory cache for all methods.
The RefreshTime is a property that adds an Expiration date to the cached value, in the example below you can see that after 20 seconds the in memory cache requests again to the repository pattern a new value for each key.
The Methods is a flag that allows to setup what operations have to be cached.

Query -> query will be cached with this key

    var keyAsString = $"{nameof(RepositoryMethods.Query)}_{typeof(T).Name}_{FactoryName}_{filter.ToKey()}";

Operation -> operation will be cached with this key

    var keyAsString = $"{nameof(RepositoryMethods.Operation)}_{operation.Name}_{typeof(T).Name}_{FactoryName}_{filter.ToKey()}";

Get -> query will be cached with this key
    
    var keyAsString = $"{nameof(RepositoryMethods.Get)}_{typeof(T).Name}_{FactoryName}_{key.AsString()}";

Exist -> query will be cached with this key
    
    var keyAsString = $"{nameof(RepositoryMethod.Exist)}_{typeof(T).Name}_{FactoryName}_{key.AsString()}";

Now you can understand the special behavior for commands. If you set Insert and/or Update and/or Delete, during any command if you allowed it for each command automatically the framework will update the cache value, with updated or inserted value or removing the deleted value.
The code below allows everything

    x.Methods = RepositoryMethod.All

In the example below you're setting up the following behavior: setting up a cache only for Get operation, and update the Get cache when exists a new Insert or an Update, or a removal when Delete operation were perfomed.
    
    x.Methods = RepositoryMethod.Get | RepositoryMethod.Insert | RepositoryMethod.Update | RepositoryMethod.Delete

## Setup in DI

	services
        .AddRepository<Plant, int>(settings =>
        {
            settings
                .WithInMemory();
            settings
                .WithInMemoryCache(x =>
                {
                    x.ExpiringTime = TimeSpan.FromSeconds(1);
                    x.Methods = RepositoryMethods.All;
                });
        });

## Usage
You always will find the same interface. For instance

    IRepository<Plant, int> repository

or if you added a query pattern or command pattern

    IQuery<Plant, int> query 
    ICommand<Plant, int> command

## Distributed Cache
Based on this [link](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed) you may use the standard interface IDistributedCache instead of create a custom IDistributedCache<T, TKey, TState>.
For instance you may choose between three libraries: Distributed SQL Server cache, Distributed Redis cache, Distributed NCache cache.
You need to add the cache

    builder.Services.AddStackExchangeRedisCache(options =>
     {
         options.Configuration = builder.Configuration.GetConnectionString("MyRedisConStr");
         options.InstanceName = "SampleInstance";
     });

then you add the IDistributedCache implementation to your repository patterns or CQRS.

    .AddRepository<Country, CountryKey>(builder =>
    {
        builder
            .WithInMemory(inMemoryBuilder =>
            {
                inMemoryBuilder
                    .PopulateWithRandomData(NumberOfEntries, NumberOfEntries);
            });
        builder
            .WithDistributedCache(distributedCacheBuilder =>
            {
                distributedCacheBuilder.ExpiringTime = TimeSpan.FromSeconds(10);
            });
    });

or a mix of them

    .AddRepository<Country, CountryKey>(builder =>
    {
        builder
            .WithInMemory(inMemoryBuilder =>
            {
                inMemoryBuilder
                    .PopulateWithRandomData(NumberOfEntries, NumberOfEntries);
            });
        builder
            .WithInMemoryCache(inMemoryCacheBuilder =>
            {
                inMemoryCacheBuilder.ExpiringTime = TimeSpan.FromSeconds(10);
            })
            .WithDistributedCache(distributedCacheBuilder =>
            {
                distributedCacheBuilder.ExpiringTime = TimeSpan.FromSeconds(10);
            });
    });

and as always you will use the standard interface that is automatically integrated in the repository flow.
    
    IRepository<User, string> repository;

The same is valid for ICommand and IQuery.