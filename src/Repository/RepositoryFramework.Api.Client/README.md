# Rystem.RepositoryFramework.Api.Client

HTTP client integration for Repository/CQRS services exposed by `Rystem.RepositoryFramework.Api.Server`.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Client
```

## Quick start

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://localhost:7058");
    });
});
```

## Add resilience policy

```csharp
builder.Services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder
            .WithHttpClient("https://localhost:7058")
            .WithDefaultRetryPolicy();
    });
});
```

You can also use `ClientBuilder` to add custom Polly policies.

## CQRS-only registrations

```csharp
builder.Services.AddCommand<User, string>(commandBuilder =>
{
    commandBuilder.WithApiClient(apiBuilder => apiBuilder.WithHttpClient("https://localhost:7058"));
});

builder.Services.AddQuery<User, string>(queryBuilder =>
{
    queryBuilder.WithApiClient(apiBuilder => apiBuilder.WithHttpClient("https://localhost:7058"));
});
```

## Interceptors

### Global interceptor for all clients

```csharp
builder.Services.AddApiClientInterceptor<MyGlobalInterceptor>();
```

### Model-specific interceptor

```csharp
builder.Services.AddApiClientSpecificInterceptor<User, MyUserInterceptor>();
```

### Model + key specific interceptor

```csharp
builder.Services.AddApiClientSpecificInterceptor<User, string, MyUserKeyInterceptor>();
```

## JWT authorization interceptor

Use one of these packages for default auth interceptors:

- `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorServer`
- `Rystem.RepositoryFramework.Api.Client.Authentication.BlazorWasm`

Then register:

```csharp
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
```
