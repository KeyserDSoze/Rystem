### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Api.Server

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Server)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Server)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Api.Server)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Server)

Minimal-API endpoint generation for repositories and CQRS registrations created through the Rystem repository framework.

This package reads the runtime repository registry and turns registered repositories, commands, and queries into HTTP endpoints without controllers.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Server
```

The current package metadata in `src/Repository/RepositoryFramework.Api.Server/RepositoryFramework.Api.Server.csproj` is:

- package id: `Rystem.RepositoryFramework.Api.Server`
- version: `10.0.6`
- target framework: `net10.0`
- framework reference: `Microsoft.AspNetCore.App`
- Swagger package: `Swashbuckle.AspNetCore` `10.1.4`

---

## Package architecture

| Area | Purpose |
|---|---|
| `AddApiFromRepositoryFramework()` | Collect API-level settings such as path, version, Swagger, CORS, and diagnostics |
| `UseApiFromRepositoryFramework()` | Read `RepositoryFrameworkRegistry` and map endpoints |
| Authorization builder | Apply endpoint-level auth rules by repository method group |
| Repository authorization handlers | Add repository-specific authorization logic via `IRepositoryAuthorization<T, TKey>` |
| Map/models diagnostics | Expose generated endpoint metadata and generated TypeScript model definitions |
| Swagger/OpenID helpers | Configure Swagger UI and OpenID metadata for the generated API |

---

## Mental model

This package does not replace repository registration. It sits on top of it.

The flow is:

1. Register repositories, commands, or queries through the abstractions layer.
2. Optionally mark methods as exposable or hidden on each repository registration.
3. Configure API behavior with `AddApiFromRepositoryFramework()`.
4. Map the generated endpoints with `UseApiFromRepositoryFramework()`.

The package discovers what to expose from `RepositoryFrameworkRegistry`, so there is no controller layer to keep in sync.

---

## Quick start

This is the real shape used in the sample host under `src/Repository/RepositoryFramework.Test/RepositoryFramework.WebApi/Program.cs`.

```csharp
builder.Services.AddRepository<SuperUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(120, 5)
            .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
    });
});

builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository Api")
    .WithSwagger()
    .WithMapApi()
    .WithModelsApi()
    .WithDocumentation()
    .WithDefaultCorsWithAllOrigins();

var app = builder.Build();
await app.Services.WarmUpAsync();

app.UseAuthentication();
app.UseAuthorization();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

---

## Service-side configuration

`AddApiFromRepositoryFramework()` returns `IApiBuilder`.

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("My API")
    .WithPath("api")
    .WithVersion("v2")
    .WithSwagger()
    .WithMapApi()
    .WithModelsApi();
```

### Configuration methods

| Method | What it does |
|---|---|
| `WithDescriptiveName(string)` | Sets the API name used by Swagger UI |
| `WithPath(string)` | Sets the base path segment, default `api` |
| `WithVersion(string)` | Adds a version segment after the base path |
| `WithName<T>(string)` | Overrides the route segment used for model `T` |
| `WithSwagger()` | Registers Swagger/OpenAPI services and UI support |
| `WithDocumentation()` | Enables extra Swagger UI customization and PDF export support |
| `WithMapApi()` | Enables the repository map endpoint |
| `WithModelsApi()` | Enables the generated-model endpoint |
| `WithDefaultCors(params string[] domains)` | Registers a default named CORS policy for the provided origins |
| `WithDefaultCorsWithAllOrigins()` | Registers a permissive named CORS policy |
| `WithCors(Action<CorsOptions>)` | Lets you configure CORS directly |
| `WithOpenIdAuthentication(...)` | Configures OpenID metadata for Swagger UI and returns `IIdentityApiBuilder` |
| `ConfigureAzureActiveDirectory(IConfiguration)` | Shortcut that reads `AzureAd:*` settings and configures OpenID metadata |

### Important note about `WithDocumentation()`

`WithDocumentation()` does not load XML doc comments.

In the current implementation it enhances Swagger UI by injecting custom UI content and a RapiPDF-based export button. It only has visible effect when Swagger is also enabled.

---

## Route generation

The main generated route shape is:

- default registration: `/{path}/{version?}/{modelName}/{method}`
- named registration: `/{path}/{version?}/{modelName}/{factoryName}/{method}`

Examples:

- `/api/v1/SuperUser/Get`
- `/api/v1/SuperUser/Insert`
- `/api/v1/SuperUser/inmemory/Get`

If you override a name with `WithName<T>(...)`, that custom segment is used instead of `typeof(T).Name`.

---

## Mapping all repositories or one repository

### Map every registered repository

```csharp
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();
```

### Map a single entity type

```csharp
app.UseApiFromRepositoryFramework<WebApplication, SuperUser, string>()
    .WithNoAuthorization();
