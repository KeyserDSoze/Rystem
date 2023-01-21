### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Services extensions
You may add a repository client for your model. You may choose the domain (domain where the api is), and the custom path by default is "api", you may add custom configuration to the HttpClient and the service lifetime with singleton as default. The api url will be https://{domain}/{startingPath}/{ModelName}/{Type of Api (from Insert, Update, Delete, Batch, Get, Query, Exist, Operation)}

    public static IRepositoryBuilder<T, TKey> AddRepositoryApiClient<T, TKey>(this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TKey : notnull

You have the same client for CQRS, with command
    
     public static IRepositoryBuilder<T, TKey> AddCommandApiClient<T, TKey>(this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TKey : notnull

and query
    
      public static IRepositoryBuilder<T, TKey> AddQueryApiClient<T, TKey>(this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TKey : notnull

### HttpClient to use your API (example)
You can add a client for a specific url

    builder.Services.AddRepository<User, string>(settings =>
    {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

You may add a Polly policy to your api client for example:

    var retryPolicy = HttpPolicyExtensions
      .HandleTransientHttpError()
      .Or<TimeoutRejectedException>()
      .RetryAsync(3);

    builder.Services.AddRepository<User, string>(settings =>
    {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058")
                .ClientBuilder
            .AddPolicyHandler(retryPolicy);
    });
    
and use it in DI with
    
    IRepository<User, string> repository

### Query and Command
In DI you install the services

    services.AddCommand<User, string>(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddQuery<User, string>(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

And you may inject the objects
## Please, use ICommand, IQuery and not ICommandPattern, IQueryPattern

    ICommand<User, string> command
    IQuery<User, string> command

### With a non default key
In DI you install the services with a bool key for example.

    services.AddRepository<User, bool>(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddCommand<User, bool>(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddQuery<User, bool>(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

And you may inject the objects
## Please, use ICommand, IQuery, IRepository and not ICommandPattern, IQueryPattern, IRepositoryPattern
    
    IRepository<User, string> repository
    ICommand<User, string> command
    IQuery<User, string> command

### Interceptors
You may add a custom interceptor for every request for every model

    public static IServiceCollection AddApiClientInterceptor<TInterceptor>(this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TInterceptor : class, IRepositoryClientInterceptor

or a specific interceptor for each model
    
    public static IServiceCollection AddApiClientInterceptor<TInterceptor>(this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TInterceptor : class, IRepositoryClientInterceptor

or for a string as default TKey

     public static RepositorySettings<T, TKey> AddApiClientSpecificInterceptor<T, TKey, TInterceptor>(
        this RepositorySettings<T, TKey> settings,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TInterceptor : class, IRepositoryClientInterceptor<T>
        where TKey : notnull   

Maybe you can use it to add a token as JWT o another pre-request things.

### Default interceptor for Authentication with JWT
You may use the default interceptor to deal with the identity manager in .Net DI.

    builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

This line of code inject an interceptor that works with ITokenAcquisition, injected by the framework during OpenId integration (for example AAD integration).
Automatically it adds the token to each request.

You may use the default identity interceptor not on all repositories, you can specificy them with

    builder.Services.AddRepository(settings => {
        settings
            .WithApiClient()
            .WithHttpClient("localhost:7058")
            .AddCustomAuthorizationInterceptorForApiHttpClient<T, TKey>();
    });