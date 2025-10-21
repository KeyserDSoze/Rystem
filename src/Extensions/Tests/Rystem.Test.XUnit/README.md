### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.Test.XUnit

**Advanced XUnit integration testing framework** with built-in Dependency Injection and ASP.NET Core Test Host support. Perfect for testing APIs, services, and complex application architectures.

## 📦 Installation

```bash
dotnet add package Rystem.Test.XUnit
```

## 🎯 Features

- ✅ **Built-in Dependency Injection** for XUnit tests
- ✅ **ASP.NET Core Test Server** with full middleware pipeline
- ✅ **Automatic Controller Discovery** and mapping
- ✅ **User Secrets Integration** for configuration management
- ✅ **Health Check Support** for server validation
- ✅ **HTTPS/HTTP Configuration** with customizable options
- ✅ **Automatic HttpClient Factory** for API testing

---

## 🚀 Quick Start

### Scenario 1: Testing Without HTTP Server (Business Logic Only)

Perfect for testing services, repositories, and business logic without API layer.

#### 1. Create Startup Class

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

namespace MyApp.Tests
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.json";
        protected override bool HasTestHost => false; // ❌ No HTTP server
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null; // Not needed
        
        protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add your business services here
            services.AddMyBusinessLogic();
            services.AddMyRepositories();
            services.AddMyTestHelpers();
            
            return services;
        }
    }
}
```

#### 2. Write Tests with Constructor Injection

```csharp
using Xunit;

namespace MyApp.Tests
{
    public class BusinessLogicTest
    {
        private readonly IBookManager _bookManager;
        private readonly ITestHelpers _testHelpers;

        // Constructor injection works automatically! 🎉
        public BusinessLogicTest(IBookManager bookManager, ITestHelpers testHelpers)
        {
            _bookManager = bookManager;
            _testHelpers = testHelpers;
        }

        [Fact]
        public async Task CreateBook_ShouldReturnValidBook()
        {
            // Arrange
            var bookId = Guid.NewGuid();

            // Act
            var book = await _bookManager.CreateBookAsync(bookId);

            // Assert
            Assert.NotNull(book);
            Assert.Equal(bookId, book.Id);
        }

        [Theory]
        [InlineData("Title 1")]
        [InlineData("Title 2")]
        public async Task UpdateBookTitle_ShouldSucceed(string title)
        {
            // Arrange
            var book = await _testHelpers.CreateTestBookAsync();

            // Act
            await _bookManager.UpdateTitleAsync(book.Id, title);
            var updated = await _bookManager.GetByIdAsync(book.Id);

            // Assert
            Assert.Equal(title, updated.Title);
        }
    }
}
```

---

### Scenario 2: Testing With HTTP Server (Full API Integration)

Full integration testing with real HTTP requests to your ASP.NET Core API.

#### 1. Create Startup Class with Test Host

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

namespace MyApp.Api.Tests
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.test.json";
        protected override bool HasTestHost => true; // ✅ Enable HTTP server
        protected override bool WithHttps => true; // HTTPS enabled
        protected override bool AddHealthCheck => true; // Add /healthz endpoint
        
        // Use Startup or any controller class from your API project
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program); // Your API's Program class
        
        // Use test project's Startup to load User Secrets
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        
        // Configure services for TEST CLIENT (HTTP consumers)
        protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add HTTP client to call your API
            services.AddHttpClient<IMyApiClient, MyApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost");
            });
            
            return services;
        }
        
        // Configure services for TEST SERVER (API dependencies)
        protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
        {
            // Add your API services
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddMyApiServices();
            services.AddMyRepositories();
            services.AddMyBusinessLogic();
            
            return;
        }
        
        // Configure middleware pipeline for TEST SERVER
        protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            // Configure your API middleware chain
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            return;
        }
    }
}
```

#### 2. Write API Tests with HTTP Calls

```csharp
using Xunit;

namespace MyApp.Api.Tests
{
    public class ApiIntegrationTest
    {
        private readonly IMyApiClient _apiClient;

        public ApiIntegrationTest(IMyApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("admin@example.com")]
        public async Task Login_ShouldReturnUser(string email)
        {
            // Act - Real HTTP call to localhost test server
            var user = await _apiClient.LoginAsync(email);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public async Task GetBooks_ShouldReturnList()
        {
            // Arrange
            await _apiClient.LoginAsync("user@example.com");

            // Act
            var books = await _apiClient.GetBooksAsync();

            // Assert
            Assert.NotEmpty(books);
        }

        [Fact]
        public async Task CreateBook_ThenGetById_ShouldMatch()
        {
            // Arrange
            var createRequest = new CreateBookRequest
            {
                Title = "Test Book",
                Author = "Test Author"
            };

            // Act
            var created = await _apiClient.CreateBookAsync(createRequest);
            var retrieved = await _apiClient.GetBookByIdAsync(created.Id);

            // Assert
            Assert.Equal(created.Id, retrieved.Id);
            Assert.Equal(createRequest.Title, retrieved.Title);
        }
    }
}
```

