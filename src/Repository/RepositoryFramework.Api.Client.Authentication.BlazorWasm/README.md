# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm)

Bearer token integration for `Rystem.RepositoryFramework.Api.Client` in **Blazor WebAssembly** applications. Uses `IAccessTokenProvider` from `Microsoft.AspNetCore.Components.WebAssembly.Authentication` to attach access tokens to every repository HTTP request.

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm
```

---

## Quick Start

```csharp
// Program.cs (Blazor WASM)
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("OidcProvider", options.ProviderOptions);
    options.ProviderOptions.DefaultScopes.Add("api.access");
});

builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});

// Global — applies to all repository clients
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
```

---

## DI Registration

### Global — all repository clients

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
```

### With explicit scopes

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = ["api://your-api-id/.default", "api.read"];
});
```

### Model-specific

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

### Model + key specific

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product, int>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

---

## How it works

`TokenManager` uses `IAccessTokenProvider` (injected by the Blazor WASM auth infrastructure):

1. **Cache check** — if a token was acquired previously and it does not expire within the next 5 minutes, the cached token is reused.
2. **Token request** — if no cached token exists or it is about to expire, `IAccessTokenProvider.RequestAccessToken()` is called (with `AccessTokenRequestOptions.Scopes` when scopes are configured).
3. **Header injection** — a valid token is attached as `Authorization: Bearer {token}` via `HttpClient.DefaultRequestHeaders.Authorization`.
4. **Silent failure** — if token acquisition throws, the request proceeds without the `Authorization` header (no exception propagated to the caller).

---

## `AuthenticatorSettings`

```csharp
public class AuthenticatorSettings
{
    // OAuth scopes to request. When null or empty, RequestAccessToken() is called without scopes.
    public string[]? Scopes { get; set; }

    // Optional — handle token acquisition exceptions
    public Func<Exception, IServiceProvider, Task>? ExceptionHandler { get; set; }
}
```

---

## Extension method reference

All methods are on `IServiceCollection`:

| Method | Scope |
|---|---|
| `AddDefaultAuthorizationInterceptorForApiHttpClient(settings?)` | All repository clients |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T>(settings?)` | Only `T` repository client |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>(settings?)` | Only `T`/`TKey` repository client |

---

## Related Packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Api.Client` | Base HTTP client — interceptor registration |
| `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer` | Bearer token integration for Blazor Server (MSAL + Social Login) |
| `Rystem.RepositoryFramework.Api.Server` | Server-side REST endpoint generation |
