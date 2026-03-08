### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)

Blazor Server authentication helpers for `Rystem.RepositoryFramework.Api.Client`.

This package plugs concrete token managers into the API client's interceptor pipeline so repository calls can automatically add bearer tokens in Blazor Server hosts.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer
```

The current package metadata in `src/Repository/RepositoryFramework.Api.Client.Authentication.BlazorServer/RepositoryFramework.Api.Client.Authentication.BlazorServer.csproj` is:

- package id: `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer`
- version: `10.0.6`
- target framework: `net10.0`
- main auth dependency: `Microsoft.Identity.Abstractions` `11.0.0`
- companion dependency: `Rystem.Authentication.Social.Blazor`

---

## Package architecture

| Area | Purpose |
|---|---|
| `TokenManager` | Microsoft Identity / MSAL path via `IAuthorizationHeaderProvider` |
| `SocialTokenManager` | Social-login path via `SocialLoginManager` |
| Convenience extensions | Register the right token manager into the base API-client interceptor pipeline |
| Shared `AuthenticatorSettings` | Reuse the settings model from `Rystem.RepositoryFramework.Api.Client` |

---

## Mental model

This package does not implement a new HTTP client. It only provides Blazor Server-specific token sources for the base repository API client.

The actual HTTP behavior still comes from `Rystem.RepositoryFramework.Api.Client`:

- request interceptors add headers
- response interceptors can retry after `401 Unauthorized`
- repository calls still use the same API-client routes and payload shapes

This package only decides how the bearer token is acquired.

---

## Option 1: Microsoft Identity / MSAL

This path is built on `IAuthorizationHeaderProvider` from `Microsoft.Identity.Abstractions`.

### Typical setup

```csharp
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(["api://your-api-id/.default"])
    .AddInMemoryTokenCaches();

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});

builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});
```

### How `TokenManager` works

- it asks `IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(...)` for a full authorization header value
- it expects that value to look like `Bearer {token}`
- it splits the returned string and applies it to `HttpClient.DefaultRequestHeaders.Authorization`
- if token acquisition fails, it returns `null` and the request proceeds without an auth header

### Scope requirement

In practice you should provide `settings.Scopes` for this path, because the underlying token-manager implementation passes `_settings.Scopes!` to `CreateAuthorizationHeaderForUserAsync(...)`.

---

## Option 2: Rystem social login

This path is built on `SocialLoginManager` from `Rystem.Authentication.Social.Blazor`.

### Typical setup

```csharp
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();

builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});
```

### How `SocialTokenManager` works

- it calls `SocialLoginManager.FetchTokenAsync()`
- when a token exists, it sends `Authorization: Bearer {accessToken}`
- when a token is missing, it calls `LogoutAsync()`
- if `NavigationManager` is available, it forces a page refresh so the login flow can restart

This makes the social-login path more aggressive than the MSAL path: missing auth state can immediately push the user back into a sign-in flow.

### Scope of the helper

This package only exposes the social-login convenience method in its global form:

```csharp
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();
```

There is no package-level convenience overload for model-only or model-plus-key-only social auth.

---

## Extension methods

All convenience methods are registered on `IServiceCollection`.

| Method | Token source | Scope |
|---|---|---|
| `AddDefaultAuthorizationInterceptorForApiHttpClient(settings?)` | `TokenManager` | all repository clients |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T>(settings?)` | `TokenManager` | one model |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>(settings?)` | `TokenManager` | one model + key |
| `AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient(settings?)` | `SocialTokenManager` | all repository clients |

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

- `Scopes` matters for the Microsoft Identity path
- `ExceptionHandler` is used by the base bearer interceptor when request-time token enrichment throws

---

## Source-backed behavior notes

### Request/response behavior comes from the base client package

These helpers ultimately call the generic registration methods in `Rystem.RepositoryFramework.Api.Client`, so the same caveats still apply here:

- global and model-specific registrations add both request and response interceptors
- model-plus-key registration adds only the request interceptor in the current implementation

That means automatic `401` refresh-and-retry is available for the global and model-specific registrations, but not fully for the model-plus-key registration path.

### Token acquisition failures are soft failures

For the Microsoft Identity path, the token manager catches token-acquisition exceptions and returns `null`, so the outgoing request can continue without the bearer header.

### Social-login failures trigger logout behavior

For the social-login path, failure to fetch a token leads to logout plus optional forced navigation refresh.

---

## Practical examples

### Global MSAL registration

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = builder.Configuration["AzureAd:Scopes"]!.Split(' ');
});
```

### Model-specific MSAL registration

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

### Global social-login registration

```csharp
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();
```

---

## Related packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Api.Client` | Base repository HTTP client and interceptor pipeline |
| `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm` | Equivalent auth helpers for Blazor WebAssembly |
| `Rystem.Authentication.Social.Blazor` | Social-login infrastructure used by `SocialTokenManager` |
| `Rystem.RepositoryFramework.Api.Server` | Matching server package |

Read this package after `src/Repository/RepositoryFramework.Api.Client/README.md` when your repository client runs in Blazor Server.
