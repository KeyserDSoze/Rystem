### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Test.XUnit

`Rystem.Test.XUnit` is a test-project helper for xUnit v3 that combines constructor injection with an optional in-process ASP.NET Core test host.

The package builds on `Xunit.DependencyInjection` and adds a `StartupHelper` model so your test project can choose between:

- DI-only tests
- full HTTP integration tests with `Microsoft.AspNetCore.TestHost`

It is most useful when:

- you want test classes to receive services directly in their constructors
- you want one reusable test startup pipeline for a whole test assembly
- you want an in-process API host without managing your own `WebApplicationFactory`
- you want to reuse the same Rystem DI and runtime service patterns inside tests

The strongest source-backed examples are the package implementation itself and the real startup used by `src/Core/Test/Rystem.Test.UnitTest/Startup.cs` plus the HTTP tests in `src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.Test.XUnit
```

The current `10.x` package targets `net10.0` and references:

- `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.TestHost`
- `xunit.v3`
- `Xunit.DependencyInjection`
- `Rystem.DependencyInjection.Web`
- `Rystem.Concurrency`

The package is designed for xUnit v3 projects, not the older `xunit` package line.

## Package Architecture

The package is centered around three main pieces.

| Piece | Purpose |
|---|---|
| `StartupHelper` | Declarative startup model for the test project |
| `HostTester` | Lazily creates and starts the shared in-process test host |
| `TestHttpClientFactory` | Exposes `IHttpClientFactory` backed by the shared `TestServer` |

At a high level, the flow is:

- xUnit bootstraps the test assembly through `Xunit.DependencyInjection`
- your test project provides a `Startup : StartupHelper`
- `ConfigureHost(...)` builds configuration for the test run
- `ConfigureServices(...)` either configures DI only or starts the shared test host
- constructor injection works for all test classes in the assembly

## Table of Contents

