# Repository API Client - .NET/C#

**Purpose**: This tool explains how to **consume auto-generated Repository APIs** from .NET/C# applications (Blazor Server, Blazor WASM, MAUI, WPF, Console apps). The client provides the same `IRepository<T, TKey>` interface you use on the server, making it seamless to work with remote data.

---

## 🎯 What is Rystem Repository API Client?

The **Rystem.RepositoryFramework.Api.Client** package provides:
- ✅ **Same interface as server** - use `IRepository<T, TKey>` everywhere
- ✅ **Automatic HttpClient configuration** with Polly retry policies
- ✅ **Built-in authentication** interceptors for JWT tokens
- ✅ **Factory pattern support** for multiple endpoints
- ✅ **CQRS support** with `ICommand` and `IQuery`
- ✅ **Works everywhere**: Blazor Server, Blazor WASM, MAUI, WPF, Console

**Key Benefit**: Write once, use the same repository interface on server and client!

---

## 📦 Installation

```bash
# Core API Client
dotnet add package Rystem.RepositoryFramework.Api.Client --version 9.1.3

# For Blazor Server with JWT authentication
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer --version 9.1.3

# For Blazor WASM with JWT authentication
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm --version 9.1.3
```

---

## 🚀 Quick Start - Simple Repository

### Step 1: Register Repository Client

```csharp
// Program.cs (Blazor Server, MAUI, WPF, Console)
using Rystem;

var builder = WebApplication.CreateBuilder(args);

// Register repository client
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/");
});

var app = builder.Build();
app.Run();
```

### Step 2: Use in Your Service

```csharp
// Services/OrderService.cs
public class OrderService
{
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public OrderService(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _orderRepository.GetAsync(orderId);
    }
    
    public async Task<State<Order, Guid>> CreateOrderAsync(Order order)
    {
        return await _orderRepository.InsertAsync(order.Id, order);
    }
    
    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        var results = await _orderRepository
            .Where(x => x.Status == OrderStatus.Pending)
            .ToListAsync();
        
        return results.Select(x => x.Value!).ToList();
    }
}
```

**That's it!** The same interface works on client and server.

---

## 🔄 Advanced Setup - Multiple Repositories

### Extension Method Pattern

```csharp
// Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiRepositories(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var apiUri = configuration["Api:Uri"] ?? "https://api.myapp.com/api/";
        
        // Order Repository
        services.AddHttpClientIntegration<Order, Guid>(apiUri);
        
        // Customer Repository
        services.AddHttpClientIntegration<Customer, Guid>(apiUri);
        
        // Product Repository
        services.AddHttpClientIntegration<Product, int>(apiUri);
        
        // Complex key example
        services.AddHttpClientIntegration<OrderItem, OrderItemKey>(apiUri);
        
        return services;
    }
    
    // Helper method for consistent configuration
    private static IServiceCollection AddHttpClientIntegration<T, TKey>(
        this IServiceCollection services,
        string apiUri,
        string? factoryName = null)
        where TKey : notnull
    {
        return services.AddRepository<T, TKey>(builder =>
        {
            builder.WithApiClient(apiBuilder =>
            {
                apiBuilder
                    .WithHttpClient(apiUri)
                    .WithDefaultRetryPolicy();
                
                if (factoryName != null)
                    apiBuilder.WithServerFactoryName(factoryName);
            }, factoryName, ServiceLifetime.Transient);
        });
    }
}
```

### Usage in Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add all API repositories
builder.Services.AddApiRepositories(builder.Configuration);

var app = builder.Build();
app.Run();
```

---

## 🔐 Authentication with JWT

### Blazor Server

```csharp
// Program.cs (Blazor Server)
using Rystem;

var builder = WebApplication.CreateBuilder(args);

// Add authentication services
builder.Services.AddAuthentication(/* your auth config */);
builder.Services.AddAuthorization();

// Add default JWT interceptor
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

// Register repositories
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/");
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

**Package required**: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer`

### Blazor WASM

```csharp
// Program.cs (Blazor WASM)
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rystem;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add authentication services
builder.Services.AddAuthorizationCore();

