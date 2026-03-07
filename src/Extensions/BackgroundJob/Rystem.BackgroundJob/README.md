### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.BackgroundJob

`Rystem.BackgroundJob` adds CRON-based recurring jobs on top of the Rystem DI stack.

The package is designed for applications that want small recurring jobs without introducing a heavier external scheduler. Jobs are registered through dependency injection, started during DI warm-up, and executed according to standard 5-field or 6-field CRON expressions through `Cronos`.

It is most useful for:

- recurring maintenance jobs
- polling and synchronization tasks
- cleanup or archival work
- lightweight scheduled automation inside ASP.NET Core or generic host applications

The best source-backed examples for this package are the package implementation itself plus the sample web app in `src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.BackgroundJob
```

The current `10.x` package targets `net10.0` and builds on top of:

- `Cronos`
- `Rystem.Concurrency`
- the warm-up flow from [`Rystem.DependencyInjection`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection/README.md)

## Package Architecture

The package is intentionally compact and revolves around four pieces.

| Piece | Purpose |
|---|---|
| `IBackgroundJob` | Contract implemented by each scheduled job |
| `BackgroundJobOptions` | CRON, startup behavior, and logical key |
| `IBackgroundJobManager` | Scheduler abstraction responsible for running jobs |
| `AddBackgroundJob<TJob>` | DI entry point that registers the job and wires startup through warm-up |

At a high level, the flow is:

- register a job with `AddBackgroundJob<TJob>(...)`
- the package adds the job as a transient service
- warm-up starts the schedule after the provider is built
- the manager computes the next CRON occurrence and arms a timer
- each tick executes the job and schedules the next one

## Table of Contents

