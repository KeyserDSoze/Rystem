# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer

Default JWT/Bearer interceptor integration for Repository Api.Client in Blazor Server applications.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer
```

## Prerequisites

- `Rystem.RepositoryFramework.Api.Client`
- Blazor Server auth configured with Microsoft Identity

## Quick start

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://localhost:7058");
    });
});

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
```

## Configure identity pipeline (example)

```csharp
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { "api://my-api/access_as_user" })
    .AddInMemoryTokenCaches();

builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();
```

## Scope interceptor to a specific repository

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<User, string>();
```

## Social login variant

If your app uses Rystem social login token manager, use:

```csharp
builder.Services.AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient();
```