// Add default JWT interceptor for WASM
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

// Register repositories
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/");
});

await builder.Build().RunAsync();
```

**Package required**: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm`

---

## 🔧 Polly Retry Policies

Add resilience with Polly retry policies:

```csharp
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Define retry policy
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutRejectedException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

// Register repository with policy
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/")
            .ClientBuilder  // Access underlying HttpClientBuilder
        .AddPolicyHandler(retryPolicy);
});
```

### Advanced Polly Configuration

```csharp
// Retry with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s");
        });

// Circuit breaker
var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30));

// Timeout
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
    TimeSpan.FromSeconds(10));

// Register with multiple policies
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/")
            .ClientBuilder
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy)
        .AddPolicyHandler(timeoutPolicy);
});
```

---

## 🏭 Factory Pattern - Multiple Endpoints

Use multiple API endpoints for the same entity:

```csharp
// Setup multiple repositories with factory names
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder
            .WithHttpClient("https://api-primary.myapp.com/api/")
            .WithDefaultRetryPolicy();
        apiBuilder.WithServerFactoryName("primary");
    }, "primary");
});

builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder
            .WithHttpClient("https://api-backup.myapp.com/api/")
            .WithDefaultRetryPolicy();
        apiBuilder.WithServerFactoryName("backup");
    }, "backup");
});

// Usage with Factory
public class OrderService
{
    private readonly IFactory<IRepository<Order, Guid>> _repositoryFactory;
    
    public OrderService(IFactory<IRepository<Order, Guid>> repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
    }
    
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        // Try primary first
        var primaryRepo = _repositoryFactory.Create("primary");
        var order = await primaryRepo.GetAsync(orderId);
        
        if (order != null)
            return order;
        
        // Fallback to backup
        var backupRepo = _repositoryFactory.Create("backup");
        return await backupRepo.GetAsync(orderId);
    }
}
```

---

## 📊 CQRS Pattern

Separate read and write operations with different endpoints:

### Setup

```csharp
// Command (write) to primary API
builder.Services.AddCommand<Order, Guid>(commandBuilder =>
{
    commandBuilder
        .WithApiClient()
        .WithHttpClient("https://api-write.myapp.com/api/");
});

// Query (read) from read replica API
builder.Services.AddQuery<Order, Guid>(queryBuilder =>
{
    queryBuilder
        .WithApiClient()
        .WithHttpClient("https://api-read.myapp.com/api/");
});
```

### Usage

```csharp
public class OrderService
{
    private readonly ICommand<Order, Guid> _orderCommand;
    private readonly IQuery<Order, Guid> _orderQuery;
    
    public OrderService(
        ICommand<Order, Guid> orderCommand,
        IQuery<Order, Guid> orderQuery)
    {
        _orderCommand = orderCommand;
        _orderQuery = orderQuery;
    }
    
    // Write operations use Command
    public async Task<State<Order, Guid>> CreateOrderAsync(Order order)
    {
        return await _orderCommand.InsertAsync(order.Id, order);
    }
    
    public async Task<State<Order, Guid>> UpdateOrderAsync(Order order)
    {
        return await _orderCommand.UpdateAsync(order.Id, order);
    }
    
    // Read operations use Query
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _orderQuery.GetAsync(orderId);
    }
    
    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        var results = await _orderQuery
            .Where(x => x.Status == OrderStatus.Pending)
            .ToListAsync();
        
        return results.Select(x => x.Value!).ToList();
    }
}
```

**⚠️ Important**: Always inject `ICommand`, `IQuery`, `IRepository` - NOT `ICommandPattern`, `IQueryPattern`, `IRepositoryPattern`!

---

## 🎭 Custom Interceptors

Add custom logic before/after every HTTP request:

### Create Interceptor