- [Package Architecture](#package-architecture)
- [Implement a Job](#implement-a-job)
  - [IBackgroundJob contract](#ibackgroundjob-contract)
  - [Dependency injection behavior](#dependency-injection-behavior)
- [Register a Job](#register-a-job)
- [BackgroundJobOptions](#backgroundjoboptions)
- [Scheduling and Warm-up](#scheduling-and-warm-up)
  - [Warm-up starts the scheduler](#warm-up-starts-the-scheduler)
  - [RunImmediately](#runimmediately)
  - [CRON format](#cron-format)
- [Multiple Registrations of the Same Job Type](#multiple-registrations-of-the-same-job-type)
- [Custom Job Manager](#custom-job-manager)
- [Repository Examples](#repository-examples)

---

## Implement a Job

Jobs implement `IBackgroundJob`.

The public job types live in the `System.Timers` namespace, so the usual starting point is:

```csharp
using System.Timers;
```

Then implement the job itself:

```csharp
using System.Timers;

public sealed class ReportJob : IBackgroundJob
{
    private readonly ILogger<ReportJob> _logger;

    public ReportJob(ILogger<ReportJob> logger)
    {
        _logger = logger;
    }

    public async Task ActionToDoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Report job running at {Time}", DateTime.UtcNow);
        await Task.Delay(10, cancellationToken);
    }

    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Report job failed");
        return Task.CompletedTask;
    }
}
```

### IBackgroundJob contract

The interface is intentionally small:

```csharp
public interface IBackgroundJob
{
    Task ActionToDoAsync(CancellationToken cancellationToken = default);
    Task OnException(Exception exception);
}
```

- `ActionToDoAsync(...)` contains the recurring work
- `OnException(...)` is called when `ActionToDoAsync(...)` throws

If you want the scheduler loop to keep going, `OnException(...)` should usually log or handle the exception rather than rethrow it.

### Dependency injection behavior

`AddBackgroundJob<TJob>(...)` registers the job as `Transient`.

The sample app in `src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp/BackgroundJob.cs` shows the intended lifetime behavior clearly:

- singleton dependencies stay stable across executions
- scoped dependencies are shared inside one execution but change across later executions
- transient dependencies are recreated even inside the same execution when resolved twice

That makes constructor injection work the same way it does in the rest of the application.

---

## Register a Job

Register the job during service setup:

```csharp
builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Cron = "*/5 * * * *";
    options.RunImmediately = true;
});
```

What `AddBackgroundJob<TJob>(...)` does internally:

- registers the default `IBackgroundJobManager` with `TryAddSingleton(...)`
- enables the Rystem lock service with `AddLock()`
- registers `TJob` as transient
- creates effective options
- schedules startup through `AddWarmUp(...)`

So this package depends on the same warm-up lifecycle documented in [`src/Core/Rystem.DependencyInjection/README.md`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection/README.md).

---

## BackgroundJobOptions

`BackgroundJobOptions` contains the scheduling settings:

```csharp
public sealed class BackgroundJobOptions
{
    public string? Key { get; set; }
    public bool RunImmediately { get; set; }
    public string Cron { get; set; } = "* * * * *";
}
```

In practice, `AddBackgroundJob<TJob>(...)` starts from these effective defaults before your configuration delegate runs:

| Property | Effective default in `AddBackgroundJob(...)` | Purpose |
|---|---|---|
| `Key` | random GUID | Distinguishes one registration from another |
| `RunImmediately` | `false` | Executes once during warm-up before the timer begins |
| `Cron` | `"0 1 * * *"` | Schedules the job daily at 01:00 UTC |

That distinction matters because the raw `BackgroundJobOptions` class initializes `Cron` to `"* * * * *"`, while `AddBackgroundJob(...)` overwrites the starting default to `"0 1 * * *"` unless you set a different value.

---

## Scheduling and Warm-up

### Warm-up starts the scheduler

Jobs do not start automatically just because they were registered. They start when warm-up runs.

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

Without `WarmUpAsync()`, the recurring timers are never started.

### RunImmediately

If `RunImmediately` is `true`, the manager executes `ActionToDoAsync(...)` once during warm-up before creating the first scheduled timer.

```csharp
builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Cron = "0 */2 * * *";
    options.RunImmediately = true;
});
```

This is useful when the first execution should happen during startup instead of waiting for the first future CRON occurrence.

### CRON format

The package uses `Cronos` and supports both standard and second-based expressions. The manager detects the format by counting space-separated fields.

```text
*/1 * * * *
```

- 5 fields -> standard CRON format
- 6 fields -> CRON format with seconds

Examples:

```text
*/1 * * * *
0 */2 * * *
0 9 * * 1-5
*/30 * * * * *
0 0 9 * * 1-5
```

For quick validation, [crontab.guru](https://crontab.guru/) is still a convenient companion for the 5-field form.

---

## Multiple Registrations of the Same Job Type

You can register the same job type more than once.

```csharp
builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Key = "daily";
    options.Cron = "0 8 * * *";
});

builder.Services.AddBackgroundJob<ReportJob>(options =>
{
    options.Key = "weekly";
    options.Cron = "0 8 * * 1";
});
```

The built-in manager uses a key shaped like:

```text
BackgroundWork_{options.Key}_{jobTypeFullName}
```

So multiple registrations stay independent as long as they do not collide on both job type and key.

If you do not set `Key`, the package generates a GUID automatically, which already keeps separate registrations distinct.

---

## Custom Job Manager

If you need different scheduling or execution semantics, replace the default manager.

```csharp
builder.Services.AddBackgroundJobManager<MyCustomJobManager>();
```

Your implementation must satisfy:

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

The optional `factory` parameter is the hook the default manager uses to recreate jobs for later scheduled executions.

Important: call `AddBackgroundJobManager<TJobManager>()` before any `AddBackgroundJob(...)` registration, because the built-in manager is registered with `TryAddSingleton(...)`.

The built-in `BackgroundJobManager` also uses the Rystem lock service to prevent concurrent timer updates for the same logical job key.

---

## Repository Examples

The most useful references for this package are:

- Package registration entry point: [src/Extensions/BackgroundJob/Rystem.BackgroundJob/ServiceCollectionExtensions/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Rystem.BackgroundJob/ServiceCollectionExtensions/ServiceCollectionExtensions.cs)
- Built-in scheduler manager: [src/Extensions/BackgroundJob/Rystem.BackgroundJob/BackgroundJob/BackgroundJobManager.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Rystem.BackgroundJob/BackgroundJob/BackgroundJobManager.cs)
- Background job contract: [src/Extensions/BackgroundJob/Rystem.BackgroundJob/Interfaces/IBackgroundJob.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Rystem.BackgroundJob/Interfaces/IBackgroundJob.cs)
- Sample application startup: [src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp/Program.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp/Program.cs)
- Sample job showing DI lifetime behavior: [src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp/BackgroundJob.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Test/Rystem.BackgroundJob.WebApp/BackgroundJob.cs)

This README stays focused because `Rystem.BackgroundJob` is a narrow package: one job contract, one manager abstraction, one DI registration path, and a CRON-driven execution loop.
