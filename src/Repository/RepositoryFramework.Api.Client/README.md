### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Services extensions

### HttpClient to use your API (example)
You can add a client for a specific url

    builder.Services.AddRepository<User, string>(builder =>
    {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

You may add a Polly policy to your api client for example:

    var retryPolicy = HttpPolicyExtensions
      .HandleTransientHttpError()
      .Or<TimeoutRejectedException>()
      .RetryAsync(3);

    builder.Services.AddRepository<User, string>(builder =>
    {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058")
                .ClientBuilder
            .AddPolicyHandler(retryPolicy);
    });
    
and use it in DI with
    
    IRepository<User, string> repository

### Query and Command
In DI you install the services

    services.AddCommand<User, string>(builder => {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddQuery<User, string>(builder => {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });

And you may inject the objects
## Please, use ICommand, IQuery and not ICommandPattern, IQueryPattern

    ICommand<User, string> command
    IQuery<User, string> command

### With a non default key
In DI you install the services with a bool key for example.

    services.AddRepository<User, bool>(builder => {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddCommand<User, bool>(builder => {
        builder
            .WithApiClient()
            .WithHttpClient("localhost:7058");
    });
    services.AddQuery<User, bool>(builder => {
        builder
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

with package RepositoryFramework.Api.Client.Authentication.BlazorServer 
or if you need to use in Wasm blazor use with Rystem.RepositoryFramework.Api.Client.Authentication.Wasm

