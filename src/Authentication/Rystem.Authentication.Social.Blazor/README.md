# Rystem.Authentication.Social.Blazor

[![Version](https://img.shields.io/nuget/v/Rystem.Authentication.Social.Blazor)](https://www.nuget.org/packages/Rystem.Authentication.Social.Blazor)
[![Downloads](https://img.shields.io/nuget/dt/Rystem.Authentication.Social.Blazor)](https://www.nuget.org/packages/Rystem.Authentication.Social.Blazor)

Blazor client-side social authentication for Rystem. Handles the OAuth redirect flow, PKCE, token storage, user resolution, and localization â€” for web and mobile (MAUI Blazor Hybrid).

---

## Install

```bash
dotnet add package Rystem.Authentication.Social.Blazor
```

> **Dependencies**: `Rystem.Authentication.Social.Abstractions`, `Microsoft.Identity.Abstractions`

---

## Registration

### `AddSocialLoginUI`

```csharp
builder.Services.AddSocialLoginUI(settings =>
{
    settings.ApiUrl = "https://api.example.com";

    settings.Google.ClientId    = "your-google-client-id";
    settings.Microsoft.ClientId = "your-microsoft-client-id";
    settings.Facebook.ClientId  = "your-facebook-app-id";

    settings.Platform.RedirectPath = "/account/login"; // web default
    settings.Platform.Type         = PlatformType.Auto;
});
```

Full signature:

```csharp
public static IServiceCollection AddSocialLoginUI(
    this IServiceCollection services,
    Action<SocialLoginAppSettings> settings)
```

Registers: `SocialLoginLocalStorageService`, `SocialLoginManager` (transient), `LocalizationMiddleware` (singleton), and `IAuthorizationHeaderProvider` backed by token storage.

### `UseSocialLoginAuthorization`

Adds the localization middleware. Call it after `UseAuthentication()`:

```csharp
app.UseSocialLoginAuthorization();
```

---

## Configuration

### `SocialLoginAppSettings`

```csharp
public sealed class SocialLoginAppSettings
{
    public string? ApiUrl { get; set; }           // base URL of the Rystem.Authentication.Social API
    public SocialParameter Google { get; set; }
    public SocialParameter Facebook { get; set; }
    public SocialParameter Microsoft { get; set; }
    public PlatformConfig Platform { get; set; }  // redirect URI and platform type
    public LoginMode LoginMode { get; set; } = LoginMode.Redirect;
}
```

### `SocialParameter`

```csharp
public sealed class SocialParameter
{
    public string? ClientId { get; set; }
}
```

### `PlatformConfig`

Controls how the redirect URI is built and which login mode is used:

```csharp
public sealed class PlatformConfig
{
    public PlatformType Type { get; set; } = PlatformType.Auto;
    public string? RedirectPath { get; set; }
    public LoginMode LoginMode { get; set; } = LoginMode.Redirect;
}
```

**`RedirectPath` smart detection** (applied in `SocialLoginManager.GetFullRedirectUri()`):

| `RedirectPath` value | Result |
|---|---|
| Contains `"://"` | Used as-is (mobile deep link, e.g. `"myapp://oauth/callback"`) |
| Starts with `"/"` | Prepended with `NavigationManager.BaseUri` |
| Empty | Defaults to `{BaseUri}/account/login` |

### `PlatformType`

```csharp
public enum PlatformType
{
    Web = 0,
    iOS = 1,
    Android = 2,
    Auto = 3    // auto-detect at runtime
}
```

### `LoginMode`

```csharp
public enum LoginMode
{
    Redirect = 0,   // navigate in same window (default)
    Popup = 1       // open in new tab (not yet implemented in Blazor)
}
```

---

## Components

### `<SocialAuthentication TUser="...">`

Wraps the authenticated section of the app. Renders `LoginPage` when the user is not logged in; otherwise cascades `SocialUser` (the wrapper) and `LogoutCallback` to all child content.

```razor
<SocialAuthentication TUser="AppUser" SetUser="OnUserSet">
    <ChildContent>
        <!-- accessible when authenticated -->
        <!-- access user via cascading parameter below -->
    </ChildContent>
    <LoginPage>
        <MyLoginPage />
    </LoginPage>
</SocialAuthentication>
```

Parameters:

| Parameter | Type | Description |
|---|---|---|
| `ChildContent` | `RenderFragment` | Rendered when authenticated |
| `LoginPage` | `RenderFragment?` | Rendered inside `<SocialLogin>` when not authenticated |
| `SetUser` | `SetUser<TUser>?` | Async callback invoked after login with the resolved user |

Access the user from any descendant component:

```razor
@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper<AppUser>? User { get; set; }

    [CascadingParameter(Name = "LogoutCallback")]
    public SocialLogout? Logout { get; set; }
}
```

### `<SocialAuthenticationRouter TUser="...">`

Drop-in replacement for Blazor's `<Router>`. Wraps all routes inside `<SocialAuthentication>`.

```razor
<SocialAuthenticationRouter
    TUser="AppUser"
    AppAssembly="typeof(App).Assembly"
    DefaultLayout="typeof(MainLayout)"
    LoginPage="@(() => @<LoginPage />)"
    SetUser="OnUserSet">
    <NotFound>
        <p>Page not found.</p>
    </NotFound>
</SocialAuthenticationRouter>
```

Parameters:

| Parameter | Type | Description |
|---|---|---|
| `AppAssembly` | `Assembly` | App assembly for route discovery |
| `DefaultLayout` | `Type` | Default layout type |
| `NotFoundLayout` | `Type?` | Layout for 404 pages (falls back to `DefaultLayout`) |
| `LoginPage` | `RenderFragment?` | Login content |
| `NotFound` | `RenderFragment?` | 404 content |
| `SetUser` | `SetUser<TUser>?` | Callback after login |

### `<SocialLogin>`

Renders Google and Microsoft login buttons, depending on which `ClientId` values are configured in `SocialLoginAppSettings`. `ChildContent` appears above the buttons.

```razor
<SocialLogin>
    <p>Sign in to continue.</p>
</SocialLogin>
```

### `<SocialLogout>`

Renders a logout button that clears the stored token and state.

---

## `SocialLoginManager`

Injectable service for manual OAuth flow control.

```csharp
public sealed class SocialLoginManager
{
    // Try to obtain a valid token: from storage, OAuth callback, or refresh
    public Task<Token?> FetchTokenAsync();

    // Resolve the authenticated user from the API
    public Task<SocialUserWrapper<TUser>?> MeAsync<TUser>() where TUser : ISocialUser, new();

    // Clear token and state from localStorage
    public ValueTask LogoutAsync();

    // Build the full redirect URI using platform configuration
    public string GetFullRedirectUri();
}
```

`FetchTokenAsync` resolution order:

1. Token in localStorage and not expired â†’ return it
2. Token expired â†’ exchange refresh token via the `DotNet` provider â†’ update storage â†’ return refreshed token
3. No token â†’ read `?code=` + `?state=` from the current URL â†’ call `POST /api/Authentication/Social/Token` (with PKCE body if `code_verifier` is in storage) â†’ store and return new token

---

## Models

### `Token`

The bearer token returned by the server endpoint:

```csharp
public sealed class Token
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public bool IsExpired { get; set; }
    public long ExpiresIn { get; set; }
    public DateTime Expiring { get; set; }
}
```

### `SocialUserWrapper<TUser>`

Wraps the resolved user after a successful login:

```csharp
public sealed class SocialUserWrapper<TUser> where TUser : ISocialUser, new()
{
    public required TUser User { get; set; }
    public required string CurrentToken { get; set; }
    public SocialLogout LogoutAsync { get; set; }
}

public delegate ValueTask SocialLogout(bool forceReload);
```

---

## Localization Support

If your user type implements `ILocalizedSocialUser`:

```csharp
public class AppUser : ILocalizedSocialUser
{
    public string? Username { get; set; }
    public string? Language { get; set; }  // e.g. "en", "it"
}
```

After login, the Blazor client automatically persists the language to `localStorage`. The `LocalizationMiddleware` (registered via `UseSocialLoginAuthorization()`) reads it and sets `CultureInfo.CurrentUICulture` for each request.

---

## Full Setup Example

```csharp
// Program.cs
builder.Services.AddSocialLoginUI(settings =>
{
    settings.ApiUrl             = "https://api.example.com";
    settings.Google.ClientId    = builder.Configuration["Google:ClientId"];
    settings.Microsoft.ClientId = builder.Configuration["Microsoft:ClientId"];
    settings.Platform.RedirectPath = "/account/login";
});

var app = builder.Build();
app.UseSocialLoginAuthorization();
await app.RunAsync();
```

```razor
@* App.razor *@
<SocialAuthenticationRouter
    TUser="AppUser"
    AppAssembly="typeof(App).Assembly"
    DefaultLayout="typeof(MainLayout)"
    LoginPage="@(() => @<LoginPage />)" />
```

---

## Provider Coverage

| Provider | Client Buttons | Server Exchange |
|---|---|---|
| Google | âœ… | âœ… |
| Microsoft | âœ… | âœ… |
| Facebook | âœ… | âœ… |
| GitHub | â€” | âœ… |
| Amazon | â€” | âœ… |
| LinkedIn | â€” | âœ… |
| X (Twitter) | â€” | âœ… |
| Instagram | â€” | âœ… |
| Pinterest | â€” | âœ… |
| TikTok | â€” | âœ… |

> The Blazor client renders buttons only for Google, Microsoft, and Facebook. All providers can be used via manual OAuth redirect through the server endpoint.
