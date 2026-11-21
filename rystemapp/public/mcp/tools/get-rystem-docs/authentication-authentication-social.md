# Rystem.Authentication.Social

`Rystem.Authentication.Social` is the server-side social-login package in the Authentication area.

It is not a full identity platform. Its job is narrower:

- accept provider-specific codes or access tokens from a client
- validate or exchange them through a provider-specific `ITokenChecker`
- issue ASP.NET Core bearer tokens
- expose a small authenticated `/User` endpoint

## Installation

```bash
dotnet add package Rystem.Authentication.Social
```

## Architecture

The server package revolves around two entry points:

- `AddSocialLogin<TProvider>(...)`
- `UseSocialLoginEndpoints()`

The high-level flow is:

1. configure one or more providers in `SocialLoginBuilder`
2. implement `ISocialUserProvider` for app-specific claims and user payloads
3. call `AddSocialLogin<TProvider>(...)`
4. call `UseSocialLoginEndpoints()`
5. let a Blazor or TypeScript client complete the browser-side OAuth flow and call the token endpoint

The package then issues standard ASP.NET bearer tokens through `Results.SignIn(...)`.

## Registration

The real public registration method is:

```csharp
builder.Services.AddSocialLogin<MySocialUserProvider>(settings =>
{
    settings.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    settings.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
    settings.Google.AddUris("https://localhost:7100", "https://app.example.com");

    settings.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    settings.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
    settings.Microsoft.AddUris("https://localhost:7100", "https://app.example.com");

    settings.GitHub.ClientId = builder.Configuration["SocialLogin:GitHub:ClientId"];
    settings.GitHub.ClientSecret = builder.Configuration["SocialLogin:GitHub:ClientSecret"];
}, bearer =>
{
    bearer.BearerTokenExpiration = TimeSpan.FromHours(1);
    bearer.RefreshTokenExpiration = TimeSpan.FromDays(10);
});
```

Signature:

```csharp
IServiceCollection AddSocialLogin<TProvider>(
    Action<SocialLoginBuilder> settings,
    Action<BearerTokenOptions>? action = null,
    ServiceLifetime userProviderLifeTime = ServiceLifetime.Transient)
    where TProvider : class, ISocialUserProvider
```

What it registers:

- ASP.NET bearer-token authentication
- `ISocialUserProvider`
- one named `ITokenChecker` per provider, plus the internal `DotNet` checker for refresh-token reuse
- named `HttpClient`s only for providers whose configuration is active

## Provider configuration model

`SocialLoginBuilder` currently exposes these providers:

- `Google`
- `Microsoft`
- `Facebook`
- `GitHub`
- `Amazon`
- `Linkedin`
- `X`
- `Instagram`
- `Pinterest`
- `TikTok`

The activation rules depend on the settings type:

| Settings type | Used by | Active when |
| --- | --- | --- |
| `SocialDefaultLoginSettings` | Facebook, Amazon | always `true` |
| `SocialLoginSettings` | base type | `ClientId != null` |
| `SocialLoginWithSecretsSettings` | GitHub | `ClientId` and `ClientSecret` are set |
| `SocialLoginWithSecretsAndRedirectSettings` | Google, Microsoft, Linkedin, X, Instagram, Pinterest, TikTok | client id, client secret, and at least one allowed URI are set |

For redirect-based providers, add allowed origins with:

```csharp
settings.Google.AddUri("https://app.example.com");
settings.Google.AddUris("https://app.example.com", "https://staging.example.com");
settings.Google.AddDomainWithProtocolAndPort("localhost", "https", 7100);
```

Important detail: allowed redirects are matched by scheme, host, and port. The stored path is not used as a differentiator when validating the incoming domain.

## `ISocialUserProvider`

`ISocialUserProvider` is the application extension point.

```csharp
public interface ISocialUserProvider
{
    Task<ISocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken);
    IAsyncEnumerable<Claim> GetClaimsAsync(TokenResponse response, CancellationToken cancellationToken);
}
```

Responsibilities:

- `GetClaimsAsync(...)` controls which claims are embedded into the issued bearer token
- `GetAsync(...)` controls what `/api/Authentication/Social/User` returns

The sample provider in `src/Authentication/Tests/Rystem.Authentication.Social.TestApi/Services/SocialUserProvider.cs` shows both:

- adding `ClaimTypes.Name`
- adding a custom language claim with `RystemClaimTypes.Language`
- returning a richer app user model that implements `ISocialUser`

## Endpoints

Expose the runtime endpoints with:

```csharp
app.UseSocialLoginEndpoints();
```

This method does more than mapping endpoints: it also calls `UseAuthentication()` and `UseAuthorization()` internally.

### `api/Authentication/Social/Token`

This endpoint is mapped with `Map(...)`, so it accepts any verb. In practice the bundled clients use:

- `GET` for simple exchanges
- `POST` when they need to send extra body parameters such as `code_verifier`

Inputs:

| Name | Source | Meaning |
| --- | --- | --- |
| `provider` | query | `ProviderType` value |
| `code` | query | authorization code, provider access token, or refresh token depending on provider |
| `redirectPath` | query | optional redirect path from the client |
| `additionalParameters` | JSON body | optional provider-specific extras like PKCE `code_verifier` |

Behavior:

- reads `Origin` first, then `Referer`
- derives a domain from those headers
- builds a `TokenCheckerSettings` object with `Domain`, `RedirectPath`, and `AdditionalParameters`
- resolves the provider-specific named `ITokenChecker`
- on success builds a `ClaimsPrincipal` and returns `Results.SignIn(...)`

The actual wire payload is the ASP.NET bearer-token sign-in response, not a custom social-auth DTO.

### `api/Authentication/Social/User`

This endpoint requires authorization and returns either:

- `ISocialUserProvider.GetAsync(...)` output, when a provider is registered
- or `ISocialUser.OnlyUsername(...)` when no provider is available

## Refresh flow and the `DotNet` provider

`ProviderType.DotNet` is the package's internal refresh path, not a social provider.

It validates a refresh token previously issued by ASP.NET bearer auth and only accepts it when the original token domain matches the current request domain claim.

## Important caveats

### `Origin` or `Referer` is effectively required

The token endpoint refuses to proceed when it cannot infer a non-empty domain from `Origin` or `Referer`, even for providers that do not use redirect whitelists in their settings object.

### Redirect behavior is provider-specific

The overall contract supports `redirectPath`, but provider implementations are not fully uniform. Some use the computed redirect URI, while others rely on harder-coded callback shapes.

### Allowed URI validation is host-oriented

For redirect-whitelist providers, matching is based on scheme, host, and port. Multiple callback paths on the same host are not distinguished during domain validation.

### This package issues bearer tokens; it does not define your user model

Your claims and `/User` payload come from `ISocialUserProvider`. Without it, the package falls back to a username-only response.

## Grounded by sample files

- `src/Authentication/Tests/Rystem.Authentication.Social.TestApi/Program.cs`
- `src/Authentication/Tests/Rystem.Authentication.Social.TestApi/Services/SocialUserProvider.cs`
- `src/Authentication/Rystem.Authentication.Social/Extensions/ServiceCollectionExtensions.cs`
- `src/Authentication/Rystem.Authentication.Social/Extensions/EndpointRouteBuilderExtensions.cs`

Use this package when you want a small ASP.NET Core token-exchange backend for social-login clients, not a full end-to-end identity platform.
