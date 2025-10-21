# Background Jobs

Schedule **recurring tasks** in your .NET application using **CRON expressions**.

**Use Cases:**
- Data synchronization
- Report generation
- Cleanup tasks
- Email/notification sending
- API polling
- Cache warming
- Database maintenance

---

## Installation

```bash
dotnet add package Rystem.BackgroundJob --version 9.1.3
```

---

## Configuration

### Basic Setup

```csharp
builder.Services.AddBackgroundJob<EmailNotificationJob>(options =>
{
    options.Cron = "0 */5 * * * *"; // Every 5 minutes
    options.RunImmediately = true;  // Run on startup
});
```

### CRON Expression Format

```
┌───────────── second (0 - 59)
│ ┌───────────── minute (0 - 59)
│ │ ┌───────────── hour (0 - 23)
│ │ │ ┌───────────── day of month (1 - 31)
│ │ │ │ ┌───────────── month (1 - 12)
│ │ │ │ │ ┌───────────── day of week (0 - 6) (Sunday=0)
│ │ │ │ │ │
│ │ │ │ │ │
* * * * * *
```

**Common Examples:**
- `0 */5 * * * *` - Every 5 minutes
- `0 0 * * * *` - Every hour
- `0 0 3 * * *` - Every day at 3:00 AM
- `0 0 0 * * 1` - Every Monday at midnight
- `0 30 9-17 * * 1-5` - Monday-Friday, 9:30 AM to 5:30 PM (every hour)

🔗 **CRON Helper**: https://crontab.guru/

---

## Create a Background Job

Implement `IBackgroundJob` interface:

```csharp
using System.Timers;

public class DataSyncJob : IBackgroundJob
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IExternalApiClient _apiClient;
    private readonly ILogger<DataSyncJob> _logger;
    
    public DataSyncJob(
        IRepository<User, Guid> userRepository,
        IExternalApiClient apiClient,
        ILogger<DataSyncJob> logger)
    {
        _userRepository = userRepository;
        _apiClient = apiClient;
        _logger = logger;
    }
    
    // Main method called when CRON fires
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data sync...");
        
        var externalUsers = await _apiClient.GetUsersAsync(cancellationToken);
        
        foreach (var externalUser in externalUsers)
        {
            var user = await _userRepository
                .Query()
                .Where(x => x.ExternalId == externalUser.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (user == null)
            {
                // Create new user
                await _userRepository.InsertAsync(new User
                {
                    Id = Guid.NewGuid(),
                    ExternalId = externalUser.Id,
                    Email = externalUser.Email,
                    Name = externalUser.Name,
                    SyncedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                // Update existing user
                user.Email = externalUser.Email;
                user.Name = externalUser.Name;
                user.SyncedAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }
        
        _logger.LogInformation("Data sync completed: {Count} users", externalUsers.Count);
    }
    
    // Called when exception occurs in ActionToDoAsync
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error during data sync");
        return Task.CompletedTask;
    }
}
```

---

## Register and Warm Up

### Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register multiple background jobs
builder.Services
    .AddBackgroundJob<DataSyncJob>(options =>
    {
        options.Cron = "0 0 */6 * * *"; // Every 6 hours
        options.RunImmediately = true;
    })
    .AddBackgroundJob<CleanupJob>(options =>
    {
        options.Cron = "0 0 2 * * *"; // Daily at 2:00 AM
    })
    .AddBackgroundJob<ReportJob>(options =>
    {
        options.Cron = "0 0 9 * * 1"; // Every Monday at 9:00 AM
    });
```

### Warm Up (Start Jobs)

```csharp
var app = builder.Build();

// Start all background jobs
await app.Services.WarmUpAsync();

app.Run();
```

---

## Multiple Jobs with Same Class

Use the `Key` property to register multiple instances:

```csharp
builder.Services
    .AddBackgroundJob<NotificationJob>(options =>
    {
        options.Cron = "0 0 9 * * *";
        options.Key = "morning-notifications";
    })
    .AddBackgroundJob<NotificationJob>(options =>
    {
        options.Cron = "0 0 18 * * *";
        options.Key = "evening-notifications";
    });