```csharp
// Interceptors/CustomHeaderInterceptor.cs
using Rystem.RepositoryFramework.Api.Client;

public class CustomHeaderInterceptor : IRepositoryClientInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CustomHeaderInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async ValueTask<HttpClient> EnrichAsync(
        HttpClient client,
        RepositoryMethod method,
        CancellationToken cancellationToken = default)
    {
        // Add custom headers
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (userId != null)
        {
            client.DefaultRequestHeaders.Add("X-User-Id", userId);
        }
        
        // Add correlation ID
        client.DefaultRequestHeaders.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        
        // Add tenant ID for multi-tenancy
        var tenantId = await GetTenantIdAsync();
        if (tenantId != null)
        {
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        }
        
        return client;
    }
    
    private async Task<string?> GetTenantIdAsync()
    {
        // Your tenant resolution logic
        return "tenant-123";
    }
}
```

### Register Interceptor

#### Global Interceptor (for all repositories)

```csharp
builder.Services.AddApiClientInterceptor<CustomHeaderInterceptor>(ServiceLifetime.Scoped);
```

#### Specific Interceptor (for one repository)

```csharp
builder.Services.AddRepository<Order, Guid>(repositoryBuilder =>
{
    repositoryBuilder
        .WithApiClient()
        .WithHttpClient("https://api.myapp.com/api/")
        .AddApiClientSpecificInterceptor<Order, Guid, CustomHeaderInterceptor>(ServiceLifetime.Scoped);
});
```

### Built-in JWT Interceptor

For standard JWT authentication:

```csharp
// For Blazor Server
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

// For Blazor WASM
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
```

This automatically adds the JWT token from the current user's authentication context.

---

## 🌐 Real-World Example - Complete Setup

```csharp
// Program.cs (Blazor Server)
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Polly;
using Polly.Extensions.Http;
using Rystem;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
    });

builder.Services.AddAuthorization();

// Add HttpContextAccessor for interceptors
builder.Services.AddHttpContextAccessor();

// Add default JWT interceptor
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

// Add custom interceptor
builder.Services.AddApiClientInterceptor<CustomHeaderInterceptor>(ServiceLifetime.Scoped);

// Add API repositories
builder.Services.AddApiRepositories(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

```csharp
// Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiUri = configuration["Api:Uri"];
        if (!string.IsNullOrEmpty(apiUri) && !apiUri.StartsWith("http"))
            apiUri = $"https://{apiUri}";
        
        // Orders
        services.AddHttpClientIntegration<Order, Guid>(apiUri);
        
        // Customers
        services.AddHttpClientIntegration<Customer, Guid>(apiUri);
        
        // Products
        services.AddHttpClientIntegration<Product, int>(apiUri);
        
        // Shipments with factory name
        services.AddHttpClientIntegration<Shipment, Guid>(apiUri, "primary");
        
        // Live data with different endpoint
        services.AddRepository<LiveData, string>(builder =>
        {
            builder.WithApiClient(apiBuilder =>
            {
                apiBuilder
                    .WithHttpClient($"{apiUri}/live")
                    .WithDefaultRetryPolicy();
                apiBuilder.WithServerFactoryName("live");
            }, "live");
        });
        
        return services;
    }
    
    private static IServiceCollection AddHttpClientIntegration<T, TKey>(
        this IServiceCollection services,
        string apiUri,
        string? factoryName = null)
        where TKey : notnull
    {
        return services.AddRepository<T, TKey>(builder =>
        {
            builder.WithApiClient(apiBuilder =>
            {
                apiBuilder
                    .WithHttpClient(apiUri)
                    .WithDefaultRetryPolicy();
                
                if (factoryName != null)
                    apiBuilder.WithServerFactoryName(factoryName);
            }, factoryName, ServiceLifetime.Transient);
        });
    }
}
```

---

## 📱 Platform-Specific Examples

### Blazor Server

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
builder.Services.AddApiRepositories(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.Run();
```

### Blazor WASM

```csharp
// Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
builder.Services.AddApiRepositories(builder.Configuration);

await builder.Build().RunAsync();
```

### MAUI

```csharp
// MauiProgram.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Hosting;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
        
        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        builder.Services.AddSingleton<IConfiguration>(configuration);
        
        // Add API repositories
        builder.Services.AddApiRepositories(configuration);
        
        return builder.Build();
    }
}
```

### WPF

