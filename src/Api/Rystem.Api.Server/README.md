# Rystem.Api.Server

[![NuGet](https://img.shields.io/nuget/v/Rystem.Api.Server)](https://www.nuget.org/packages/Rystem.Api.Server)

ASP.NET Core integration for the **Rystem.Api** framework. Maps all endpoints registered with `AddEndpoint<T>` to minimal-API routes and optionally exposes them via **OpenAPI**, **Scalar**, and **Swagger UI**.

Target framework: `net10.0`  
Dependencies: `Scalar.AspNetCore`, `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.OpenApi`

## Installation

```bash
dotnet add package Rystem.Api.Server
```

---

## Setup

### 1 — Register server services (`Program.cs`)

```csharp
builder.Services.AddServerIntegrationForRystemApi(options =>
{
    options.HasScalar  = true;   // serve Scalar API UI at /scalar/v1
    options.HasSwagger = true;   // serve Swagger UI at /swagger
});
```

`AddServerIntegrationForRystemApi` also registers the OpenAPI document transformer and Swagger/Scalar middleware automatically.

### 2 — Map routes (`Program.cs`)

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpointApi();
```

`UseEndpointApi()` iterates over every `EndpointValue` registered through `AddEndpoint<T>` / `AddEndpointWithFactory<T>` and maps them to minimal-API `MapGet` / `MapPost` routes.

---

## Complete example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Register DI service
builder.Services.AddTransient<IProductService, ProductService>();

// 2. Declare endpoints
builder.Services
    .ConfigureEndpoints(x => x.BasePath = "api/v1/")
    .AddEndpoint<IProductService>(b =>
    {
        b.SetEndpointName("Products")
         .AddAuthorizationForAll("ReadPolicy");
    });

// 3. Add server integration
builder.Services.AddServerIntegrationForRystemApi(opt =>
{
    opt.HasScalar  = true;
    opt.HasSwagger = true;
});

builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization(x =>
{
    x.AddPolicy("ReadPolicy", p => p.RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// 4. Map all endpoints
app.UseEndpointApi();

app.Run();
```

---

## Generated routes

For each method in a registered interface the framework maps a route following this pattern:

```
{BasePath}{EndpointName}/{FactoryName?}/{MethodName}
```

| Condition | HTTP verb |
|-----------|-----------|
| All parameters are primitives / query / path / header / cookie | `GET` |
| Any parameter is a complex object, `Stream`, or `IFormFile` | `POST` |
| Multiple streams / form files | `POST` + `multipart/form-data` |

---

## OpenAPI / Swagger / Scalar

| Option | Endpoint |
|--------|----------|
| `HasScalar = true` | `/scalar/v1` — interactive Scalar API reference |
| `HasSwagger = true` | `/swagger` — Swagger UI, `/openapi/v1.json` — raw OpenAPI JSON |

Both can be enabled simultaneously.

---

## `EndpointOptions` reference

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HasSwagger` | `bool` | `false` | Enable Swagger UI and Swashbuckle |
| `HasScalar` | `bool` | `false` | Enable Scalar API reference UI |
