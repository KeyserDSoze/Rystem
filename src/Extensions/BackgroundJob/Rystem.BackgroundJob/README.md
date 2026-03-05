### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.BackgroundJob

A lightweight library for scheduling recurring background work in ASP.NET Core and generic host applications using **CRON expressions**. Jobs are registered through dependency injection and started automatically during warm-up.

Use [crontab.guru](https://crontab.guru/) to build and validate CRON expressions.

## 📦 Installation

```bash
dotnet add package Rystem.BackgroundJob
```

## Table of Contents

- [Rystem.BackgroundJob](#rystembackgroundjob)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [Implement a Job](#implement-a-job)
  - [IBackgroundJob interface](#ibackgroundjob-interface)
- [Register a Job](#register-a-job)
- [BackgroundJobOptions](#backgroundjoboptions)
- [CRON format](#cron-format)
- [Warm Up](#warm-up)
- [Multiple Jobs of the Same Type](#multiple-jobs-of-the-same-type)
- [Custom Job Manager](#custom-job-manager)

---

## Implement a Job

Implement the `IBackgroundJob` interface. Your class is resolved from DI on every tick, so constructor injection works normally.

```csharp
public class MyBackgroundJob : IBackgroundJob
{
    private readonly ILogger<MyBackgroundJob> _logger;

    public MyBackgroundJob(ILogger<MyBackgroundJob> logger)
    {
        _logger = logger;
    }

    // Called automatically on every CRON tick
    public async Task ActionToDoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Job running at {Time}", DateTime.UtcNow);
        await DoWorkAsync(cancellationToken);
    }

    // Called when ActionToDoAsync throws; prevents the scheduler from crashing
    public async Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Job failed");
        await Task.CompletedTask;
    }
}
```

### IBackgroundJob interface

| Member | Description |
|---|---|
| `ActionToDoAsync(CancellationToken)` | The work to execute on each CRON tick |
| `OnException(Exception)` | Error handler; called if `ActionToDoAsync` throws |

---

## Register a Job

Call `AddBackgroundJob<TJob>` during service registration and configure it with `BackgroundJobOptions`:

```csharp
builder.Services.AddBackgroundJob<MyBackgroundJob>(options =>
{
    options.Cron = "*/5 * * * *"; // every 5 minutes
    options.RunImmediately = true; // also run once at startup
});
```

---

## BackgroundJobOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `Cron` | `string` | `"0 1 * * *"` | CRON schedule expression |
| `RunImmediately` | `bool` | `false` | Run one execution immediately at warm-up, before the first scheduled tick |
| `Key` | `string?` | random GUID | Unique key; required only when registering multiple jobs of the same type |

---

## CRON format

Both standard 5-field and extended 6-field (with seconds) CRON expressions are supported. The number of space-separated fields determines the format automatically.

```
# Standard (5 fields): minute hour day month weekday
"*/1 * * * *"       # every minute
"0 */2 * * *"       # every 2 hours
"0 9 * * 1-5"       # 09:00 on weekdays

# Extended with seconds (6 fields)
"*/30 * * * * *"    # every 30 seconds
"0 0 9 * * 1-5"     # 09:00:00 on weekdays
```

---

## Warm Up

Jobs start their schedules during `WarmUpAsync`. Call it after `Build()`:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

If `RunImmediately = true`, `ActionToDoAsync` is called once synchronously during warm-up before the recurring timer starts.

---

## Multiple Jobs of the Same Type

Register the same job class multiple times with different options using the `Key` property:

```csharp
builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Key = "daily";
    options.Cron = "0 8 * * *";   // 08:00 daily
});

builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Key = "weekly";
    options.Cron = "0 8 * * 1";   // 08:00 every Monday
});
```

Each registration runs independently with its own timer and lock key, based on `Key` + job type name.

---

## Custom Job Manager

The built-in `BackgroundJobManager` uses an async lock (from `Rystem.Concurrency`) to prevent overlapping timers for the same key. To replace it with your own implementation:

```csharp
builder.Services.AddBackgroundJobManager<MyCustomJobManager>();
```

`MyCustomJobManager` must implement `IBackgroundJobManager`:

```csharp
public interface IBackgroundJobManager
{
    Task RunAsync(
        IBackgroundJob job,
        BackgroundJobOptions options,
        Func<IBackgroundJob>? factory = null,
        CancellationToken cancellationToken = default);
}
```

`AddBackgroundJobManager` must be called **before** any `AddBackgroundJob` call, as the built-in manager is registered with `TryAddSingleton`.