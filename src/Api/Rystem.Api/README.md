# Rystem.Api

`Rystem.Api` is the shared metadata package behind the Api area.

It does not expose routes by itself and it does not create HTTP clients by itself. Instead, it records endpoint definitions from interfaces so that:

- `Rystem.Api.Server` can map them as minimal APIs
- `Rystem.Api.Client` can create runtime HTTP proxies from the same definitions

## Installation

```bash
dotnet add package Rystem.Api
```

## Package boundaries

| Package | Role |
| --- | --- |
| `Rystem.Api` | shared endpoint metadata, binding attributes, `IHttpFile`, and builder APIs |
| `Rystem.Api.Server` | ASP.NET Core mapping, OpenAPI, Swagger, Scalar |
| `Rystem.Api.Client` | runtime `DispatchProxy` HTTP clients |
| `Rystem.Api.Client.Authentication.BlazorServer` | request enhancers for Blazor Server token flows |
| `Rystem.Api.Client.Authentication.BlazorWasm` | request enhancers for Blazor WebAssembly token flows |

## Architecture

The core flow is:

1. register your interface implementation in DI on the server
2. call `ConfigureEndpoints(...)` once to set shared defaults
3. call `AddEndpoint<T>(...)` or `AddEndpointWithFactory<T>(...)` to record API metadata
4. let `Rystem.Api.Server` map those endpoints with `UseEndpointApi()`
5. let `Rystem.Api.Client` build proxies from the same endpoint registrations

Internally, endpoint definitions are stored in a singleton `EndpointsManager`.

Because of that, shared configuration should be done before or together with endpoint registration. In practice, treat `ConfigureEndpoints(...)` as startup configuration, not something to call later.

## Minimal shared registration

This pattern is what the sample domain project does in `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`.

```csharp
services
    .ConfigureEndpoints(options =>
    {
        options.BasePath = "rapi/";
    })
    .AddEndpoint<ISalubry>(endpoint =>
    {
        endpoint.SetEndpointName("Salubriend");
        endpoint.AddAuthorizationForAll("policy");
    });
```

That registration is shared metadata. You still need:

- a real DI implementation on the server
- `UseEndpointApi()` from `Rystem.Api.Server`
- `AddClientsForAllEndpointsApi(...)` or `AddClientForEndpointApi<T>(...)` from `Rystem.Api.Client`

## Defaults and route shape

`EndpointsManager` defaults are:

| Setting | Default |
| --- | --- |
| `BasePath` | `api/` |
| `RemoveAsyncSuffix` | `true` |

`EndpointValue` defaults are:

- endpoint name = interface name without the leading `I`
- method name = CLR method name, optionally without `Async`

So this interface:

```csharp
public interface IProductService
{
    Task<Product?> GetAsync(string id);
}
```

becomes this route shape by default:

```text
api/ProductService/Get?id=...
```

If a method has path-bound parameters, the placeholders are appended after the method segment.

## Registration APIs

| Method | Purpose |
| --- | --- |
| `ConfigureEndpoints(Action<EndpointsManager>)` | configure shared defaults like `BasePath` and async-suffix trimming |
| `AddEndpoint<T>(Action<ApiEndpointBuilder<T>>, name?)` | register one interface as one endpoint set |
| `AddEndpointWithFactory<T>(Action<ApiEndpointBuilder<T>>?)` | declare a factory-backed endpoint set to be expanded server-side |

The optional `name` on `AddEndpoint<T>(..., name)` targets a specific named factory instance.

On the server, that means the endpoint is resolved through `IFactory<T>`. On the client, the matching named proxy is also a factory-backed registration rather than a plain default interface registration.

## `ApiEndpointBuilder<T>`

The actual public builder surface is:

| Method | Purpose |
| --- | --- |
| `SetEndpointName(name)` | override the endpoint segment |
| `SetMethodName(expr, name)` | rename a method by expression |
| `SetMethodName(methodInfo, name)` | rename a method by `MethodInfo` |
| `Remove(methodInfo)` | exclude a method |
| `Remove(methodName)` | exclude a method by stored key |
| `AddAuthorization(expr)` | require authenticated access on one method |
| `AddAuthorization(expr, params policies)` | require named policies on one method |
| `AddAuthorizationForAll()` | require authentication on all methods |
| `AddAuthorizationForAll(params policies)` | require named policies on all methods |
| `SetupParameter(expr, parameterName, setup)` | override parameter metadata by expression |
| `SetupParameter(methodInfo, parameterName, setup)` | override parameter metadata by `MethodInfo` |

### Recommendation for async methods

The expression-based lookup methods are less reliable when `RemoveAsyncSuffix = true`, because route names and expression names can diverge.

For async methods, prefer the `MethodInfo` overloads when you need precise customization.

Example:

```csharp
var getMethod = typeof(ISalubry).GetMethod(nameof(ISalubry.GetAsync))!;

services.AddEndpoint<ISalubry>(endpoint =>
{
    endpoint.SetEndpointName("Salubriend");
    endpoint.SetMethodName(getMethod, "Gimme");
    endpoint.SetupParameter(getMethod, "id", parameter =>
    {
        parameter.Location = ApiParameterLocation.Body;
        parameter.Example = 56;
    });
});
```

## Parameter binding attributes

These attributes are the shared binding contract for both server and client packages.

| Attribute | Meaning |
| --- | --- |
| `[Query]` | bind from query string |
| `[Path(Index = n)]` | bind from path segments |
| `[Header]` | bind from request headers |
| `[Cookie]` | bind from cookies |
| `[Body]` | bind from request body |
| `[Form]` | bind from multipart form-data |

Properties on these attributes include:

- `Name` for query/header/cookie/form overrides
- `IsRequired` on all binding attributes
- `Index` on `[Path]`

## Default parameter behavior

Without an attribute:

- primitive values default to query binding
- non-primitive values default to body binding
- `CancellationToken` is recognized specially
- `Stream`, `IFormFile`, and `IHttpFile` are treated as streamed body values

`POST` and multipart behavior are inferred later from this metadata by the server and client packages.

## `IHttpFile`

`Rystem.Api` also defines `IHttpFile`, a lightweight file abstraction used by the generated API pipeline.

Use it when you want an interface-level file type that is not tied directly to ASP.NET Core's `IFormFile`.

## Important caveats

### All interface methods are discovered

Registration currently uses `typeof(T).GetMethods()`.

That means property getters are also exposed unless you remove them. The sample `ITeamCalculator` in `src/Api/Test/Rystem.Api.Test.Domain/ITeamCalculator.cs` would expose getter methods such as `get_IsLive` if left untouched.

### Overloads are auto-suffixed

If an interface contains overloaded methods, the builder keeps them unique by renaming later ones to `_2`, `_3`, and so on.

### Factory expansion is a server concern

`AddEndpointWithFactory<T>()` records the intent to expand named factory endpoints, but the actual fan-out happens in `Rystem.Api.Server` during `UseEndpointApi()`.

The client package does not currently perform the same automatic expansion from factory names.

## Grounded by sample files

- `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IColam.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/ISalubry.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/ITeamCalculator.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IEmbeddingService.cs`

Use this package when you want to define the shared API contract once and let the server and client packages interpret it consistently.
