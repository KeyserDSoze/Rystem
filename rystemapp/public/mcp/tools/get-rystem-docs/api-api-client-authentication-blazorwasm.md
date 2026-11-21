# Rystem.Api.Client.Authentication.BlazorWasm

`Rystem.Api.Client.Authentication.BlazorWasm` adds bearer-token request enhancers for generated `Rystem.Api.Client` proxies in Blazor WebAssembly applications.

Like the Blazor Server package, it is an enhancer layer, not a full auth/client registration package.

## Installation

```bash
dotnet add package Rystem.Api.Client.Authentication.BlazorWasm
```

## What this package adds

The package exposes:

| Method | Scope |
| --- | --- |
| `AddAuthenticationForAllEndpoints(settings)` | all generated clients |
| `AddAuthenticationForEndpoint<T>(settings)` | one generated client |

`AuthorizationSettings` currently exposes only:

- `Scopes`

There are no social-auth variants in this package.

## Typical Blazor WASM setup

```csharp
var scopes = new[] { builder.Configuration["AzureAd:Scopes"]! };

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(scopes[0]);
});

builder.Services.AddBusiness();

builder.Services.AddClientsForAllEndpointsApi(http =>
{
    http.ConfigurationHttpClientForApi(client =>
    {
        client.BaseAddress = new Uri("https://api.myapp.io");
    });
});

builder.Services.AddAuthenticationForAllEndpoints(settings =>
{
    settings.Scopes = scopes;
});
```

## Token flow

The package registers a request enhancer (`TokenManager` / `TokenManager<T>`) that:

1. asks `IAccessTokenProvider` for an access token
2. caches the last token in memory
3. refreshes it when it is missing or within five minutes of expiry
4. attaches it as `Bearer <token>` to the outgoing request

If `Scopes` is configured, token acquisition uses those scopes. Otherwise it requests the default token.

## Important caveats

### This package does not register clients

You still need:

- `Rystem.Api.Client`
- shared endpoint registrations
- generated client registration with `AddClientsForAllEndpointsApi(...)` or `AddClientForEndpointApi<T>(...)`

### Endpoint-specific settings are weaker than they look

The package exposes `AddAuthenticationForEndpoint<T>(...)`, but the base token manager currently resolves `AuthorizationSettings` without using the named client key.

So per-endpoint auth settings are not as isolated as the API shape suggests.

### Failed acquisition handling is rough

When token acquisition fails, the current implementation can produce an empty token string rather than a cleaner null-or-challenge flow.

So treat this package as a lightweight enhancer, not as a replacement for the richer `AuthorizationMessageHandler` patterns in custom Blazor WASM code.

### No social helpers here

If you need the separate social-login helpers from this repo, they are not part of the WASM auth package.

## Grounded by source files

- `src/Api/Client/Rystem.Api.Client.Authentication.BlazorWasm/DefaultInterceptor/ServiceCollectionExtensionsForAuthenticator.cs`
- `src/Api/Client/Rystem.Api.Client.Authentication.BlazorWasm/Authorization/TokenManager.cs`
- `src/Api/Test/Rystem.Api.Test.Domain/ServiceCollectionExtensions.cs`

Use this package when you already have Blazor WebAssembly MSAL auth configured and you want generated `Rystem.Api.Client` calls to pick up bearer tokens automatically.