- [Package Architecture](#package-architecture)
- [How it Works](#how-it-works)
- [Implement a Startup Class](#implement-a-startup-class)
- [StartupHelper Reference](#startuphelper-reference)
  - [Required members](#required-members)
  - [Optional members](#optional-members)
- [DI-only Tests](#di-only-tests)
- [HTTP Integration Tests with Test Host](#http-integration-tests-with-test-host)
- [Shared Test Server Behavior](#shared-test-server-behavior)
- [IHttpClientFactory in Tests](#ihttpclientfactory-in-tests)
- [Repository Examples](#repository-examples)

---

## How it Works

`StartupHelper` exposes two framework-facing methods that `Xunit.DependencyInjection` can call:

- `ConfigureHost(IHostBuilder)`
- `ConfigureServices(IServiceCollection, HostBuilderContext)`

From the current implementation:

- `ConfigureHost(...)` loads `AppSettingsFileName`, then optional user secrets, then environment variables
- `ConfigureServices(...)` checks `HasTestHost`
- if `HasTestHost = true`, it calls `HostTester.CreateHostServerAsync(...)`
- the shared `TestHttpClientFactory` is then registered into the test DI container
- `ConfigureClientServices(...)` always runs and is where you add the services that test classes consume

So the package gives you one central startup object for both configuration and test DI wiring.

---

## Implement a Startup Class

Create a `Startup` class in the test project that inherits from `StartupHelper`.

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rystem.Test.XUnit;

public sealed class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => false;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;

    protected override IServiceCollection ConfigureClientServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMyBusinessLogic();
        return services;
    }
}
```

The real project already contains a concrete example in `src/Core/Test/Rystem.Test.UnitTest/Startup.cs`, where:

- `HasTestHost` is `true`
- controllers are mapped from the API assembly containing `ServiceController`
- client DI gets an `HttpClient` named `client`
- server DI gets application services from `AddTestServices()`

---

## StartupHelper Reference

### Required members

`StartupHelper` requires these members:

| Member | Purpose |
|---|---|
| `AppSettingsFileName` | Test configuration file name, for example `appsettings.test.json` |
| `HasTestHost` | Enables or disables the in-process ASP.NET Core host |
| `TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration` | Selects the assembly used for user secrets discovery |
| `TypeToChooseTheRightAssemblyWithControllersToMap` | Selects the controller assembly for the test host |
| `ConfigureClientServices(...)` | Registers services consumed directly by test classes |

Even when the host is enabled, `ConfigureClientServices(...)` is still the place for the services injected into the tests themselves.

### Optional members

These virtual members can be overridden when needed:

| Member | Default | Purpose |
|---|---|---|
| `WithHttps` | `true` | Configures the in-process server base address as `https://localhost:443` |
| `PreserveExecutionContext` | `false` | Passes through to `TestServerOptions.PreserveExecutionContext` |
| `AddHealthCheck` | `true` | Adds `/healthz` and validates host startup through it |
| `ConfigureServerServicesAsync(...)` | no-op | Registers services inside the test server container |
| `ConfigureServerMiddlewareAsync(...)` | no-op | Configures the in-process middleware pipeline |

If `HasTestHost` is `false`, the server-specific members are effectively ignored.

---

## DI-only Tests

Use this mode when you only need constructor injection and no HTTP pipeline.

```csharp
public sealed class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => false;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => null;

    protected override IServiceCollection ConfigureClientServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMyRepositories();
        services.AddMyBusinessLogic();
        return services;
    }
}

public sealed class OrderServiceTest
{
    private readonly IOrderService _orderService;

    public OrderServiceTest(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnValidOrder()
    {
        var order = await _orderService.CreateAsync(new CreateOrderRequest { ProductId = 1 });
        Assert.NotNull(order);
    }
}
```

In this mode, the package behaves mainly as an xUnit DI bootstrapper.

---

## HTTP Integration Tests with Test Host

Use this mode when the tests should hit a real ASP.NET Core pipeline in-process.

A trimmed version looks like this:

```csharp
using Microsoft.AspNetCore.Builder;

public sealed class Startup : StartupHelper
{
    protected override string? AppSettingsFileName => "appsettings.test.json";
    protected override bool HasTestHost => true;
    protected override Type? TypeToChooseTheRightAssemblyToRetrieveSecretsForConfiguration => typeof(Startup);
    protected override Type? TypeToChooseTheRightAssemblyWithControllersToMap => typeof(Program);

    protected override IServiceCollection ConfigureClientServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient("client", x =>
        {
            x.BaseAddress = new Uri("https://localhost:443");
        });
        return services;
    }

    protected override ValueTask ConfigureServerServicesAsync(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddMyApiServices();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask ConfigureServerMiddlewareAsync(
        IApplicationBuilder app,
        IServiceProvider serviceProvider)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        return ValueTask.CompletedTask;
    }
}
```

The real repository example is `src/Core/Test/Rystem.Test.UnitTest/Startup.cs`.

And a real HTTP test using constructor injection is `src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs`:

```csharp
public class RuntimeServiceProviderTest
{
    private readonly HttpClient _httpClient;

    public RuntimeServiceProviderTest(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("client");
    }

    [Fact]
    public async Task AddOneServiceAtRuntimeAsync()
    {
        var response = await _httpClient.GetAsync("Service/Get");
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

That pattern is the core value of the package: tests can just request `IHttpClientFactory`, typed clients, repositories, or any other service directly in their constructors.

---

## Shared Test Server Behavior

When `HasTestHost = true`, `HostTester` creates the in-process server lazily and only once.

Important implementation details:

- server creation is guarded with `Rystem.Concurrency` via `ILock`
- the built host is stored on the singleton `TestHttpClientFactory.Instance`
- later test classes reuse the same host instead of rebuilding it
- controllers are discovered with `AddApplicationPart(...)`
- if `AddHealthCheck = true`, the framework maps `/healthz` and performs a startup probe after the host starts

No real TCP port is opened. The package uses `Microsoft.AspNetCore.TestHost`, so requests stay in-process.

---

## IHttpClientFactory in Tests

When the test host is enabled, `ConfigureServices(...)` registers:

```csharp
services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Instance);
```

`TestHttpClientFactory` creates clients from the shared `TestServer` and pre-populates these request headers:

| Header | Value |
|---|---|
| `Origin` | `https://localhost` |
| `Access-Control-Request-Method` | `POST` |
| `Access-Control-Request-Headers` | `X-Requested-With` |

That means tests can either:

- inject `IHttpClientFactory` directly
- register named or typed clients in `ConfigureClientServices(...)`

Example:

```csharp
public sealed class HealthCheckTest
{
    private readonly HttpClient _httpClient;

    public HealthCheckTest(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldBeHealthy()
    {
        var response = await _httpClient.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## Repository Examples

The most useful references for this package are:

- `StartupHelper`: [src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/StartupHelper.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/StartupHelper.cs)
- Shared test host builder: [src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/HostTester.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/HostTester.cs)
- Test HTTP client factory: [src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/TestHttpClientFactory.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Rystem.Test.XUnit/TestHostWithDi/TestHttpClientFactory.cs)
- Real startup example: [src/Core/Test/Rystem.Test.UnitTest/Startup.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Startup.cs)
- Real HTTP integration tests: [src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs)
- Test API used by that startup: [src/Extensions/Tests/Test/Rystem.Test.TestApi/Program.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Test/Rystem.Test.TestApi/Program.cs)

This README is intentionally architecture-first because `Rystem.Test.XUnit` is mostly infrastructure. The main value is not a long API surface, but the way it composes xUnit DI, configuration loading, and an optional shared in-process ASP.NET Core host.
