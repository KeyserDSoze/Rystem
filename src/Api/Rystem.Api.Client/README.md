# Rystem.Api.Client

`Rystem.Api.Client` is the runtime client layer for `Rystem.Api`.

It does not generate source files. Instead, it builds runtime `DispatchProxy` implementations for your interfaces and sends HTTP requests based on the shared endpoint metadata.

## Installation

```bash
dotnet add package Rystem.Api.Client
```

## Architecture

The client package expects the same endpoint registrations that the server uses.

In practice, both sides should call a shared method, like the sample `AddBusiness()` in `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`.

The flow is:

1. share `ConfigureEndpoints(...)` and `AddEndpoint...(...)` registrations between client and server
2. register clients with `AddClientsForAllEndpointsApi(...)` or `AddClientForEndpointApi<T>(...)`
3. optionally add `IRequestEnhancer` implementations
4. inject the interface directly and call it like a normal service

Each generated client is a transient factory-backed proxy that uses a named `HttpClient` internally.

## Minimal setup

This follows the sample app in `src/Api/Test/Rystem.Api.TestClient/Program.cs`.

```csharp
builder.Services.AddBusiness();

builder.Services.AddClientsForAllEndpointsApi(http =>
{
    http.ConfigurationHttpClientForApi(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7117");
    });
});
```

After that, you can inject the interface directly:

```csharp
public sealed class ProductPage
{
    private readonly ISalubry _service;

    public ProductPage(ISalubry service)
        => _service = service;

    public Task<bool> RunAsync(int id, Stream stream)
        => _service.GetAsync(id, stream);
}
```

## Registration APIs

| Method | Purpose |
| --- | --- |
| `AddClientsForAllEndpointsApi(Action<HttpClientBuilder>)` | register proxies for every endpoint currently known to `EndpointsManager` |
| `AddClientForEndpointApi<T>(Action<HttpClientBuilder>, factoryName?)` | register a proxy for one endpoint type, optionally targeting a named instance |

Generated proxies are registered through the same factory system used elsewhere in the repo and currently use `ServiceLifetime.Transient`.

When you target a named endpoint instance, resolve that proxy through `IFactory<T>` rather than relying on plain interface injection.

## `HttpClientBuilder`

`HttpClientBuilder` exposes:

| Method | Purpose |
| --- | --- |
| `ConfigurationHttpClientForApi(Action<HttpClient>)` | default `HttpClient` configuration for all endpoint proxies |
| `ConfigurationHttpClientForEndpointApi<T>(Action<HttpClient>)` | `HttpClient` configuration only for interface `T` |

Precedence is:

1. endpoint-specific `ConfigurationHttpClientForEndpointApi<T>(...)`
2. shared `ConfigurationHttpClientForApi(...)`
3. default `AddHttpClient(...)` with no extra configuration

## Request enhancers

`IRequestEnhancer` is the client-side interception hook.

```csharp
public sealed class CorrelationEnhancer : IRequestEnhancer
{
    public ValueTask EnhanceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString());
        return ValueTask.CompletedTask;
    }
}
```

Register enhancers with:

| Method | Scope |
| --- | --- |
| `AddEnhancerForAllEndpoints<TEnhancer>()` | all generated clients |
| `AddEnhancerForEndpoint<TEnhancer, T>()` | only the client for interface `T` |

Enhancer execution order is:

1. all-endpoint enhancers
2. endpoint-specific enhancers

The sample client registers both kinds in `src/Api/Test/Rystem.Api.TestClient/Program.cs`.

## Request building behavior

The client proxy uses the endpoint metadata from `Rystem.Api` to build requests.

- query parameters are appended to the URI
- path parameters are appended as URI segments
- cookies are written into the `cookie` header
- headers are copied into request headers
- body parameters become either plain content or multipart form-data
- `IAsyncEnumerable<T>` results are read as streamed JSON
- `Stream`, `IHttpFile`, and `IFormFile` response shapes are supported

The route pattern mirrors the server-side metadata:

```text
{BasePath}{EndpointName}/{FactoryName?}{MethodName}
```

## Important caveats

### The client depends on shared registrations

If the client and server do not share the same `ConfigureEndpoints(...)` and `AddEndpoint...(...)` definitions, route generation drifts and calls fail.

### Factory auto-expansion is not mirrored client-side

`AddEndpointWithFactory<T>()` is expanded automatically on the server during `UseEndpointApi()`, but the client package does not do the same factory-name fan-out automatically.

If you need named factory-backed clients, use explicit named registrations or `AddClientForEndpointApi<T>(..., factoryName)` for the specific named endpoint you want.

### URI composition is intentionally simple

Query, path, header, and cookie values are composed very directly.

The current implementation does not provide polished URL encoding or rich handling for non-primitive query/header/cookie payloads, so keep those cases simple.

### This is runtime proxy generation

There is no compile-time client code generation here. Debugging behavior is closer to a dynamic proxy than to a handwritten typed `HttpClient`.

## Grounded by sample files

- `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`
- `src/Api/Test/Rystem.Api.TestClient/Program.cs`
- `src/Api/Test/Rystem.Api.TestClient/Services/Enhancer.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IColam.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IEmbeddingService.cs`

Use this package when you want to call `Rystem.Api.Server` endpoints through the same interface contracts instead of writing manual HTTP code.
