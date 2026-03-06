# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer

[![NuGet](https://img.shields.io/nuget/v/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)](https://www.nuget.org/packages/Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer)

Bearer token integration for `Rystem.RepositoryFramework.Api.Client` in **Blazor Server** applications. Provides two ready-to-use token managers:

- **MSAL / Microsoft Identity** — uses `IAuthorizationHeaderProvider` (from `Microsoft.Identity.Abstractions`)
- **Rystem Social Login** — uses `SocialLoginManager` (from `Rystem.Authentication.Social.Blazor`)

---

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer
```

---

## MSAL / Microsoft Identity setup

Use this when your Blazor Server app authenticates users via Azure AD / Entra ID with MSAL.

### 1. Configure MSAL in `Program.cs`

```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(["api://your-api-id/.default"])
    .AddInMemoryTokenCaches();
```

### 2. Register the bearer interceptor

```csharp
// Global — applies to all repository clients
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});

// Optional: model-specific
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});

// Optional: model + key specific
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<Product, int>(settings =>
{
    settings.Scopes = ["api://your-api-id/.default"];
});
```

### 3. Add the repository client

```csharp
builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});
```

### How it works

`TokenManager` calls `IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(scopes)` which returns a full `"Bearer {token}"` header value. The value is split and applied to `HttpClient.DefaultRequestHeaders.Authorization`. If token acquisition fails, the request proceeds without the header (no exception is thrown to the caller).

---

## Social Login (Rystem.Authentication.Social.Blazor) setup

Use this when your Blazor Server app uses `Rystem.Authentication.Social.Blazor` for social login (Google, Microsoft, GitHub, etc.).

### 1. Configure social login

Follow the setup from [`Rystem.Authentication.Social.Blazor`](https://www.nuget.org/packages/Rystem.Authentication.Social.Blazor).

### 2. Register the social bearer interceptor

```csharp
// Global — applies to all repository clients
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();
```

### 3. Add the repository client

```csharp
builder.Services.AddRepository<Product, int>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://api.example.com");
    });
});
```

### How it works

`SocialTokenManager` calls `SocialLoginManager.FetchTokenAsync()` to retrieve the current access token:

- If a valid token is found, it is attached as `Authorization: Bearer {token}`.
- If the token is expired or missing, `SocialLoginManager.LogoutAsync()` is called and the page is **force-refreshed** (`NavigationManager.Refresh(forceReload: true)`) to redirect the user back to the login flow.

---

## `AuthenticatorSettings`

```csharp
public class AuthenticatorSettings
{
    // Required for MSAL path — the OAuth scopes to request
    public string[]? Scopes { get; set; }

    // Optional — handle token acquisition exceptions
    public Func<Exception, IServiceProvider, Task>? ExceptionHandler { get; set; }
}
```

---

## Extension method reference

All methods are registered on `IServiceCollection`:

| Method | Token source | Scope |
|---|---|---|
| `AddDefaultAuthorizationInterceptorForApiHttpClient(settings?)` | MSAL `IAuthorizationHeaderProvider` | All repository clients |
| `AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient(settings?)` | `SocialLoginManager` | All repository clients |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T>(settings?)` | MSAL `IAuthorizationHeaderProvider` | Only `T` repository client |
| `AddDefaultAuthorizationInterceptorForApiHttpClient<T, TKey>(settings?)` | MSAL `IAuthorizationHeaderProvider` | Only `T`/`TKey` repository client |

> The Social Login variant is only available globally. For model-scoped social token injection, use `AddApiClientSpecificInterceptor` from `Rystem.RepositoryFramework.Api.Client` with a custom `IRepositoryClientInterceptor<T>`.

---

## Related Packages

| Package | Purpose |
|---|---|
| `Rystem.RepositoryFramework.Api.Client` | Base HTTP client — interceptor registration |
| `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm` | Bearer token integration for Blazor WASM |
| `Rystem.Authentication.Social.Blazor` | Social login for Blazor Server/WASM |
| `Rystem.RepositoryFramework.Api.Server` | Server-side REST endpoint generation |
