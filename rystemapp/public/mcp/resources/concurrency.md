# Concurrency Control

> Implement distributed locks and prevent race conditions with Rystem Concurrency

## Overview

Rystem provides concurrency control mechanisms including locks, semaphores, and race condition prevention for distributed systems.

## Installation

```bash
# In-memory concurrency
dotnet add package Rystem.Concurrency

# Distributed with Redis
dotnet add package Rystem.Concurrency.Redis
```

## Features

- **Async Locks** - Asynchronous locking mechanisms
- **Distributed Locks** - Locks across multiple application instances
- **Semaphores** - Control access to limited resources
- **Race Condition Prevention** - Ensure data integrity
- **Deadlock Detection** - Automatic deadlock prevention
- **Timeout Support** - Avoid infinite waiting

## Configuration

### In-Memory (Single Instance)
```csharp
builder.Services.AddConcurrency();
```

### Distributed with Redis
```csharp
builder.Services.AddConcurrency(options =>
{
    options.UseRedis(redis =>
    {
        redis.ConnectionString = builder.Configuration["Redis:ConnectionString"];
    });
});
```

## Usage

### Basic Lock
```csharp
public class OrderService
{
    private readonly IConcurrencyManager _concurrency;

    public OrderService(IConcurrencyManager concurrency)
    {
        _concurrency = concurrency;
    }

    public async Task ProcessOrderAsync(Guid orderId)
    {
        var lockKey = $"order:{orderId}";
        
        await using var @lock = await _concurrency.AcquireLockAsync(lockKey);
        
        // Critical section - only one process can execute this at a time
        var order = await GetOrderAsync(orderId);
        order.Status = OrderStatus.Processing;
        await SaveOrderAsync(order);
    }
}
```

### Lock with Timeout
```csharp
public async Task<bool> TryProcessAsync(string resourceId)
{
    var lockKey = $"resource:{resourceId}";
    
    var @lock = await _concurrency.TryAcquireLockAsync(
        lockKey,
        timeout: TimeSpan.FromSeconds(5));
    
    if (@lock == null)
    {
        return false; // Could not acquire lock
    }

    await using (@lock)
    {
        // Process the resource
        return true;
    }
}
```

### Semaphore for Limited Resources
```csharp
public class ApiClient
{
    private readonly IConcurrencyManager _concurrency;
    private const string SemaphoreKey = "api-rate-limit";
    private const int MaxConcurrentRequests = 10;

    public async Task<T> MakeRequestAsync<T>(string endpoint)
    {
        await using var permit = await _concurrency.AcquireSemaphoreAsync(
            SemaphoreKey,
            MaxConcurrentRequests);
        
        // Only 10 concurrent requests allowed
        return await CallApiAsync<T>(endpoint);
    }
}
```

### Race Condition Prevention
```csharp
public class InventoryService
{
    private readonly IConcurrencyManager _concurrency;
    private readonly IRepository<Product, int> _repository;

    public async Task<bool> ReserveStockAsync(int productId, int quantity)
    {
        var lockKey = $"product:{productId}:stock";
        
        await using var @lock = await _concurrency.AcquireLockAsync(lockKey);
        
        var product = await _repository.GetAsync(productId);
        
        if (product.StockQuantity < quantity)
        {
            return false; // Insufficient stock
        }
        
        product.StockQuantity -= quantity;
        await _repository.UpdateAsync(product);
        
        return true;
    }
}
```

## Advanced Patterns

### Optimistic Concurrency with ETag
```csharp
public async Task<bool> UpdateWithOptimisticLockAsync(Product product)
{
    try
    {
        await _repository.UpdateAsync(product);
        return true;
    }
    catch (ConcurrencyException)
    {
        // ETag mismatch - someone else modified the entity
        return false;
    }
}
```

### Distributed Lock with Retry
```csharp
public async Task ProcessWithRetryAsync(string id)
{
    var maxRetries = 3;
    var delay = TimeSpan.FromSeconds(1);

    for (int i = 0; i < maxRetries; i++)
    {
        var @lock = await _concurrency.TryAcquireLockAsync(
            $"resource:{id}",
            timeout: TimeSpan.FromSeconds(5));

        if (@lock != null)
        {
            await using (@lock)
            {
                await ProcessAsync(id);
                return;
            }
        }

        await Task.Delay(delay);
    }

    throw new InvalidOperationException("Could not acquire lock after retries");
}
```

## Best Practices

1. **Keep Critical Sections Short** - Hold locks for minimum time
2. **Use Timeouts** - Prevent indefinite waiting
3. **Handle Lock Failures** - Always check if lock was acquired
4. **Dispose Locks Properly** - Use `await using` or try-finally
5. **Use Descriptive Keys** - Make lock keys meaningful
6. **Avoid Nested Locks** - Prevent deadlocks

## Common Use Cases

- **Inventory Management** - Prevent overselling
- **Payment Processing** - Ensure one-time charges
- **Resource Allocation** - Limit concurrent access
- **Data Migration** - Coordinate across instances
- **Cache Updates** - Prevent duplicate work
- **Rate Limiting** - Control API usage

## Performance Considerations

### In-Memory vs Distributed

**In-Memory**
- ✅ Faster
- ✅ No external dependencies
- ❌ Single instance only

**Distributed (Redis)**
- ✅ Works across multiple instances
- ✅ Survives application restarts
- ❌ Network overhead
- ❌ Requires Redis infrastructure

## See Also

- [Rystem.Concurrency Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/Concurrency)
- [Redis Integration](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis)
- [Background Jobs](./background-jobs.md)
