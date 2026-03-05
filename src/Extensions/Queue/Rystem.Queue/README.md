### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Queue

A batching queue library for ASP.NET Core and generic host applications. Items are accumulated in a queue and flushed automatically when either a **maximum buffer size** or a **retention time window** is exceeded. The flush callback is your `IQueueManager<T>` implementation.

Internally, the flushing is driven by a `BackgroundJob` (from `Rystem.BackgroundJob`).

## 📦 Installation

```bash
dotnet add package Rystem.Queue
```

## Table of Contents

- [Rystem.Queue](#rystemqueue)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [Implement a Queue Manager](#implement-a-queue-manager)
- [Register — In-Memory FIFO Queue](#register--in-memory-fifo-queue)
- [Register — In-Memory LIFO Stack Queue](#register--in-memory-lifo-stack-queue)
- [QueueProperty](#queueproperty)
- [Warm Up](#warm-up)
- [Using IQueue](#using-iqueue)
- [IQueue interface](#iqueue-interface)
- [Custom Queue Integration](#custom-queue-integration)

---

## Implement a Queue Manager

Implement `IQueueManager<T>` to handle the batch of items delivered when the queue flushes:

```csharp
public class SampleQueueManager : IQueueManager<Sample>
{
    private readonly ILogger<SampleQueueManager> _logger;

    public SampleQueueManager(ILogger<SampleQueueManager> logger)
        => _logger = logger;

    public async Task ManageAsync(IEnumerable<Sample> items)
    {
        // Process the batch — write to DB, send to an external API, etc.
        _logger.LogInformation("Processing {Count} items", items.Count());
        await PersistAsync(items);
    }
}
```

`IQueueManager<T>` is registered as **Transient**, so a new instance is created per flush and constructor injection is fully supported.

---

## Register — In-Memory FIFO Queue

Items are dequeued in **first-in / first-out** order.

```csharp
services.AddMemoryQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer          = 1000;             // flush when 1 000+ items accumulate
    options.MaximumRetentionCronFormat = "*/3 * * * * *"; // or flush every 3 seconds
    options.BackgroundJobCronFormat    = "*/1 * * * * *"; // check every 1 second
});
```

---

## Register — In-Memory LIFO Stack Queue

Items are dequeued in **last-in / first-out** order. Options are identical to the FIFO variant.

```csharp
services.AddMemoryStackQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer          = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat    = "*/1 * * * * *";
});
```

---

## QueueProperty

| Property | Type | Default | Description |
|---|---|---|---|
| `MaximumBuffer` | `int` | `5000` | Item count threshold that triggers an immediate flush |
| `MaximumRetentionCronFormat` | `string` | `"*/1 * * * *"` | CRON expression defining the maximum time before a flush is forced |
| `BackgroundJobCronFormat` | `string` | `"*/1 * * * *"` | CRON expression for how often the background job checks both conditions |

Both 5-field (minute-level) and 6-field (second-level) CRON expressions are supported — the field count is detected automatically.

> **Tip:** `BackgroundJobCronFormat` should be ≤ `MaximumRetentionCronFormat`. A check interval longer than the retention window means the retention deadline can be missed.

---

## Warm Up

The underlying background job is started during `WarmUpAsync`. Call it after `Build()`:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
app.Run();
```

---

## Using IQueue

Inject `IQueue<T>` to enqueue items. The library takes care of batching and flushing:

```csharp
public class OrderService
{
    private readonly IQueue<Sample> _queue;

    public OrderService(IQueue<Sample> queue) => _queue = queue;

    public async Task EnqueueAsync()
    {
        for (var i = 0; i < 100; i++)
            await _queue.AddAsync(new Sample { Id = i.ToString() });
    }
}
```

You can also read or manually dequeue items if needed:

```csharp
// Peek at items without removing them
var items = await _queue.ReadAsync(top: 10);

// Dequeue (remove and return) items
var batch = await _queue.DequeueAsync(top: 50);

// Check how many items are waiting
var count = await _queue.CountAsync();
```

---

## IQueue interface

| Method | Description |
|---|---|
| `AddAsync(T entity)` | Enqueue a single item |
| `ReadAsync(int? top)` | Read items without removing them; `null` returns all |
| `DequeueAsync(int? top)` | Remove and return items; `null` dequeues all |
| `CountAsync()` | Return the current number of queued items |

---

## Custom Queue Integration

To back the queue with a distributed system (Azure Storage Queue, Service Bus, Event Hub, etc.) implement `IQueue<T>` and register it with `AddQueueIntegration`:

```csharp
public class ServiceBusQueue<T> : IQueue<T>
{
    public Task AddAsync(T entity) { /* send to Service Bus */ }
    public Task<IEnumerable<T>> ReadAsync(int? top = null) { /* peek */ }
    public Task<IEnumerable<T>> DequeueAsync(int? top = null) { /* receive */ }
    public Task<int> CountAsync() { /* approximate count */ }
}
```

```csharp
services.AddQueueIntegration<Sample, SampleQueueManager, ServiceBusQueue<Sample>>(options =>
{
    options.MaximumBuffer          = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat    = "*/1 * * * * *";
});
```

`AddMemoryQueue` and `AddMemoryStackQueue` are convenience wrappers around `AddQueueIntegration` using the built-in in-memory implementations.