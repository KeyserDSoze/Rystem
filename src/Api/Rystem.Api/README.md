# Rystem.Api

[![NuGet](https://img.shields.io/nuget/v/Rystem.Api)](https://www.nuget.org/packages/Rystem.Api)

Core package for the **Rystem.Api** framework. Turns any .NET interface registered in DI into a minimal-API endpoint automatically — no controllers, no routing boilerplate. The same interface is then consumed by the client package (`Rystem.Api.Client`) as a strongly-typed HttpClient proxy.

Target framework: `net10.0`

## Installation

```bash
dotnet add package Rystem.Api
```

---

## Packages overview

| Package | Role |
|---------|------|
| **`Rystem.Api`** | Core: endpoint configuration, attributes, builder API |
| `Rystem.Api.Server` | Maps endpoints to ASP.NET Core minimal-API routes, OpenAPI/Swagger/Scalar |
| `Rystem.Api.Client` | Generates strongly-typed HttpClient proxy for each registered interface |
| `Rystem.Api.Client.Authentication.BlazorServer` | JWT / Social auth interceptor for Blazor Server |
| `Rystem.Api.Client.Authentication.BlazorWasm` | JWT auth interceptor for Blazor WASM |

---

## How it works

1. Register your DI service as usual (`AddTransient<IProductService, ProductService>`).
2. Call `AddEndpoint<IProductService>(...)` to declare it as an API endpoint.
3. Call `app.UseEndpointApi()` to map all declared endpoints to minimal-API routes.
4. Optionally call `AddClientsForAllEndpointsApi(...)` on the client to get a proxy that calls those routes.

Routes follow the pattern: `{BasePath}{EndpointName}/{MethodName}`  
Default: `api/IProductService/GetProduct`

---

## Endpoint registration

### Simple registration

```csharp
services.AddEndpoint<IProductService>(builder =>
{
    // all public methods are automatically discovered
});
```

### Full customisation

```csharp
services
    .ConfigureEndpoints(x =>
    {
        x.BasePath = "api/v2/";        // default: "api/"
        x.RemoveAsyncSuffix = true;    // GetProductAsync → GetProduct (default: true)
    })
    .AddEndpoint<IProductService>(builder =>
    {
        builder
            .SetEndpointName("Products")                    // override route segment (default: interface name minus "I")
            .SetMethodName(x => x.GetProductAsync, "Get")  // rename a method
            .Remove(x => x.InternalMethodAsync)            // hide a method from the API
            .AddAuthorizationForAll("AdminPolicy")          // require policy on all methods
            .AddAuthorization(x => x.DeleteProductAsync, "SuperAdminPolicy"); // single method
    })
    // Factory-named service: one endpoint per registered factory name
    .AddEndpointWithFactory<IEmbeddingService>(builder =>
    {
        builder.SetupParameter(
            x => x.SearchAsync,
            "query",
            p => p.Example = "example query");  // Swagger example
    });
```

### Named-factory services

When a service is registered multiple times under different factory names, use `AddEndpointWithFactory<T>`. The framework resolves all registered factory names at startup and generates one endpoint per name automatically:

```csharp
// DI
services.AddFactory<IEmbeddingService, OpenAiEmbeddingService>(EmbeddingType.OpenAi);
services.AddFactory<IEmbeddingService, CohereEmbeddingService>(EmbeddingType.Cohere);

// API
services.AddEndpointWithFactory<IEmbeddingService>();
// generates:
//   POST api/EmbeddingService/OpenAi/Search
//   POST api/EmbeddingService/Cohere/Search
```

For a specific named instance (not auto-discovery):

```csharp
services.AddEndpoint<ISalubry>(builder => { ... }, name: "Doma");
```

---

## `ApiEndpointBuilder<T>` method reference

| Method | Description |
|--------|-------------|
| `SetEndpointName(name)` | Override the route segment (default: type name without `I` prefix) |
| `SetMethodName(expr, name)` | Rename a method in the route |
| `SetMethodName(MethodInfo, name)` | Rename by reflection |
| `Remove(expr)` | Exclude a method from the API |
| `Remove(name)` | Exclude by method name |
| `AddAuthorization(expr)` | Require authentication on a method (any authenticated user) |
| `AddAuthorization(expr, policies)` | Require named policies on a method |
| `AddAuthorizationForAll()` | Require authentication on all methods |
| `AddAuthorizationForAll(policies)` | Require named policies on all methods |
| `SetupParameter(expr, paramName, setup)` | Override parameter binding/example for a specific parameter |

---

## Parameter binding attributes

Decorate interface method parameters to control how ASP.NET Core binds and how the client sends them:

| Attribute | Binding source | Notes |
|-----------|---------------|-------|
| `[Query]` | Query string | `Name` overrides the query key; `IsRequired` (default `true`) |
| `[Path(Index = n)]` | Route path segment | `Index` selects which `{param}` slot; `-1` = all |
| `[Header]` | Request header | `Name` overrides the header name |
| `[Cookie]` | Request cookie | `Name` overrides the cookie name |
| `[Body]` | JSON request body | `IsRequired` (default `true`) |
| `[Form]` | Multipart form-data field | `Name` overrides the form key |

Parameters **without** an attribute are bound automatically:
- Primitive types → query string
- Complex types / `Stream` / `IFormFile` / `IHttpFile` → body (triggers POST + multipart if multiple streams)

### Example

```csharp
public interface IDocumentService
{
    Task<bool> UploadAsync(
        [Path(Index = 0)] string folderId,
        [Query] string fileName,
        [Header] string correlationId,
        [Cookie(Name = "tenant")] string tenantId,
        [Form(Name = "file")] Stream content,
        [Form(Name = "meta")] DocumentMetadata metadata);

    Task<Document?> GetAsync([Query] string id);
    Task<bool> DeleteAsync([Body] DeleteRequest request);
}
```

The framework automatically selects `GET` or `POST` based on whether complex/stream parameters exist, and uses multipart encoding when multiple streams/form fields are present.

---

## `EndpointsManager` (global settings)

Configure via `ConfigureEndpoints`:

| Property | Default | Description |
|----------|---------|-------------|
| `BasePath` | `"api/"` | URL prefix for all generated routes |
| `RemoveAsyncSuffix` | `true` | Strip `Async` from method names in the route |

