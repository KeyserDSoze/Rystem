# Background Jobs

**Purpose**: This resource explains how to configure and use Rystem background jobs for scheduled and recurring tasks.

---

## Overview

Rystem provides a powerful background job system for executing tasks asynchronously, scheduling recurring jobs, and managing long-running operations.

---

## Installation

```bash
dotnet add package Rystem.BackgroundJob
```

## Features

- **Simple API** - Easy to configure and use
- **Recurring Jobs** - Schedule jobs with cron expressions
- **Delayed Execution** - Execute jobs after a specific delay
- **Job Cancellation** - Cancel running or scheduled jobs
- **Error Handling** - Built-in retry mechanisms
- **Distributed Support** - Works across multiple instances

## Configuration

```csharp
// Program.cs
builder.Services.AddBackgroundJob(options =>
{
    options.AddJob<MyBackgroundJob>(job =>
    {
        job.Every(TimeSpan.FromMinutes(5));
        job.WithMaxParallelism(3);
    });
});
```

## Creating a Background Job

```csharp
public class MyBackgroundJob : IBackgroundJob
{
    private readonly ILogger<MyBackgroundJob> _logger;
    
    public MyBackgroundJob(ILogger<MyBackgroundJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Background job executing...");
        
        // Your background logic here
        await Task.Delay(1000, cancellationToken);
        
        _logger.LogInformation("Background job completed");
    }
}
```

## Scheduling Patterns

### Run Once After Delay
```csharp
builder.Services.AddBackgroundJob<SendEmailJob>(job =>
{
    job.RunOnce().After(TimeSpan.FromMinutes(5));
});
```

### Recurring with Fixed Interval
```csharp
builder.Services.AddBackgroundJob<CleanupJob>(job =>
{
    job.Every(TimeSpan.FromHours(1));
});
```

### Cron Expression
```csharp
builder.Services.AddBackgroundJob<ReportGenerationJob>(job =>
{
    job.WithCron("0 0 * * *"); // Every day at midnight
});
```

## Advanced Usage

### With Dependencies
```csharp
public class DataSyncJob : IBackgroundJob
{
    private readonly IRepository<Product, int> _repository;
    private readonly IExternalApiClient _apiClient;

    public DataSyncJob(
        IRepository<Product, int> repository,
        IExternalApiClient apiClient)
    {
        _repository = repository;
        _apiClient = apiClient;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var products = await _apiClient.GetProductsAsync();
        
        foreach (var product in products)
        {
            await _repository.InsertAsync(product, cancellationToken);
        }
    }
}
```

### Error Handling
```csharp
builder.Services.AddBackgroundJob<ResilientJob>(job =>
{
    job.Every(TimeSpan.FromMinutes(5))
       .WithMaxRetries(3)
       .WithRetryDelay(TimeSpan.FromSeconds(30));
});
```

## Use Cases

- **Data Synchronization** - Keep data in sync between systems
- **Report Generation** - Generate reports on a schedule
- **Cleanup Tasks** - Remove old data periodically
- **Email Processing** - Send emails asynchronously
- **Cache Warming** - Pre-populate caches
- **Health Checks** - Monitor external services

## See Also

- [Rystem.BackgroundJob Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/BackgroundJob)
- [Queue Management](./queue-management.md)
