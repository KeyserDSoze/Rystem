# Dependency Injection Factory Pattern

Advanced **factory pattern** for **Rystem.DependencyInjection** to register and resolve multiple implementations of the same interface by **name**.

---

## Installation

```bash
dotnet add package Rystem.DependencyInjection --version 9.1.3
```

---

## Why Use Factory Pattern?

When you need **multiple implementations** of the same interface and want to **choose at runtime** which one to use:

### ❌ Traditional DI Problem

```csharp
// Can only register one implementation
services.AddScoped<IPaymentGateway, StripePaymentGateway>();

// This replaces the previous registration!
services.AddScoped<IPaymentGateway, PayPalPaymentGateway>();
```

### ✅ Factory Pattern Solution

```csharp
// Register multiple implementations by name
services.AddFactory<IPaymentGateway, StripePaymentGateway>(
    "stripe", 
    ServiceLifetime.Scoped
);

services.AddFactory<IPaymentGateway, PayPalPaymentGateway>(
    "paypal", 
    ServiceLifetime.Scoped
);

// Resolve by name at runtime
var factory = serviceProvider.GetRequiredService<IFactory<IPaymentGateway>>();
var stripe = factory.Create("stripe");
var paypal = factory.Create("paypal");
```

---

## Quick Start

### 1. Define Interface and Implementations

```csharp
public interface INotificationService
{
    Task SendAsync(string message);
}

public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string message)
    {
        // Send email
        await Task.CompletedTask;
    }
}

public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string message)
    {
        // Send SMS
        await Task.CompletedTask;
    }
}
```

### 2. Register with Factory

```csharp
services.AddFactory<INotificationService, EmailNotificationService>(
    "email",
    ServiceLifetime.Scoped
);

services.AddFactory<INotificationService, SmsNotificationService>(
    "sms",
    ServiceLifetime.Scoped
);
```

### 3. Resolve by Name

```csharp
public class NotificationController : ControllerBase
{
    private readonly IFactory<INotificationService> _factory;
    
    public NotificationController(IFactory<INotificationService> factory)
    {
        _factory = factory;
    }
    
    [HttpPost("send")]
    public async Task<IActionResult> SendNotification(string type, string message)
    {
        var service = _factory.Create(type); // "email" or "sms"
        await service.SendAsync(message);
        
        return Ok();
    }
}
```

---

## Factory with Options

Pass **configuration options** to each factory service:

### 1. Define Options

```csharp
public class PaymentGatewayOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
}
```

### 2. Implement IServiceWithOptions

```csharp
public class StripePaymentGateway : IPaymentGateway, IServiceWithOptions<PaymentGatewayOptions>
{
    public PaymentGatewayOptions Options { get; set; }
    
    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Use Options.ApiKey and Options.BaseUrl
        var client = new HttpClient { BaseAddress = new Uri(Options.BaseUrl) };
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Options.ApiKey}");
        
        // Process payment
        return true;
    }
}

public class PayPalPaymentGateway : IPaymentGateway, IServiceWithOptions<PaymentGatewayOptions>
{
    public PaymentGatewayOptions Options { get; set; }
    
    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        // Use Options.ApiKey and Options.BaseUrl
        // Process payment
        return true;
    }
}
```

### 3. Register with Options

```csharp
services.AddFactory<IPaymentGateway, StripePaymentGateway, PaymentGatewayOptions>(
    options =>
    {
        options.ApiKey = "sk_test_stripe_key";
        options.BaseUrl = "https://api.stripe.com";
    },
    "stripe",
    ServiceLifetime.Scoped
);

services.AddFactory<IPaymentGateway, PayPalPaymentGateway, PaymentGatewayOptions>(
    options =>
    {
        options.ApiKey = "paypal_api_key";
        options.BaseUrl = "https://api.paypal.com";
    },
    "paypal",
    ServiceLifetime.Scoped
);
```

---

## Service Lifetimes

Factory pattern respects **service lifetimes**: Singleton, Scoped, Transient.

### Complete Example

```csharp
public interface IMyService
{
    string GetName();
    string Id { get; }
}

public class SingletonService : IMyService, IServiceWithOptions<ServiceOptions>
{
    public ServiceOptions Options { get; set; }
    public string Id { get; } = Guid.NewGuid().ToString();
    
    public string GetName() => $"{Options.ServiceName} with id {Id}";
}

public class TransientService : IMyService, IServiceWithOptions<ServiceOptions>
{
    public ServiceOptions Options { get; set; }
    public string Id { get; } = Guid.NewGuid().ToString();
    
    public string GetName() => $"{Options.ServiceName} with id {Id}";
}

public class ScopedService : IMyService, IServiceWithOptions<ServiceOptions>
{
    public ServiceOptions Options { get; set; }
    public string Id { get; } = Guid.NewGuid().ToString();
    
    public string GetName() => $"{Options.ServiceName} with id {Id}";
}

public class ServiceOptions
{
    public string ServiceName { get; set; }
}
```

