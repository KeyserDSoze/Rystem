---
title: "Rystem.Test.XUnit - XUnit Integration Testing Framework"
description: "Complete guide to Rystem.Test.XUnit with Dependency Injection and ASP.NET Core Test Host support for integration testing"
category: "Testing"
tags: ["xunit", "testing", "dependency-injection", "integration-testing", "test-host"]
---

# Rystem.Test.XUnit

**Advanced XUnit integration testing framework** with built-in Dependency Injection and ASP.NET Core Test Host support. Perfect for testing APIs, services, and complex application architectures.

## 📦 Installation

```bash
dotnet add package Rystem.Test.XUnit
```

### ⚠️ IMPORTANT: XUnit v3 Requirements

**Rystem.Test.XUnit requires XUnit v3**. Recent XUnit updates introduced breaking changes. Ensure correct package versions.

**Complete test project setup:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Rystem Test Framework -->
    <PackageReference Include="Rystem.Test.XUnit" Version="9.1.3" />
    
    <!-- XUnit v3 (NOT xunit v2!) -->
    <PackageReference Include="xunit.v3" Version="3.0.1" />
    
    <!-- Test SDK and Runners -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    
    <!-- Code Coverage -->
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

**Quick CLI Setup:**

```bash
# 1. Create test project
dotnet new xunit -n MyApp.Tests
cd MyApp.Tests

# 2. Remove old xunit package (if present)
dotnet remove package xunit

# 3. Add XUnit v3 and Rystem
dotnet add package xunit.v3 --version 3.0.1
dotnet add package xunit.runner.visualstudio --version 3.1.4
dotnet add package Rystem.Test.XUnit --version 9.1.3
dotnet add package Microsoft.NET.Test.Sdk --version 17.14.1
```

**Common Mistakes to Avoid:**

| ❌ Wrong | ✅ Correct |
|---------|-----------|
| `<PackageReference Include="xunit" />` | `<PackageReference Include="xunit.v3" />` |
| `xunit.runner.visualstudio` v2.x | `xunit.runner.visualstudio` v3.1.4+ |
| Missing `IsTestProject` property | `<IsTestProject>true</IsTestProject>` |

**XUnit v2 vs v3 Key Differences:**

