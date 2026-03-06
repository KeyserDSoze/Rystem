# Rystem.RepositoryFramework.Api.Server

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Server)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Server)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Automatically exposes all registered Repository/CQRS services as HTTP endpoints with a single call. No controllers required.

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Server
```

---

## Quick Start

```csharp
// Program.cs
builder.Services
    .AddRepository<SuperUser, string>(repo => repo.WithInMemory());

builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository API")
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger()
    .WithDocumentation()
    .WithDefaultCors("https://example.com");

var app = builder.Build();

app.UseApiFromRepositoryFramework()
   .WithNoAuthorization();

app.Run();
```

Generated endpoints follow the pattern `/{path}/{version}/{EntityName}/{Method}`, e.g. `GET /api/v1/SuperUser/Get?key=...`.

---

## `AddApiFromRepositoryFramework()` — Service Configuration

Call this in `builder.Services` to configure how the API is generated.

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("My API")
    .WithPath("api")
    .WithVersion("v2")
    .WithSwagger()
    .WithDocumentation()
    .WithDefaultCors("https://myapp.com", "https://staging.myapp.com");
```

### Configuration Methods

| Method | Description |
|---|---|
| `WithDescriptiveName(string)` | Sets the API display name (used in Swagger UI). Defaults to the assembly name. |
| `WithPath(string)` | Base path prefix for all generated endpoints. Default: `"api"`. |
| `WithVersion(string)` | Version segment appended after the path (e.g. `"v1"` → `/api/v1/...`). |
| `WithDocumentation()` | Enables XML documentation on generated Swagger operations. |
| `WithSwagger()` | Registers Swagger/OpenAPI generation and UI. |
| `WithMapApi()` | Enables the `/Map` endpoint that returns the full repository map. |
| `WithModelsApi()` | Enables the `/Models` endpoint that returns entity model schemas. |
| `WithName<T>(string)` | Overrides the route segment for entity type `T` (default is `typeof(T).Name`). |
| `WithDefaultCors(params string[] domains)` | Adds a CORS policy allowing the specified origins. |
| `WithDefaultCorsWithAllOrigins()` | Adds a CORS policy that allows any origin. |
| `WithCors(Action<CorsOptions>)` | Full control over CORS policy registration. |
| `WithOpenIdAuthentication(Action<ApiIdentitySettings>)` | Configures OpenID Connect in Swagger UI. Returns `IIdentityApiBuilder`. |
| `ConfigureAzureActiveDirectory(IConfiguration)` | Shortcut that reads `AzureAd:*` settings from `appsettings.json`. Returns `IIdentityApiBuilder`. |

### OpenID / Azure AD Authentication in Swagger

```csharp
// Generic OpenID
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .WithOpenIdAuthentication(settings =>
    {
        settings.AuthorizationUrl = new Uri("https://login.example.com/oauth2/authorize");
        settings.TokenUrl = new Uri("https://login.example.com/oauth2/token");
        settings.ClientId = "your-client-id";
        settings.Scopes.Add(new ApiIdentityScopeSettings { Value = "api.read", Description = "Read access" });
    })
    .WithAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    });
```

```csharp
// Azure Active Directory shortcut (reads from appsettings.json "AzureAd" section)
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .ConfigureAzureActiveDirectory(builder.Configuration)
    .WithAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    });
```

`appsettings.json` section expected by `ConfigureAzureActiveDirectory`:

```json
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "Scopes": "api.read api.write"
}
```

---

## `UseApiFromRepositoryFramework()` — Endpoint Registration + Authorization

Call this on `app` (or any `IEndpointRouteBuilder`) to register the endpoints and configure authorization.

### No Authorization

```csharp
app.UseApiFromRepositoryFramework()
   .WithNoAuthorization();
```

### Require Authentication (No Specific Policy)

```csharp
app.UseApiFromRepositoryFramework()
   .WithDefaultAuthorization();
```

### Policy per Method Group

```csharp
app.UseApiFromRepositoryFramework()
    .SetPolicyForQuery().With("ReadUser")
    .And()
    .SetPolicyForCommand().With("WriteUser")
    .Build();
```

### Policy per Specific Methods

