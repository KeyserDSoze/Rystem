# Rystem.Api.Client.Authentication.BlazorWasm

[![NuGet](https://img.shields.io/nuget/v/Rystem.Api.Client.Authentication.BlazorWasm)](https://www.nuget.org/packages/Rystem.Api.Client.Authentication.BlazorWasm)

OpenID Connect / Azure AD authentication interceptor for `Rystem.Api.Client` on **Blazor WebAssembly**. Automatically attaches a Bearer token to every outgoing API request using `Microsoft.Identity.Web` MSAL token acquisition.

Target framework: `net10.0`

## Installation

```bash
dotnet add package Rystem.Api.Client.Authentication.BlazorWasm
```

---

## Prerequisites

Configure MSAL authentication in your Blazor WASM `Program.cs`:

```csharp
var scopes = new[] { builder.Configuration["AzureAd:Scopes"]! };

builder.Services
    .AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes[0]);
    });
```

---

## JWT / Microsoft Identity interceptor

### Apply to all endpoint clients

```csharp
builder.Services.AddAuthenticationForAllEndpoints(settings =>
{
    settings.Scopes = scopes;   // string[] of OAuth2 scopes
});
```

### Apply only to a specific interface

```csharp
builder.Services.AddAuthenticationForEndpoint<IProductService>(settings =>
{
    settings.Scopes = scopes;
});
```

Both methods register a `TokenManager` as an `IRequestEnhancer` that acquires a token from the MSAL cache and attaches it as `Bearer <token>` to every HTTP request.

---

## Complete Blazor WASM example

```csharp
// Program.cs — Blazor WASM
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var scopes  = new[] { builder.Configuration["AzureAd:Scopes"]! };

// MSAL
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes[0]);
});

// Shared endpoint declarations (same as server)
builder.Services.AddBusiness();

// Register HTTP clients
builder.Services.AddClientsForAllEndpointsApi(http =>
    http.ConfigurationHttpClientForApi(c => c.BaseAddress = new Uri("https://api.myapp.io")));

// Attach token to all requests
builder.Services.AddAuthenticationForAllEndpoints(s => s.Scopes = scopes);

await builder.Build().RunAsync();
```

---

## API reference

| Method | Scope | Description |
|--------|-------|-------------|
| `AddAuthenticationForAllEndpoints(settings)` | All clients | Registers MSAL `TokenManager` for all endpoint clients |
| `AddAuthenticationForEndpoint<T>(settings)` | One interface | Registers MSAL `TokenManager` only for interface `T` |

> **Note:** The Blazor WASM package does not include the social-login variants. Use [`Rystem.Api.Client.Authentication.BlazorServer`](../Rystem.Api.Client.Authentication.BlazorServer) if you need social login.

### `AuthorizationSettings`

| Property | Type | Description |
|----------|------|-------------|
| `Scopes` | `string[]` | OAuth2 / OIDC scopes requested when acquiring the token from MSAL |