```

---

## Complete Example

```csharp
using System.Timers;
using RepositoryFramework;

// Program.cs
var builder = WebApplication.WebBuilder(args);

builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

builder.Services.AddRepository<EmailLog, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

builder.Services
    .AddBackgroundJob<OrderStatusUpdateJob>(options =>
    {
        options.Cron = "0 */10 * * * *"; // Every 10 minutes
        options.RunImmediately = false;
    })
    .AddBackgroundJob<DailyReportJob>(options =>
    {
        options.Cron = "0 0 8 * * *"; // Daily at 8:00 AM
        options.RunImmediately = false;
    })
    .AddBackgroundJob<CacheWarmUpJob>(options =>
    {
        options.Cron = "0 */30 * * * *"; // Every 30 minutes
        options.RunImmediately = true; // Run on startup
    })
    .AddBackgroundJob<CleanupOldLogsJob>(options =>
    {
        options.Cron = "0 0 3 * * *"; // Daily at 3:00 AM
    });

var app = builder.Build();

// Warm up background jobs
await app.Services.WarmUpAsync();

app.Run();

// ============================================
// OrderStatusUpdateJob.cs
// ============================================
public class OrderStatusUpdateJob : IBackgroundJob
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly ILogger<OrderStatusUpdateJob> _logger;
    
    public OrderStatusUpdateJob(
        IRepository<Order, Guid> orderRepository,
        ILogger<OrderStatusUpdateJob> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var pendingOrders = await _orderRepository
            .Query()
            .Where(x => x.Status == OrderStatus.Processing 
                     && x.UpdatedAt < DateTime.UtcNow.AddHours(-24))
            .ToListAsync(cancellationToken);
        
        foreach (var order in pendingOrders)
        {
            order.Status = OrderStatus.Cancelled;
            order.CancelledReason = "Timeout - no update in 24 hours";
            await _orderRepository.UpdateAsync(order, cancellationToken);
        }
        
        _logger.LogInformation(
            "Order status update completed: {Count} orders cancelled", 
            pendingOrders.Count
        );
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error updating order statuses");
        return Task.CompletedTask;
    }
}

// ============================================
// DailyReportJob.cs
// ============================================
public class DailyReportJob : IBackgroundJob
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<DailyReportJob> _logger;
    
    public DailyReportJob(
        IRepository<Order, Guid> orderRepository,
        IEmailService emailService,
        ILogger<DailyReportJob> logger)
    {
        _orderRepository = orderRepository;
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        
        var orders = await _orderRepository
            .Query()
            .Where(x => x.CreatedAt >= yesterday && x.CreatedAt < yesterday.AddDays(1))
            .ToListAsync(cancellationToken);
        
        var report = new
        {
            Date = yesterday.ToString("yyyy-MM-dd"),
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(x => x.Total),
            CompletedOrders = orders.Count(x => x.Status == OrderStatus.Completed),
            CancelledOrders = orders.Count(x => x.Status == OrderStatus.Cancelled)
        };
        
        await _emailService.SendReportAsync(
            "admin@company.com",
            $"Daily Report - {report.Date}",
            report,
            cancellationToken
        );
        
        _logger.LogInformation("Daily report sent: {TotalOrders} orders", report.TotalOrders);
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error generating daily report");
        return Task.CompletedTask;
    }
}

// ============================================
// CleanupOldLogsJob.cs
// ============================================
public class CleanupOldLogsJob : IBackgroundJob
{
    private readonly IRepository<EmailLog, Guid> _emailLogRepository;
    private readonly ILogger<CleanupOldLogsJob> _logger;
    