**Registration:**

```csharp
services.AddFactory<IMyService, SingletonService, ServiceOptions>(
    x => x.ServiceName = "singleton",
    "singleton",
    ServiceLifetime.Singleton
);

services.AddFactory<IMyService, TransientService, ServiceOptions>(
    x => x.ServiceName = "transient",
    "transient",
    ServiceLifetime.Transient
);

services.AddFactory<IMyService, ScopedService, ServiceOptions>(
    x => x.ServiceName = "scoped",
    "scoped",
    ServiceLifetime.Scoped
);
```

**Usage:**

```csharp
var factory1 = serviceProvider.GetService<IFactory<IMyService>>();
var factory2 = serviceProvider.GetService<IFactory<IMyService>>();

var singleton1 = factory1.Create("singleton").Id;
var singleton2 = factory2.Create("singleton").Id;
Assert.Equal(singleton1, singleton2); // ✅ Same instance

var transient1 = factory1.Create("transient").Id;
var transient2 = factory2.Create("transient").Id;
Assert.NotEqual(transient1, transient2); // ✅ Different instances

var scoped1 = factory1.Create("scoped").Id;
var scoped2 = factory2.Create("scoped").Id;
Assert.Equal(scoped1, scoped2); // ✅ Same instance within scope
```

---

## Built Options (Async Factory)

Use **`IServiceOptions<T>`** for **asynchronous configuration building**:

### 1. Define Built Options

```csharp
public class DatabaseConnectionOptions
{
    public string ConnectionString { get; set; }
}

public class BuiltDatabaseOptions : IServiceOptions<DatabaseConnectionOptions>
{
    public string ServerName { get; set; }
    public string DatabaseName { get; set; }
    
    public Task<Func<DatabaseConnectionOptions>> BuildAsync()
    {
        return Task.FromResult(() => new DatabaseConnectionOptions
        {
            ConnectionString = $"Server={ServerName};Database={DatabaseName};..."
        });
    }
}
```

### 2. Register with AddFactoryAsync

```csharp
await services.AddFactoryAsync<IDatabaseService, SqlServerService, BuiltDatabaseOptions, DatabaseConnectionOptions>(
    x =>
    {
        x.ServerName = "localhost";
        x.DatabaseName = "MyDatabase";
    },
    "sqlserver"
);

await services.AddFactoryAsync<IDatabaseService, PostgresService, BuiltDatabaseOptions, DatabaseConnectionOptions>(
    x =>
    {
        x.ServerName = "localhost";
        x.DatabaseName = "MyDatabase";
    },
    "postgres"
);
```

**Use Cases:**
- Fetch secrets from Azure Key Vault
- Load configuration from remote API
- Decrypt configuration values
- Build connection strings dynamically

---

## Decorator Pattern

Add **decorators** to existing services to **wrap functionality** without modifying original code.

### Without Factory

```csharp
public interface ITestService
{
    string GetName();
}

public class TestService : ITestService
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string GetName() => $"TestService {Id}";
}

public class TestServiceDecorator : ITestService, IDecoratorService<ITestService>
{
    public ITestService DecoratedService { get; private set; }
    
    public void SetDecoratedService(ITestService service)
    {
        DecoratedService = service;
    }
    
    public string GetName()
    {
        return $"[DECORATED] {DecoratedService.GetName()}";
    }
}
```

**Registration:**

```csharp
services.AddService<ITestService, TestService>(ServiceLifetime.Scoped);
services.AddDecoration<ITestService, TestServiceDecorator>(null, ServiceLifetime.Scoped);
```

**Usage:**

```csharp
var decorator = provider.GetRequiredService<ITestService>();
Console.WriteLine(decorator.GetName());
// Output: "[DECORATED] TestService abc-123"

var previousService = provider.GetRequiredService<IDecoratedService<ITestService>>();
Console.WriteLine(previousService.GetName());
// Output: "TestService abc-123"
```

### With Factory

Decorate only **specific factory services**:

```csharp
services.AddFactory<IPaymentGateway, StripePaymentGateway>(
    "stripe",
    ServiceLifetime.Scoped
);

services.AddFactory<IPaymentGateway, PayPalPaymentGateway>(
    "paypal",
    ServiceLifetime.Scoped
);

// Decorate only "stripe"
services.AddDecoration<IPaymentGateway, LoggingPaymentGatewayDecorator>(
    "stripe",
    ServiceLifetime.Scoped
);
```

**Usage:**

