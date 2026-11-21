# Rystem.Authentication.Social.Blazor

`Rystem.Authentication.Social.Blazor` is the Blazor-side companion to `Rystem.Authentication.Social`.

It handles browser-local state, callback processing, token retrieval, user loading, and a small amount of localization glue. It does not replace your server package and it does not register arbitrary social-login providers by itself.

## Installation

```bash
dotnet add package Rystem.Authentication.Social.Blazor
```

## Architecture

The Blazor package is centered around:

- `AddSocialLoginUI(...)`
- `UseSocialLoginAuthorization()`
- `SocialLoginManager`
- UI components like `SocialAuthentication` and `SocialAuthenticationRouter`

The normal flow is:

1. register the Blazor package with the API base URL and provider client ids
2. include the package JavaScript asset in the host page
3. wrap the app or route tree in `SocialAuthentication` or `SocialAuthenticationRouter`
4. let `SocialLoginManager` read stored state, process the OAuth callback, exchange codes with the server, and load the authenticated user

## Registration

The real setup API is:

```csharp
builder.Services.AddSocialLoginUI(settings =>
{
    settings.ApiUrl = "https://localhost:7017";
    settings.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    settings.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    settings.Platform.RedirectPath = "/account/login";
});
```

`AddSocialLoginUI(...)` currently registers:

- singleton `SocialLoginAppSettings`
- scoped `SocialLoginLocalStorageService`
- named `HttpClient` for `SocialLoginManager`
- transient `SocialLoginManager`
- singleton `LocalizationMiddleware`
- scoped `IAuthorizationHeaderProvider` via `SocialLoginAuthorizationHeaderProvider`

### Middleware

```csharp
app.UseSocialLoginAuthorization();
```

In the current implementation this only adds the localization middleware. It does not set up authentication middleware or map social-login endpoints.

## Required host-page script

The package needs its JS asset loaded explicitly. The sample app does this in `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Components/App.razor`:

```html
<script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
```

Without that script, local storage helpers and language cookie helpers will not work.

## Configuration model

`SocialLoginAppSettings` currently exposes:

- `ApiUrl`
- `Google`
- `Facebook`
- `Microsoft`
- `Platform`
- `LoginMode`

Important runtime note: the built-in `<SocialLogin>` component currently renders only Microsoft and Google buttons, even though `SocialLoginAppSettings` also exposes `Facebook`.

Another important detail: the current runtime flow is effectively driven by `Platform.LoginMode`; the top-level `LoginMode` property exists on the settings type but is not the part the runtime actively reads during callback handling.

### Platform behavior

`Platform.RedirectPath` is interpreted like this by `SocialLoginManager.GetFullRedirectUri()`:

- full URI if it contains `://`
- web-relative path if it starts with `/`
- default to `/account/login` under the current base URI when omitted

`Platform.LoginMode` exists, but popup support is not meaningfully implemented in this Blazor package. In practice the flow is redirect-oriented.

## Components

### `SocialAuthentication<TUser>`

`SocialAuthentication<TUser>` wraps authenticated content.

- when no user is loaded, it renders `<SocialLogin>` plus optional `LoginPage`
- when a user is loaded, it cascades `SocialUser` and `LogoutCallback`
- if the user implements `ILocalizedSocialUser`, it persists the language locally

Sample usage from `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Components/Routes.razor`:

```razor
<SocialAuthenticationRouter AppAssembly="typeof(Program).Assembly"
                            DefaultLayout="typeof(Layout.MainLayout)"
                            TUser="SocialUser"
                            SetUser="SetUserAsync">
    <LoginPage>
        <h1>Your customization here.</h1>
    </LoginPage>
</SocialAuthenticationRouter>
```

### `SocialAuthenticationRouter<TUser>`

This is a wrapper around Blazor's router that nests every route inside `SocialAuthentication<TUser>`.

Use it when you want route-level protection with the package's built-in login shell.

### `SocialLogin`

`SocialLogin` renders the built-in login buttons above optional child content.

Current built-in button support:

- Microsoft
- Google

### `SocialLogout`

`SocialLogout` renders a logout button and participates in the package's local state cleanup flow.

## `SocialLoginManager`

`SocialLoginManager` is the imperative entry point.

Key methods:

- `FetchTokenAsync()`
- `MeAsync<TUser>()`
- `LogoutAsync()`
- `GetFullRedirectUri()`

### Token resolution flow

`FetchTokenAsync()` currently does this:

1. tries local storage token first
2. if expired, tries refresh through `/api/Authentication/Social/Token?provider=DotNet&code=<refreshToken>`
3. if no token, reads `code` and `state` from the current URI
4. validates the stored local state
5. exchanges the callback code with the server

The callback exchange includes:

- `Origin` based on the current base URI
- `redirectPath` from the current page path
- optional request body containing `code_verifier`

### User loading flow

`MeAsync<TUser>()` calls `/api/Authentication/Social/User` with the bearer token. If that call returns `401`, it attempts a `DotNet` refresh first and retries.

When user loading succeeds, `SocialLoginAuthorizationHeaderProvider` is updated with the current bearer token so other consumers can read it.

## Localization behavior

If the loaded user implements `ILocalizedSocialUser`, the package:

- stores the language through the JS helper
- writes the `lang` cookie
- updates `CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture`

`UseSocialLoginAuthorization()` then applies that cookie through `LocalizationMiddleware` on later requests.

## Important caveats

### This package depends on the server package

It expects a backend that exposes:

- `/api/Authentication/Social/Token`
- `/api/Authentication/Social/User`

Without `Rystem.Authentication.Social` on the server side, the Blazor flow cannot complete.

### The built-in UI is narrower than the config surface

The settings type exposes `Facebook`, but the built-in `<SocialLogin>` component currently renders only Microsoft and Google buttons.

### PKCE support is effectively Microsoft-specific today

The current callback implementation looks for `microsoft_code_verifier` in local storage when building the token exchange request.

### `UseSocialLoginAuthorization()` is only localization middleware

Despite its name, it does not configure auth handlers or attach bearer tokens to your API clients by itself.

If you want repository/API client calls to reuse the stored social token, you need a separate integration layer. The sample app does that with `AddDefaultSocialLoginAuthorizationInterceptorForApiHttpClient()` from `RepositoryFramework.Api.Client.Authentication.BlazorServer` in `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Program.cs`.

### Logout cleanup is split across manager and component code

`SocialLoginManager.LogoutAsync()` clears token storage and the in-memory authorization header, while `SocialAuthentication` also clears state and optionally forces a page reload.

## Grounded by sample and source files

- `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Program.cs`
- `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Components/App.razor`
- `src/Authentication/Tests/RystemAuthentication.Social.TestBlazorApp/Components/Routes.razor`
- `src/Authentication/Rystem.Authentication.Social.Blazor/Services/SocialLoginManager.cs`
- `src/Authentication/Rystem.Authentication.Social.Blazor/Components/SocialAuthentication.razor`
- `src/Authentication/Rystem.Authentication.Social.Blazor/Components/SocialAuthenticationRouter.razor`

Use this package when you want a Blazor UI and local-session wrapper around the server-side social-login endpoints, not when you need a fully standalone auth system.
