---
title: Race Condition
description: Allow only the first request to execute within a time window - block duplicate operations with key-based race condition prevention for purchases, payments, submissions
---

# Race Condition

Allow **only the first request** to execute within a time window, **block all others**.

**Use Cases:**
- Prevent duplicate purchases
- Block double-click submissions
- Allow only first payment attempt
- Prevent concurrent API calls
- Debounce user actions

---

## What is Race Condition?

A **race condition** occurs when multiple operations compete to execute, and only one should win.

**Example:** User clicks "Purchase" button twice:
- ❌ Without protection: 2 purchases created
- ✅ With race condition: Only first purchase created, second blocked

Learn more: [Race Condition on Wikipedia](https://en.wikipedia.org/wiki/Race_condition)

---

## Installation

```bash
dotnet add package Rystem.Concurrency --version 9.1.3
```

---

## Configuration

```csharp
builder.Services.AddRaceCondition();
```

---

## Usage

### Basic Example

```csharp
public class PurchaseService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public PurchaseService(
        IRaceCondition raceCondition,
        IRepository<Order, Guid> orderRepository)
    {
        _raceCondition = raceCondition;
        _orderRepository = orderRepository;
    }
    
    public async Task<Order?> PurchaseItemAsync(Guid itemId, Guid userId)
    {
        // Only first request succeeds, others blocked for 5 seconds
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _orderRepository.InsertAsync(order);
                return order;
            },
            key: $"purchase-item-{itemId}-user-{userId}",
            timeWindow: TimeSpan.FromSeconds(5)
        );
    }
}
```

**What happens:**
1. **First request** (t=0s): Executes, creates order
2. **Second request** (t=1s): Blocked, returns `null`
3. **Third request** (t=3s): Blocked, returns `null`
4. **Fourth request** (t=6s): Executes (after time window expired)

---

## API Reference

```csharp
public interface IRaceCondition
{
    /// <summary>
    /// Execute action if no other execution with same key is in progress
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="key">Unique key for this operation</param>
    /// <param name="timeWindow">Block duration from first execution</param>
    /// <returns>Action result or null if blocked</returns>
    Task<T?> ExecuteAsync<T>(
        Func<Task<T>> action, 
        string key, 
        TimeSpan timeWindow
    );
}
```

---

## Complete Example

```csharp
using Rystem.Concurrency;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRaceCondition();

builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

var app = builder.Build();

app.MapPost("/purchase", async (
    Guid itemId,
    Guid userId,
    IRaceCondition raceCondition,
    IRepository<Order, Guid> orderRepository) =>
{
    var result = await raceCondition.ExecuteAsync(
        async () =>
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };
            
            await orderRepository.InsertAsync(order);
            return order;
        },
        key: $"purchase-{itemId}-{userId}",
        timeWindow: TimeSpan.FromSeconds(5)
    );
    
    if (result != null)
        return Results.Ok(result);
    else
        return Results.Conflict("Duplicate request detected");
});

app.Run();
```

---

## Real-World Examples

### Prevent Double Purchase

```csharp
public class CheckoutService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public CheckoutService(
        IRaceCondition raceCondition,
        IRepository<Product, Guid> productRepository,
        IRepository<Order, Guid> orderRepository)
    {
        _raceCondition = raceCondition;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }
    
    public async Task<Order?> CheckoutAsync(Guid productId, Guid userId)
    {
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var product = await _productRepository.GetAsync(productId);
                
                if (product.Stock <= 0)
                    throw new InvalidOperationException("Out of stock");
                
                product.Stock--;
                await _productRepository.UpdateAsync(product);
                
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    UserId = userId,
                    Price = product.Price,
                    Status = OrderStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _orderRepository.InsertAsync(order);
                return order;
            },
            key: $"checkout-{productId}-{userId}",
            timeWindow: TimeSpan.FromSeconds(10) // Block for 10 seconds
        );
    }
}
```

### Payment Processing

```csharp
public class PaymentService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IRepository<Transaction, Guid> _transactionRepository;
    
    public PaymentService(
        IRaceCondition raceCondition,
        IPaymentGateway paymentGateway,
        IRepository<Transaction, Guid> transactionRepository)
    {
        _raceCondition = raceCondition;
        _paymentGateway = paymentGateway;
        _transactionRepository = transactionRepository;
    }
    
    public async Task<Transaction?> ProcessPaymentAsync(Guid orderId, decimal amount)
    {
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var paymentResult = await _paymentGateway.ChargeAsync(amount);
                
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Amount = amount,
                    PaymentId = paymentResult.Id,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _transactionRepository.InsertAsync(transaction);
                return transaction;
            },
            key: $"payment-{orderId}",
            timeWindow: TimeSpan.FromSeconds(30) // Block for 30 seconds
        );
    }
}
```

### Form Submission

```csharp
public class FormService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IRepository<FormSubmission, Guid> _submissionRepository;
    
    public FormService(
        IRaceCondition raceCondition,
        IRepository<FormSubmission, Guid> submissionRepository)
    {
        _raceCondition = raceCondition;
        _submissionRepository = submissionRepository;
    }
    
    public async Task<FormSubmission?> SubmitFormAsync(Guid userId, FormData data)
    {
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var submission = new FormSubmission
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Data = data,
                    SubmittedAt = DateTime.UtcNow
                };
                
                await _submissionRepository.InsertAsync(submission);
                
                // Send confirmation email
                await SendConfirmationEmailAsync(userId);
                
                return submission;
            },
            key: $"form-submit-{userId}",
            timeWindow: TimeSpan.FromSeconds(5) // Block double-click
        );
    }
}
```

### API Rate Limiting Per User

```csharp
public class ApiService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IExternalApiClient _apiClient;
    
    public ApiService(
        IRaceCondition raceCondition,
        IExternalApiClient apiClient)
    {
        _raceCondition = raceCondition;
        _apiClient = apiClient;
    }
    
    public async Task<ApiResponse?> CallExternalApiAsync(Guid userId, ApiRequest request)
    {
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var response = await _apiClient.SendAsync(request);
                return response;
            },
            key: $"api-call-{userId}",
            timeWindow: TimeSpan.FromSeconds(1) // Max 1 call per second
        );
    }
}
```

### Prevent Concurrent Seat Booking

```csharp
public class BookingService
{
    private readonly IRaceCondition _raceCondition;
    private readonly IRepository<Seat, Guid> _seatRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    
    public BookingService(
        IRaceCondition raceCondition,
        IRepository<Seat, Guid> seatRepository,
        IRepository<Booking, Guid> bookingRepository)
    {
        _raceCondition = raceCondition;
        _seatRepository = seatRepository;
        _bookingRepository = bookingRepository;
    }
    
    public async Task<Booking?> BookSeatAsync(Guid seatId, Guid userId)
    {
        return await _raceCondition.ExecuteAsync(
            async () =>
            {
                var seat = await _seatRepository.GetAsync(seatId);
                
                if (seat.IsBooked)
                    throw new InvalidOperationException("Seat already booked");
                
                seat.IsBooked = true;
                await _seatRepository.UpdateAsync(seat);
                
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    SeatId = seatId,
                    UserId = userId,
                    BookedAt = DateTime.UtcNow
                };
                
                await _bookingRepository.InsertAsync(booking);
                return booking;
            },
            key: $"seat-{seatId}",
            timeWindow: TimeSpan.FromSeconds(10)
        );
    }
}
```

---

## Difference from Async Lock

### Async Lock (Queue Execution)

```csharp
// Async Lock: ALL requests execute (queued)
await _lock.ExecuteAsync(async () =>
{
    // Request 1: Executes immediately
    // Request 2: Waits, then executes
    // Request 3: Waits, then executes
}, key: "item-21");
```

**Result:** All 3 requests execute in order

### Race Condition (Block Duplicates)

```csharp
// Race Condition: ONLY FIRST executes (others blocked)
await _raceCondition.ExecuteAsync(async () =>
{
    // Request 1: Executes
    // Request 2: Returns null (blocked)
    // Request 3: Returns null (blocked)
}, key: "item-21", timeWindow: TimeSpan.FromSeconds(5));
```

**Result:** Only first request executes, others return `null`

---

## Use Cases Comparison

| Scenario | Use Async Lock | Use Race Condition |
|----------|---------------|-------------------|
| **Purchase item** | If you want to queue purchases | If you want to block duplicates ✅ |
| **Update profile** | If all updates should apply ✅ | If only first update matters |
| **Payment** | If retry payments should queue | If duplicates should be blocked ✅ |
| **Seat booking** | If you want to queue bookings | If only first booking wins ✅ |
| **Form submit** | If resubmits should queue | If duplicates should be blocked ✅ |

---

## When to Use

### Use Race Condition When:
- ✅ Prevent duplicate purchases
- ✅ Block double-click submissions
- ✅ Allow only first payment
- ✅ Debounce user actions
- ✅ Rate limit API calls per user

### Use Async Lock Instead When:
- ❌ Need to queue all operations (not block them)
- ❌ All requests should eventually execute
- ❌ Need ordered execution

---

## Best Practices

- ✅ **Choose appropriate time window**: Long enough to cover operation + network delay
- ✅ **Use specific keys**: `"purchase-{itemId}-{userId}"` not just `"purchase"`
- ✅ **Handle null results**: Inform user about duplicate request
- ✅ **Log blocked requests**: Monitor for potential issues
- ✅ **Test time windows**: Ensure they match your use case

---

## Benefits

- ✅ **Prevents Duplicates**: Only first request executes
- ✅ **Simple API**: Just wrap with `ExecuteAsync()`
- ✅ **Key-Based**: Different resources don't block each other
- ✅ **Time Window**: Automatic expiration
- ✅ **Thread-Safe**: Works with concurrent requests

---

## Related Tools

- **[Async Lock](https://rystem.net/mcp/tools/rystem-async-lock.md)** - Queue operations, execute all
- **[Concurrency Control](https://rystem.net/mcp/resources/concurrency.md)** - Best practices
- **[Background Jobs](https://rystem.net/mcp/tools/rystem-backgroundjob.md)** - Scheduled tasks

---

## References

- **NuGet Package**: [Rystem.Concurrency](https://www.nuget.org/packages/Rystem.Concurrency) v9.1.3
- **Documentation**: https://rystem.net
- **Wikipedia**: https://en.wikipedia.org/wiki/Race_condition
- **GitHub**: https://github.com/KeyserDSoze/Rystem
