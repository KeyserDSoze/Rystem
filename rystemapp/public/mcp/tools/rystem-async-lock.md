# Async Lock

Execute **async methods sequentially** using key-based locking to prevent race conditions.

**Use Cases:**
- Prevent concurrent updates to the same resource
- Lock by ID (user, order, product, etc.)
- Ensure single execution per key
- Process queued operations in order
- Distributed locking across multiple instances

---

## Why Async Lock?

The C# `lock` keyword **doesn't work with async methods**:

```csharp
// ❌ This doesn't compile
lock (_someLock)
{
    await DoSomethingAsync(); // Error: Cannot await in lock
}
```

Async Lock solves this problem:

```csharp
// ✅ This works
await _lock.ExecuteAsync(async () => 
{
    await DoSomethingAsync();
}, key: "resource-123");
```

---

## Installation

```bash
# In-memory lock (single instance)
dotnet add package Rystem.Concurrency --version 9.1.3

# Distributed lock (multi-instance with Redis)
dotnet add package Rystem.Concurrency.Redis --version 9.1.3
```

---

## In-Memory Lock (Single Instance)

### Configuration

```csharp
builder.Services.AddLock();
```

### Usage

```csharp
public class OrderService
{
    private readonly ILock _lock;
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public OrderService(ILock @lock, IRepository<Order, Guid> orderRepository)
    {
        _lock = @lock;
        _orderRepository = orderRepository;
    }
    
    public async Task ProcessOrderAsync(Guid orderId)
    {
        // Lock by order ID - only one execution per order at a time
        await _lock.ExecuteAsync(async () =>
        {
            var order = await _orderRepository.GetAsync(orderId);
            
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Processing;
                await _orderRepository.UpdateAsync(order);
                
                // Process order...
                await Task.Delay(1000); // Simulate processing
                
                order.Status = OrderStatus.Completed;
                await _orderRepository.UpdateAsync(order);
            }
        }, key: $"order-{orderId}");
    }
}
```

---

## Distributed Lock with Redis (Multi-Instance)

### Configuration

```csharp
builder.Services.AddRedisLock(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"];
});
```

### appsettings.json

```json
{
  "ConnectionString": {
    "Redis": "localhost:6379,ssl=false"
  }
}
```

### Usage

Same API as in-memory lock, but **works across multiple app instances**:

```csharp
public class InventoryService
{
    private readonly ILock _redisLock;
    private readonly IRepository<Product, Guid> _productRepository;
    
    public InventoryService(ILock redisLock, IRepository<Product, Guid> productRepository)
    {
        _redisLock = redisLock;
        _productRepository = productRepository;
    }
    
    public async Task DecrementStockAsync(Guid productId, int quantity)
    {
        // Distributed lock - works across all instances
        await _redisLock.ExecuteAsync(async () =>
        {
            var product = await _productRepository.GetAsync(productId);
            
            if (product.Stock >= quantity)
            {
                product.Stock -= quantity;
                await _productRepository.UpdateAsync(product);
            }
            else
            {
                throw new InvalidOperationException("Insufficient stock");
            }
        }, key: $"product-{productId}");
    }
}
```

---

## Complete Example

```csharp
using Rystem.Concurrency;

var builder = WebApplication.CreateBuilder(args);

// In-memory lock for single instance
builder.Services.AddLock();

// OR Distributed Redis lock for multi-instance
// builder.Services.AddRedisLock(options =>
// {
//     options.ConnectionString = builder.Configuration["ConnectionString:Redis"];
// });

builder.Services.AddRepository<BankAccount, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

var app = builder.Build();

app.MapPost("/transfer", async (
    Guid fromAccountId,
    Guid toAccountId,
    decimal amount,
    ILock @lock,
    IRepository<BankAccount, Guid> accountRepository) =>
{
    // Lock BOTH accounts to prevent race conditions
    var lockKey = string.Join("-", 
        new[] { fromAccountId, toAccountId }.OrderBy(x => x));
    
    await @lock.ExecuteAsync(async () =>
    {
        var fromAccount = await accountRepository.GetAsync(fromAccountId);
        var toAccount = await accountRepository.GetAsync(toAccountId);
        
        if (fromAccount.Balance >= amount)
        {
            fromAccount.Balance -= amount;
            toAccount.Balance += amount;
            
            await accountRepository.UpdateAsync(fromAccount);
            await accountRepository.UpdateAsync(toAccount);
            
            return Results.Ok(new { Success = true });
        }
        
        return Results.BadRequest("Insufficient funds");
    }, key: lockKey);
});

app.Run();
```

