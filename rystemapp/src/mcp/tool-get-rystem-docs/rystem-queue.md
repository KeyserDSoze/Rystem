---
title: In-Memory Queue
description: Buffer operations in-memory and process in batches - collect items until buffer size or time limit is reached, then execute batch processing with FIFO/LIFO support
---

# In-Memory Queue

Buffer operations **in-memory** and process them **in batches** based on size or time limits.

**Use Cases:**
- Batch insert to database
- Bulk email sending
- Event aggregation
- Log buffering
- API request batching
- Data pipeline buffering

---

## How It Works

Items are added to an **in-memory queue** and processed when:
1. **Buffer size reached**: Queue has `MaximumBuffer` items
2. **Time limit reached**: `MaximumRetentionCronFormat` time expires

**Example:**
- `MaximumBuffer = 1000` → Process when 1000 items in queue
- `MaximumRetentionCronFormat = "*/3 * * * * *"` → Process every 3 seconds (even if < 1000 items)
- `BackgroundJobCronFormat = "*/1 * * * * *"` → Check every 1 second if processing needed

---

## Installation

```bash
dotnet add package Rystem.Queue --version 9.1.3
```

---

## Configuration

### FIFO Queue (First In First Out)

```csharp
builder.Services.AddMemoryQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer = 1000;                           // Process after 1000 items
    options.MaximumRetentionCronFormat = "*/3 * * * * *";   // Process after 3 seconds
    options.BackgroundJobCronFormat = "*/1 * * * * *";      // Check every 1 second
});
```

### LIFO Stack (Last In First Out)

```csharp
builder.Services.AddMemoryStackQueue<Sample, SampleQueueManager>(options =>
{
    options.MaximumBuffer = 1000;
    options.MaximumRetentionCronFormat = "*/3 * * * * *";
    options.BackgroundJobCronFormat = "*/1 * * * * *";
});
```

---

## Queue Manager

Create a manager to process batches:

```csharp
public class SampleQueueManager : IQueueManager<Sample>
{
    private readonly IRepository<Sample, Guid> _repository;
    private readonly ILogger<SampleQueueManager> _logger;
    
    public SampleQueueManager(
        IRepository<Sample, Guid> repository,
        ILogger<SampleQueueManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<Sample> items)
    {
        // Process batch
        var itemList = items.ToList();
        _logger.LogInformation("Processing {Count} items", itemList.Count);
        
        foreach (var item in itemList)
        {
            await _repository.InsertAsync(item);
        }
        
        _logger.LogInformation("Batch processing completed");
    }
}
```

---

## Warm Up

Start the background job:

```csharp
var app = builder.Build();

// Start queue background job
await app.Services.WarmUpAsync();

app.Run();
```

---

## Usage

### Add Items to Queue

```csharp
public class OrderService
{
    private readonly IQueue<OrderEvent> _queue;
    
    public OrderService(IQueue<OrderEvent> queue)
    {
        _queue = queue;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        // Add to queue (non-blocking)
        await _queue.AddAsync(new OrderEvent
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Timestamp = DateTime.UtcNow
        });
        
        // Returns immediately, batch processing happens later
    }
}
```

### Check Queue Status

```csharp
var count = await _queue.CountAsync();
Console.WriteLine($"Queue has {count} items");
```

---

## Complete Example

