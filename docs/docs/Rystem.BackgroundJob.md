# Rystem.BackgroundJob Library Documentation

This documentation covers the `Rystem.BackgroundJob` library, which provides tools for managing background jobs in your project efficiently. The library comprises of two main classes: `BackgroundJobOptions` and `ServiceCollectionExtensions`.

## Class: BackgroundJobOptions

### Method Name: BackgroundJobOptions

**Description**: This method is a constructor for the `BackgroundJobOptions` class, which provides various setting options for a background job.

**Parameters**: There are no parameters in the constructor method. 

**Properties**: 

1. `Key` (Type: `string`): It represents a unique identifier for the background job. The default setting is `null`.

2. `RunImmediately` (Type: `bool`): It allows the background job to run immediately after being queued. The default setting is `false`.

3. `Cron` (Type: `string`): It provides the schedule for running the background job in cron format. The default setting is `"* * * * *"` which represents every minute.

There are no return values for this method as it is a class constructor.

## Class: ServiceCollectionExtensions

### Method Name: AddBackgroundJobManager<TJobManager>

**Description**: This method adds a job manager in the structured implementation through dependency injection. The job manager provides methods to effectively manage and control background jobs.

**Parameters**:
- `services` (Type: `IServiceCollection`): A reference to an instance of a service collection to which the IBackgroundJobManager service is registered.

**Return Value**: Returns an instance of `IServiceCollection` to continue the IServiceCollection method chains.

**Usage Example**:
```CSharp
services.AddBackgroundJobManager<MyCustomJobManager>();
```
Here, `MyCustomJobManager` should be a class implementing `IBackgroundJobManager` interface.

### Method Name: AddBackgroundJob<TJob>

**Description**: This method adds a background job and its options in the structured implementation through dependency injection. It allows adding jobs to the queue with necessary options including execution schedule and immediate execution.

**Parameters**:
- `services` (Type: `IServiceCollection`): A reference to an instance of `IServiceCollection` to which the `IBackgroundJob` service is registered.
- `options` (Type: `Action<BackgroundJobOptions>`): A delegate to modify job options like key, cron scheduling, and immediate execution.

**Return Value**: Returns an instance of `IServiceCollection` to continue the IServiceCollection method chains.

**Usage Example**:
```CSharp
services.AddBackgroundJob<SampleBackgroundJob>(options =>
    {
        options.RunImmediately = true;
        options.Cron = "0 1 * * *";
    });
```
In the above example, `SampleBackgroundJob` is a background job class implementing `IBackgroundJob` and is setup to run immediately according to the Cron setting: once every hour.

### Using Our Library in Action

Consider a scenario where you want to utilise various services (e.g., Singleton, Scoped, Transient) in your application and monitor them using background jobs for any potential issues. The `Rystem.Test.WebApp.BackgroundJob` test class demonstrates this scenario.

In this case, `Rystem.Test.WebApp.BackgroundJob` verifies that Singleton, Scoped, and Transient services are correctly generated each time the background job is performed. It checks that Singleton service remains constant across job performances, Scoped services are renewed per job performance (but remain constant within a single performance), and the Transient services are always renewed. 

The implementation of this background job goes in-line with best practices for utilizing and monitoring your services using the powerful tools that `Rystem.BackgroundJob` provides.