## Class Documentation

This library is built on a few different technologies which include Rystem.Api, Microsoft.AspNetCore.OpenApi, and Swashbuckle.AspNetCore as a Nuget package. Primarily, it allows users to automatically add multiple functionalities, such as Swagger and API endpoint routing, to their service collection.

There are two static classes available for extension purpose:

# 1. `ServiceCollectionExtensions`

This class provides static methods for `IServiceCollection` to add server integrations.

## Method `AddServerIntegrationForRystemApi`

This method integrates the Rystem API into the current service collection.

### Parameters:

- `IServiceCollection services`: The current instance of services being bound.

### Return Value:

- `IServiceCollection`: Returns the service collection with Rystem API integration.

### Usage Example:

```csharp
services.AddServerIntegrationForRystemApi();
```

# 2. `EndpointRouteBuilderRystemExtensions`

This class contains methods extending the functionality of the `IEndpointRouteBuilder`.

## Method `UseEndpointApi`

This method creates endpoints for the API.

### Parameters:

- `IEndpointRouteBuilder builder`: The route builder with defined parameters.

### Return Value:

- `IEndpointRouteBuilder`: Returns the route builder with added endpoints.

### Usage Example:

```csharp
app.UseEndpointApi();
```

## Method `UseEndpointApiModels`

This function creates API models for the given programming languages.

### Parameters:

- `IEndpointRouteBuilder builder`: The instance of the endpoint route builder.

### Return Value:

- None. This method does not return a value.

### Usage Example:

```csharp
app.UseEndpointApiModels();
```
This method iterates through all the endpoints, grabs the Types related, and generates a map route that contains the models for each Programming Language outlined in the list.

More about possible private methods used and other specific workflow routes can be found in the source code for more advanced usage.

## Test Classes:

|||

The tests for these methods can be found in the integration tests of the Rystem API. This includes checking if the endpoints are created successfully for each programming language specified in the Languages list and if the code compiles successfully in their respective languages.

The tests would involve creating service collections, adding the server integration, and checking for successful integration. Possible edge cases to test could include how to handle invalid programming languages, overloading the API server with multiple routes, and providing invalid routes.