# Task Extensions

Task utilities to simplify **async/await patterns** and **concurrent task execution**.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## NoContext() - ConfigureAwait(false)

Tired of writing `.ConfigureAwait(false)` everywhere? Use **`NoContext()`** instead!

### Why ConfigureAwait(false)?

[Microsoft's ConfigureAwait FAQ](https://devblogs.microsoft.com/dotnet/configureawait-faq/)

**TL;DR**: In library code, use `ConfigureAwait(false)` to avoid capturing the synchronization context, improving performance and avoiding deadlocks.

### Before

```csharp
var result = await SomeMethodAsync().ConfigureAwait(false);
var data = await GetDataAsync().ConfigureAwait(false);
```

### After (With Rystem)

```csharp
using Rystem;

var result = await SomeMethodAsync().NoContext();
var data = await GetDataAsync().NoContext();
```

**Example:**

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    var response = await httpClient.GetAsync($"/api/users/{userId}").NoContext();
    var json = await response.Content.ReadAsStringAsync().NoContext();
    
    return json.FromJson<User>();
}
```

---

## ToResult() - Synchronous Execution

Execute an async task **synchronously** with `ConfigureAwait(false)`:

```csharp
var result = GetDataAsync().ToResult();
```

**Equivalent to:**

```csharp
var result = GetDataAsync().ConfigureAwait(false).GetAwaiter().GetResult();
```

**Example:**

```csharp
public class UserService
{
    public User GetUser(Guid userId)
    {
        // Synchronously execute async method
        return GetUserAsync(userId).ToResult();
    }
    
    public async Task<User> GetUserAsync(Guid userId)
    {
        return await userRepository.GetAsync(userId).NoContext();
    }
}
```

---

## Change Default Behavior

By default, `NoContext()` and `ToResult()` use `ConfigureAwait(false)`. You can change this behavior:

```csharp
// In application startup (Program.cs or Startup.cs)
RystemTask.WaitYourStartingThread = true;
```

**When to use `true`?**
- **Windows Forms** applications
- **WPF** applications
- **WinUI** applications
- Any UI application where you need to return to the **UI thread**

**Example:**

```csharp
// In WPF application
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set to true for UI applications
        RystemTask.WaitYourStartingThread = true;
    }
    
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var data = await LoadDataAsync().NoContext();
        
        // This will execute on UI thread because WaitYourStartingThread = true
        DataGrid.ItemsSource = data;
    }
}
```

---

## TaskManager - Concurrent Execution

Execute **multiple tasks concurrently** with control over concurrency levels.

### WhenAll - Execute with Concurrency Limit

Run multiple tasks with a **maximum concurrency limit** and **run as soon as a slot is free**:

```csharp
var bag = new ConcurrentBag<int>();
int times = 8;
int concurrentTasks = 3;
bool runEverytimeASlotIsFree = true;

await TaskManager.WhenAll(ExecuteAsync, times, concurrentTasks, runEverytimeASlotIsFree).NoContext();

Assert.Equal(times, bag.Count);

async Task ExecuteAsync(int i, CancellationToken cancellationToken)
{
    await Task.Delay(i * 20, cancellationToken).NoContext();
    bag.Add(i);
}
```

**How it works:**
1. Starts first **3 tasks** (concurrentTasks = 3)
2. As soon as one finishes, starts the **4th task**
3. As soon as another finishes, starts the **5th task**
4. Continues until all **8 tasks** (times = 8) are executed

**Visualization:**

```
Time 0ms:    [Task 0] [Task 1] [Task 2] - 3 running
Time 20ms:   [Done 0] [Task 1] [Task 2] [Task 3] - Slot freed, Task 3 starts
Time 40ms:   [Done 0] [Done 1] [Task 2] [Task 3] [Task 4] - Slot freed, Task 4 starts
...
```

### WhenAtLeast - Execute Until Minimum Complete

Run tasks until **at least X tasks** complete, then stop waiting for the rest:

```csharp
var bag = new ConcurrentBag<int>();
int times = 10;
int atLeast = 5;
int concurrentTasks = 3;

await TaskManager.WhenAtLeast(ExecuteAsync, times, atLeast, concurrentTasks).NoContext();

Assert.True(bag.Count < times);        // Not all tasks completed
Assert.True(bag.Count >= atLeast);     // At least 5 completed

