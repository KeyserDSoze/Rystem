# Class Documentation

## Class: HostTester

The `HostTester` class contains a single static method useful for setting up integration tests for a web-host-based application. The class utilizes the `Microsoft.AspNetCore.TestHost` namespace for simulating the web host.

### Method: CreateHostServerAsync

This method aims to configure and initiate a web host server for testing purposes. It allows you to set up services, middlewares, and health checks. It also handles synchronization and potential asynchronous exceptions. Exception handling is ensured with the help of the `Rystem.Concurrency` library.

**Parameters**
- `IConfiguration configuration`: This provides application's configuration details such as connection strings and app settings.
- `Type? applicationPartToAdd`: An optional parameter representing the part of the application - often a class type - to be added to the application services. If not provided, the method adds the currently executing assembly.
- `Func<IServiceCollection, IConfiguration, ValueTask> configureServicesAsync`: Function to configure application services. It takes services collection and configuration as input, allowing you to add your own services to the services collection asynchronously.
- `Func<IApplicationBuilder, IServiceProvider, ValueTask> configureMiddlewaresAsync`: Function to configure middlewares. It takes the application builder and service provider as input, permitting middleware modules to be lined up in your application's pipeline asynchronously.
- `bool addHealthCheck`: An optional boolean parameter to decide whether to include health checks to services and middleware or not. Default value is 'false'.

**Return Value**
- Returns a `Task<Exception?>` where the task result will be an `Exception` object if an exception occurred during the server startup or health-check process, or `null` if the process completes smoothly.

**Usage Example**
```csharp
Exception? exception = await HostTester.CreateHostServerAsync(
new ConfigurationBuilder().Build(),
typeof(Startup),
(async (services, configuration) => await Task.CompletedTask),
(async (app, provider) => await Task.CompletedTask),
true
);

if (exception != null)
{
    Console.WriteLine($"Action failed with error: {exception}");
}
```
In this example, an empty configuration and the 'Startup' class are passed to the method while setting up the server. Also, the addHealthCheck option is activated. If an exception occurs during the setup, it will be logged.