```csharp
var factory = provider.GetRequiredService<IFactory<IPaymentGateway>>();

var stripe = factory.Create("stripe");
// Returns LoggingPaymentGatewayDecorator wrapping StripePaymentGateway

var paypal = factory.Create("paypal");
// Returns PayPalPaymentGateway (no decoration)

var originalStripe = factory.CreateWithoutDecoration("stripe");
// Returns original StripePaymentGateway without decorator
```

---

## Factory Fallback

Handle **missing factory keys** with fallback services:

### Class-Based Fallback

```csharp
public class PaymentGatewayFallback : IFactoryFallback<IPaymentGateway>
{
    public IPaymentGateway Create(string key)
    {
        // Return default implementation when key not found
        return new DefaultPaymentGateway();
    }
}

services.AddFactoryFallback<IPaymentGateway, PaymentGatewayFallback>();
```

### Action-Based Fallback

```csharp
services.AddActionAsFallbackWithServiceProvider<IPaymentGateway>(fallbackBuilder =>
{
    var logger = fallbackBuilder.ServiceProvider.GetRequiredService<ILogger<PaymentGateway>>();
    logger.LogWarning("Payment gateway {Key} not found, using default", fallbackBuilder.Key);
    
    return new DefaultPaymentGateway();
});
```

**Usage:**

```csharp
var factory = provider.GetRequiredService<IFactory<IPaymentGateway>>();

var stripe = factory.Create("stripe"); // Returns StripePaymentGateway
var unknown = factory.Create("unknown"); // Returns DefaultPaymentGateway from fallback
```

---

## Assembly Scanning

Automatically discover and register services by scanning assemblies.

### Manual Scan

```csharp
public interface IAnything { }

internal class ServiceA : IAnything { }
internal class ServiceB : IAnything { }
internal class ServiceC : IAnything { }

services.Scan<IAnything>(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

### IScannable Interface

```csharp
public interface IAnything { }

internal class ServiceA : IAnything, IScannable<IAnything> { }
internal class ServiceB : IAnything, IScannable<IAnything> { }

services.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

### Override Lifetime

```csharp
internal class ServiceA : IAnything, IScannable<IAnything>, ISingletonScannable { }
internal class ServiceB : IAnything, IScannable<IAnything>, IScopedScannable { }
internal class ServiceC : IAnything, IScannable<IAnything>, ITransientScannable { }

services.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
// ServiceA -> Singleton (overridden)
// ServiceB -> Scoped (overridden)
// ServiceC -> Transient (overridden)
```

### Scan from Different Sources

```csharp
// Scan dependency context
services.ScanDependencyContext(ServiceLifetime.Scoped);

// Scan calling assembly
services.ScanCallingAssembly(ServiceLifetime.Scoped);

// Scan current domain
services.ScanCurrentDomain(ServiceLifetime.Scoped);

// Scan entry assembly
services.ScanEntryAssembly(ServiceLifetime.Scoped);

// Scan executing assembly
services.ScanExecutingAssembly(ServiceLifetime.Scoped);

// Scan from specific type
services.ScanFromType<Program>(ServiceLifetime.Scoped);

// Scan from multiple types
services.ScanFromTypes<Program, Startup>(ServiceLifetime.Scoped);
```

### Scan with References

Scan assemblies **and all referenced assemblies**:

```csharp
services.ScanWithReferences(ServiceLifetime.Scoped, typeof(Program).Assembly);
```

---

## Real-World Examples

### Payment Gateway Factory

```csharp
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessAsync(decimal amount, string currency);
}

public class PaymentGatewayOptions
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
}

public class StripePaymentGateway : IPaymentGateway, IServiceWithOptions<PaymentGatewayOptions>
{
    public PaymentGatewayOptions Options { get; set; }
    
    public async Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        // Stripe implementation
        return new PaymentResult { Success = true };
    }
}

public class PayPalPaymentGateway : IPaymentGateway, IServiceWithOptions<PaymentGatewayOptions>
{
    public PaymentGatewayOptions Options { get; set; }
    
    public async Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        // PayPal implementation
        return new PaymentResult { Success = true };
    }
}

// Registration
services.AddFactory<IPaymentGateway, StripePaymentGateway, PaymentGatewayOptions>(
    x =>
    {
        x.ApiKey = configuration["Stripe:ApiKey"];
        x.BaseUrl = "https://api.stripe.com";
    },
    "stripe",
    ServiceLifetime.Scoped
);

services.AddFactory<IPaymentGateway, PayPalPaymentGateway, PaymentGatewayOptions>(
    x =>
    {
        x.ApiKey = configuration["PayPal:ApiKey"];
        x.BaseUrl = "https://api.paypal.com";
    },
    "paypal",
    ServiceLifetime.Scoped
);

// Usage
public class OrderService
{
    private readonly IFactory<IPaymentGateway> _paymentFactory;
    
    public OrderService(IFactory<IPaymentGateway> paymentFactory)
    {
        _paymentFactory = paymentFactory;
    }
    
    public async Task<bool> ProcessOrderAsync(Order order)
    {
        var gateway = _paymentFactory.Create(order.PaymentMethod);
        var result = await gateway.ProcessAsync(order.Total, order.Currency);
        
        return result.Success;
    }
}
```

