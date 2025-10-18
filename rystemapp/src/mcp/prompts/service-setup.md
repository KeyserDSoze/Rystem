# Service Setup with Dependency Injection

> Configure and organize services using Rystem's advanced dependency injection features

## Context

You need to set up a .NET application with proper service organization, dependency injection patterns, and service lifetime management using Rystem.

## Prerequisites

- .NET 6.0 or higher
- Rystem.DependencyInjection package installed

## Installation

```bash
dotnet add package Rystem.DependencyInjection
dotnet add package Rystem.DependencyInjection.Web
```

## Service Organization Patterns

### 1. Service Factory Pattern

Use when you need to create multiple instances of a service with different configurations.

```csharp
// Service interface
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

// Different implementations
public class SmtpEmailService : IEmailService { /* ... */ }
public class SendGridEmailService : IEmailService { /* ... */ }

// Registration
builder.Services.AddServiceFactory<IEmailService>()
    .Add<SmtpEmailService>("smtp")
    .Add<SendGridEmailService>("sendgrid");

// Usage
public class NotificationService
{
    private readonly IServiceFactory<IEmailService> _emailFactory;

    public NotificationService(IServiceFactory<IEmailService> emailFactory)
    {
        _emailFactory = emailFactory;
    }

    public async Task SendAsync(string provider, string to, string subject, string body)
    {
        var emailService = _emailFactory.Get(provider);
        await emailService.SendEmailAsync(to, subject, body);
    }
}
```

### 2. Keyed Services

For .NET 8+ applications using keyed services.

```csharp
// Registration
builder.Services.AddKeyedScoped<IPaymentProcessor, StripeProcessor>("stripe");
builder.Services.AddKeyedScoped<IPaymentProcessor, PayPalProcessor>("paypal");

// Usage
public class CheckoutService
{
    private readonly IPaymentProcessor _stripeProcessor;
    private readonly IPaymentProcessor _paypalProcessor;

    public CheckoutService(
        [FromKeyedServices("stripe")] IPaymentProcessor stripeProcessor,
        [FromKeyedServices("paypal")] IPaymentProcessor paypalProcessor)
    {
        _stripeProcessor = stripeProcessor;
        _paypalProcessor = paypalProcessor;
    }
}
```

### 3. Service Lifetimes

```csharp
// Singleton - One instance for the application lifetime
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Scoped - One instance per request
builder.Services.AddScoped<IRepository<User, int>, UserRepository>();

// Transient - New instance every time
builder.Services.AddTransient<IEmailBuilder, EmailBuilder>();
```

### 4. Organized Registration

Create extension methods to organize service registration:

```csharp
// Infrastructure/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Business Services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUserService, UserService>();

        // Background Jobs
        services.AddBackgroundJob<OrderProcessingJob>(job =>
        {
            job.Every(TimeSpan.FromMinutes(5));
        });

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddRepository<Order, Guid>(repo =>
        {
            repo.WithEntityFramework<ApplicationDbContext>();
        });

        services.AddRepository<Product, int>(repo =>
        {
            repo.WithEntityFramework<ApplicationDbContext>()
                .WithCache(cache => cache.WithDistributedCache());
        });

        // External Services
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"]);
        });

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(/* ... */);
        services.AddAuthorization(/* ... */);
        
        return services;
    }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddAuthenticationServices(builder.Configuration);
```

### 5. Configuration Pattern

```csharp
// Options class
public class EmailSettings
{
    public string SmtpServer { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

// Registration
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

// Usage
public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }
}
```

### 6. Decorator Pattern

```csharp
// Base service
public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderRequest request);
}

public class OrderService : IOrderService
{
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Core implementation
    }
}

// Logging decorator
public class LoggingOrderService : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger<LoggingOrderService> _logger;

    public LoggingOrderService(IOrderService inner, ILogger<LoggingOrderService> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Creating order...");
        var order = await _inner.CreateOrderAsync(request);
        _logger.LogInformation("Order created: {OrderId}", order.Id);
        return order;
    }
}

// Registration
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService>(sp =>
{
    var orderService = sp.GetRequiredService<OrderService>();
    var logger = sp.GetRequiredService<ILogger<LoggingOrderService>>();
    return new LoggingOrderService(orderService, logger);
});
```

### 7. Conditional Registration

```csharp
// Development vs Production services
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, FakeEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}

// Feature flags
if (builder.Configuration.GetValue<bool>("Features:UseNewPaymentProcessor"))
{
    builder.Services.AddScoped<IPaymentProcessor, NewPaymentProcessor>();
}
else
{
    builder.Services.AddScoped<IPaymentProcessor, LegacyPaymentProcessor>();
}
```

## Complete Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Core Services
builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(Program).Assembly);

// Application Layer
builder.Services
    .AddScoped<IOrderService, OrderService>()
    .AddScoped<IProductService, ProductService>()
    .AddScoped<ICartService, CartService>();

// Infrastructure Layer
builder.Services
    .AddRepository<Order, Guid>(repo => repo.WithEntityFramework<AppDbContext>())
    .AddRepository<Product, int>(repo => repo.WithEntityFramework<AppDbContext>())
    .AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// External Services
builder.Services
    .AddHttpClient<IPaymentGateway, StripeGateway>()
    .AddHttpClient<IShippingService, FedExService>();

// Background Jobs
builder.Services.AddBackgroundJob<OrderCleanupJob>(job =>
    job.Every(TimeSpan.FromHours(1)));

// Caching
builder.Services.AddDistributedMemoryCache();

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* options */);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Best Practices

1. **Organize by Layer** - Group services by application layer
2. **Use Extension Methods** - Keep Program.cs clean
3. **Configure Options** - Use IOptions<T> pattern
4. **Choose Correct Lifetime** - Understand Singleton/Scoped/Transient
5. **Avoid Service Locator** - Use constructor injection
6. **Register Interfaces** - Program against abstractions
7. **Use Factories** - For complex object creation
8. **Document Dependencies** - Make service dependencies clear

## Common Pitfalls

❌ **Captive Dependencies** - Don't inject scoped/transient into singleton
❌ **Service Locator** - Don't use IServiceProvider directly in business logic
❌ **Circular Dependencies** - Design to avoid circular references
❌ **Too Many Dependencies** - Consider facade pattern if constructor has 5+ parameters

## See Also

- [Rystem.DependencyInjection Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Core/Rystem.DependencyInjection)
- [Microsoft DI Documentation](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [Repository Pattern Setup](../tools/repository-setup.md)