```

### Map a specific named registration

```csharp
app.UseApiFromRepositoryFramework<WebApplication, SuperUser, string>(name: "inmemory")
    .WithNoAuthorization();
```

---

## Request shapes by method

The generated HTTP shape depends not only on the repository method, but also on whether `TKey` is JSON-serializable according to `KeySettings<TKey>`.

### Non-JSON keys

For primitive/string/`IKey`-style keys, the generated endpoints are:

| Method | HTTP | Shape |
|---|---|---|
| `Get` | `GET` | `?key=...` |
| `Exist` | `GET` | `?key=...` |
| `Delete` | `GET` | `?key=...` |
| `Insert` | `POST` | query `key` + body `T` |
| `Update` | `POST` | query `key` + body `T` |
| `Query` | `POST` | body `SerializableFilter?` |
| `Operation` | `POST` | query `op`, optional query `returnType`, body `SerializableFilter?` |
| `Batch` | `POST` | body `BatchOperations<T, TKey>` |

### JSON-style keys

For keys treated as JSONable by `KeySettings<TKey>`, the generated endpoints switch to request bodies for key-based methods.

| Method | HTTP | Shape |
|---|---|---|
| `Get` | `POST` | body `TKey` |
| `Exist` | `POST` | body `TKey` |
| `Delete` | `POST` | body `TKey` |
| `Insert` | `POST` | body `Entity<T, TKey>` |
| `Update` | `POST` | body `Entity<T, TKey>` |

### Stream endpoints

Two methods also generate streaming variants:

- `POST .../Query/Stream`
- `POST .../Batch/Stream`

These return the underlying async stream instead of materializing everything into a list first.

---

## Repository methods and exposure

The API server only maps methods that are both:

- implemented by the registered service
- allowed by the repository's `ExposedMethods`

That means abstractions-layer settings such as these directly affect the generated API:

- `SetExposable(...)`
- `SetOnlyQueryExposable()`
- `SetOnlyCommandExposable()`
- `SetNotExposable()`

For custom storages that are not marked as default integrations, the mapper also skips methods whose body is just `throw new NotImplementedException(...)`.

---

## Diagnostics endpoints

Two optional endpoints provide metadata about the generated API.

### Repository map

Enable it with:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithMapApi();
```

Route:

```text
/Repository/Map/All
```

This endpoint is not placed under your configured API base path. It is a fixed diagnostic route.

It returns:

- every generated API entry
- route URIs
- repository method names
- authentication/policy metadata
- sample request and response payloads

If you registered repository examples with `SetExamples(...)`, those values are used. Otherwise the package falls back to random sample generation.

### Models endpoint

Enable it with:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithModelsApi();
```

Current route:

```text
/Repository/Models/Typescript
```

This route returns generated TypeScript definitions for repository models and non-primitive keys. It is also outside the main API base path.

---

## Authorization at endpoint-mapping time

`UseApiFromRepositoryFramework()` returns `IApiAuthorizationBuilder`.

### No authorization

```csharp
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();
```

### Require authentication on all endpoints

```csharp
app.UseApiFromRepositoryFramework()
    .WithDefaultAuthorization();
```

This maps endpoints and applies `RequireAuthorization()` without naming a specific policy.

### Query vs command policies

```csharp
app.UseApiFromRepositoryFramework()
    .SetPolicyForQuery().With("ReadUser")
    .And()
    .SetPolicyForCommand().With("WriteUser")
    .Build();
```

### Specific methods

```csharp
app.UseApiFromRepositoryFramework()
    .SetPolicyForAll().With("AuthenticatedUser")
    .And()
    .SetPolicy(RepositoryMethods.Insert, RepositoryMethods.Update, RepositoryMethods.Delete).With("AdminUser")
    .Build();
