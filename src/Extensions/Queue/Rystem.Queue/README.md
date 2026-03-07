### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Queue

`Rystem.Queue` is a batching queue library built on top of `Rystem.BackgroundJob`.

Items are stored in an `IQueue<T>` implementation and periodically flushed to an `IQueueManager<T>` implementation. The package includes in-memory FIFO and LIFO backends, plus an integration hook for custom queue providers.

It is most useful for:

- batching writes to external systems
- lightweight buffered ingestion
- collecting events before periodic processing
- swapping queue backends while keeping the same enqueue and flush contracts

The best real examples for this package come from the source itself and the unit test in `src/Extensions/Queue/Test/Rystem.Queue.UnitTest/QueueTest.cs`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.Queue
```

The current `10.x` package targets `net10.0` and builds on top of [`Rystem.BackgroundJob`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/BackgroundJob/Rystem.BackgroundJob/README.md).

## Package Architecture

The package is centered around five pieces.

| Piece | Purpose |
|---|---|
| `IQueue<T>` | Storage abstraction for queued items |
| `IQueueManager<T>` | Batch processor invoked when a flush happens |
| `QueueProperty<T>` | Per-queue settings for thresholds and schedules |
| `QueueJobManager<T>` | Internal background job that decides when to flush |
| `AddQueueIntegration(...)` | DI entry point that wires queue, manager, settings, and background job |

The two built-in queue backends are:

- `MemoryQueue<T>` for FIFO behavior
- `MemoryStackQueue<T>` for LIFO behavior

At a high level, the flow is:

- register a queue and a queue manager
- enqueue items through `IQueue<T>`
- run `WarmUpAsync()` so the background job starts
- the queue job checks size and schedule conditions
- when a flush occurs, it dequeues items and passes them to `IQueueManager<T>.ManageAsync(...)`

## Table of Contents

- [Package Architecture](#package-architecture)
- [Implement a Queue Manager](#implement-a-queue-manager)
  - [IQueueManager contract](#iqueuemanager-contract)
  - [Dependency injection behavior](#dependency-injection-behavior)
- [Register a Queue](#register-a-queue)
  - [In-memory FIFO queue](#in-memory-fifo-queue)
  - [In-memory LIFO stack queue](#in-memory-lifo-stack-queue)
  - [Generic queue integration](#generic-queue-integration)
- [QueueProperty](#queueproperty)
- [Flush Behavior and Warm-up](#flush-behavior-and-warm-up)
  - [Warm-up starts the queue worker](#warm-up-starts-the-queue-worker)
  - [Buffer threshold](#buffer-threshold)
  - [Retention and polling cadence](#retention-and-polling-cadence)
- [Using IQueue](#using-iqueue)
  - [IQueue contract](#iqueue-contract)
  - [Memory queue semantics](#memory-queue-semantics)
- [Custom Queue Backends](#custom-queue-backends)
- [Repository Examples](#repository-examples)

---

## Implement a Queue Manager

`IQueueManager<T>` is the consumer that receives a flushed batch.

```csharp
using Rystem.Queue;

public sealed class SampleQueueManager : IQueueManager<Sample>
{
    private readonly ILogger<SampleQueueManager> _logger;

    public SampleQueueManager(ILogger<SampleQueueManager> logger)
    {
        _logger = logger;
    }