---

## 📖 Configuration Reference

### StartupHelper Properties

#### `AppSettingsFileName` (Required)
Path to your appsettings file for test configuration.

```csharp
protected override string? AppSettingsFileName => "appsettings.test.json";
```

**Best Practice**: Use separate `appsettings.test.json` for test configurations.

---

#### `HasTestHost` (Required)
Enables or disables the ASP.NET Core Test Server.

```csharp
protected override bool HasTestHost => true; // Enable test server
protected override bool HasTestHost => false; // Disable (business logic only)
```

**When to enable:**
- ✅ Testing REST APIs, gRPC services, or SignalR hubs
- ✅ Testing middleware pipeline behavior
- ✅ Testing authentication/authorization flows
- ✅ Integration tests requiring real HTTP requests

**When to disable:**
- ❌ Testing business logic without HTTP layer
- ❌ Repository pattern tests
- ❌ Service layer unit tests

---

#### `TypeToChooseTheRightAssemblyWithControllersToMap` (Required if `HasTestHost = true`)
Specifies which assembly contains your API controllers/endpoints.

```csharp
// Option 1: Use Program class from your API project
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);

// Option 2: Use any controller from your API project
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(BooksController);

// Option 3: Not needed if HasTestHost = false
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;
```

**Purpose**: Automatically discovers and maps all controllers/endpoints in that assembly.

---

#### `TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration` (Required)
Specifies which assembly to load User Secrets from (Visual Studio Secret Manager).

```csharp
// Usually points to your TEST project's Startup class
protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
```

**User Secrets Example:**
```json
{
  "ConnectionStrings:Database": "Server=...;Database=Test;",
  "ExternalApi:ApiKey": "secret-key-here"
}
```

**How to manage secrets:**
```bash
# Right-click test project in Visual Studio → Manage User Secrets
# Or use CLI:
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost;..."
```

---

#### `WithHttps` (Optional, default: `true`)
Configures test server to use HTTPS.

```csharp
protected override bool WithHttps => true; // https://localhost:443
protected override bool WithHttps => false; // http://localhost
```

---

#### `PreserveExecutionContext` (Optional, default: `false`)
Preserves execution context across async operations.

```csharp
protected override bool PreserveExecutionContext => true;
```

**When to enable:**
- Testing code that relies on `AsyncLocal<T>`
- Testing authentication contexts that flow across async calls

---

#### `AddHealthCheck` (Optional, default: `true`)
Automatically adds `/healthz` endpoint to test server.

```csharp
protected override bool AddHealthCheck => true; // Adds /healthz endpoint
```

**Validation**: Test framework automatically calls `/healthz` after server startup to ensure health.

---

### Abstract Methods

#### `ConfigureClientServices` (Required)
Configure dependency injection for **test consumers** (your test classes).

```csharp
protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
{
    // Add services used by test classes
    services.AddHttpClient<IMyApiClient, MyApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost");
    });
    
    services.AddSingleton<ITestDataGenerator, TestDataGenerator>();
    
    return services;
}
```

---

#### `ConfigureServerServicesAsync` (Virtual, implement if `HasTestHost = true`)
Configure dependency injection for **test server** (API dependencies).

```csharp
protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
{
    // Add your API services here
    services.AddControllers();
    services.AddMyBusinessLogic();
    services.AddMyRepositories();
    
    // Configure database for testing
    services.AddDbContext<MyDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
    
    return;
}
```

---

#### `ConfigureServerMiddlewareAsync` (Virtual, implement if `HasTestHost = true`)
Configure middleware pipeline for **test server**.

```csharp
protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
{
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCors("AllowAll");
    
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapGrpcService<MyGrpcService>();
    });
    
    return;
}
```

---

## 🏗️ Architecture