```

### Builder methods

| Method | Purpose |
|---|---|
| `WithNoAuthorization()` | Map endpoints without auth requirements |
| `WithDefaultAuthorization()` | Require authentication on all mapped endpoints |
| `SetPolicyForAll()` | Target every repository method |
| `SetPolicyForCommand()` | Target `Insert`, `Update`, `Delete`, `Batch` |
| `SetPolicyForQuery()` | Target `Exist`, `Get`, `Query`, `Operation` |
| `SetPolicy(...)` | Target specific `RepositoryMethods` |
| `Build()` | Finalize endpoint mapping |

### Policy chaining methods

| Method | Purpose |
|---|---|
| `With(params string[])` | Attach ASP.NET policy names |
| `Empty()` | Require auth but clear explicit policy names |
| `And()` | Return to the main authorization builder |
| `Build()` | Finalize mapping |

---

## Repository-specific authorization handlers

This package also supports custom authorization handlers that run with repository-specific context.

### Register directly on a repository builder

```csharp
repositoryBuilder
    .ConfigureSpecificPolicies()
    .WithAuthorizationHandler<PolicyHandlerForSuperUser>();
```

### Scan assemblies for handlers

```csharp
builder.Services.ScanAuthorizationForRepositoryFramework();
```

Handlers implement:

```csharp
public interface IRepositoryAuthorization<in T, in TKey>
{
    Task<AuthorizedRepositoryResponse> HandleRequirementAsync(
        IHttpContextAccessor httpContextAccessor,
        AuthorizationHandlerContext context,
        RepositoryRequirement requirement,
        RepositoryMethods method,
        TKey? key,
        T? value);
}
```

This gives the handler access to:

- the current repository method
- the current request
- the parsed key when available
- the parsed entity body for insert/update when available

`ScanAuthorizationForRepositoryFramework()` attaches discovered handlers to repositories with the matching model/key pair and registers the backing ASP.NET authorization policies automatically.

---

## Swagger and OpenID notes

### Swagger

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository Api")
    .WithSwagger();
```

Swagger UI is activated when the endpoints are mapped through `UseApiFromRepositoryFramework(...)`.

### OpenID metadata for Swagger UI

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .WithOpenIdAuthentication(settings =>
    {
        settings.AuthorizationUrl = new Uri("https://login.example.com/oauth2/authorize");
        settings.TokenUrl = new Uri("https://login.example.com/oauth2/token");
        settings.ClientId = "client-id";
        settings.Scopes.Add(new ApiIdentityScopeSettings
        {
            Value = "api.read",
            Description = "Read access"
        });
    })
    .WithAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });
```

Important: `WithOpenIdAuthentication(...)` and `ConfigureAzureActiveDirectory(...)` configure Swagger/OpenAPI authentication metadata. They do not register your runtime ASP.NET authentication handler.

You still need the normal server-side authentication setup, for example:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
```

### Azure Active Directory shortcut

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .ConfigureAzureActiveDirectory(builder.Configuration);
```

This reads:

- `AzureAd:Instance`
- `AzureAd:TenantId`
- `AzureAd:ClientId`
- `AzureAd:Scopes`

and converts them into the OpenID settings used by Swagger UI.

---

## Warm-up and bootstrap

If your repositories rely on startup work such as in-memory data population or other bootstrap actions, keep calling:

```csharp
await app.Services.WarmUpAsync();
```

before serving requests.

The API package can also expose repository `Bootstrap` endpoints when that method is exposable.

One source-backed nuance: the current bootstrap endpoint still flows through the same key-binding pipeline as other key-based methods, even though the key is ignored by the implementation.

---

## Practical examples from the repo

### Rename an entity route segment

The API tests configure custom names like this:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithName<ExtremelyRareUser>("extremelyrareuserrefresh")
    .WithName<CalamityUniverseUser>("calamityuser")
    .WithPath("SuperApi")
    .WithVersion("v2");
```

That produces routes like:

```text
/SuperApi/v2/extremelyrareuserrefresh/Get
```

### Map endpoint metadata and models

The same API tests also enable:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithSwagger()
    .WithMapApi()
    .WithModelsApi()
    .WithDocumentation();
```

### Clear no-auth mapping in tests and samples

The tests and sample host generally use the explicit no-auth mapping form:

```csharp
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();
```

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Abstractions` | Repository registration, exposure flags, keys, filters, and business hooks |
| `Rystem.RepositoryFramework.Api.Client` | .NET client for the generated server endpoints |
| `rystem.repository.client` | TypeScript/npm client for the generated server endpoints |

If you are continuing through the repository area, this package is the bridge between repository registrations and HTTP exposure.