    public Task ManageAsync(IEnumerable<Sample> items)
    {
        _logger.LogInformation("Processing {Count} items", items.Count());
        return Task.CompletedTask;
    }
}
```

### IQueueManager contract

```csharp
public interface IQueueManager<in T>
{
    Task ManageAsync(IEnumerable<T> items);
}
```

The manager should be able to process the whole batch in one call.

### Dependency injection behavior

`AddQueueIntegration(...)` registers `IQueueManager<T>` as `Transient`.

When a flush happens, `QueueJobManager<T>` resolves the manager from a fresh DI scope:

```csharp
var service = _serviceProvider.CreateScope().ServiceProvider.GetService<IQueueManager<T>>();
```

That means scoped dependencies behave per flush, not for the lifetime of the application. The unit test manager in `src/Extensions/Queue/Test/Rystem.Queue.UnitTest/QueueTest.cs` demonstrates this by resolving singleton, scoped, and transient dependencies inside the manager.

---

## Register a Queue

### In-memory FIFO queue

Register the built-in FIFO queue with `AddMemoryQueue<T, TQueueManager>()`:

```csharp
services.AddMemoryQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat = "*/1 * * * * *";
});
```

This uses `MemoryQueue<T>`, which is backed by `ConcurrentQueue<T>`.

### In-memory LIFO stack queue

If you want stack-like behavior instead, use `AddMemoryStackQueue<T, TQueueManager>()`:

```csharp
services.AddMemoryStackQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat = "*/1 * * * * *";
});
```

This uses `MemoryStackQueue<T>`, which is backed by `ConcurrentStack<T>`.

### Generic queue integration

The common registration path is:

```csharp
services.AddQueueIntegration<T, TQueueManager, TQueue>(options =>
{
    // configure QueueProperty<T>
});
```

Internally it registers:

- `QueueProperty<T>` as singleton
- `IQueue<T>` as singleton
- `IQueueManager<T>` as transient
- `QueueJobManager<T>` through `AddBackgroundJob(...)`

The background queue worker is configured with:

```csharp
x.Cron = settings.BackgroundJobCronFormat;
x.RunImmediately = false;
```

So queue processing always depends on the `Rystem.BackgroundJob` scheduler and does not run immediately at startup unless the first scheduled tick occurs.

---

## QueueProperty

`QueueProperty<T>` contains the queue settings:

```csharp
public sealed class QueueProperty<T>
{
    public int MaximumBuffer { get; set; } = 5000;
    public string MaximumRetentionCronFormat { get; set; } = "*/1 * * * *";
    public string BackgroundJobCronFormat { get; set; } = "*/1 * * * *";
}
```

| Property | Default | Purpose |
|---|---|---|
| `MaximumBuffer` | `5000` | Flush when the queued item count goes above this value |
| `MaximumRetentionCronFormat` | `"*/1 * * * *"` | Retention schedule used by `QueueJobManager<T>` when computing flush timing |
| `BackgroundJobCronFormat` | `"*/1 * * * *"` | How often the background worker checks the queue |

`QueueProperty<T>` is generic only so it can be registered separately per queue type.

---

## Flush Behavior and Warm-up

### Warm-up starts the queue worker

Because the queue worker is implemented as a background job, it starts only after warm-up runs:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

The unit test does the same with:

```csharp
serviceProvider.WarmUpAsync().ToResult();
```

Without warm-up, the scheduled queue flushes never begin.

### Buffer threshold

The internal queue worker flushes when:

```csharp
await _queue.CountAsync() > _property.MaximumBuffer
```

Note the comparison is `>` rather than `>=`.

That is why the test uses `1001` items when `MaximumBuffer = 1000`:

```csharp
for (int i = 0; i < 1001; i++)
    await queue.AddAsync(new Sample { Id = i.ToString() });
```

After a short wait, the queue is empty again:

```csharp
Assert.Equal(0, await queue.CountAsync());
```

### Retention and polling cadence

The queue worker runs on `BackgroundJobCronFormat`, and that scheduled execution is the outer polling loop for flushes.

Inside the worker, `MaximumRetentionCronFormat` is parsed with `Cronos` to compute the next retention occurrence when flush logic runs. In practice, the background job cadence is what determines how often the queue can be inspected, so keep `BackgroundJobCronFormat` at or below the level of responsiveness you want.

The test-backed example configures:

```csharp
options.MaximumRetentionCronFormat = "*/3 * * * * *";
options.BackgroundJobCronFormat = "*/1 * * * * *";
```

With that setup, the queue is checked every second and flushed during the scheduled processing loop.

---

## Using IQueue

Inject `IQueue<T>` wherever items should be buffered.

```csharp
using Rystem.Queue;

