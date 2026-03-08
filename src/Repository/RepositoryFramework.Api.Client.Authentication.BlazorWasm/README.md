### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)

Blazor WebAssembly authentication helpers for `Rystem.RepositoryFramework.Api.Client`.

This package plugs a WASM-friendly token manager based on `IAccessTokenProvider` into the repository API client's interceptor pipeline.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm
```

The current package metadata in `src/Repository/RepositoryFramework.Api.Client.Authentication.BlazorWasm/RepositoryFramework.Api.Client.Authentication.BlazorWasm.csproj` is:

- package id: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm`
- version: `10.0.6`
- target framework: `net10.0`
- main auth dependency: `Microsoft.AspNetCore.Components.WebAssembly.Authentication` `10.0.3`

---

## Package architecture

| Area | Purpose |
|---|---|
| `TokenManager` | Acquire and cache access tokens through `IAccessTokenProvider` |
| Convenience extensions | Register the token manager in the base repository API-client interceptor pipeline |
| Shared `AuthenticatorSettings` | Carry scopes and optional exception handling settings |

---

## Mental model

Like the Blazor Server package, this package does not replace the repository API client. It only supplies a WASM-specific token source.

All HTTP behavior still comes from `Rystem.RepositoryFramework.Api.Client`:

- repository methods map to the same server routes
- request interceptors enrich the outgoing client
- response interceptors can react to non-success responses

This package only handles token acquisition in the browser-hosted auth model.

---

## Typical setup

```csharp
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("OidcProvider", options.ProviderOptions);
    options.ProviderOptions.DefaultScopes.Add("api.access");
});

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = ["api.access"];
});

builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});
```

---

## Extension methods

All convenience methods are registered on `IServiceCollection`.

| Method | Scope |
|---|---|
| `AddDefaultAuthorizationInterceptorForApiHttpClient(settings?)` | all repository clients |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T>(settings?)` | one model |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>(settings?)` | one model + key |

These are just WASM-friendly wrappers over the generic bearer-registration helpers from `Rystem.RepositoryFramework.Api.Client`.

---

## How `TokenManager` works

The token manager keeps a cached `AccessToken` and refreshes it when needed.

### Token lifecycle

1. If there is no cached token, request a new one.
2. If the cached token expires within the next 5 minutes, request a new one.
3. If the cached token is still valid, reuse it.
4. When a token is available, set `Authorization: Bearer {token}` on the outgoing `HttpClient`.

### Scopes

- when `AuthenticatorSettings.Scopes` is present, it calls `RequestAccessToken(new AccessTokenRequestOptions { Scopes = ... })`
- when scopes are missing, it calls the parameterless `RequestAccessToken()` overload

---

## `AuthenticatorSettings`

This package reuses the shared settings model from the core API client package.

```csharp
public class AuthenticatorSettings
{
    public string[]? Scopes { get; set; }
    public Func<Exception, IServiceProvider, Task>? ExceptionHandler { get; set; }
}
```

- `Scopes` directly affects the `IAccessTokenProvider` request
- `ExceptionHandler` is executed by the base bearer interceptor when token enrichment throws

---

## Source-backed behavior notes

### Client-side token cache

The token manager stores the last successful `AccessToken` instance and reuses it until the token is within 5 minutes of expiry.

### Exception behavior

If token acquisition throws, the base bearer interceptor catches the exception and can call `ExceptionHandler` if configured.

### No-token result nuance

If `IAccessTokenProvider.RequestAccessToken()` completes without a usable token, the current implementation returns `string.Empty` rather than `null`.

That means the base bearer interceptor can still set an empty bearer header on that path. The happy path is solid, but this edge case is less polished than the Blazor Server variant.

### Inherited interceptor caveat

Because these helpers use the base API-client registration pipeline:

- global and model-specific registrations add both request and response interceptors
- model-plus-key registration currently adds only the request interceptor

So automatic `401` refresh-and-retry is available for the global and model-specific paths, but not fully for the model-plus-key path.

---

## Practical examples

### Global registration

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = ["api://your-api-id/.default", "api.read"];
});
```

### Model-specific registration

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

### Model-and-key-specific registration

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product, int>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Api.Client` | Base repository HTTP client and interceptor pipeline |
| `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer` | Equivalent auth helpers for Blazor Server |
| `Rystem.RepositoryFramework.Api.Server` | Matching server package |

Read this package after `src/Repository/RepositoryFramework.Api.Client/README.md` when your repository client runs in Blazor WebAssembly.