---

## Real-World Examples

### E-Commerce: Purchase Item

```csharp
public class PurchaseService
{
    private readonly ILock _lock;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public PurchaseService(
        ILock @lock,
        IRepository<Product, Guid> productRepository,
        IRepository<Order, Guid> orderRepository)
    {
        _lock = @lock;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }
    
    public async Task<Order> PurchaseAsync(Guid productId, Guid userId, int quantity)
    {
        return await _lock.ExecuteAsync(async () =>
        {
            var product = await _productRepository.GetAsync(productId);
            
            if (product.Stock < quantity)
                throw new InvalidOperationException("Insufficient stock");
            
            // Decrement stock
            product.Stock -= quantity;
            await _productRepository.UpdateAsync(product);
            
            // Create order
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                Quantity = quantity,
                Total = product.Price * quantity,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };
            
            await _orderRepository.InsertAsync(order);
            
            return order;
        }, key: $"product-{productId}"); // Lock per product
    }
}
```

### User Profile Update

```csharp
public class UserService
{
    private readonly ILock _lock;
    private readonly IRepository<User, Guid> _userRepository;
    
    public UserService(ILock @lock, IRepository<User, Guid> userRepository)
    {
        _lock = @lock;
        _userRepository = userRepository;
    }
    
    public async Task UpdateProfileAsync(Guid userId, UserProfile profile)
    {
        await _lock.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetAsync(userId);
            
            user.Name = profile.Name;
            user.Email = profile.Email;
            user.UpdatedAt = DateTime.UtcNow;
            
            await _userRepository.UpdateAsync(user);
        }, key: $"user-{userId}");
    }
}
```

### Multi-Resource Lock (Prevent Deadlock)

```csharp
public class TransferService
{
    private readonly ILock _lock;
    private readonly IRepository<Wallet, Guid> _walletRepository;
    
    public TransferService(ILock @lock, IRepository<Wallet, Guid> walletRepository)
    {
        _lock = @lock;
        _walletRepository = walletRepository;
    }
    
    public async Task TransferAsync(Guid fromWalletId, Guid toWalletId, decimal amount)
    {
        // Lock both wallets - order IDs to prevent deadlock
        var sortedIds = new[] { fromWalletId, toWalletId }.OrderBy(x => x);
        var lockKey = string.Join("-", sortedIds);
        
        await _lock.ExecuteAsync(async () =>
        {
            var fromWallet = await _walletRepository.GetAsync(fromWalletId);
            var toWallet = await _walletRepository.GetAsync(toWalletId);
            
            if (fromWallet.Balance < amount)
                throw new InvalidOperationException("Insufficient balance");
            
            fromWallet.Balance -= amount;
            toWallet.Balance += amount;
            
            await _walletRepository.UpdateAsync(fromWallet);
            await _walletRepository.UpdateAsync(toWallet);
        }, key: lockKey);
    }
}
```

### File Upload with Quota Check

```csharp
public class FileUploadService
{
    private readonly ILock _lock;
    private readonly IContentRepository _storage;
    private readonly IRepository<UserQuota, Guid> _quotaRepository;
    
    public FileUploadService(
        ILock @lock,
        IContentRepository storage,
        IRepository<UserQuota, Guid> quotaRepository)
    {
        _lock = @lock;
        _storage = storage;
        _quotaRepository = quotaRepository;
    }
    
    public async Task UploadFileAsync(Guid userId, string fileName, byte[] data)
    {
        await _lock.ExecuteAsync(async () =>
        {
            var quota = await _quotaRepository.GetAsync(userId);
            
            if (quota.UsedBytes + data.Length > quota.MaxBytes)
                throw new InvalidOperationException("Quota exceeded");
            
            // Upload file
            await _storage.UploadAsync($"users/{userId}/{fileName}", data);
            
            // Update quota
            quota.UsedBytes += data.Length;
            await _quotaRepository.UpdateAsync(quota);
        }, key: $"user-quota-{userId}");
    }
}
```

