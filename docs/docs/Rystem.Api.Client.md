# Rystem.Api.Client Library Documentation

## API Client Classes

### Class: ApiClientChainRequest<T>

This class is responsible for managing a group of `ApiClientCreateRequestMethod` objects.

#### Properties:

- **Methods**: A key-value dictionary where the key is a string and values are objects of type `ApiClientCreateRequestMethod`. This property holds all API methods for a particular client.

### Class: ApiClientCreateRequestMethod

This class helps in the creation of methods for an API client.

#### Properties:

- **IsPost**: A boolean that indicates whether the API method is a POST or not.
- **FixedPath**: A string representing the API path to complement the base URL.
    - **Parameters**: None
- **ResultStreamType**: A `StreamType` object that defines what type of stream the API result should be converted into.
- **ReturnType**: A `Type` object specifying the return type of the method.
- **Parameters**: A list of `ApiClientCreateRequestParameterMethod` objects specifying the parameters to pass to the API method.

### Class: ApiClientCreateRequestParameterMethod

This class is used for creating parameters for API methods.

#### Properties:

- **Name**: A string representing the name of the parameter.
- **Executor**: An action delegate that defines how to execute this method and format its parameters.

### Class: ApiClientRequestBearer

This class is used to construct and manage HTTP requests.

#### Properties:

- **Path**: A `StringBuilder` that represents the API path to complement the base URL.
- **Query**: A `StringBuilder` used to append query parameters to the API request URL.
- **Cookie**: A `StringBuilder` used to build cookie headers for the API request.
- **Content**: An `HttpContent` object which holds the body of the request.
- **Headers**: A `Dictionary` where the key and value are both a string. This property holds all headers for the API request.

### Class: ApiHttpClient<T>

This class is a dispatch proxy used to create a proxy of the specified type T. It's responsible for calling HTTP methods synchronously or asynchronously. 

#### Properties:
Not all properties are listed here, just the primary members of the class.

- **_httpClient**:  An instance of `HttpClient` used to send HTTP requests and receive HTTP responses from a resource identified by a URI.

1. `Invoke` method

This method is called when the object of this class is invoked. Depending on the calling method's signature, it calls other methods to execute HTTP requests.

- Method Name: Invoke
- Parameters:
   - MethodInfo? method: Method info for the method to be invoked.
   - object?[]? args: Array of objects that specifies the method arguments.
- Return Value: Returns an object of any type which is the response of the HTTP request or `null`.
- Usage Example: Not directly used as it's a protected method of the class which is automatically called when the API client calls a method.

2. `InvokeHttpRequestAsync<TResponse>` method

This method is made to send a HTTP request asynchronously and handles the HTTP response depending on the response type and whether the response should be read or not.

- Method Name: InvokeHttpRequestAsync<TResponse>
- Parameters:
   - ApiClientCreateRequestMethod currentMethod: Object of the method that has to be invoked.
   - object?[]? args: Array of objects that specifies the method arguments.
   - bool readResponse: A boolean that determines whether to read HTTP response or not.
   - Type returnType: Specifies the return type of the method.
   - CancellationToken cancellationToken: Propagates notification that operations should be cancelled.
- Return Value: Returns a `ValueTask<TResponse>` which eventually gives the HTTP response (if read) or `null`.
- Usage Example: Not directly used as it's a private method of the class.

### Class: HttpClientBuilder

This class is used to configure HTTP clients for different API endpoints.

#### Methods: 

1. `ConfigurationHttpClientForEndpointApi<T>` method

This method is used for the configuration of the HttpClient specifically for an endpoint based on the type of the endpoint.

- Method Name: ConfigurationHttpClientForEndpointApi<T>
- Parameters:
   - Action<HttpClient>? settings: An action that represents the HttpClient settings that are specific to this endpoint.
- Return Value: Returns an object of type `HttpClientBuilder` which can further be used for the configuration information.
- Usage Example:

```csharp
var builder = new HttpClientBuilder();
builder.ConfigurationHttpClientForEndpointApi<UserApi>(client => { /* client settings */ });
```

2. `ConfigurationHttpClientForApi` method

This method provides a default setting for HTTP clients with no specific endpoint settings defined.

- Method Name: ConfigurationHttpClientForApi
- Parameters:
   - Action<HttpClient>? settings: An action that represents the HttpClient settings applicable for all endpoints that don't have their own specific settings.
- Return Value: Returns an object of type `HttpClientBuilder` which can further be used for the configuration information.
- Usage Example:

```csharp
var builder = new HttpClientBuilder();
builder.ConfigurationHttpClientForApi(client => { /* default client settings */ });
```

### Class: RystemApiServiceCollectionExtensions

This extension class provides ways to configure `Rystem` for a specific endpoint and all endpoints. 

#### Methods:

1. `AddEnhancerForAllEndpoints<TEnhancer>` method

This method is used to add an `IRequestEnhancer` for all possible endpoints. 

- Method Name: AddEnhancerForAllEndpoints<TEnhancer>
- Parameters: None
- Return Value: This method returns an instance of `IServiceCollection` which can be used for further configuration.
- Usage Example:

```csharp
services.AddEnhancerForAllEndpoints<MyRequestEnhancer>();
```

2. `AddEnhancerForEndpoint<TEnhancer, T>` method

This method is used add an `IRequestEnhancer` for a specific endpoint.

- Method Name: AddEnhancerForEndpoint<TEnhancer, T>
- Parameters: None
- Return Value: This method returns an instance of `IServiceCollection` which can be used for further configuration.
- Usage Example:

```csharp
services.AddEnhancerForEndpoint<MyRequestEnhancer, UserApi>();
```

## Test Classes:

No test classes are provided. The provided code focuses on setting up an API client with various extensions and wrappers for easy usage and configuration. The anatomy and behavior of the components mirror common behavior in many industry-standard API clients, allowing developers familiar with such clients to quickly understand the provided classes.