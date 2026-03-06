# Rystem.Api.Client.Authentication.BlazorServer

[![NuGet](https://img.shields.io/nuget/v/Rystem.Api.Client.Authentication.BlazorServer)](https://www.nuget.org/packages/Rystem.Api.Client.Authentication.BlazorServer)

OpenID Connect / Azure AD authentication interceptor for `Rystem.Api.Client` on **Blazor Server**. Automatically attaches a Bearer token (or social-login token) to every outgoing API request, using `Microsoft.Identity.Web` token acquisition.

Target framework: `net10.0`

## Installation

```bash
dotnet add package Rystem.Api.Client.Authentication.BlazorServer
```

---

## Prerequisites

You must have `Microsoft.Identity.Web` configured (Azure AD, Azure AD B2C, or similar OpenID Connect provider):

```csharp
var scopes = new[] { builder.Configuration["AzureAd:Scopes"]! };

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(scopes)
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
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

Both methods register a `TokenManager` as an `IRequestEnhancer` that calls `IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync` and injects the resulting `Bearer <token>` header.

---

## Social login interceptor

For social authentication (e.g. Google, Microsoft social, GitHub) use the social variants:

```csharp
// All endpoints
builder.Services.AddSocialAuthenticationForAllEndpoints(settings =>
{
    settings.Scopes = scopes;
});

// Specific endpoint
builder.Services.AddSocialAuthenticationForEndpoint<IProductService>(settings =>
{
    settings.Scopes = scopes;
});
```

---

## Complete Blazor Server example

```csharp
// Program.cs — Blazor Server
var builder = WebApplication.CreateBuilder(args);
var scopes  = new[] { builder.Configuration["AzureAd:Scopes"]! };

// Microsoft Identity Web
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(scopes)
    .AddInMemoryTokenCaches();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
builder.Services.AddAuthorization(o => o.FallbackPolicy = o.DefaultPolicy);

// Shared endpoint declarations (same as server)
builder.Services.AddBusiness();

// Register HTTP clients
builder.Services.AddClientsForAllEndpointsApi(http =>
    http.ConfigurationHttpClientForApi(c => c.BaseAddress = new Uri("https://api.myapp.io")));

// Attach token to all requests
builder.Services.AddAuthenticationForAllEndpoints(s => s.Scopes = scopes);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
```

---

## API reference

| Method | Scope | Description |
|--------|-------|-------------|
| `AddAuthenticationForAllEndpoints(settings)` | All clients | Registers `TokenManager` (Microsoft Identity) for all endpoints |
| `AddAuthenticationForEndpoint<T>(settings)` | One interface | Registers `TokenManager` only for `T` |
| `AddSocialAuthenticationForAllEndpoints(settings)` | All clients | Registers `SocialTokenManager` for all endpoints |
| `AddSocialAuthenticationForEndpoint<T>(settings)` | One interface | Registers `SocialTokenManager` only for `T` |

### `AuthorizationSettings`

| Property | Type | Description |
|----------|------|-------------|
| `Scopes` | `string[]` | OAuth2 / OIDC scopes requested when acquiring the token |