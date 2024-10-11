# Api auto-generated
In your web application you have only to add one row after service build.

     services.AddApiFromRepositoryFramework()
        .WithDescriptiveName("Repository Api")
        .WithPath(Path)
        .WithSwagger()
        .WithVersion(Version)
        .WithDocumentation()
        .WithDefaultCors("http://example.com");

    var app = builder.Build();
    app.UseApiFromRepositoryFramework()
       .WithNoAuthorization();

    public static ApiAuthorizationBuilder UseApiFromRepositoryFramework<TEndpointRouteBuilder>(
        this TEndpointRouteBuilder app,
        string startingPath = "api")
        where TEndpointRouteBuilder : IEndpointRouteBuilder
    
You may add api for each service by

    public static ApiAuthorizationBuilder UseApiForRepository<T>(this IEndpointRouteBuilder app,
        string startingPath = "api")

## Startup example
In the example below you may find the setup of three populated repositories, two of them are of the same kind (SuperUser).
The SuperiorUser will be added to the app but will be not exposed as Api cause the SetNotExposable() method.
Futhermore, we are adding a configuration for AAD to implement authentication on api.

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
    .AddRepository<SuperUser, string>(settins =>
    {
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        });
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(2, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        }, "inmemory");
    });

    builder.Services.AddRepository<SuperiorUser, string>(settings =>
    {
        settings.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
                .WithPattern(x => x.Value!.Port, @"[1-9]{3,4}");
        });
        settings.SetNotExposable();
    });
        
    builder.Services.AddApiFromRepositoryFramework()
        .WithDescriptiveName("Repository Api")
        .WithPath(Path)
        .WithSwagger()
        .WithVersion(Version)
        .WithDocumentation()
        .WithDefaultCors("http://example.com");  
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();
    
    app.UseHttpsRedirection();
    app.UseApiFromRepositoryFramework()
        .WithDefaultAuthorization();
    app.Run();

## No Authorization flow - default

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
    .AddRepository<SuperUser, string>(settins =>
    {
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        });
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(2, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        }, "inmemory");
    });

    builder.Services.AddRepository<SuperiorUser, string>(settings =>
    {
        settings.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
                .WithPattern(x => x.Value!.Port, @"[1-9]{3,4}");
        });
        settings.SetNotExposable();
    });
    builder.Services.AddApiFromRepositoryFramework()
        .WithDescriptiveName("Repository Api")
        .WithPath(Path)
        .WithSwagger()
        .WithVersion(Version)
        .WithDocumentation()
        .WithDefaultCors("http://example.com");    
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseHttpsRedirection();
    app.UseApiFromRepositoryFramework()
        .WithNoAuthorization();
    app.Run();

## Authorization flow - custom policies
You may configure the scoper for each method of your repository and for each repository, as you wish.

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
    .AddRepository<SuperUser, string>(settins =>
    {
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        });
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(2, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        }, "inmemory");
    });

    builder.Services.AddRepository<SuperiorUser, string>(settings =>
    {
        settings.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com")
                .WithPattern(x => x.Value!.Port, @"[1-9]{3,4}");
        });
        settings.SetNotExposable();
    });
    
    builder.Services.AddAuthorization(
        options =>
        {
            options.AddPolicy("NormalUser", x =>
            {
                x.RequireClaim(ClaimTypes.Name);
            });
            options.AddPolicy("SuperAdmin", x =>
            {
                x.RequireRole("SuperAdmin");
            });
        });

    builder.Services.AddApiFromRepositoryFramework()
        .WithDescriptiveName("Repository Api")
        .WithPath(Path)
        .WithSwagger()
        .WithVersion(Version)
        .WithDocumentation()
        .WithDefaultCors("http://example.com");     
    
    var app = builder.Build();
    await app.Services.WarmUpAsync();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseHttpsRedirection();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapHealthChecks("/healthz");
        endpoints.UseApiFromRepository<SuperUser>()
            .SetPolicyForCommand()
            .With("SuperAdmin")
            .Build();
        endpoints.UseApiFromRepositoryFramework()
            .SetPolicyForAll()
            .With("NormalUser")
            .And()
            .SetPolicy(RepositoryMethods.Insert)
            .With("SuperAdmin")
            .And()
            .SetPolicy(RepositoryMethods.Update)
            .With("SuperAdmin")
            .Build();
        endpoints
            .MapControllers();
    });
    app.Run();

In this example, I'm configuring a policy named "NormalUser" for all methods and all repositories, and a policy named "SuperAdmin" for the methods Insert and Update for all repositories and for the command (Insert, Updated and Delete) of SuperUser repository.
You can customize it repository for repository, using UseApiFromRepository<T>() method.

## Sample of filter usage when you use the api directly
All the requests are basic requests, the strangest request is only the query and you must use the Linq query.
You may find some examples down below:

    ƒ => (((ƒ.X == "dasda") AndAlso ƒ.X.Contains("dasda")) AndAlso ((ƒ.E == Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2")) Or (ƒ.Id == 32)))
    ƒ => ((((ƒ.X == "dasda") AndAlso ƒ.Sol) AndAlso ƒ.X.Contains("dasda")) AndAlso ((ƒ.E == Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2")) Or (ƒ.Id == 32)))
    ƒ => (((((ƒ.X == "dasda") AndAlso ƒ.Sol) AndAlso ƒ.X.Contains("dasda")) AndAlso ((ƒ.E == Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))
    ƒ => (ƒ.Type == 2)
    ƒ => (((((ƒ.X == "dasda") AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains("dasda") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))
    ƒ => ((((((ƒ.X == "dasda") AndAlso ƒ.Samules.Any(x => (x == "ccccde"))) AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains("dasda") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))
    ƒ => (ƒ.ExpirationTime > Convert.ToDateTime("7/6/2022 9:48:56 AM"))
    ƒ => (ƒ.TimeSpan > new TimeSpan(1000 as long))
    ƒ => Not(ƒ.Inside.Inside.A.Equals("dasdad"))
    ƒ => Not(String.IsNullOrWhiteSpace(ƒ.Inside.Inside.A))