async Task ExecuteAsync(int i, CancellationToken cancellationToken)
{
    await Task.Delay(i * 20, cancellationToken).NoContext();
    bag.Add(i);
}
```

**Use Cases:**
- **Load balancing**: Call 10 servers, return as soon as 5 respond
- **Redundancy**: Send to 5 services, return when 3 succeed
- **Sampling**: Process 100 items, stop after 20 succeed

---

## Real-World Examples

### Parallel API Calls with Concurrency Limit

```csharp
public async Task<List<OrderDetails>> GetOrderDetailsAsync(List<Guid> orderIds)
{
    var results = new ConcurrentBag<OrderDetails>();
    
    await TaskManager.WhenAll(
        async (index, cancellationToken) =>
        {
            var orderId = orderIds[index];
            var details = await httpClient.GetFromJsonAsync<OrderDetails>(
                $"/api/orders/{orderId}",
                cancellationToken
            ).NoContext();
            
            results.Add(details);
        },
        times: orderIds.Count,
        concurrentTasks: 5, // Max 5 concurrent API calls
        runEverytimeASlotIsFree: true
    ).NoContext();
    
    return results.ToList();
}
```

### Batch Processing with Early Exit

```csharp
public async Task<List<Product>> FindAvailableProductsAsync(List<string> skus)
{
    var availableProducts = new ConcurrentBag<Product>();
    
    // Stop as soon as we find 10 available products
    await TaskManager.WhenAtLeast(
        async (index, cancellationToken) =>
        {
            var sku = skus[index];
            var product = await productRepository.GetBySkuAsync(sku, cancellationToken).NoContext();
            
            if (product?.IsAvailable == true)
            {
                availableProducts.Add(product);
            }
        },
        times: skus.Count,
        atLeast: 10, // Stop after finding 10 available
        concurrentTasks: 5
    ).NoContext();
    
    return availableProducts.Take(10).ToList();
}
```

### Background Processing

```csharp
public async Task ProcessPendingOrdersAsync()
{
    var orders = await orderRepository.QueryAsync(x => x.Status == OrderStatus.Pending).NoContext();
    
    var processed = new ConcurrentBag<Order>();
    
    await TaskManager.WhenAll(
        async (index, cancellationToken) =>
        {
            var order = orders[index];
            
            try
            {
                await ProcessOrderAsync(order, cancellationToken).NoContext();
                processed.Add(order);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing order {OrderId}", order.Id);
            }
        },
        times: orders.Count,
        concurrentTasks: 10, // Process 10 orders concurrently
        runEverytimeASlotIsFree: true
    ).NoContext();
    
    logger.LogInformation("Processed {Count} orders", processed.Count);
}
```

### Load Balancer

```csharp
public async Task<ApiResponse> CallWithRedundancyAsync(string endpoint)
{
    var servers = new[] { "server1", "server2", "server3", "server4", "server5" };
    var responses = new ConcurrentBag<ApiResponse>();
    
    // Call 5 servers, return as soon as 2 respond successfully
    await TaskManager.WhenAtLeast(
        async (index, cancellationToken) =>
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ApiResponse>(
                    $"https://{servers[index]}/{endpoint}",
                    cancellationToken
                ).NoContext();
                
                responses.Add(response);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Server {Server} failed", servers[index]);
            }
        },
        times: servers.Length,
        atLeast: 2, // Stop after 2 successful responses
        concurrentTasks: 5
    ).NoContext();
    
    // Return first successful response
    return responses.FirstOrDefault();
}
```

### Retry with Multiple Attempts

```csharp
public async Task<bool> TryExecuteWithRetriesAsync(Func<Task> operation, int maxRetries)
{
    var success = new ConcurrentBag<bool>();
    
    await TaskManager.WhenAtLeast(
        async (index, cancellationToken) =>
        {
            try
            {
                await operation().NoContext();
                success.Add(true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Attempt {Attempt} failed", index + 1);
            }
        },
        times: maxRetries,
        atLeast: 1, // Stop after first success
        concurrentTasks: 1 // One at a time (sequential retries)
    ).NoContext();
    
    return success.Any();
}
```

---

## Benefits

- ✅ **NoContext()**: Cleaner than `ConfigureAwait(false)`
- ✅ **ToResult()**: Safe synchronous execution
- ✅ **TaskManager.WhenAll()**: Controlled concurrency
- ✅ **TaskManager.WhenAtLeast()**: Early exit when minimum met
- ✅ **Performance**: Efficient task scheduling
- ✅ **Flexibility**: Customizable behavior

---

## Related Tools

- **[Stopwatch](https://rystem.net/mcp/tools/rystem-stopwatch.md)** - Monitor task execution time
- **[Background Jobs](https://rystem.net/mcp/resources/background-jobs.md)** - Scheduled background processing
- **[Concurrency](https://rystem.net/mcp/resources/concurrency.md)** - Distributed locks

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **ConfigureAwait FAQ**: https://devblogs.microsoft.com/dotnet/configureawait-faq/
