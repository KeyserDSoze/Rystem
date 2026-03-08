# rystem.authentication.social.client

`rystem.authentication.social.client` is the JavaScript/TypeScript client package for the Authentication/Social area.

It wraps provider login buttons, callback handling, token exchange, user caching, and pluggable storage/routing/platform services. Despite the name "framework-agnostic", the package also ships React components and hooks, so the practical experience is best in React-style apps.

## Installation

```bash
npm install rystem.authentication.social.client
```

## Architecture

The package is centered around:

- `setupSocialLogin(...)`
- `SocialLoginManager`
- `SocialLoginWrapper`
- hooks like `useSocialToken()` and `useSocialUser()`
- button components such as `GoogleButton`, `MicrosoftButton`, and `SocialLoginButtons`

The normal flow is:

1. call `setupSocialLogin(...)` once at app startup
2. provide storage, routing, and platform services or call `useBrowserDefaults()`
3. wrap the app in `SocialLoginWrapper`
4. let a provider button start the OAuth flow
5. let the wrapper detect the callback, exchange the code with the server, store the token, fetch the user, and refresh app state

## Required setup

The minimum real setup is:

```tsx
import { setupSocialLogin, SocialLoginWrapper, SocialLoginButtons } from 'rystem.authentication.social.client';

setupSocialLogin(settings => {
    settings.useBrowserDefaults();
    settings.apiUri = 'https://api.example.com';

    settings.google.clientId = 'google-client-id';
    settings.microsoft.clientId = 'microsoft-client-id';

    settings.automaticRefresh = true;
    settings.onLoginFailure = error => console.error(error.message);
});

export function Root() {
    return (
        <SocialLoginWrapper>
            <SocialLoginButtons />
        </SocialLoginWrapper>
    );
}
```

`setupSocialLogin(...)` validates that these three services exist:

- `storageService`
- `routingService`
- `platformService`

For browser apps, `useBrowserDefaults()` wires all three automatically.

## Public API highlights

Main exports include:

- `setupSocialLogin`
- `SocialLoginManager`
- `SocialLoginWrapper`
- `SocialLoginButtons`
- per-provider buttons such as `GoogleButton`, `MicrosoftButton`, `GitHubButton`, `FacebookButton`, `XButton`, `TikTokButton`
- `useSocialToken()`
- `useSocialUser()`
- service contracts like `IStorageService`, `IRoutingService`, `IPlatformService`

If you are not using React, the lower-level pieces to focus on are `setupSocialLogin(...)`, `SocialLoginManager`, and the service abstractions. The hooks and wrapper component are React-oriented convenience layers.

## Platform and login mode

`platform` configuration contains:

- `type`
- `redirectPath`
- `loginMode`

Default behavior from `setupSocialLogin(...)` is:

- auto-detect platform when `type` is `Auto`
- prefer popup on web
- prefer redirect on mobile

`redirectPath` handling is:

- full URI if it already contains `://`
- origin-relative when it starts with `/`
- omitted from the token request when not configured

## Callback and token flow

`SocialLoginWrapper` watches the current URL for `code` and `state`.

When it sees a callback:

- it parses `state` as a numeric `ProviderType`
- detects popup vs redirect mode
- calls `SocialLoginManager.updateToken(provider, code)`
- stores success or failure details for popup mode
- or navigates back to the stored return URL for redirect mode

`SocialLoginManager.updateToken(...)` then:

- reads any stored PKCE verifier
- calls `/api/Authentication/Social/Token`
- stores the returned token
- calls `/api/Authentication/Social/User`
- stores the returned user

If the token exchange fails, it clears PKCE state, calls `onLoginFailure`, and resolves to an empty object instead of throwing a rich typed result.

## Hooks

### `useSocialToken()`

`useSocialToken()` reads the stored token and marks it expired when needed.

If `automaticRefresh` is enabled and the token is expired, it triggers a refresh through the internal `DotNet` provider using the refresh token.

Important detail: that refresh call is fire-and-forget, so the current render can still see an expired token first.

### `useSocialUser()`

`useSocialUser()` reads the cached user from storage.

It does not fetch `/User` on its own. It only returns what was already stored by the login flow.

If an `identityTransformer` is configured, it transforms the stored user before returning it.

## Storage keys and runtime behavior

The package stores data under keys such as:

- `socialUserToken`
- `socialUserToken_expiry`
- `socialUser`
- `social_login_return_url`
- `social_result_<provider>` for popup results
- PKCE keys under the `rystem_pkce_*` pattern

This means the package expects durable client-side storage and a stable routing abstraction.

## Important caveats

### The setup really is mandatory

If `storageService`, `routingService`, or `platformService` is missing, `setupSocialLogin(...)` throws immediately.

### The package is not equally polished across all environments

Some files still touch browser globals such as `window`, so SSR and non-browser environments need more care than the README headline might suggest.

### `state` is not a strong CSRF nonce

The callback flow commonly uses the numeric `ProviderType` as the `state` value. That is enough for internal routing in this package, but it is not the strongest possible CSRF design.

### Error handling is soft

`SocialLoginManager.updateToken(...)` can swallow failures, call `onLoginFailure`, and return an empty object-like token result. Do not assume every failure rejects in a clean, typed way.

### `useSocialUser()` is cache-based

If the cached user is stale or missing, the hook does not go back to the server for you.

## Grounded by source files

- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/setup/setupSocialLogin.ts`
- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/setup/SocialLoginManager.ts`
- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/context/SocialLoginWrapper.tsx`
- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/hooks/useSocialToken.ts`
- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/hooks/useSocialUser.ts`
- `src/Authentication/rystem.authentication.social.client/src/rystem.authentication.social.client/src/index.tsx`

Use this package when you want a browser or mobile client wrapper for the server-side Authentication/Social endpoints and you are comfortable plugging in the platform services it expects.
