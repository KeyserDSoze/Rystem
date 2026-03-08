# Rystem.Api.Client.Authentication.BlazorServer

`Rystem.Api.Client.Authentication.BlazorServer` adds token-based request enhancers for `Rystem.Api.Client` in Blazor Server applications.

It does not register generated API clients for you and it does not configure ASP.NET Core authentication middleware for you. Its job is narrower: attach authorization headers to the HTTP requests created by `Rystem.Api.Client`.

## Installation

```bash
dotnet add package Rystem.Api.Client.Authentication.BlazorServer
```

## What this package adds

The package exposes four extension methods:

| Method | Scope | Token source |
| --- | --- | --- |
| `AddAuthenticationForAllEndpoints(settings)` | all generated clients | `Microsoft.Identity.Web` |
| `AddAuthenticationForEndpoint<T>(settings)` | one generated client | `Microsoft.Identity.Web` |
| `AddSocialAuthenticationForAllEndpoints(settings)` | all generated clients | `Rystem.Authentication.Social.Blazor` |
| `AddSocialAuthenticationForEndpoint<T>(settings)` | one generated client | `Rystem.Authentication.Social.Blazor` |

`AuthorizationSettings` currently exposes only:

- `Scopes`

## Typical Blazor Server setup

This follows the sample client app in `src/Api/Test/Rystem.Api.TestClient/Program.cs`.

```csharp
var scopes = new[] { builder.Configuration["AzureAd:Scopes"]! };

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(scopes)
    .AddInMemoryTokenCaches();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddMicrosoftIdentityConsentHandler();

builder.Services.AddBusiness();

builder.Services.AddClientsForAllEndpointsApi(http =>
{
    http.ConfigurationHttpClientForApi(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7117");
    });
});

builder.Services.AddAuthenticationForAllEndpoints(settings =>
{
    settings.Scopes = scopes;
});
```

## Microsoft Identity flow

The `AddAuthenticationForAllEndpoints(...)` and `AddAuthenticationForEndpoint<T>(...)` methods:

1. create named `AuthorizationSettings`
2. register them as singleton factory options
3. add a request enhancer (`TokenManager` / `TokenManager<T>`)

At request time, that enhancer calls:

```csharp
IAuthorizationHeaderProvider.CreateAuthorizationHeaderForUserAsync(scopes)
```

and applies the returned `Authorization` header to the outgoing request.

For endpoint-specific registration, the generic `TokenManager<T>` uses the named settings key for that interface.

## Social flow

The social variants use `SocialLoginManager` from `Rystem.Authentication.Social.Blazor` and try to attach a bearer token fetched from the current social session.

If a social token cannot be fetched, the enhancer logs the user out and refreshes the navigation state.

## Important caveats

### This package is only an enhancer layer

You still need:

- `Rystem.Api.Client`
- endpoint/client registrations such as `AddClientsForAllEndpointsApi(...)`
- your own Blazor Server auth setup

### The social path is rough in the current implementation

Source-backed caveats:

- the `settings` callback for the social registration methods is currently not stored anywhere meaningful
- `SocialTokenManager` currently builds the `Authorization` header from the wrapper result object instead of the fetched token string

So the Microsoft Identity path is the better-documented and more reliable path today. Treat the social helpers as limited/experimental until they are cleaned up.

### Enhancer ordering still comes from `Rystem.Api.Client`

If you register both global enhancers and endpoint-specific enhancers, all-endpoint enhancers run first, then endpoint-specific enhancers.

## Grounded by sample files

- `src/Api/Test/Rystem.Api.TestClient/Program.cs`
- `src/Api/Client/Rystem.Api.Client.Authentication.BlazorServer/DefaultInterceptor/ServiceCollectionExtensionsForAuthenticator.cs`
- `src/Api/Client/Rystem.Api.Client.Authentication.BlazorServer/Authorization/TokenManager.cs`
- `src/Api/Client/Rystem.Api.Client.Authentication.BlazorServer/Authorization/SocialTokenManager.cs`

Use this package when you already have Blazor Server authentication in place and you want generated `Rystem.Api.Client` calls to carry bearer tokens automatically.
