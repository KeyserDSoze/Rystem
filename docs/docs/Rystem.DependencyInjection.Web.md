# Documentation

## Class: `FallbackBuilderForServiceCollection`
Namespace: `Microsoft.Extensions.DependencyInjection`

This class is responsible for managing fallbacks in the dependency injection process. It provides the ability to add back-up mechanisms when a service cannot be resolved during dependency injection.

**Properties**

- **ServiceColletionBuilder**: Type is `Action<IServiceCollection>`. This property sets an action that adds fallback services to the service collection. If not specified, the default action will not add any services.

- **ServiceProvider**: Type is `IServiceProvider`. This property provides an existing instance of the service provider. It is used internally for initializing the class.

- **Name**: Type is `string?`. This property sets a name for the fallback builder.

## Class: `ServiceCollectionExtensions`
Namespace: `Microsoft.Extensions.DependencyInjection`

This static class extends the `IServiceCollection` with additional helper methods for managing the services dependency injection process.

**Methods**

1. **AddActionAsFallbackWithServiceCollectionRebuilding**

    **Method Purpose**: 
    This method allows adding an action as a fallback mechanism during the dependency injection resolution process.
    
    **Parameters**:
     - `services`: Type is `IServiceCollection`. This represents the Microsoft.Extensions.DependencyInjection.IServiceCollection extended by this helper method.
     - `fallbackBuilder`: Type is `Func<FallbackBuilderForServiceCollection, ValueTask>`. This is a function that takes a `FallbackBuilderForServiceCollection` instance as input and returns a `ValueTask`. The function is responsible for defining the fallback behavior.

    **Return Value**:
     - Returns `IServiceCollection` to allow method chaining.
     
     **Usage Example**:
    ```
    services.AddActionAsFallbackWithServiceCollectionRebuilding<T>(async fallbackBuilder =>
    {
        // Configure fallback action
    });
    ```
2. **AddActionAsFallbackWithServiceCollectionRebuilding**

    **Method Purpose**: 
    This method is a variant of the method above and allows specifying a service type at run-time.
    
    **Parameters**:
     - `services`: Type is `IServiceCollection`. This represents the Microsoft.Extensions.DependencyInjection.IServiceCollection extended by this helper method.
     - `serviceType`: Type is `Type`. This represents the type of the service class to be extended.
     - `fallbackBuilder`: Type is `Func<FallbackBuilderForServiceCollection, ValueTask>`. This is a function that takes a `FallbackBuilderForServiceCollection` instance as input and returns a `ValueTask`. The function is responsible for defining the fallback behavior.

    **Return Value**:
     - Returns `IServiceCollection` to allow method chaining.

    **Usage Example**:
    ```
    services.AddActionAsFallbackWithServiceCollectionRebuilding(serviceType, async fallbackBuilder =>
    {
        // Configure fallback action
    });
    ```