```csharp
app.UseApiFromRepositoryFramework()
    .SetPolicyForAll().With("AuthenticatedUser")
    .And()
    .SetPolicy(RepositoryMethods.Insert, RepositoryMethods.Update, RepositoryMethods.Delete).With("AdminUser")
    .Build();
```

### Authorization for a Single Entity Type

```csharp
app.UseApiFromRepositoryFramework<WebApplication, SuperUser, string>()
    .SetPolicyForCommand().With("SuperAdmin")
    .Build();
```

---

## Authorization Builder Reference

### `IApiAuthorizationBuilder`

| Method | Returns | Description |
|---|---|---|
| `WithDefaultAuthorization()` | `IEndpointRouteBuilder` | Enables authentication on all endpoints, no policy name required. |
| `WithNoAuthorization()` | `IEndpointRouteBuilder` | Registers all endpoints without any auth middleware. |
| `SetPolicyForAll()` | `IApiAuthorizationPolicy` | Targets all `RepositoryMethods`. |
| `SetPolicyForCommand()` | `IApiAuthorizationPolicy` | Targets `Insert`, `Update`, `Delete`, `Batch`. |
| `SetPolicyForQuery()` | `IApiAuthorizationPolicy` | Targets `Exist`, `Get`, `Query`, `Operation`. |
| `SetPolicy(params RepositoryMethods[])` | `IApiAuthorizationPolicy` | Targets the specific methods listed. |
| `Build()` | `IEndpointRouteBuilder` | Finalizes and registers all endpoints. |

### `IApiAuthorizationPolicy`

| Method | Returns | Description |
|---|---|---|
| `With(params string[] policies)` | `IApiAuthorizationPolicy` | Assigns one or more policy names to the selected methods. |
| `And()` | `IApiAuthorizationBuilder` | Returns the builder to chain another `SetPolicy*` call. |
| `Empty()` | `IApiAuthorizationBuilder` | Clears policies for the selected methods (require auth, no policy name). |
| `Build()` | `IEndpointRouteBuilder` | Finalizes and registers all endpoints. |

---

## `RepositoryMethods` Enum

| Value | HTTP | Description |
|---|---|---|
| `Get` | `GET` | Retrieve a single entity by key. |
| `Exist` | `GET` | Check whether an entity with the given key exists. |
| `Query` | `POST` | Execute a filtered, sorted, paged query. |
| `Operation` | `POST` | Execute aggregates: `Count`, `Max`, `Min`, `Average`, `Sum`. |
| `Insert` | `POST` | Insert a new entity. |
| `Update` | `POST` | Update an existing entity. |
| `Delete` | `GET` | Delete an entity by key. |
| `Batch` | `POST` | Execute multiple insert/update/delete operations in one request. |
| `Bootstrap` | `GET` | Trigger schema/data warm-up (e.g. EF Core migrations). |
| `All` | — | Shorthand that matches every method in a policy assignment. |

---

## Warm-Up / Bootstrap

If you use EF Core migrations, in-memory pre-population, or other initialization that must run before the first request, call `WarmUpAsync()` on the app before `app.Run()`:

```csharp
await app.Services.WarmUpAsync();
app.Run();
```

Alternatively, expose the `Bootstrap` endpoint so clients can trigger it on demand (it is included automatically when available).

---

## Multiple Repositories Example

All registered repositories are exposed automatically — no extra configuration per entity:

```csharp
builder.Services
    .AddRepository<User, string>(repo => repo.WithInMemory())
    .AddRepository<Product, Guid>(repo => repo.WithInMemory())
    .AddQuery<Order, long>(repo => repo.WithInMemory());

builder.Services.AddApiFromRepositoryFramework()
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger();

var app = builder.Build();

app.UseApiFromRepositoryFramework()
   .SetPolicyForAll().With("AuthenticatedUser")
   .And()
   .SetPolicyForCommand().With("Manager")
   .Build();
```

This generates endpoints like:
- `GET  /api/v1/User/Get?key=...`
- `POST /api/v1/User/Query`
- `POST /api/v1/User/Insert`
- `GET  /api/v1/Product/Get?key=...`
- `POST /api/v1/Order/Query`
- etc.

---

## Related Packages

| Package | Description |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Core interfaces and repository registration |
| `Rystem.RepositoryFramework.Api.Client` | .NET client for consuming these endpoints |
| `rystem.repository.client` | TypeScript/npm client for consuming these endpoints |
