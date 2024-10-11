# Classes Documentation

## Class: AuthenticatorSettings

This class works as a configuration holder for authentication settings needed in the HTTP requests.

### Properties:
- **Scopes**: An array of `string`. It represents the scopes for an API request. Its purpose is to allow the API to limit the types of data returned according to the scopes provided.
- **ExceptionHandler**: A `Func` that takes an `Exception` and an `IServiceProvider`, and returns a `Task`. This is used to handle exceptions that can occur during the authentication process. It allows for centralized handling and possibly recovery from certain exceptional circumstances.

## Class: AuthenticatorSettings<T>

This class extends `AuthenticatorSettings`. It's currently not adding any new property or method, but it's designed to support future settings that may be specific to the type `T` it is associated with.

## Class: AuthenticatorSettings<T, TKey>

This class extends `AuthenticatorSettings<T>`. It does not add anything new currently, but it's designed to support future settings that may be specific to the type `T` and key type `TKey` it is associated with. The `TKey` parameter must be not null as indicated by the `where TKey : notnull` clause.

## Class: RepositoryBuilderExtensions

This class contains extension methods for `IServiceCollection`, `IRepositoryBuilder`, `IQueryBuilder` and `ICommandBuilder`. 

It includes methods for adding authorization interceptors, adding clients to repository builders, and adding API client interceptors. All the methods take a `ServiceLifetime` parameter which controls the lifetime of these services in dependency injection. The methods also make use of several generic type parameters, which allow the methods to work with a variety of repository types, keys, and corresponding token manager or interceptor classes.

Note: Most methods return `IServiceCollection`, `IRepositoryBuilder`, `IQueryBuilder` or `ICommandBuilder` to allow for chaining multiple operations together. These methods typically perform configuration tasks such as adding services to the dependency injection container.

## Class: HttpClientRepositoryBuilder<T, TKey>

Contains configuration settings for an HttpClient repository builder. 

### Properties:
- **ApiBuilder**: IApiRepositoryBuilder<T, TKey>, which contains the configuration for API repository.
- **ClientBuilder**: IHttpClientBuilder, which is the standard builder for HttpClient in .NET Core. 

This builder is specifically for the repositories that use an HttpClient for the data source and can be tailored to different types of repositories and keys.

## Class: ApiClientSettings<T, TKey>

Hold the settings required for an API client with a corresponding repository model of type `T` and a key type `TKey`. The paths will be constructed using these settings and will point to the URLs that the client will make requests to.

### Note:
"Key" refers to the unique identifier for a specific item in the repository. For example, if `T` is `Product` and TKey is `int`, then each product has an integer key that can be used to retrieve it from the repository.

# Additional Information
This documentation does not cover the full functionality and usage examples for all the methods in these classes due to lack of method-level information. Please provide detailed information on public methods to enhance the documentations further.