public sealed class OrderService
{
    private readonly IQueue<Sample> _queue;

    public OrderService(IQueue<Sample> queue)
    {
        _queue = queue;
    }

    public async Task EnqueueAsync()
    {
        for (int i = 0; i < 100; i++)
            await _queue.AddAsync(new Sample { Id = i.ToString() });
    }
}
```

### IQueue contract

```csharp
public interface IQueue<T>
{
    Task AddAsync(T entity);
    Task<IEnumerable<T>> DequeueAsync(int? top = null);
    Task<IEnumerable<T>> ReadAsync(int? top = null);
    Task<int> CountAsync();
}
```

Typical usage:

```csharp
await queue.AddAsync(new Sample { Id = "1" });

IEnumerable<Sample> preview = await queue.ReadAsync(top: 10);
IEnumerable<Sample> batch = await queue.DequeueAsync(top: 50);
int count = await queue.CountAsync();
```

### Memory queue semantics

The built-in in-memory implementations behave like this:

- `MemoryQueue<T>` is FIFO
- `MemoryStackQueue<T>` is LIFO
- both implementations are singleton-backed, so queued items live for the application lifetime unless dequeued

---

## Custom Queue Backends

To plug in your own queue storage, implement `IQueue<T>` and register it through `AddQueueIntegration(...)`.

```csharp
using Rystem.Queue;

public sealed class ServiceBusQueue<T> : IQueue<T>
{
    public Task AddAsync(T entity) => Task.CompletedTask;
    public Task<IEnumerable<T>> ReadAsync(int? top = null) => Task.FromResult(Enumerable.Empty<T>());
    public Task<IEnumerable<T>> DequeueAsync(int? top = null) => Task.FromResult(Enumerable.Empty<T>());
    public Task<int> CountAsync() => Task.FromResult(0);
}
```

```csharp
services.AddQueueIntegration<Sample, SampleQueueManager, ServiceBusQueue<Sample>>(options =>
{
    options.MaximumBuffer = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat = "*/1 * * * * *";
});
```

When writing a custom backend, keep these semantics aligned with the queue worker:

- `CountAsync()` should reflect the current queued count as accurately as possible
- `DequeueAsync()` should remove the returned items
- `ReadAsync()` should not remove items

---

## Repository Examples

The most useful references for this package are:

- Queue registration entry point: [src/Extensions/Queue/Rystem.Queue/ServiceCollectionExtensions/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/ServiceCollectionExtensions/ServiceCollectionExtensions.cs)
- Queue settings model: [src/Extensions/Queue/Rystem.Queue/Models/QueueProperty.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/Models/QueueProperty.cs)
- Queue worker background job: [src/Extensions/Queue/Rystem.Queue/BackgroundJob/QueueJobManager.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/BackgroundJob/QueueJobManager.cs)
- Queue contract: [src/Extensions/Queue/Rystem.Queue/Interfaces/IQueue.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/Interfaces/IQueue.cs)
- Queue manager contract: [src/Extensions/Queue/Rystem.Queue/Interfaces/IQueueManager.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/Interfaces/IQueueManager.cs)
- FIFO in-memory backend: [src/Extensions/Queue/Rystem.Queue/InMemory/MemoryQueue.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/InMemory/MemoryQueue.cs)
- LIFO in-memory backend: [src/Extensions/Queue/Rystem.Queue/InMemory/MemoryStackQueue.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Rystem.Queue/InMemory/MemoryStackQueue.cs)
- Unit test: [src/Extensions/Queue/Test/Rystem.Queue.UnitTest/QueueTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Queue/Test/Rystem.Queue.UnitTest/QueueTest.cs)

This README is intentionally architecture-first because `Rystem.Queue` is more than just an in-memory queue. It is a small batching pipeline built from a queue abstraction, a batch manager abstraction, and a scheduled worker from `Rystem.BackgroundJob`.
