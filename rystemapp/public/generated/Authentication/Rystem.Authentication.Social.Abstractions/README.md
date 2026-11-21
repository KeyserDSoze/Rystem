# Rystem.Authentication.Social.Abstractions

`Rystem.Authentication.Social.Abstractions` contains the shared contracts and models used by the Authentication/Social packages.

It is the common .NET vocabulary between:

- `Rystem.Authentication.Social`
- `Rystem.Authentication.Social.Blazor`

The TypeScript client mirrors many of the same concepts, but it does not consume this package directly.

## Installation

```bash
dotnet add package Rystem.Authentication.Social.Abstractions
```

## What this package adds

The core public types are:

- `ProviderType`
- `ISocialUser`
- `ILocalizedSocialUser`
- `TokenResponse`
- `TokenCheckerSettings`
- `RystemClaimTypes`

## `ProviderType`

`ProviderType` identifies the server-side token checker to use.

Current values are:

- `DotNet`
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

Important note: `DotNet` is not a social provider. It is the internal provider used when refreshing bearer tokens that were already issued by the server package.

## `ISocialUser`

`ISocialUser` is the minimum user contract across the stack.

```csharp
public interface ISocialUser
{
    string? Username { get; set; }
}
```

The interface also exposes static helpers in the current implementation:

- `ISocialUser.Empty`
- `ISocialUser.OnlyUsername(username)`

Those helpers are used by the server package when no custom `ISocialUserProvider` is present.

## `ILocalizedSocialUser`

`ILocalizedSocialUser` extends `ISocialUser` with:

```csharp
string? Language { get; set; }
```

This matters mainly for the Blazor package, where the login flow can persist a language choice and feed the localization middleware.

## `TokenResponse`

`TokenResponse` is the normalized result of a successful provider check inside the server package.

```csharp
public sealed class TokenResponse
{
    public required string Username { get; set; }
    public required List<Claim> Claims { get; set; }
}
```

Important detail: `TokenResponse.Empty` is currently just `null`.

## `TokenCheckerSettings`

`TokenCheckerSettings` carries request context from the token endpoint into a provider-specific token checker.

```csharp
public sealed class TokenCheckerSettings
{
    public string? Domain { get; set; }
    public string? RedirectPath { get; set; }
    public Dictionary<string, string>? AdditionalParameters { get; set; }
}
```

Use cases:

- `Domain` comes from `Origin` or `Referer`
- `RedirectPath` lets a client communicate the callback path it used
- `AdditionalParameters` carries provider-specific extras such as PKCE `code_verifier`

Helper methods:

- `GetRedirectUri()` combines `Domain` and `RedirectPath`
- `GetParameter(key)` reads from `AdditionalParameters`
- `WithParameter(key, value)` appends a parameter fluently

Example:

```csharp
var settings = new TokenCheckerSettings
{
    Domain = "https://app.example.com",
    RedirectPath = "/account/login"
}.WithParameter("code_verifier", verifier);

string redirectUri = settings.GetRedirectUri();
```

## `RystemClaimTypes`

This package currently defines one custom claim constant:

- `RystemClaimTypes.Language`

It is intended for language values such as `en`, `it`, or `es` and is used by the sample social user provider plus the Blazor localization flow.

## Important caveats

### This package is shared-model oriented, not truly dependency-minimal

Even though it is called `Abstractions`, its project file currently references token and OIDC-related packages. So it is not a pure POCO-only dependency.

### `DotNet` is easy to misunderstand

If you see `ProviderType.DotNet`, think refresh-token reuse inside the server package, not an external login provider.

## Grounded by source files

- `src/Authentication/Rystem.Authentication.Social.Abstractions/Models/ProviderType.cs`
- `src/Authentication/Rystem.Authentication.Social.Abstractions/Models/ISocialUser.cs`
- `src/Authentication/Rystem.Authentication.Social.Abstractions/Models/ILocalizedSocialUser.cs`
- `src/Authentication/Rystem.Authentication.Social.Abstractions/Models/TokenResponse.cs`
- `src/Authentication/Rystem.Authentication.Social.Abstractions/Models/TokenCheckerSettings.cs`

Use this package when you need the shared Authentication/Social contracts without taking a dependency on the full server or Blazor runtime package.
