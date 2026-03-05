# Rystem.Authentication.Social.Abstractions

[![Version](https://img.shields.io/nuget/v/Rystem.Authentication.Social.Abstractions)](https://www.nuget.org/packages/Rystem.Authentication.Social.Abstractions)
[![Downloads](https://img.shields.io/nuget/dt/Rystem.Authentication.Social.Abstractions)](https://www.nuget.org/packages/Rystem.Authentication.Social.Abstractions)

Shared models and interfaces used by all Rystem social authentication libraries.

---

## Install

```bash
dotnet add package Rystem.Authentication.Social.Abstractions
```

---

## Models

### `ProviderType`

Identifies the OAuth provider for a token exchange request:

```csharp
public enum ProviderType
{
    DotNet,
    Google,
    Microsoft,
    Facebook,
    GitHub,
    Amazon,
    Linkedin,
    X,
    Instagram,
    Pinterest,
    TikTok
}
```

`DotNet` is the internal .NET bearer token provider, used for token refresh via the `DotNetTokenChecker`.

---

### `ISocialUser`

Base interface for the authenticated user model:

```csharp
public interface ISocialUser
{
    string? Username { get; set; }
    static ISocialUser Empty { get; }
    static ISocialUser OnlyUsername(string? username);
}
```

- `Empty` — returns a `DefaultSocialUser` with a null username
- `OnlyUsername(username)` — creates a minimal `DefaultSocialUser` with only the username set

Implement this interface to define your application-specific user type.

---

### `ILocalizedSocialUser`

Extends `ISocialUser` with a language preference:

```csharp
public interface ILocalizedSocialUser : ISocialUser
{
    string? Language { get; set; }
}
```

When a Blazor client detects that the authenticated user implements `ILocalizedSocialUser`, the language value is automatically persisted to `localStorage` and applied via the localization middleware. The claim type used is `RystemClaimTypes.Language`.

---

### `TokenResponse`

Result returned by a successful OAuth code exchange:

```csharp
public sealed class TokenResponse
{
    public required string Username { get; set; }
    public required List<Claim> Claims { get; set; }
    public static TokenResponse? Empty => null;
}
```

---

### `TokenCheckerSettings`

Passed to `ITokenChecker` to carry the redirect URI and any additional OAuth parameters:

```csharp
public sealed class TokenCheckerSettings
{
    public string? Domain { get; set; }
    public string? RedirectPath { get; set; }
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    public string GetRedirectUri();
    public string? GetParameter(string key);
    public TokenCheckerSettings WithParameter(string key, string value);
}
```

`GetRedirectUri()` combines `Domain` and `RedirectPath` into the full callback URL:

```csharp
// Domain = "https://app.example.com", RedirectPath = "/account/login"
settings.GetRedirectUri(); // -> "https://app.example.com/account/login"
```

`AdditionalParameters` carries provider-specific extras such as `code_verifier` for PKCE:

```csharp
var settings = new TokenCheckerSettings
{
    Domain = "https://app.example.com",
    RedirectPath = "/account/login"
};
settings.WithParameter("code_verifier", pkceVerifier);
```

---

### `RystemClaimTypes`

Custom claim type constants:

```csharp
public static class RystemClaimTypes
{
    // Two-letter ISO 639-1 language code (e.g. "en", "it")
    public const string Language = "http://schemas.rystem.org/claims/language";
}
```
