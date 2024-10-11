# In memory integration by default
With this library you can add in memory integration with the chance to create random data with random values, random based on regular expressions and delegated methods
        
## How to populate with random data?

## Simple random (example)
Populate your in memory storage with 120 users

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddRepository<IperUser, string>(repositoryBuilder =>
    {
        repositoryBuilder
            .WithInMemory(inMemoryBuilder =>
            {
                inMemoryBuilder
                    .PopulateWithRandomData(120, 5)
                    .WithPattern(x => x.Value.Email, @"[a-z]{5,10}@gmail\.com");
            });
        repositoryBuilder
            .AddBusiness()
                .AddBusinessBeforeInsert<IperRepositoryBeforeInsertBusiness>();
        repositoryBuilder
            .Translate<IperUser>();
    });

and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();
    
## Simple random with regex (example)
Populate your in memory storage with 100 users and property Email with a random regex @"[a-z]{4,10}@gmail\.com"

    .AddRepository<User, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100)
                    .WithPattern(x => x.Email, @"[a-z]{4,10}@gmail\.com")
            });
    });

and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();

## Where can I use the regex pattern?
You can use regex pattern on all primitives type and most used structs.
### Complete list:
### int, uint, byte, sbyte, short, ushort, long, ulong, nint, nuint, float, double, decimal, bool, char, Guid, DateTime, TimeSpan, Range, string, int?, uint?, byte?, sbyte?, short?, ushort?, long?, ulong?, nint?, nuint?, float?, double?, decimal?, bool?, char?, Guid?, DateTime?, TimeSpan?, Range?, string?

You can use the pattern in Class, IEnumerable, IDictionary, or Array, and in everything that extends IEnumerable or IDictionary

**Important!! You can override regex service in your DI**
    
    public static IServiceCollection AddRegexService<T>(
            this IServiceCollection services)
            where T : class, IRegexService

## IEnumerable or Array one-dimension (example)
You have your model x (User) that has a property Groups as IEnumerable or something that extends IEnumerable, Groups is a class with a property Id as string.
In the code below you are creating a list of class Groups with 8 elements in each 100 User instances, in each element of Groups you randomize based on this regex "[a-z]{4,5}".
You may take care of use First() linq method to set correctly the Id property.
    
    .AddRepository<User, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100, 8)
                    .WithPattern(x => x.Groups!.First().Id, "[a-z]{4,5}");
            });
    });
    
and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();

## IDictionary (example)
Similar to IEnumerable population you may populate your Claims property (a dictionary) with random key but with values based on regular expression "[a-z]{4,5}". As well as IEnumerable implementation you will have 6 elements (because I choose to create 6 elements in Populate method)

    .AddRepository<User, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100, 6)
                    .WithPattern(x => x.Claims!.First().Value, "[a-z]{4,5}");
            });
    });

and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();
    
or if you have in Value an object
    
    AddRepository<User, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100, 6)
                    .WithPattern(x => x.Claims!.First().Value.SomeProperty, "[a-z]{4,5}");
            });
    });
    
and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();

## Populate with delegation
Similar to regex pattern, you can use a delegation to populate something.

## Dictionary (example)
Here you can see that all 6 elements in each 100 users are populated in Value with string "A"

    .AddRepository<User, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100, 6)
                    .WithPattern(x => x.Claims!.First().Value, () => "A");
            });
    });
    
and in app after build during startup of your application
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();

## Populate with Implementation
If you have an interface or abstraction in your model, you can specify an implementation type for population.
You have two different methods, with typeof

    .AddRepository<PopulationTest, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100, 2)
                    .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation));
            });
    });

or generics

    .AddRepository<PopulationTest, string>(builder => {
        builder
            .WithInMemory(inMemoryBuilder => {
                inMemoryBuilder
                    .PopulateWithRandomData(100)
                    .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
            });
    });

# In Memory, simulate real implementation
If you want to test with possible exceptions (for your reliability tests) and waiting time (for your load tests) you may do it with this library and in memory behavior settings.

## Add random exceptions
You can set different custom exceptions and different percentage for each operation: Delete, Get, Insert, Update, Query.
In the code below I'm adding three exceptions with a percentage of throwing them, they are the same for each operation.
I have a 0.45% for normal Exception, 0.1% for "Big Exception" and 0.548% for "Great Exception"

    .AddRepository<Car, string>(settings =>
    {
        settings.WithInMemory(builder =>
        {
            var customExceptions = new List<ExceptionOdds>
            {
                new ExceptionOdds()
                {
                    Exception = new Exception("Normal Exception"),
                    Percentage = 10.352
                },
                new ExceptionOdds()
                {
                    Exception = new Exception("Big Exception"),
                    Percentage = 49.1
                },
                new ExceptionOdds()
                {
                    Exception = new Exception("Great Exception"),
                    Percentage = 40.548
                }
            };
            builder.Settings.AddForRepositoryPattern(new MethodBehaviorSetting
            {
                ExceptionOdds = customExceptions
            });
        });
    });
    
## Add random waiting time
You can set different range in milliseconds for each operation to simulate the await of an external integration.
In the code below I'm adding a same custom range for all Repository interfaces between 1000ms and 2000ms.

    .AddRepository<User, string>(builder =>
    {
        builder.WithInMemory(inMemoryBuilder =>
        {
            var customRange = new Range(1000, 2000);
            inMemoryBuilder.Settings.AddForRepositoryPattern(new MethodBehaviorSetting
            {
                MillisecondsOfWait = customRange
            });
        });
    });