```csharp
using Rystem.Queue;

var builder = WebApplication.CreateBuilder(args);

// Configure queue
builder.Services.AddMemoryQueue<LogEntry, LogBatchProcessor>(options =>
{
    options.MaximumBuffer = 100;                           // Process after 100 logs
    options.MaximumRetentionCronFormat = "*/5 * * * * *";  // Process after 5 seconds
    options.BackgroundJobCronFormat = "*/1 * * * * *";     // Check every 1 second
});

builder.Services.AddRepository<LogEntry, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

var app = builder.Build();

// Start queue processing
await app.Services.WarmUpAsync();

// API endpoint
app.MapPost("/log", async (LogEntry log, IQueue<LogEntry> queue) =>
{
    await queue.AddAsync(log);
    return Results.Ok("Log queued");
});

app.Run();

// ============================================
// LogBatchProcessor.cs
// ============================================
public class LogBatchProcessor : IQueueManager<LogEntry>
{
    private readonly IRepository<LogEntry, Guid> _logRepository;
    private readonly ILogger<LogBatchProcessor> _logger;
    
    public LogBatchProcessor(
        IRepository<LogEntry, Guid> logRepository,
        ILogger<LogBatchProcessor> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<LogEntry> items)
    {
        var logs = items.ToList();
        
        _logger.LogInformation("Processing batch of {Count} logs", logs.Count);
        
        // Bulk insert
        foreach (var log in logs)
        {
            await _logRepository.InsertAsync(log);
        }
        
        _logger.LogInformation("Batch insert completed");
    }
}

public class LogEntry
{
    public Guid Id { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## Real-World Examples

### Batch Email Sending

```csharp
// Configuration
builder.Services.AddMemoryQueue<EmailMessage, EmailBatchSender>(options =>
{
    options.MaximumBuffer = 50;                            // Send 50 emails at once
    options.MaximumRetentionCronFormat = "0 */10 * * * *"; // Send every 10 minutes
    options.BackgroundJobCronFormat = "0 */1 * * * * *";   // Check every minute
});

// Queue Manager
public class EmailBatchSender : IQueueManager<EmailMessage>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailBatchSender> _logger;
    
    public EmailBatchSender(IEmailService emailService, ILogger<EmailBatchSender> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<EmailMessage> items)
    {
        var emails = items.ToList();
        
        _logger.LogInformation("Sending batch of {Count} emails", emails.Count);
        
        await _emailService.SendBulkAsync(emails);
        
        _logger.LogInformation("Email batch sent successfully");
    }
}

// Usage
public class NotificationService
{
    private readonly IQueue<EmailMessage> _emailQueue;
    
    public NotificationService(IQueue<EmailMessage> emailQueue)
    {
        _emailQueue = emailQueue;
    }
    
    public async Task SendWelcomeEmailAsync(User user)
    {
        await _emailQueue.AddAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Welcome!",
            Body = $"Hello {user.Name}, welcome to our platform!"
        });
    }
}
```

### Event Aggregation

```csharp
// Configuration
builder.Services.AddMemoryQueue<AnalyticsEvent, AnalyticsProcessor>(options =>
{
    options.MaximumBuffer = 1000;                          // Process 1000 events
    options.MaximumRetentionCronFormat = "*/30 * * * * *"; // Process every 30 seconds
    options.BackgroundJobCronFormat = "*/10 * * * * *";    // Check every 10 seconds
});

// Queue Manager
public class AnalyticsProcessor : IQueueManager<AnalyticsEvent>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnalyticsProcessor> _logger;
    
    public AnalyticsProcessor(
        IHttpClientFactory httpClientFactory,
        ILogger<AnalyticsProcessor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<AnalyticsEvent> items)
    {
        var events = items.ToList();
        
        _logger.LogInformation("Processing {Count} analytics events", events.Count);
        
        var client = _httpClientFactory.CreateClient("Analytics");
        await client.PostAsJsonAsync("/batch", events);
        
        _logger.LogInformation("Analytics batch sent");
    }
}

// Usage
public class TrackingService
{
    private readonly IQueue<AnalyticsEvent> _analyticsQueue;
    
    public TrackingService(IQueue<AnalyticsEvent> analyticsQueue)
    {
        _analyticsQueue = analyticsQueue;
    }
    
    public async Task TrackPageViewAsync(Guid userId, string page)
    {
        await _analyticsQueue.AddAsync(new AnalyticsEvent
        {
            UserId = userId,
            EventType = "PageView",
            Page = page,
            Timestamp = DateTime.UtcNow
        });
    }
}
```

### Database Bulk Insert

```csharp
// Configuration
builder.Services.AddMemoryQueue<Product, ProductBatchInserter>(options =>
{
    options.MaximumBuffer = 500;                           // Bulk insert 500 products
    options.MaximumRetentionCronFormat = "0 */5 * * * *";  // Insert every 5 minutes
    options.BackgroundJobCronFormat = "0 */1 * * * * *";   // Check every minute
});

