# Rystem.Api.Server

`Rystem.Api.Server` is the ASP.NET Core runtime layer for `Rystem.Api`.

It takes the endpoint metadata recorded by `AddEndpoint<T>(...)` / `AddEndpointWithFactory<T>(...)` and turns it into minimal API routes.

## Installation

```bash
dotnet add package Rystem.Api.Server
```

## What this package adds

The server package provides two main entry points:

- `AddServerIntegrationForRystemApi(...)`
- `UseEndpointApi()`

It also adds:

- OpenAPI document setup
- optional Swagger UI
- optional Scalar UI
- optional model-export endpoint via `UseEndpointApiModels()`

## Typical setup

This follows the structure used in `src/Api/Test/Rystem.Api.TestServer/Program.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IColam, Comad>();
builder.Services.AddFactory<ISalubry, Salubry>();
builder.Services.AddFactory<ISalubry, Salubry2>("Doma");

builder.Services.AddBusiness();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("policy", policy =>
    {
        policy.RequireClaim("name");
    });
});

builder.Services.AddServerIntegrationForRystemApi(options =>
{
    options.HasScalar = true;
    options.HasSwagger = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpointApi();
app.UseEndpointApiModels();

app.Run();
```

## `AddServerIntegrationForRystemApi(...)`

`AddServerIntegrationForRystemApi(...)` currently does three things:

1. registers `EndpointOptions` as a singleton
2. adds OpenAPI with a custom document transformer
3. adds Swagger generation only when `HasSwagger = true`

`EndpointOptions` exposes:

| Property | Default | Meaning |
| --- | --- | --- |
| `HasSwagger` | `false` | enable Swagger UI |
| `HasScalar` | `false` | enable Scalar API reference |

## `UseEndpointApi()`

`UseEndpointApi()` is the real mapping step.

At runtime it:

1. reads `EndpointOptions`
2. maps raw OpenAPI JSON
3. optionally maps Scalar and Swagger UI
4. expands `AddEndpointWithFactory<T>()` registrations into one endpoint set per factory name
5. maps every endpoint method as `MapGet` or `MapPost`

Without `UseEndpointApi()`, the metadata registered in `Rystem.Api` never becomes HTTP routes.

## Route pattern

The effective route pattern is:

```text
{BasePath}{EndpointName}/{FactoryName?}{MethodName}/{PathParams...}
```

Examples from the sample domain registrations:

- `rapi/Salubriend/Get`
- `rapi/E/Doma/Get`
- `rapi/EmbeddingService/First/Search`
- `rapi/EmbeddingService/Second/Search`

Path-bound parameters are appended after the method segment.

## HTTP verb selection

The server uses only `GET` and `POST`.

| Condition | Verb |
| --- | --- |
| no body-bound parameters | `GET` |
| at least one body-bound parameter | `POST` |

Multipart handling is inferred separately:

| Condition | Request shape |
| --- | --- |
| more than one body parameter | `multipart/form-data` |
| any `Stream`, `IFormFile`, or `IHttpFile` parameter | `multipart/form-data` |
| single non-stream body parameter | plain request body |

## Binding behavior

`UseEndpointApi()` honors the shared binding metadata from `Rystem.Api`:

- query parameters come from `Request.Query`
- cookie parameters come from `Request.Cookies`
- header parameters come from `Request.Headers`
- path parameters come from route segments
- body parameters come either from the raw request body or from multipart form fields/files

It also handles:

- `CancellationToken` injection
- `IFormFile` input
- `IHttpFile` input
- `Stream` input and output
- `IAsyncEnumerable<T>` output

## Authorization behavior

Authorization is driven by the metadata attached in `ApiEndpointBuilder<T>`.

At mapping time the server applies these rules:

- `Policies == null` -> `AllowAnonymous()`
- `Policies.Length == 0` -> `RequireAuthorization()`
- named policies -> `RequireAuthorization(policies)`

That means:

- no authorization metadata leaves the route anonymous
- `AddAuthorizationForAll()` means authenticated users only
- `AddAuthorizationForAll("policy")` means authenticated users that satisfy that policy

## Factory-backed endpoints

`AddEndpointWithFactory<T>()` is expanded on the server by resolving `IFactoryNames<T>`.

For each factory name, `UseEndpointApi()` creates a concrete endpoint set with the factory segment baked into the route.

So this pattern:

```csharp
builder.Services.AddFactory<IEmbeddingService, EmbeddingService1>(EmbeddingType.First);
builder.Services.AddFactory<IEmbeddingService, EmbeddingService2>(EmbeddingType.Second);

builder.Services.AddEndpointWithFactory<IEmbeddingService>();
```

becomes routes such as:

- `rapi/EmbeddingService/First/Search`
- `rapi/EmbeddingService/Second/Search`

## `UseEndpointApiModels()`

`UseEndpointApiModels()` adds a model-export endpoint for the currently hardcoded language list.

Right now that means:

- `GET /Business/Models/Typescript`

It inspects endpoint method return types and parameter types, filters out primitives and infrastructure types, and returns generated model text.

This endpoint does not use `BasePath`.

## Important caveats

### Property getters can become routes

If the shared `Rystem.Api` registration includes interface properties, the server will map them unless you removed those methods earlier.

### Factory expansion happens here, not in the core package

If you rely on `AddEndpointWithFactory<T>()`, remember that the real fan-out happens only when the server maps endpoints.

### OpenAPI raw JSON is always mapped

`UseEndpointApi()` always maps the raw OpenAPI document. Swagger UI and Scalar are the optional parts.

## Grounded by sample files

- `src/Api/Test/Rystem.Api.TestServer/Program.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IColam.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/IEmbeddingService.cs`

Use this package when you want the interface metadata from `Rystem.Api` to become real ASP.NET Core endpoints.
