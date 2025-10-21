---
title: Stopwatch Utilities
description: Monitor execution time of methods, tasks, and actions with Rystem's Stopwatch helpers - includes Start/Stop, Monitor, and MonitorAsync with automatic time tracking
---

# Stopwatch Utilities

Monitor the **execution time** of actions, tasks, or methods with Rystem's enhanced Stopwatch utilities.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Quick Start

### Start and Stop

```csharp
using Rystem;

var started = Stopwatch.Start();

// Do something
await Task.Delay(2000);

var result = started.Stop();

Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
```

---

## Monitor Synchronous Actions

Use `Monitor()` to execute an action and automatically measure its execution time:

```csharp
var result = Stopwatch.Monitor(() =>
{
    Thread.Sleep(2000);
    // Your code here
});

Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
```

**With return value:**

```csharp
var result = Stopwatch.Monitor(() =>
{
    Thread.Sleep(2000);
    return 42;
});

Console.WriteLine($"Result: {result.Value}");
Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
```

---

## Monitor Asynchronous Tasks

Use `MonitorAsync()` for async operations:

```csharp
var result = await Stopwatch.MonitorAsync(async () =>
{
    await Task.Delay(2000);
    // Your async code here
});

Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
```

**With return value:**

```csharp
var result = await Stopwatch.MonitorAsync(async () =>
{
    await Task.Delay(2000);
    return 42;
});

Console.WriteLine($"Result: {result.Value}");
Console.WriteLine($"Elapsed: {result.ElapsedMilliseconds}ms");
```

---

## Result Properties

All monitoring methods return a result with:

- **`ElapsedMilliseconds`**: Time in milliseconds
- **`ElapsedTicks`**: Time in ticks
- **`Elapsed`**: TimeSpan representation
- **`Value`** (when applicable): Return value of the monitored operation

---

## Real-World Examples

### API Call Monitoring

```csharp
public async Task<OrderResponse> GetOrderAsync(Guid orderId)
{
    var result = await Stopwatch.MonitorAsync(async () =>
    {
        return await httpClient.GetFromJsonAsync<OrderResponse>($"/api/orders/{orderId}");
    });

    logger.LogInformation(
        "Order {OrderId} retrieved in {ElapsedMs}ms",
        orderId,
        result.ElapsedMilliseconds
    );

    return result.Value;
}
```

### Database Query Performance

```csharp
public async Task<List<Product>> GetProductsAsync()
{
    var result = await Stopwatch.MonitorAsync(async () =>
    {
        return await dbContext.Products
            .Where(p => p.IsActive)
            .ToListAsync();
    });

    if (result.ElapsedMilliseconds > 1000)
    {
        logger.LogWarning(
            "Slow query detected: {ElapsedMs}ms",
            result.ElapsedMilliseconds
        );
    }

    return result.Value;
}
```

### Background Job Monitoring

```csharp
public async Task ProcessOrdersAsync()
{
    var result = await Stopwatch.MonitorAsync(async () =>
    {
        var orders = await orderRepository.QueryAsync(x => x.Status == OrderStatus.Pending);
        
        foreach (var order in orders)
        {
            await ProcessOrderAsync(order);
        }
        
        return orders.Count;
    });

    logger.LogInformation(
        "Processed {Count} orders in {ElapsedMs}ms",
        result.Value,
        result.ElapsedMilliseconds
    );
}
```

### Method Profiling

```csharp
public decimal CalculateTotal(Order order)
{
    var result = Stopwatch.Monitor(() =>
    {
        decimal total = 0;
        foreach (var item in order.Items)
        {
            total += item.Price * item.Quantity;
        }
        return total;
    });

    if (result.ElapsedMilliseconds > 100)
    {
        logger.LogWarning(
            "Slow calculation for order {OrderId}: {ElapsedMs}ms",
            order.Id,
            result.ElapsedMilliseconds
        );
    }

    return result.Value;
}
```

---

## Benefits

- ✅ **Simple API**: Start/Stop or Monitor pattern
- ✅ **Async Support**: MonitorAsync for async operations
- ✅ **Return Values**: Capture both result and timing
- ✅ **Performance Insights**: Identify slow operations
- ✅ **Logging Integration**: Easy to log execution times

---

## Related Tools

- **[Task Extensions](https://rystem.net/mcp/tools/rystem-task-extensions.md)** - Task utilities including NoContext() and TaskManager
- **[Background Jobs](https://rystem.net/mcp/resources/background-jobs.md)** - Monitor background job execution times
- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Monitor repository operations

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