// Queue Manager
public class ProductBatchInserter : IQueueManager<Product>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ProductBatchInserter> _logger;
    
    public ProductBatchInserter(AppDbContext dbContext, ILogger<ProductBatchInserter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<Product> items)
    {
        var products = items.ToList();
        
        _logger.LogInformation("Bulk inserting {Count} products", products.Count);
        
        // EF Core bulk insert
        await _dbContext.Products.AddRangeAsync(products);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Bulk insert completed");
    }
}

// Usage
public class ImportService
{
    private readonly IQueue<Product> _productQueue;
    
    public ImportService(IQueue<Product> productQueue)
    {
        _productQueue = productQueue;
    }
    
    public async Task ImportProductAsync(Product product)
    {
        await _productQueue.AddAsync(product);
    }
}
```

### Log Buffering

```csharp
// Configuration
builder.Services.AddMemoryQueue<AuditLog, AuditLogWriter>(options =>
{
    options.MaximumBuffer = 200;                           // Write 200 logs
    options.MaximumRetentionCronFormat = "*/10 * * * * *"; // Write every 10 seconds
    options.BackgroundJobCronFormat = "*/2 * * * * *";     // Check every 2 seconds
});

// Queue Manager
public class AuditLogWriter : IQueueManager<AuditLog>
{
    private readonly IContentRepository _blobStorage;
    private readonly ILogger<AuditLogWriter> _logger;
    
    public AuditLogWriter(
        IContentRepositoryFactory factory,
        ILogger<AuditLogWriter> logger)
    {
        _blobStorage = factory.Create("audit-logs");
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<AuditLog> items)
    {
        var logs = items.ToList();
        
        _logger.LogInformation("Writing {Count} audit logs", logs.Count);
        
        var fileName = $"audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var json = JsonSerializer.Serialize(logs);
        var data = Encoding.UTF8.GetBytes(json);
        
        await _blobStorage.UploadAsync($"logs/{fileName}", data);
        
        _logger.LogInformation("Audit logs written to {FileName}", fileName);
    }
}
```

### API Request Batching

```csharp
// Configuration
builder.Services.AddMemoryQueue<SyncRequest, ApiSyncBatcher>(options =>
{
    options.MaximumBuffer = 100;                           // Batch 100 requests
    options.MaximumRetentionCronFormat = "*/20 * * * * *"; // Sync every 20 seconds
    options.BackgroundJobCronFormat = "*/5 * * * * *";     // Check every 5 seconds
});

// Queue Manager
public class ApiSyncBatcher : IQueueManager<SyncRequest>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiSyncBatcher> _logger;
    
    public ApiSyncBatcher(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiSyncBatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task ManageAsync(IEnumerable<SyncRequest> items)
    {
        var requests = items.ToList();
        
        _logger.LogInformation("Syncing {Count} requests", requests.Count);
        
        var client = _httpClientFactory.CreateClient("ExternalApi");
        
        var response = await client.PostAsJsonAsync("/batch-sync", new
        {
            Items = requests,
            Timestamp = DateTime.UtcNow
        });
        
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Batch sync completed");
    }
}
```

---

## FIFO vs LIFO

### FIFO (Queue) - First In First Out

```csharp
services.AddMemoryQueue<Item, ItemProcessor>(options => { ... });

// Items processed in order added:
// Add: Item1 → Item2 → Item3
// Process: Item1 → Item2 → Item3
```

**Use when:** Order matters (logs, events, messages)

### LIFO (Stack) - Last In First Out

```csharp
services.AddMemoryStackQueue<Item, ItemProcessor>(options => { ... });

// Items processed in reverse order:
// Add: Item1 → Item2 → Item3
// Process: Item3 → Item2 → Item1
```

**Use when:** Most recent items should be processed first

---

## Custom Integration (Distributed Queue)

Create custom integration for distributed queues (Azure Storage Queue, Event Hub, Service Bus):

```csharp
// Configuration
builder.Services.AddQueueIntegration<Sample, SampleQueueManager, AzureStorageQueueIntegration>(
    options =>
    {
        options.MaximumBuffer = 1000;
        options.MaximumRetentionCronFormat = "*/3 * * * * *";
        options.BackgroundJobCronFormat = "*/1 * * * * *";
    }
);

