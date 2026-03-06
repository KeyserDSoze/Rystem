# Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm

Default JWT/Bearer interceptor integration for Repository Api.Client in Blazor WebAssembly applications.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm
```

## Prerequisites

- `Rystem.RepositoryFramework.Api.Client`
- Blazor WebAssembly auth configured with access tokens

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

## Scope interceptor to a specific repository

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient<User, string>();
```

## Notes

- The interceptor adds bearer tokens before API requests.
- Keep token acquisition and refresh configured in your WASM authentication setup.