### Multi-Database Repository

```csharp
public interface IDatabaseRepository<T>
{
    Task<T> GetAsync(Guid id);
    Task InsertAsync(T entity);
}

public class SqlServerRepository<T> : IDatabaseRepository<T>, IServiceWithOptions<DatabaseOptions>
{
    public DatabaseOptions Options { get; set; }
    
    public async Task<T> GetAsync(Guid id)
    {
        // SQL Server implementation
        return default;
    }
    
    public async Task InsertAsync(T entity)
    {
        // SQL Server implementation
    }
}

public class MongoDbRepository<T> : IDatabaseRepository<T>, IServiceWithOptions<DatabaseOptions>
{
    public DatabaseOptions Options { get; set; }
    
    public async Task<T> GetAsync(Guid id)
    {
        // MongoDB implementation
        return default;
    }
    
    public async Task InsertAsync(T entity)
    {
        // MongoDB implementation
    }
}

// Registration
services.AddFactory<IDatabaseRepository<Product>, SqlServerRepository<Product>, DatabaseOptions>(
    x => x.ConnectionString = configuration.GetConnectionString("SqlServer"),
    "sqlserver",
    ServiceLifetime.Scoped
);

services.AddFactory<IDatabaseRepository<Product>, MongoDbRepository<Product>, DatabaseOptions>(
    x => x.ConnectionString = configuration.GetConnectionString("MongoDb"),
    "mongodb",
    ServiceLifetime.Scoped
);

// Usage
public class ProductService
{
    private readonly IFactory<IDatabaseRepository<Product>> _repositoryFactory;
    
    public ProductService(IFactory<IDatabaseRepository<Product>> repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
    }
    
    public async Task MigrateProductAsync(Product product, string fromDb, string toDb)
    {
        var sourceRepo = _repositoryFactory.Create(fromDb);
        var targetRepo = _repositoryFactory.Create(toDb);
        
        var productData = await sourceRepo.GetAsync(product.Id);
        await targetRepo.InsertAsync(productData);
    }
}
```

### Logging with Decorator

```csharp
public class LoggingPaymentGatewayDecorator : IPaymentGateway, IDecoratorService<IPaymentGateway>
{
    private readonly ILogger<LoggingPaymentGatewayDecorator> _logger;
    public IPaymentGateway DecoratedService { get; private set; }
    
    public LoggingPaymentGatewayDecorator(ILogger<LoggingPaymentGatewayDecorator> logger)
    {
        _logger = logger;
    }
    
    public void SetDecoratedService(IPaymentGateway service)
    {
        DecoratedService = service;
    }
    
    public async Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        _logger.LogInformation("Processing payment: {Amount} {Currency}", amount, currency);
        
        var result = await DecoratedService.ProcessAsync(amount, currency);
        
        _logger.LogInformation("Payment result: {Success}", result.Success);
        
        return result;
    }
}

// Add logging to all payment gateways
services.AddDecoration<IPaymentGateway, LoggingPaymentGatewayDecorator>(
    null, // Apply to all
    ServiceLifetime.Scoped
);
```

---

## Warm Up

Execute initialization code **after service provider is built**:

```csharp
builder.Services.AddWarmUp(() =>
{
    // Initialize cache
    var cache = serviceProvider.GetRequiredService<ICache>();
    cache.LoadAsync().Wait();
});

builder.Services.AddWarmUp(async () =>
{
    // Async warm-up
    var database = serviceProvider.GetRequiredService<IDatabase>();
    await database.MigrateAsync();
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

---

## Benefits

- ✅ **Runtime Resolution**: Choose implementation by name at runtime
- ✅ **Multiple Implementations**: Register multiple services of same interface
- ✅ **Lifetime Support**: Singleton, Scoped, Transient work as expected
- ✅ **Options Pattern**: Configure each service independently
- ✅ **Decorators**: Wrap services without modifying code
- ✅ **Fallbacks**: Handle missing keys gracefully
- ✅ **Assembly Scanning**: Auto-discover services

---

## Related Tools

- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Uses factory pattern for multiple repositories
- **[Service Setup](https://rystem.net/mcp/prompts/service-setup.md)** - DI setup patterns
- **[Discriminated Union](https://rystem.net/mcp/tools/rystem-discriminated-union.md)** - Type-safe unions

---

## References

- **NuGet Package**: [Rystem.DependencyInjection](https://www.nuget.org/packages/Rystem.DependencyInjection) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
