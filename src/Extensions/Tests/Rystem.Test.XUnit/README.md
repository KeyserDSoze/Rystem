### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Test.XUnit

XUnit integration testing framework with built-in **Dependency Injection** and **ASP.NET Core Test Host** support. Based on [Xunit.DependencyInjection](https://github.com/pengweiqhca/Xunit.DependencyInjection), it lets you inject services directly into test class constructors and optionally spin up a real in-process ASP.NET Core server for HTTP integration tests.

## 📦 Installation

```bash
dotnet add package Rystem.Test.XUnit
```

**Recommended test project packages:**

```xml
<ItemGroup>
  <PackageReference Include="Rystem.Test.XUnit" Version="10.0.6" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
  <PackageReference Include="xunit.v3" Version="3.2.2" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>
```

> **Requires XUnit v3.** The `xunit.v3` package replaces the old `xunit` package.

## Table of Contents

- [Rystem.Test.XUnit](#rystemtestxunit)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [How it Works](#how-it-works)
- [Implement a Startup Class](#implement-a-startup-class)
- [StartupHelper — Reference](#startuphelper--reference)
  - [Abstract members (must override)](#abstract-members-must-override)
  - [Virtual members (override if needed)](#virtual-members-override-if-needed)
- [Scenario 1: DI-only (no HTTP server)](#scenario-1-di-only-no-http-server)
- [Scenario 2: Full API integration with Test Host](#scenario-2-full-api-integration-with-test-host)
- [Test Server Behaviour](#test-server-behaviour)
- [IHttpClientFactory in Tests](#ihttpclientfactory-in-tests)

---

## How it Works

`Rystem.Test.XUnit` builds on `Xunit.DependencyInjection` to wire a standard `IHostBuilder` into the XUnit process. You provide a `Startup` class that extends `StartupHelper`; the framework calls it the same way ASP.NET Core calls a `Startup` at app startup.

- **`ConfigureHost`** — loads `appsettings.json` + User Secrets + environment variables.
- **`ConfigureServices`** — optionally boots an in-process `TestServer` (one per test run, created lazily once and shared), then calls your `ConfigureClientServices` to populate the DI container that test classes consume.
- Constructor injection then works automatically in every test class.

---

## Implement a Startup Class

Create a `Startup` class in your test project that extends `StartupHelper`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

namespace MyApp.Tests;

public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => false; // set true for HTTP tests
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null; // only needed when HasTestHost = true

    protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMyBusinessLogic();
        services.AddMyRepositories();
        return services;
    }
}
```

---

## StartupHelper — Reference

### Abstract members (must override)

| Member | Description |
|---|---|
| `AppSettingsFileName` | Path to the appsettings file to load (e.g. `"appsettings.test.json"`) |
| `HasTestHost` | `true` to start an in-process ASP.NET Core `TestServer`; `false` for DI-only |
| `TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration` | Type from the assembly whose User Secrets to load; usually `typeof(Startup)` |
| `TypeToChooseTheRightAssemblyWithControllersToMap` | Type from the API assembly to discover controllers from; `null` when `HasTestHost = false` |
| `ConfigureClientServices` | Register services consumed by test classes (HTTP clients, helpers, mocks, …) |

### Virtual members (override if needed)

| Member | Default | Description |
|---|---|---|
| `WithHttps` | `true` | Use `https://localhost:443` for the test server; `false` for plain HTTP |
| `PreserveExecutionContext` | `false` | Preserve `AsyncLocal<T>` context across async calls in the test server |
| `AddHealthCheck` | `true` | Add a `/healthz` endpoint; framework validates it returns `Healthy` after server start |
| `ConfigureServerServicesAsync` | no-op | Register services for the test server DI container |
| `ConfigureServerMiddlewareAsync` | no-op | Configure the test server's middleware pipeline |

---

## Scenario 1: DI-only (no HTTP server)

Use this when testing services, repositories, and business logic that don't require HTTP.

```csharp
public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => false;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;

    protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MyDbContext>(opt => opt.UseInMemoryDatabase("TestDb"));
        services.AddMyRepositories();
        services.AddMyBusinessLogic();
        return services;
    }
}

public class OrderServiceTest
{
    private readonly IOrderService _orderService;

    // Constructor injection — works automatically
    public OrderServiceTest(IOrderService orderService)
        => _orderService = orderService;

    [Fact]
    public async Task CreateOrder_ShouldReturnValidOrder()
    {
        var order = await _orderService.CreateAsync(new CreateOrderRequest { ProductId = 1 });

        Assert.NotNull(order);
        Assert.True(order.Id > 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetOrder_ShouldReturnCorrectProduct(int productId)
    {
        var order = await _orderService.CreateAsync(new CreateOrderRequest { ProductId = productId });
        var retrieved = await _orderService.GetAsync(order.Id);

        Assert.Equal(productId, retrieved.ProductId);
    }
}
```

---

## Scenario 2: Full API integration with Test Host

Use this for end-to-end HTTP testing against a real ASP.NET Core pipeline running in-process.

```csharp
using Microsoft.AspNetCore.Builder;

public class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => true;
    protected override bool WithHttps => true;
    protected override bool AddHealthCheck => true;
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program); // API assembly
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);

    // Services for test classes (HTTP clients, helpers)
    protected override IServiceCollection ConfigureClientServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMyApiClient, MyApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://localhost");
        });
        return services;
    }

    // Services for the in-process test server
    protected override async ValueTask ConfigureServerServicesAsync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddMyApiServices();
        services.AddDbContext<MyDbContext>(opt =>
            opt.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
    }

    // Middleware pipeline for the in-process test server
    protected override async ValueTask ConfigureServerMiddlewareAsync(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}

public class BooksApiTest
{
    private readonly IMyApiClient _client;

    public BooksApiTest(IMyApiClient client) => _client = client;

    [Fact]
    public async Task GetBooks_ShouldReturnNonEmptyList()
    {
        var books = await _client.GetBooksAsync();
        Assert.NotEmpty(books);
    }

    [Theory]
    [InlineData("Clean Code")]
    [InlineData("DDD")]
    public async Task CreateBook_ThenGet_ShouldMatch(string title)
    {
        var created = await _client.CreateBookAsync(new CreateBookRequest { Title = title });
        var retrieved = await _client.GetBookByIdAsync(created.Id);

        Assert.Equal(title, retrieved.Title);
    }
}
```

---

## Test Server Behaviour

- The in-process `TestServer` is **created once** per test run and shared across all test classes. A lock ensures only one instance is ever started, even with parallelism.
- If `AddHealthCheck = true`, the framework performs a `GET /healthz` after startup and throws if it does not return `Healthy`.
- The test server uses `Microsoft.AspNetCore.TestHost` — no real network port is opened; all HTTP traffic is in-process.
- Controllers are discovered via `AddApplicationPart` using the assembly of `TypeToChooseTheRightAssemblyWithControllersToMap`.

---

## IHttpClientFactory in Tests

When `HasTestHost = true`, an `IHttpClientFactory` singleton backed by the test server is registered automatically in the client DI container. Any `HttpClient` created through it connects directly to the in-process server with these default headers pre-set:

| Header | Value |
|---|---|
| `Origin` | `https://localhost` |
| `Access-Control-Request-Method` | `POST` |
| `Access-Control-Request-Headers` | `X-Requested-With` |

Inject `IHttpClientFactory` directly, or use typed clients registered in `ConfigureClientServices`:

```csharp
public class MyTest
{
    private readonly HttpClient _http;

    public MyTest(IHttpClientFactory factory)
        => _http = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_ShouldBeHealthy()
    {
        var response = await _http.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

