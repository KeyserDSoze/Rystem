# ConcurrentList

A **thread-safe** implementation of `List<T>` with automatic locking for concurrent access.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Quick Start

```csharp
using Rystem;

var items = new ConcurrentList<string>();

// Thread-safe Add
items.Add("Item 1");
items.Add("Item 2");

// Thread-safe access
var count = items.Count;
var firstItem = items[0];

// Thread-safe iteration
foreach (var item in items)
{
    Console.WriteLine(item);
}
```

---

## Why ConcurrentList?

### The Problem with List<T>

`List<T>` is **not thread-safe**. Concurrent access can cause:
- Race conditions
- Index out of range exceptions
- Collection modified during enumeration exceptions

```csharp
// ❌ NOT THREAD-SAFE
var list = new List<string>();

Parallel.For(0, 1000, i =>
{
    list.Add($"Item {i}"); // Race condition!
});
```

### The Solution: ConcurrentList<T>

```csharp
// ✅ THREAD-SAFE
var list = new ConcurrentList<string>();

Parallel.For(0, 1000, i =>
{
    list.Add($"Item {i}"); // Safe!
});
```

---

## Features

All standard `List<T>` operations with **automatic locking**:

### Add and Insert

```csharp
var items = new ConcurrentList<int>();

items.Add(1);
items.AddRange(new[] { 2, 3, 4 });
items.Insert(0, 0); // Insert at beginning

// Result: [0, 1, 2, 3, 4]
```

### Remove and Clear

```csharp
items.Remove(2);        // Remove specific item
items.RemoveAt(0);      // Remove at index
items.RemoveAll(x => x > 3); // Remove all matching
items.Clear();          // Remove all
```

### Access and Search

```csharp
var firstItem = items[0];           // Index access
var contains = items.Contains(3);   // Check existence
var index = items.IndexOf(3);       // Find index
var count = items.Count;            // Get count
```

### Enumeration

```csharp
foreach (var item in items)
{
    Console.WriteLine(item);
}

var filtered = items.Where(x => x > 10).ToList();
```

---

## Real-World Examples

### Parallel Data Processing

```csharp
public async Task<List<OrderDetails>> ProcessOrdersAsync(List<Guid> orderIds)
{
    var results = new ConcurrentList<OrderDetails>();
    
    await TaskManager.WhenAll(
        async (index, cancellationToken) =>
        {
            var orderId = orderIds[index];
            var details = await GetOrderDetailsAsync(orderId, cancellationToken).NoContext();
            
            results.Add(details); // Thread-safe
        },
        times: orderIds.Count,
        concurrentTasks: 10,
        runEverytimeASlotIsFree: true
    ).NoContext();
    
    return results.ToList();
}
```

### Background Job Processing

```csharp
public class OrderProcessingJob : IBackgroundJob
{
    private readonly ConcurrentList<string> _processedOrders = new();
    private readonly ConcurrentList<string> _failedOrders = new();
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var orders = await orderRepository.QueryAsync(
            x => x.Status == OrderStatus.Pending,
            cancellationToken
        ).NoContext();
        
        await TaskManager.WhenAll(
            async (index, ct) =>
            {
                var order = orders[index];
                
                try
                {
                    await ProcessOrderAsync(order, ct).NoContext();
                    _processedOrders.Add(order.Id.ToString());
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process order {OrderId}", order.Id);
                    _failedOrders.Add(order.Id.ToString());
                }
            },
            times: orders.Count,
            concurrentTasks: 5,
            runEverytimeASlotIsFree: true
        ).NoContext();
        
        logger.LogInformation(
            "Processed: {Processed}, Failed: {Failed}",
            _processedOrders.Count,
            _failedOrders.Count
        );
    }
}
```

### Event Collector

```csharp
public class EventCollector
{
    private readonly ConcurrentList<DomainEvent> _events = new();
    
    public void RecordEvent(DomainEvent domainEvent)
    {
        _events.Add(domainEvent); // Thread-safe
    }
    
    public async Task FlushEventsAsync()
    {
        if (_events.Count == 0) return;
        
        // Create a copy and clear
        var eventsToSave = _events.ToList();
        _events.Clear();
        
        // Save to database
        await eventRepository.InsertAsync(eventsToSave).NoContext();
    }
}
```

### Cache Management