### Background Job with Lock

```csharp
public class DataSyncJob : IBackgroundJob
{
    private readonly ILock _lock;
    private readonly IRepository<SyncStatus, string> _syncRepository;
    private readonly IExternalApiClient _apiClient;
    
    public DataSyncJob(
        ILock @lock,
        IRepository<SyncStatus, string> syncRepository,
        IExternalApiClient apiClient)
    {
        _lock = @lock;
        _syncRepository = syncRepository;
        _apiClient = apiClient;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        // Prevent multiple instances from syncing simultaneously
        await _lock.ExecuteAsync(async () =>
        {
            var status = await _syncRepository.GetAsync("global-sync");
            
            if (status.LastSyncAt > DateTime.UtcNow.AddMinutes(-5))
                return; // Skip if synced recently
            
            var data = await _apiClient.GetDataAsync(cancellationToken);
            
            // Process data...
            
            status.LastSyncAt = DateTime.UtcNow;
            await _syncRepository.UpdateAsync(status);
        }, key: "data-sync-global");
    }
    
    public Task OnException(Exception exception) => Task.CompletedTask;
}
```

---

## API Reference

### ILock Interface

```csharp
public interface ILock
{
    /// <summary>
    /// Execute action with async lock
    /// </summary>
    Task ExecuteAsync(Func<Task> action, string key);
    
    /// <summary>
    /// Execute func with async lock and return result
    /// </summary>
    Task<T> ExecuteAsync<T>(Func<Task<T>> func, string key);
}
```

---

## How It Works

### In-Memory Lock

Uses `SemaphoreSlim` per key:

```
Key: "product-21"  → SemaphoreSlim (1 concurrent)
Key: "product-22"  → SemaphoreSlim (1 concurrent)
Key: "user-123"    → SemaphoreSlim (1 concurrent)
```

**Multiple requests for same key:**
```
Request 1 → Lock "product-21" → Execute → Release
Request 2 → Wait for lock      → Execute → Release
Request 3 → Wait for lock      → Execute → Release
```

**Concurrent requests for different keys:**
```
Request 1 → Lock "product-21" → Execute → Release
Request 2 → Lock "product-22" → Execute → Release (concurrent)
Request 3 → Lock "user-123"   → Execute → Release (concurrent)
```

### Redis Lock

Uses **distributed lock** with Redis:
- All app instances share the same lock
- Prevents race conditions across multiple servers
- Automatic lock expiration (prevents deadlock if app crashes)

---

## When to Use

### Use Async Lock When:
- ✅ Updating the same resource from multiple requests
- ✅ Decrementing stock, quotas, balances
- ✅ Processing orders, payments, transfers
- ✅ Need to queue operations per resource
- ✅ Multiple app instances (use Redis lock)

### Don't Use When:
- ❌ Read-only operations
- ❌ Operations on different resources (no conflict)
- ❌ Single-threaded execution already guaranteed

---

## Benefits

- ✅ **Async/Await Support**: Works with async methods
- ✅ **Key-Based**: Lock per resource ID, not globally
- ✅ **Distributed**: Redis support for multi-instance apps
- ✅ **Prevents Race Conditions**: Ensures sequential execution
- ✅ **Simple API**: Just wrap your code with `ExecuteAsync()`

---

## Related Tools

- **[Race Condition](https://rystem.net/mcp/tools/rystem-race-condition.md)** - Allow only first request, block others
- **[Concurrency Control](https://rystem.net/mcp/resources/concurrency.md)** - Best practices
- **[Background Jobs](https://rystem.net/mcp/tools/rystem-backgroundjob.md)** - Scheduled tasks with lock

---

## References

- **NuGet Package (In-Memory)**: [Rystem.Concurrency](https://www.nuget.org/packages/Rystem.Concurrency) v9.1.3
- **NuGet Package (Redis)**: [Rystem.Concurrency.Redis](https://www.nuget.org/packages/Rystem.Concurrency.Redis) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