- ✅ Constructor injection: **Same** (no changes)
- ✅ `[Fact]`, `[Theory]`, `[InlineData]`: **Same** (no changes)
- ⚠️ Assertion APIs: **Some changes** (see [migration guide](https://xunit.net/docs/getting-started/v3/migration))
- ⚠️ Test collection behavior: **Enhanced** (better parallelization)

---

## 🎯 Key Features

- ✅ **Built-in Dependency Injection** - Constructor injection in XUnit test classes
- ✅ **ASP.NET Core Test Server** - Full middleware pipeline with real HTTP requests
- ✅ **Automatic Controller Discovery** - Auto-maps all controllers/endpoints from specified assembly
- ✅ **User Secrets Integration** - Load secrets from Visual Studio Secret Manager
- ✅ **Health Check Support** - Automatic `/healthz` endpoint validation
- ✅ **HTTPS/HTTP Configuration** - Flexible protocol configuration
- ✅ **HttpClient Factory** - Automatic HTTP client setup for API testing

---

## 🚀 Quick Start

### Scenario 1: Testing Business Logic (No HTTP Server)

Perfect for testing services, repositories, and business logic without API layer.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

namespace MyApp.Tests
{
    public class Startup : StartupHelper
    {
        protected override string? AppSettingsFileName => "appsettings.json";
        protected override bool HasTestHost => false; // ❌ No HTTP server needed
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;
        
        protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add your services for testing
            services.AddMyBusinessLogic();
            services.AddMyRepositories();
            
            return services;
        }
    }
}
```

**Write tests with constructor injection:**

```csharp
public class BusinessLogicTest
{
    private readonly IBookManager _bookManager;

    public BusinessLogicTest(IBookManager bookManager)
    {
        _bookManager = bookManager; // Injected automatically! 🎉
    }

    [Fact]
    public async Task CreateBook_ShouldReturnValidBook()
    {
        var bookId = Guid.NewGuid();
        var book = await _bookManager.CreateBookAsync(bookId);
        
        Assert.NotNull(book);
        Assert.Equal(bookId, book.Id);
    }
}
```

---

### Scenario 2: Testing APIs (With HTTP Server)

Full integration testing with real HTTP requests to your ASP.NET Core API.

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
        protected override bool HasTestHost => true; // ✅ Enable test server
        protected override bool WithHttps => true;
        protected override bool AddHealthCheck => true;
        
        // Use your API's Program class or any controller
        protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);
        
        // Use test project's Startup for User Secrets
        protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
        
        // Configure TEST CLIENT services (HTTP consumers)
        protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IMyApiClient, MyApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://localhost");
            });
            
            return services;
        }
        
        // Configure TEST SERVER services (API dependencies)
        protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            services.AddMyApiServices();
            services.AddMyRepositories();
            
            return;
        }
        
        // Configure TEST SERVER middleware
        protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            
            return;
        }
    }
}
```

**Write API tests:**

```csharp
public class ApiIntegrationTest
{
    private readonly IMyApiClient _apiClient;

    public ApiIntegrationTest(IMyApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [Theory]
    [InlineData("user@example.com")]
    public async Task Login_ShouldReturnUser(string email)
    {
        var user = await _apiClient.LoginAsync(email); // Real HTTP call!
        
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task GetBooks_ThenCreateNew_ShouldWork()
    {
        await _apiClient.LoginAsync("user@example.com");
        
        var books = await _apiClient.GetBooksAsync();
        Assert.NotEmpty(books);
        
        var newBook = await _apiClient.CreateBookAsync(new CreateBookRequest
        {
            Title = "Test Book",
            Author = "Test Author"
        });
        
        Assert.NotNull(newBook);
    }
}
```

---

## 📖 Configuration Reference

### Required Properties

#### `AppSettingsFileName`
Path to appsettings file for test configuration.

```csharp
protected override string? AppSettingsFileName => "appsettings.test.json";
```

**Best Practice**: Use separate `appsettings.test.json` for test-specific configuration.

---

#### `HasTestHost`
Enable/disable ASP.NET Core Test Server.

```csharp
protected override bool HasTestHost => true;  // Enable for API testing
protected override bool HasTestHost => false; // Disable for business logic testing
```

**When to enable:**
- ✅ Testing REST APIs, gRPC, SignalR
- ✅ Testing middleware pipeline
- ✅ Testing authentication/authorization
- ✅ Integration tests with HTTP

**When to disable:**
- ❌ Testing business logic only
- ❌ Repository pattern tests
- ❌ Service layer unit tests

---

#### `TypeToChooseTheRightAssemblyWithControllersToMap`
Specifies assembly containing API controllers/endpoints.

```csharp
// Option 1: Use API's Program class
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);

// Option 2: Use any controller from API project
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(BooksController);

// Option 3: Null if HasTestHost = false
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;
```

**Purpose**: Framework automatically discovers and maps all controllers/endpoints in that assembly.

---

#### `TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration`
Specifies assembly to load User Secrets from.

```csharp
// Usually your TEST project's Startup class
protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
```

**Why**: Test projects typically have their own User Secrets for:
- Database connection strings
- API keys for external services
- Test-specific configuration

**Manage secrets:**
```bash
# Visual Studio: Right-click test project → Manage User Secrets
# Or CLI:
dotnet user-secrets set "ConnectionStrings:TestDb" "Server=localhost;..."
dotnet user-secrets set "ExternalApi:ApiKey" "test-key-123"
```

---

#### `ConfigureClientServices`
Configure DI for **test consumers** (your test classes).

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

**Injected into test class constructors:**
```csharp
public MyTest(IMyApiClient apiClient, ITestDataGenerator generator)
{
    // Both injected automatically
}
```

---

### Optional Properties

#### `WithHttps` (default: `true`)
Enable HTTPS for test server.

```csharp
protected override bool WithHttps => true;  // https://localhost:443
protected override bool WithHttps => false; // http://localhost
```

---

#### `PreserveExecutionContext` (default: `false`)
Preserve execution context across async operations.

```csharp
protected override bool PreserveExecutionContext => true;
```

**When to enable:**
- Testing code using `AsyncLocal<T>`
- Testing authentication contexts flowing across async calls

---

#### `AddHealthCheck` (default: `true`)
Add automatic `/healthz` endpoint.

```csharp
protected override bool AddHealthCheck => true;
```

**Validation**: Framework automatically calls `/healthz` after server startup to ensure health.

---

### Virtual Methods (Override if `HasTestHost = true`)

#### `ConfigureServerServicesAsync`
Configure DI for **test server** (API dependencies).

```csharp
protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
{
    services.AddControllers();
    services.AddMyBusinessLogic();
    services.AddMyRepositories();
    
    // In-memory database for testing
    services.AddDbContext<MyDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
    
    return;
}
```

---

#### `ConfigureServerMiddlewareAsync`
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
    });
    
    return;
}
```

---

## 🏗️ Architecture Diagram

```
┌───────────────────────────────────────────────────────────┐
│              XUnit Test Runner                             │
└──────────────────────┬────────────────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────────────────┐
│           StartupHelper (Abstract Base)                    │
│  • Loads appsettings.json                                  │
│  • Loads User Secrets                                      │
│  • Configures Host Builder                                 │
└──────────────────────┬────────────────────────────────────┘
                       │
         ┌─────────────┴─────────────┐
         ▼                           ▼