### How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                      XUnit Test Runner                       │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                 StartupHelper (Abstract Base)                │
│  - Loads appsettings.json                                    │
│  - Loads User Secrets                                        │
│  - Configures Host Builder                                   │
└───────────────────────────┬─────────────────────────────────┘
                            │
              ┌─────────────┴─────────────┐
              ▼                           ▼
┌─────────────────────────┐   ┌─────────────────────────────┐
│   ConfigureClientServices│   │ ConfigureServerServicesAsync│
│   (Test DI Container)    │   │ (API DI Container)          │
│   - HTTP Clients         │   │ - Controllers               │
│   - Test Helpers         │   │ - Business Logic            │
│   - Mocks                │   │ - Repositories              │
└───────────┬─────────────┘   └──────────┬──────────────────┘
            │                            │
            │                ┌───────────┴───────────────┐
            │                ▼                           │
            │    ┌────────────────────────────┐          │
            │    │ ASP.NET Core Test Server   │          │
            │    │ - Middleware Pipeline      │          │
            │    │ - Endpoint Routing         │          │
            │    │ - Authentication           │          │
            │    └────────────┬───────────────┘          │
            │                 │                          │
            ▼                 ▼                          ▼
┌────────────────────────────────────────────────────────────┐
│              Constructor Injection in Test Classes          │
│  public MyTest(IMyService service, IHttpClientFactory http) │
└────────────────────────────────────────────────────────────┘
```

---

## 🧪 Real-World Examples

### Example 1: Repository Pattern Testing

```csharp
public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.json";
    protected override bool HasTestHost => false;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;
    
    protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        
        services.AddRepositories();
        
        return services;
    }
}

public class RepositoryTest
{
    private readonly IRepository<Book> _bookRepository;

    public RepositoryTest(IRepository<Book> bookRepository)
    {
        _bookRepository = bookRepository;
    }

    [Fact]
    public async Task InsertAndRetrieve_ShouldWork()
    {
        var book = new Book { Id = Guid.NewGuid(), Title = "Test" };
        await _bookRepository.InsertAsync(book);
        
        var retrieved = await _bookRepository.GetAsync(book.Id);
        Assert.Equal(book.Title, retrieved.Title);
    }
}
```

---

### Example 2: Full API Testing with Authentication

```csharp
public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => true;
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    
    protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IBridgeInspectionApi, BridgeInspectionApi>(client =>
        {
            client.BaseAddress = new Uri("https://localhost");
        });
        return services;
    }
    
    protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddAuthentication("TestScheme")
            .AddScheme<TestAuthOptions, TestAuthHandler>("TestScheme", options => { });
        services.AddAuthorization();
        services.AddMyApiServices();
        return;
    }
    
    protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        return;
    }
}

public class InspectionTest
{
    private readonly IBridgeInspectionApi _api;

    public InspectionTest(IBridgeInspectionApi api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("user@example.com")]
    public async Task GetBridgesAndInspections_ShouldReturnData(string email)
    {
        // Login
        var user = await _api.LoginAsAsync(email);
        Assert.NotNull(user);
        
        // Get bridges
        var bridges = await _api.BridgeApi.ListAsync();
        Assert.NotEmpty(bridges);
        
        // Get inspections for first bridge
        var bridge = bridges.First();
        var inspections = await _api.InspectionApi.ListAsync(bridge.Id);
        Assert.NotEmpty(inspections);
    }

    [Fact]
    public async Task DownloadInspectionZip_ShouldReturnBytes()
    {
        await _api.LoginAsAsync("admin@example.com");
        var bridges = await _api.BridgeApi.ListAsync();
        var inspections = await _api.InspectionApi.ListAsync(bridges.First().Id);
        
        var zipBytes = await _api.InspectionApi.DownloadInspectionsFile(
            bridges.First().Id, 
            inspections.First().Id
        );
        
        Assert.NotEmpty(zipBytes);
    }
}
```

---

## 🔧 Advanced Configuration

### Custom HTTP Headers for Test Server

```csharp
services.AddHttpClient<IMyClient, MyClient>((serviceProvider, client) =>
{
    client.BaseAddress = new Uri("https://localhost");
    client.DefaultRequestHeaders.Add("X-Test-Environment", "true");
    client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
});
```

### Mock External Dependencies

```csharp
protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
{
    // Replace real external API with mock
    services.AddSingleton<IExternalApiClient, MockExternalApiClient>();
    
    return services;
}
```

### In-Memory Database for Tests

```csharp
protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<MyDbContext>(options =>
        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")); // Unique per test run
    
    return;
}
```

---

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

