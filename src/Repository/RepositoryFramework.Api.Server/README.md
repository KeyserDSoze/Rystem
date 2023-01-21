### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Api auto-generated
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

### Startup example
In the example below you may find the DI for repository with string key for User model, populated with random data in memory, swagger to test the solution, the population method just after the build and the configuration of your API based on repository framework.
Futhermore, we are adding a configuration for AAD to implement authentication on api.

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
        .AddRepositoryInMemoryStorage<User>()
        .PopulateWithRandomData(x => x.Email!, 120, 5);
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
    app.UseApiForRepositoryFramework()
        .WithDefaultAuthorization();
    app.Run();

### No Authorization flow - default

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
        .AddRepositoryInMemoryStorage<User>()
        .PopulateWithRandomData(x => x.Email!, 120, 5);
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
    app.UseApiForRepositoryFramework()
        .WithNoAuthorization();
    app.Run();

### Authorization flow - custom policies
You may configure the scoper for each method of your repository and for each repository, as you wish.

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
        .AddRepositoryInMemoryStorage<User>()
        .PopulateWithRandomData(x => x.Email!, 120, 5);
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
    app.UseApiForRepositoryFramework()
        .SetPolicyForAll()
        .With("Normal User")
        .And()
        .SetPolicy(RepositoryMethod.Insert)
        .With("Admin")
        .And()
        .SetPolicy(RepositoryMethod.Update)
        .With("Admin")
        .And()
        .Finalize();

    app.Run();

In this example, I'm configuring a policy named "Normal User" for all methods and all repositories, and a policy named "Admin" for the methods Insert and Update for all repositories.
You can customize it repository for repository, using AddApiForRepository<T>() method.

### Sample of filter usage when you use the api directly
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