┌──────────────────────┐   ┌───────────────────────────────┐
│ ConfigureClientServices│   │ ConfigureServerServicesAsync │
│ (Test DI Container)   │   │ (API DI Container)           │
│ • HTTP Clients        │   │ • Controllers                │
│ • Test Helpers        │   │ • Business Logic             │
│ • Mocks               │   │ • Repositories               │
└─────────┬────────────┘   └──────────┬───────────────────┘
          │                           │
          │               ┌───────────┴──────────────┐
          │               ▼                          │
          │    ┌──────────────────────────┐          │
          │    │ ASP.NET Core Test Server │          │
          │    │ • Middleware Pipeline    │          │
          │    │ • Endpoint Routing       │          │
          │    │ • Authentication         │          │
          │    └──────────┬───────────────┘          │
          │               │                          │
          ▼               ▼                          ▼
┌──────────────────────────────────────────────────────────┐
│       Constructor Injection in Test Classes               │
│  public MyTest(IService service, IHttpClientFactory http) │
└──────────────────────────────────────────────────────────┘
```

---

## 🧪 Real-World Example: Complete API Testing

```csharp
// Startup.cs
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

// InspectionTest.cs
public class InspectionTest
{
    private readonly IBridgeInspectionApi _api;

    public InspectionTest(IBridgeInspectionApi api)
    {
        _api = api;
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("admin@example.com")]
    public async Task GetBridgesAndInspections_WithDifferentUsers_ShouldWork(string email)
    {
        // Arrange & Act - Login
        var user = await _api.LoginAsAsync(email);
        
        // Assert - User authenticated
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        
        // Act - Get bridges
        var bridges = await _api.BridgeApi.ListAsync();
        
        // Assert - Has bridges
        Assert.NotEmpty(bridges);
        
        // Act - Get inspections for first bridge
        var bridge = bridges.First();
        var inspections = await _api.InspectionApi.ListAsync(bridge.Id);
        
        // Assert - Has inspections
        Assert.NotEmpty(inspections);
    }

    [Fact]
    public async Task DownloadInspectionFiles_ShouldReturnZipData()
    {
        // Arrange
        await _api.LoginAsAsync("admin@example.com");
        var bridges = await _api.BridgeApi.ListAsync();
        var bridge = bridges.First();
        var inspections = await _api.InspectionApi.ListAsync(bridge.Id);
        var inspection = inspections.First();
        
        // Act
        var zipBytes = await _api.InspectionApi.DownloadInspectionsFile(bridge.Id, inspection.Id);
        
        // Assert
        Assert.NotEmpty(zipBytes);
        Assert.True(zipBytes.Length > 0);
    }