// Custom Integration
public class AzureStorageQueueIntegration : IQueueIntegration<Sample>
{
    private readonly QueueClient _queueClient;
    
    public AzureStorageQueueIntegration(IConfiguration configuration)
    {
        _queueClient = new QueueClient(
            configuration["Azure:StorageConnectionString"],
            "sample-queue"
        );
    }
    
    public async Task AddAsync(Sample item)
    {
        var message = JsonSerializer.Serialize(item);
        await _queueClient.SendMessageAsync(message);
    }
    
    public async Task<int> CountAsync()
    {
        var properties = await _queueClient.GetPropertiesAsync();
        return properties.Value.ApproximateMessagesCount;
    }
    
    // Implement other IQueueIntegration methods...
}
```

---

## Configuration Options

```csharp
public class QueueOptions
{
    /// <summary>
    /// Maximum items in queue before processing
    /// </summary>
    public int MaximumBuffer { get; set; } = 1000;
    
    /// <summary>
    /// CRON: Maximum time before processing (6 fields)
    /// Example: "*/3 * * * * *" = every 3 seconds
    /// </summary>
    public string MaximumRetentionCronFormat { get; set; }
    
    /// <summary>
    /// CRON: How often to check if processing needed (6 fields)
    /// Should be <= MaximumRetentionCronFormat
    /// Example: "*/1 * * * * *" = every 1 second
    /// </summary>
    public string BackgroundJobCronFormat { get; set; }
}
```

---

## IQueueManager Interface

```csharp
public interface IQueueManager<T>
{
    /// <summary>
    /// Process batch of items
    /// Called when MaximumBuffer reached or MaximumRetention expired
    /// </summary>
    Task ManageAsync(IEnumerable<T> items);
}
```

---

## IQueue Interface

```csharp
public interface IQueue<T>
{
    /// <summary>
    /// Add item to queue
    /// </summary>
    Task AddAsync(T item);
    
    /// <summary>
    /// Get current queue count
    /// </summary>
    Task<int> CountAsync();
}
```

---

## Best Practices

- ✅ **Set appropriate buffer size**: Balance memory usage vs batch efficiency
- ✅ **Use shorter check intervals**: `BackgroundJobCronFormat` ≤ `MaximumRetentionCronFormat`
- ✅ **Handle failures**: Implement retry logic in `ManageAsync`
- ✅ **Log batch processing**: Track queue size, processing time
- ✅ **Monitor memory**: Large buffers can consume significant memory
- ✅ **Use FIFO for ordered processing**: Logs, events, messages
- ✅ **Use LIFO for recency priority**: Notifications, cache updates

---

## When to Use

### Use In-Memory Queue When:
- ✅ Batching database inserts
- ✅ Bulk email sending
- ✅ Event aggregation
- ✅ Log buffering
- ✅ API request batching
- ✅ Single instance application

### Use Distributed Queue Instead When:
- ❌ Multiple app instances (use Azure Queue/Service Bus)
- ❌ Need persistence (queue survives app restart)
- ❌ Cross-service communication
- ❌ Guaranteed delivery required

---

## Benefits

- ✅ **Performance**: Batch processing reduces database round-trips
- ✅ **Efficiency**: Aggregate API calls, reduce network overhead
- ✅ **Simplicity**: Easy configuration with DI
- ✅ **Flexible**: Time-based or size-based processing
- ✅ **Dependency Injection**: Queue manager supports DI

---

## Related Tools

- **[Background Jobs](https://rystem.net/mcp/tools/rystem-backgroundjob.md)** - Scheduled recurring tasks
- **[Async Lock](https://rystem.net/mcp/tools/rystem-async-lock.md)** - Prevent concurrent processing
- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Batch database operations

---

## References

- **NuGet Package**: [Rystem.Queue](https://www.nuget.org/packages/Rystem.Queue) v9.1.3
- **Documentation**: https://rystem.net
- **CRON Helper**: https://crontab.guru/
- **GitHub**: https://github.com/KeyserDSoze/Rystem