    public CleanupOldLogsJob(
        IRepository<EmailLog, Guid> emailLogRepository,
        ILogger<CleanupOldLogsJob> logger)
    {
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep last 90 days
        
        var oldLogs = await _emailLogRepository
            .Query()
            .Where(x => x.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);
        
        foreach (var log in oldLogs)
        {
            await _emailLogRepository.DeleteAsync(log.Id, cancellationToken);
        }
        
        _logger.LogInformation("Cleanup completed: {Count} old logs deleted", oldLogs.Count);
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error during cleanup");
        return Task.CompletedTask;
    }
}
```

---

## Real-World Examples

### API Data Polling

```csharp
public class WeatherDataPollingJob : IBackgroundJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRepository<WeatherData, Guid> _weatherRepository;
    private readonly ILogger<WeatherDataPollingJob> _logger;
    
    public WeatherDataPollingJob(
        IHttpClientFactory httpClientFactory,
        IRepository<WeatherData, Guid> weatherRepository,
        ILogger<WeatherDataPollingJob> logger)
    {
        _httpClientFactory = httpClientFactory;
        _weatherRepository = weatherRepository;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("WeatherApi");
        
        var cities = new[] { "New York", "London", "Tokyo", "Sydney" };
        
        foreach (var city in cities)
        {
            var response = await client.GetFromJsonAsync<WeatherApiResponse>(
                $"/weather?city={city}",
                cancellationToken
            );
            
            if (response != null)
            {
                await _weatherRepository.InsertAsync(new WeatherData
                {
                    Id = Guid.NewGuid(),
                    City = city,
                    Temperature = response.Temperature,
                    Humidity = response.Humidity,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);
            }
        }
        
        _logger.LogInformation("Weather data updated for {Count} cities", cities.Length);
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error polling weather data");
        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddBackgroundJob<WeatherDataPollingJob>(options =>
{
    options.Cron = "0 */15 * * * *"; // Every 15 minutes
});
```

### Database Backup

```csharp
public class DatabaseBackupJob : IBackgroundJob
{
    private readonly IContentRepository _blobStorage;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupJob> _logger;
    
    public DatabaseBackupJob(
        IContentRepositoryFactory factory,
        IConfiguration configuration,
        ILogger<DatabaseBackupJob> logger)
    {
        _blobStorage = factory.Create("backup-storage");
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupFileName = $"database-backup-{timestamp}.bak";
        
        // Create backup (example for SQL Server)
        var backupPath = Path.Combine(Path.GetTempPath(), backupFileName);
        
        // Execute backup command (simplified)
        await ExecuteDatabaseBackupAsync(connectionString, backupPath, cancellationToken);
        
        // Upload to blob storage
        var backupData = await File.ReadAllBytesAsync(backupPath, cancellationToken);
        await _blobStorage.UploadAsync(
            $"backups/{backupFileName}",
            backupData,
            new ContentRepositoryOptions
            {
                Tags = new Dictionary<string, string>
                {
                    { "type", "database-backup" },
                    { "date", DateTime.UtcNow.ToString("o") }
                }
            }
        );
        
        // Cleanup local file
        File.Delete(backupPath);
        
        _logger.LogInformation("Database backup completed: {FileName}", backupFileName);
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error during database backup");
        return Task.CompletedTask;
    }
    
    private Task ExecuteDatabaseBackupAsync(string connectionString, string path, CancellationToken ct)
    {
        // Implementation specific to your database
        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddBackgroundJob<DatabaseBackupJob>(options =>
{
    options.Cron = "0 0 2 * * *"; // Daily at 2:00 AM
});
```

### Email Digest

```csharp
public class WeeklyDigestJob : IBackgroundJob
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IRepository<Article, Guid> _articleRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<WeeklyDigestJob> _logger;
    
    public WeeklyDigestJob(
        IRepository<User, Guid> userRepository,
        IRepository<Article, Guid> articleRepository,
        IEmailService emailService,
        ILogger<WeeklyDigestJob> logger)
    {
        _userRepository = userRepository;
        _articleRepository = articleRepository;
        _emailService = emailService;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        var lastWeek = DateTime.UtcNow.AddDays(-7);
        
        var topArticles = await _articleRepository
            .Query()
            .Where(x => x.PublishedAt >= lastWeek)
            .OrderByDescending(x => x.Views)
            .Take(10)
            .ToListAsync(cancellationToken);
        
        var subscribedUsers = await _userRepository
            .Query()
            .Where(x => x.EmailSubscription == true)
            .ToListAsync(cancellationToken);
        
        foreach (var user in subscribedUsers)
        {
            await _emailService.SendDigestAsync(
                user.Email,
                user.Name,
                topArticles,
                cancellationToken
            );
        }
        
        _logger.LogInformation(
            "Weekly digest sent to {Count} users with {Articles} articles",
            subscribedUsers.Count,
            topArticles.Count
        );
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error sending weekly digest");
        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddBackgroundJob<WeeklyDigestJob>(options =>
{
    options.Cron = "0 0 9 * * 1"; // Every Monday at 9:00 AM
});
```

### Cache Warming

```csharp
public class CacheWarmUpJob : IBackgroundJob
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheWarmUpJob> _logger;
    
    public CacheWarmUpJob(
        IRepository<Product, Guid> productRepository,
        IMemoryCache cache,
        ILogger<CacheWarmUpJob> logger)
    {
        _productRepository = productRepository;
        _cache = cache;
        _logger = logger;
    }
    
    public async Task ActionToDoAsync(CancellationToken cancellationToken)
    {
        // Load top 100 products into cache
        var topProducts = await _productRepository
            .Query()
            .OrderByDescending(x => x.Views)
            .Take(100)
            .ToListAsync(cancellationToken);
        
        foreach (var product in topProducts)
        {
            _cache.Set(
                $"product:{product.Id}",
                product,
                TimeSpan.FromHours(1)
            );
        }
        
        _logger.LogInformation("Cache warmed up with {Count} products", topProducts.Count);
    }
    
    public Task OnException(Exception exception)
    {
        _logger.LogError(exception, "Error warming up cache");
        return Task.CompletedTask;
    }
}

// Registration
builder.Services.AddBackgroundJob<CacheWarmUpJob>(options =>
{
    options.Cron = "0 */30 * * * *"; // Every 30 minutes
    options.RunImmediately = true;   // Run on startup
});
```

---

## Background Job Options

```csharp
public class BackgroundJobOptions
{
    /// <summary>
    /// CRON expression (6 fields: second minute hour day month dayOfWeek)
    /// </summary>
    public string Cron { get; set; }
    
    /// <summary>
    /// Run job immediately on startup (before first CRON trigger)
    /// </summary>
    public bool RunImmediately { get; set; } = false;
    
    /// <summary>
    /// Unique key to allow multiple jobs with same class
    /// </summary>
    public string? Key { get; set; }
}
```

---

## IBackgroundJob Interface

```csharp
namespace System.Timers
{
    public interface IBackgroundJob
    {
        /// <summary>
        /// Main method called when CRON expression fires
        /// </summary>
        Task ActionToDoAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Called when exception occurs in ActionToDoAsync
        /// </summary>
        Task OnException(Exception exception);
    }
}
```

---

## Best Practices

- ✅ **Use CancellationToken**: Respect cancellation for graceful shutdown
- ✅ **Handle Exceptions**: Always implement `OnException` with logging
- ✅ **Keep Jobs Lightweight**: Long-running tasks should process in batches
- ✅ **Use Dependency Injection**: Inject repositories, services, loggers
- ✅ **Test CRON Expressions**: Use https://crontab.guru/ to validate
- ✅ **Monitor Job Execution**: Log start, completion, and errors
- ✅ **Avoid Overlapping**: Ensure job completes before next trigger

---

## Related Tools

- **[Concurrency Control](https://rystem.net/mcp/resources/concurrency.md)** - Prevent duplicate job execution
- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Data access in jobs
- **[Content Repository](https://rystem.net/mcp/tools/content-repository.md)** - File storage in jobs

---

## References

- **NuGet Package**: [Rystem.BackgroundJob](https://www.nuget.org/packages/Rystem.BackgroundJob) v9.1.3
- **Documentation**: https://rystem.net
- **CRON Helper**: https://crontab.guru/
- **GitHub**: https://github.com/KeyserDSoze/Rystem