    [Fact]
    public async Task GetInspectionFiles_ShouldReturnFileList()
    {
        // Arrange
        await _api.LoginAsAsync("user@example.com");
        var bridges = await _api.BridgeApi.ListAsync();
        var bridge = bridges.First();
        var inspections = await _api.InspectionApi.ListAsync(bridge.Id);
        
        // Act
        var files = await _api.InspectionApi.GetAsync(bridge.Id, inspections.First().Id);
        
        // Assert
        Assert.NotEmpty(files);
    }
}
```

---

## 🔧 Advanced Patterns

### In-Memory Database for Tests

```csharp
protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
{
    // Each test run gets unique database
    services.AddDbContext<MyDbContext>(options =>
        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
    
    return;
}
```

---

### Mock External Dependencies

```csharp
protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
{
    // Replace real external API with mock
    services.AddSingleton<IExternalApiClient, MockExternalApiClient>();
    
    // Or use Moq
    var mockClient = new Mock<IExternalApiClient>();
    mockClient.Setup(x => x.GetDataAsync()).ReturnsAsync(new Data());
    services.AddSingleton(mockClient.Object);
    
    return services;
}
```

---

### Custom HTTP Headers

```csharp
services.AddHttpClient<IMyClient, MyClient>((serviceProvider, client) =>
{
    client.BaseAddress = new Uri("https://localhost");
    client.DefaultRequestHeaders.Add("X-Test-Environment", "true");
    client.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

---

### Test Data Seeding

```csharp
protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
{
    app.UseRouting();
    app.UseEndpoints(endpoints => endpoints.MapControllers());
    
    // Seed test data
    using var scope = serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    await dbContext.SeedTestDataAsync();
    
    return;
}
```

---

## 📊 Comparison: Business Logic vs API Testing

| Feature | Business Logic Only | With API Test Host |
|---------|-------------------|-------------------|
| `HasTestHost` | `false` | `true` |
| HTTP Server | ❌ No | ✅ Yes |
| Real HTTP Calls | ❌ No | ✅ Yes |
| Middleware Pipeline | ❌ No | ✅ Yes |
| Controller Discovery | ❌ No | ✅ Auto |
| Use Case | Services, Repositories | REST APIs, gRPC |
| Performance | ⚡ Fast | 🐢 Slower (HTTP overhead) |
| Complexity | 📦 Simple | 🏗️ Complex |

---

## 🎯 Best Practices

### ✅ DO

- **Use separate `appsettings.test.json`** for test configuration
- **Enable `HasTestHost`** only when testing HTTP layer
- **Use In-Memory Database** for isolated tests
- **Mock external dependencies** to avoid flaky tests
- **Use `Theory` with `InlineData`** for parameterized tests
- **Test happy path AND edge cases**
- **Use descriptive test names** like `CreateBook_WithValidData_ShouldSucceed`

### ❌ DON'T

- **Don't use real databases** in tests (use in-memory or testcontainers)
- **Don't enable `HasTestHost`** if not testing HTTP layer (performance overhead)
- **Don't share test data** between tests (use isolated databases)
- **Don't hardcode URLs** (use `IConfiguration` or constants)
- **Don't ignore cleanup** (dispose resources properly)

---

## 🚨 Troubleshooting

### Test Server Not Starting

**Problem**: `TestHttpClientFactory.Instance.Host` is null

**Solution**: Check that `HasTestHost = true` and `ConfigureServerServicesAsync`/`ConfigureServerMiddlewareAsync` are implemented.

---

### Controllers Not Found

**Problem**: 404 errors when calling API endpoints

**Solution**: Verify `TypeToChooseTheRightAssemblyWithControllersToMap` points to correct assembly containing controllers.

```csharp
// Make sure this points to your API project
protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);
```

---

### User Secrets Not Loading

**Problem**: Configuration values from User Secrets are null

**Solution**: 
1. Verify `TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup)`
2. Check User Secrets are set:
   ```bash
   dotnet user-secrets list
   ```
3. Ensure `UserSecretsId` in `.csproj`:
   ```xml
   <PropertyGroup>
     <UserSecretsId>your-guid-here</UserSecretsId>
   </PropertyGroup>
   ```

---

### XUnit v3 Package Errors

**Problem**: Build errors like "The type or namespace name 'Fact' could not be found"

**Solution**: Ensure you're using `xunit.v3` package, **NOT** `xunit`:

```bash
# Remove old package
dotnet remove package xunit

# Add XUnit v3
dotnet add package xunit.v3 --version 3.0.1
```

**Check your `.csproj`:**
```xml
<!-- ❌ WRONG -->
<PackageReference Include="xunit" Version="2.x.x" />

<!-- ✅ CORRECT -->
<PackageReference Include="xunit.v3" Version="3.0.1" />
```

---

### Test Runner Not Discovering Tests

**Problem**: Tests don't appear in Visual Studio Test Explorer or `dotnet test` finds no tests

**Solution**: 
1. Ensure `xunit.runner.visualstudio` version 3.1.4+:
   ```xml
   <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
     <PrivateAssets>all</PrivateAssets>
   </PackageReference>
   ```

2. Ensure `IsTestProject` is set:
   ```xml
   <PropertyGroup>
     <IsTestProject>true</IsTestProject>
   </PropertyGroup>
   ```

3. Rebuild solution:
   ```bash
   dotnet clean
   dotnet build
   ```

---

### Constructor Injection Not Working

**Problem**: Test constructor parameters not being resolved

**Solution**: 
1. Verify `Startup` class inherits from `StartupHelper`
2. Ensure services are registered in `ConfigureClientServices`:
   ```csharp
   protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
   {
       services.AddMyServices(); // Make sure all dependencies are registered!
       return services;
   }
   ```

3. Check test project references `Rystem.Test.XUnit`:
   ```bash
   dotnet list package | Select-String "Rystem.Test.XUnit"
   ```

---

### Health Check Fails

**Problem**: Test server fails health check validation

**Solution**: 
1. Set `AddHealthCheck = false` if you don't need health checks
2. Or ensure all dependencies are properly configured:
   ```csharp
   protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
   {
       services.AddHealthChecks()
           .AddCheck("database", () => HealthCheckResult.Healthy())
           .AddCheck("external-api", () => HealthCheckResult.Healthy());
       return;
   }
   ```

---

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)
- **🔧 NuGet Package**: [Rystem.Test.XUnit](https://www.nuget.org/packages/Rystem.Test.XUnit)