```csharp
// App.xaml.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class App : Application
{
    private readonly IHost _host;
    
    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add API repositories
                services.AddApiRepositories(context.Configuration);
                
                // Add views
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        
        base.OnStartup(e);
    }
}
```

### Console App

```csharp
// Program.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddApiRepositories(context.Configuration);
    })
    .Build();

// Use repository
var orderRepository = host.Services.GetRequiredService<IRepository<Order, Guid>>();
var order = await orderRepository.GetAsync(Guid.Parse("..."));

await host.RunAsync();
```

---

## 🎯 Best Practices

### 1. Use Extension Methods for Configuration

```csharp
// ✅ GOOD - Centralized configuration
public static IServiceCollection AddApiRepositories(this IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClientIntegration<Order, Guid>(configuration["Api:Uri"]);
    services.AddHttpClientIntegration<Customer, Guid>(configuration["Api:Uri"]);
    return services;
}

// ❌ BAD - Scattered configuration
builder.Services.AddRepository<Order, Guid>(...);
builder.Services.AddRepository<Customer, Guid>(...);
```

### 2. Always Add Retry Policies

```csharp
// ✅ GOOD - Resilient with retry
apiBuilder
    .WithHttpClient(apiUri)
    .WithDefaultRetryPolicy();

// ❌ BAD - No retry policy
apiBuilder.WithHttpClient(apiUri);
```

### 3. Use Proper Lifetime for Interceptors

```csharp
// ✅ GOOD - Scoped for user context
services.AddApiClientInterceptor<CustomHeaderInterceptor>(ServiceLifetime.Scoped);

// ❌ BAD - Singleton with user-specific data
services.AddApiClientInterceptor<CustomHeaderInterceptor>(ServiceLifetime.Singleton);
```

### 4. Inject IRepository, Not IRepositoryPattern

```csharp
// ✅ GOOD
public OrderService(IRepository<Order, Guid> repository)

// ❌ BAD
public OrderService(IRepositoryPattern<Order, Guid> repository)
```

### 5. Use Factory for Multiple Endpoints

```csharp
// ✅ GOOD - Factory for multiple endpoints
public OrderService(IFactory<IRepository<Order, Guid>> factory)
{
    var primary = factory.Create("primary");
    var backup = factory.Create("backup");
}

// ❌ BAD - Multiple injections
public OrderService(
    IRepository<Order, Guid> repo1,
    IRepository<Order, Guid> repo2) // Won't work!
```

---

## ⚠️ Important Notes

1. **Package version**: Use `9.1.3` for all Rystem packages
2. **Authentication packages**:
   - Blazor Server: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer`
   - Blazor WASM: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm`
3. **Inject interfaces**: Use `IRepository`, `ICommand`, `IQuery` (NOT Pattern versions)
4. **Polly is optional**: But highly recommended for production
5. **Interceptors run in order**: Global first, then specific
6. **Factory names must match**: Client factory name must match server factory name
7. **HttpClient base address**: Should end with `/` for proper URL joining

---

## 🔗 Related Resources

- **repository-setup**: How to configure repositories on backend
- **repository-api-server**: How to expose repositories as REST APIs
- **repository-api-client-typescript**: Client for TypeScript/JavaScript apps
- **auth-flow**: Setting up authentication with Rystem.Authentication.Social

---

## 📖 Further Reading

- [Rystem.RepositoryFramework.Api.Client GitHub](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Api.Client)
- [Authentication Integration](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Api.Client.Authentication.BlazorServer)
- [Polly Documentation](https://github.com/App-vNext/Polly)

---

## ✅ Summary

**Rystem.RepositoryFramework.Api.Client** provides:
- ✅ Same `IRepository<T, TKey>` interface as server
- ✅ Automatic HttpClient configuration
- ✅ Built-in JWT authentication interceptors
- ✅ Polly retry policies support
- ✅ Custom interceptors for headers/logging
- ✅ Factory pattern for multiple endpoints
- ✅ CQRS with `ICommand` and `IQuery`
- ✅ Works on Blazor Server, Blazor WASM, MAUI, WPF, Console

**Use this tool to build .NET/C# clients that consume your Repository APIs seamlessly!** 🚀
