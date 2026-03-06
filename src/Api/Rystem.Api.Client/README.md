# Rystem.Api.Client

[![NuGet](https://img.shields.io/nuget/v/Rystem.Api.Client)](https://www.nuget.org/packages/Rystem.Api.Client)

Client-side proxy generator for the **Rystem.Api** framework. For every interface registered with `AddEndpoint<T>` on the server, this package creates a strongly-typed `HttpClient` proxy on the client — no hand-written HTTP calls.

Target framework: `net10.0`

## Installation

```bash
dotnet add package Rystem.Api.Client
```

---

## Prerequisites

The client must see the **same `AddEndpoint<T>` / `ConfigureEndpoints` registrations** as the server. Share these registrations through a common project (e.g. a `*.Domain` library called from both `Program.cs` files):

```csharp
// Shared in both server and client
services
    .ConfigureEndpoints(x => x.BasePath = "api/v1/")
    .AddEndpoint<IProductService>(b => b.SetEndpointName("Products"));
```

---

## Register clients

### All endpoints at once

```csharp
builder.Services.AddClientsForAllEndpointsApi(httpBuilder =>
{
    httpBuilder.ConfigurationHttpClientForApi(httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://api.myapp.io");
        httpClient.Timeout     = TimeSpan.FromSeconds(30);
    });
});
```

### Specific endpoint with per-type `HttpClient` configuration

```csharp
builder.Services.AddClientForEndpointApi<IProductService>(httpBuilder =>
{
    httpBuilder.ConfigurationHttpClientForEndpointApi<IProductService>(httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://products-api.myapp.io");
    });
});
```

---

## `HttpClientBuilder` reference

| Method | Description |
|--------|-------------|
| `ConfigurationHttpClientForApi(Action<HttpClient>)` | Default `HttpClient` settings applied to all endpoints |
| `ConfigurationHttpClientForEndpointApi<T>(Action<HttpClient>)` | `HttpClient` settings applied only to interface `T` |

---

## Injecting the proxy

After `AddClientsForAllEndpointsApi` (or `AddClientForEndpointApi<T>`), inject the interface directly. The framework resolves a runtime proxy that translates every call to an HTTP request:

```csharp
public sealed class ProductPage
{
    private readonly IProductService _products;

    // Inject exactly like a local service
    public ProductPage(IProductService products)
        => _products = products;

    public async Task<Product?> LoadAsync(string id)
        => await _products.GetProductAsync(id);
}
```

---

## Request enhancers (`IRequestEnhancer`)

Implement `IRequestEnhancer` to intercept every outgoing `HttpRequestMessage` — useful for adding authentication headers, correlation IDs, logging, etc.:

```csharp
public sealed class CorrelationIdEnhancer : IRequestEnhancer
{
    public ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        return ValueTask.CompletedTask;
    }
}
```

### Register enhancers

```csharp
// Apply to ALL endpoint clients
services.AddEnhancerForAllEndpoints<CorrelationIdEnhancer>();

// Apply only to IProductService client
services.AddEnhancerForEndpoint<CorrelationIdEnhancer, IProductService>();
```

Multiple enhancers can be registered — they all run in registration order before the request is sent.

---

## Complete example

```csharp
// Program.cs (client — e.g. Blazor Server, MAUI, etc.)
var builder = WebApplication.CreateBuilder(args);

// Share endpoint declarations with server
builder.Services
    .ConfigureEndpoints(x => x.BasePath = "api/v1/")
    .AddEndpoint<IProductService>(b => b.SetEndpointName("Products"))
    .AddEndpoint<IOrderService>(b => b.SetEndpointName("Orders"));

// Register HTTP clients
builder.Services.AddClientsForAllEndpointsApi(httpBuilder =>
{
    httpBuilder.ConfigurationHttpClientForApi(httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://api.myapp.io");
    });
    // Override only for IProductService
    httpBuilder.ConfigurationHttpClientForEndpointApi<IProductService>(httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://products.myapp.io");
    });
});

// Add cross-cutting enhancers
builder.Services.AddEnhancerForAllEndpoints<CorrelationIdEnhancer>();
builder.Services.AddEnhancerForEndpoint<ProductSpecificEnhancer, IProductService>();

var app = builder.Build();
app.Run();
```