```csharp
public class CacheManager<T>
{
    private readonly ConcurrentList<CacheEntry<T>> _cache = new();
    private readonly int _maxSize;
    
    public CacheManager(int maxSize = 1000)
    {
        _maxSize = maxSize;
    }
    
    public void Add(string key, T value)
    {
        // Remove oldest entries if cache is full
        if (_cache.Count >= _maxSize)
        {
            var toRemove = _cache.OrderBy(x => x.AccessTime).Take(100).ToList();
            foreach (var entry in toRemove)
            {
                _cache.Remove(entry);
            }
        }
        
        _cache.Add(new CacheEntry<T>
        {
            Key = key,
            Value = value,
            AccessTime = DateTime.UtcNow
        });
    }
    
    public T Get(string key)
    {
        var entry = _cache.FirstOrDefault(x => x.Key == key);
        if (entry != null)
        {
            entry.AccessTime = DateTime.UtcNow; // Update access time
            return entry.Value;
        }
        
        return default;
    }
}

public class CacheEntry<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public DateTime AccessTime { get; set; }
}
```

### Real-Time Log Aggregation

```csharp
public class LogAggregator
{
    private readonly ConcurrentList<LogEntry> _logs = new();
    private readonly Timer _flushTimer;
    
    public LogAggregator()
    {
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    public void Log(LogLevel level, string message)
    {
        _logs.Add(new LogEntry
        {
            Level = level,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
    
    private async void FlushLogs(object state)
    {
        if (_logs.Count == 0) return;
        
        var logsToSave = _logs.ToList();
        _logs.Clear();
        
        await logRepository.InsertAsync(logsToSave).NoContext();
    }
}
```

### Web Scraper

```csharp
public class WebScraper
{
    private readonly ConcurrentList<ScrapedData> _results = new();
    
    public async Task<List<ScrapedData>> ScrapeUrlsAsync(List<string> urls)
    {
        await TaskManager.WhenAll(
            async (index, cancellationToken) =>
            {
                var url = urls[index];
                
                try
                {
                    var html = await httpClient.GetStringAsync(url, cancellationToken).NoContext();
                    var data = ParseHtml(html);
                    
                    _results.Add(data); // Thread-safe
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to scrape {Url}", url);
                }
            },
            times: urls.Count,
            concurrentTasks: 10,
            runEverytimeASlotIsFree: true
        ).NoContext();
        
        return _results.ToList();
    }
}
```

---

## Comparison with Other Collections

| Collection | Thread-Safe | Ordered | Random Access | Use Case |
|------------|-------------|---------|---------------|----------|
| `List<T>` | ❌ | ✅ | ✅ | Single-threaded |
| `ConcurrentBag<T>` | ✅ | ❌ | ❌ | Unordered concurrent |
| `ConcurrentQueue<T>` | ✅ | ✅ (FIFO) | ❌ | FIFO queue |
| `ConcurrentStack<T>` | ✅ | ✅ (LIFO) | ❌ | LIFO stack |
| **`ConcurrentList<T>`** | ✅ | ✅ | ✅ | Ordered concurrent with indexing |

---

## When to Use ConcurrentList

### ✅ Use ConcurrentList When:
- Multiple threads need to **add/remove items concurrently**
- You need **ordered collection** with **index access**
- You want **List<T> behavior** in multi-threaded scenarios
- You're using `Parallel.For`, `TaskManager`, or similar parallel constructs

### ❌ Don't Use ConcurrentList When:
- Single-threaded application → Use `List<T>`
- Only adding items (no removal) → Use `ConcurrentBag<T>`
- FIFO queue → Use `ConcurrentQueue<T>`
- High-performance requirements → Consider lock-free alternatives

---

## Benefits

- ✅ **Thread-Safe**: Automatic locking for all operations
- ✅ **Familiar API**: Same as `List<T>`
- ✅ **Ordered**: Maintains insertion order
- ✅ **Index Access**: Support for `items[index]`
- ✅ **LINQ Support**: Works with LINQ queries
- ✅ **No External Dependencies**: Built into Rystem

---

## Related Tools

- **[Task Extensions](https://rystem.net/mcp/tools/rystem-task-extensions.md)** - TaskManager for concurrent execution
- **[Concurrency](https://rystem.net/mcp/resources/concurrency.md)** - Distributed locks and semaphores
- **[Background Jobs](https://rystem.net/mcp/resources/background-jobs.md)** - Scheduled background processing

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
