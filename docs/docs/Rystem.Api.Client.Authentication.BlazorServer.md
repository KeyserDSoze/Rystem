# Documentation for Rystem.Api.Client.Authentication.BlazorServer Package

This documentation provides details about classes and their public methods of the library `Rystem.Api.Client.Authentication.BlazorServer`.

## Class: AuthorizationSettings

This class is used to hold the authorization settings which are required for API client authentication.

### Properties:
- **Scopes**: A string array containing the scopes (permissions) that are requested from the Microsoft Identity Platform during authorization.

## Class: RystemApiServiceCollectionExtensions 

This static class inside `Microsoft.Extensions.DependencyInjection` namespace provides extensions for `IServiceCollection`. These methods are used for configuring authorization for API endpoints.

### Method: AddAuthenticationForAllEndpoints
This method configures the services necessary to provide authentication to all API endpoints.

**Parameters**:
- **Services (IServiceCollection)**: Service collection to which configurations will be added.
- **Settings (Action<AuthorizationSettings>)**: An action delegate that configures the authorization settings.

**Return Value**: The method returns a IServiceCollection after adding all the necessary configurations for authentication.

**Usage Example**:
```
services.AddAuthenticationForAllEndpoints(options =>
{
    options.Scopes = new string[] { "openid", "profile" };
});
```

### Method: AddAuthenticationForEndpoint<T>
This is a generic method that configures the services necessary to provide authentication to a specific API endpoint of type T.

**Parameters**:
- **Services (IServiceCollection)**: Service collection to which the configuration will be added.
- **Settings (Action<AuthorizationSettings>)**: An action delegate that configures the authorization settings.

**Return Value**: The method returns a IServiceCollection after adding the necessary configuration for authentication for the specific endpoint.

**Usage Example**:
```
services.AddAuthenticationForEndpoint<WeatherForecastEndpoint>(settings =>
{
    settings.Scopes = new string[] { "weather.read" };
});
```
         
In these examples, we are providing authentication for either all available endpoints or specifically for the `WeatherForecastEndpoint`. The settings action allows us to specify the necessary scopes needed for authorization on these endpoints.

Please make sure that you update the scopes according to your application's requirements in the real scenario.