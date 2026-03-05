# rystem.authentication.social.client

[![npm version](https://img.shields.io/npm/v/rystem.authentication.social.client)](https://www.npmjs.com/package/rystem.authentication.social.client)
[![npm downloads](https://img.shields.io/npm/dm/rystem.authentication.social.client)](https://www.npmjs.com/package/rystem.authentication.social.client)

Framework-agnostic TypeScript client for [Rystem social authentication](https://github.com/KeyserDSoze/Rystem). Handles the OAuth/PKCE flow, token storage, and user resolution for any JavaScript environment.

Works with: **React**, **Next.js**, **Remix**, **React Native / Expo**, or any TypeScript app.

---

## Install

```bash
npm install rystem.authentication.social.client
```

---

## Quick Start (Web)

```typescript
import { setupSocialLogin, SocialLoginWrapper, SocialLoginButtons, useSocialToken, useSocialUser } from 'rystem.authentication.social.client';

setupSocialLogin(x => {
    x.useBrowserDefaults(); // configures localStorage + window routing + browser APIs
    x.apiUri = 'https://api.example.com';

    x.microsoft.clientId = 'your-microsoft-client-id';
    x.google.clientId    = 'your-google-client-id';
    x.github.clientId    = 'your-github-client-id';
    x.facebook.clientId  = 'your-facebook-app-id';

    x.automaticRefresh = true;
    x.onLoginFailure = error => console.error(error.message);
});

function Root() {
    return (
        <SocialLoginWrapper>
            <App />
        </SocialLoginWrapper>
    );
}
```

```tsx
function App() {
    const token = useSocialToken();
    const user  = useSocialUser();

    if (!token.isExpired && user.isAuthenticated) {
        return <p>Welcome, {user.username}</p>;
    }

    return <SocialLoginButtons />;
}
```

---

## Setup

### `setupSocialLogin`

Call once at application startup. Returns the `SocialLoginManager` singleton.

```typescript
const manager = setupSocialLogin(x => {
    x.apiUri = 'https://api.example.com';
    // ...
});
```

### `SocialLoginSettings`

| Property | Type | Default | Description |
|---|---|---|---|
| `apiUri` | `string` | `window.location.origin` | Base URL of the Rystem.Authentication.Social API |
| `storageService` | `IStorageService` | — | **Required** — token/user persistence |
| `routingService` | `IRoutingService` | — | **Required** — URL reading + navigation |
| `platformService` | `IPlatformService` | — | **Required** — browser/native APIs |
| `platform` | `PlatformConfig` | `{ type: Auto, loginMode: Popup }` | Redirect URI and login mode |
| `automaticRefresh` | `boolean` | `false` | Auto-refresh expired token via `DotNet` provider |
| `identityTransformer` | `IIdentityTransformer<T>` | `undefined` | Map raw user object to typed model |
| `onLoginFailure` | `(e: SocialLoginErrorResponse) => void` | logs `e.code` | Error callback |
| `google` / `microsoft` / `facebook` / `github` / `amazon` / `linkedin` / `x` / `instagram` / `pinterest` / `tiktok` | `SocialParameter` | `{}` | Per-provider `clientId` |

### `useBrowserDefaults()`

A helper on `SocialLoginSettings` that sets all three required services for web:

```typescript
setupSocialLogin(x => {
    x.useBrowserDefaults();
    // equivalent to:
    // x.storageService   = new LocalStorageService();
    // x.routingService   = new WindowRoutingService();
    // x.platformService  = new BrowserPlatformService();
});
```

---

## Providers

```typescript
import { ProviderType } from 'rystem.authentication.social.client';
```

```typescript
export enum ProviderType {
    DotNet    = 0,  // internal — used for token refresh
    Google    = 1,
    Microsoft = 2,
    Facebook  = 3,
    GitHub    = 4,
    Amazon    = 5,
    Linkedin  = 6,
    X         = 7,
    Instagram = 8,
    Pinterest = 9,
    TikTok    = 10
}
```

Enable a provider by setting its `clientId`:

```typescript
x.microsoft.clientId = 'your-client-id';
x.google.clientId    = 'your-client-id';
```

A provider with a `null` or missing `clientId` renders no button.

---

## Platform Configuration

```typescript
import { PlatformType, LoginMode } from 'rystem.authentication.social.client';
```

### `PlatformType`

```typescript
export enum PlatformType {
    Web     = 'web',
    iOS     = 'ios',
    Android = 'android',
    Auto    = 'auto'  // auto-detect from navigator.userAgent
}
```

### `LoginMode`

```typescript
export enum LoginMode {
    Popup    = 'popup',    // opens OAuth in a new window (web default)
    Redirect = 'redirect'  // navigates in same window (mobile)
}
```

### `PlatformConfig`

```typescript
x.platform = {
    type: PlatformType.Auto,
    redirectPath: '/account/login',  // path or full deep link
    loginMode: LoginMode.Popup
};
```

**`redirectPath` resolution:**

| Value | Result |
|---|---|
| Contains `"://"` | Used as-is (mobile deep link: `myapp://oauth/callback`) |
| Starts with `"/"` | Prepended with `window.location.origin` |
| Empty | No `redirectPath` param sent to server |

---

## Hooks

### `useSocialToken()`

Returns the current stored token:

```typescript
import { useSocialToken } from 'rystem.authentication.social.client';

const token = useSocialToken();

interface Token {
    accessToken:  string;
    refreshToken: string;
    expiresIn:    Date;
    isExpired:    boolean;
}

if (!token.isExpired) {
    fetch('/api/data', {
        headers: { Authorization: `Bearer ${token.accessToken}` }
    });
}
```

### `useSocialUser<T>()`

Returns the authenticated user, merged with an optional custom type:

```typescript
import { useSocialUser } from 'rystem.authentication.social.client';

// Default usage
const user = useSocialUser();          // SocialUser<{}>
// user.username, user.isAuthenticated

// With custom type
interface AppUser { role: string; email: string; }
const user = useSocialUser<AppUser>(); // SocialUser<AppUser>
// user.username, user.isAuthenticated, user.role, user.email
```

```typescript
// SocialUser type definition:
interface ISocialUser { username: string; isAuthenticated: boolean; }
type SocialUser<T = {}> = ISocialUser & T;
```

Returns `{ isAuthenticated: false }` when no valid token exists.

---

## Functions

### `removeSocialLogin()`

Clears the stored token and user data:

```typescript
import { removeSocialLogin } from 'rystem.authentication.social.client';

removeSocialLogin();
```

### `getSocialLoginSettings()`

Returns the current `SocialLoginSettings` from the singleton:

```typescript
import { getSocialLoginSettings } from 'rystem.authentication.social.client';

const settings = getSocialLoginSettings();
console.log(settings.apiUri);
```

### `SocialLoginManager.Instance(null).updateToken(provider, code)`

Exchanges an OAuth authorization code for a bearer token. Used after native OAuth (e.g. React Native):

```typescript
import { SocialLoginManager, ProviderType } from 'rystem.authentication.social.client';

await SocialLoginManager.Instance(null).updateToken(ProviderType.Microsoft, authCode);
```

---

## UI Components (React)

All components are exported from `rystem.authentication.social.client`.

### `<SocialLoginWrapper>`

Context provider. Wrap your app root to enable hooks and automatic OAuth callback handling:

```tsx
<SocialLoginWrapper>
    <App />
</SocialLoginWrapper>
```

### `<SocialLoginButtons>`

Renders buttons for all configured providers (those with a non-null `clientId`):

```tsx
import { SocialLoginButtons } from 'rystem.authentication.social.client';

<SocialLoginButtons />
```

Custom button order:

```tsx
import { SocialLoginButtons, MicrosoftButton, GoogleButton, GitHubButton } from 'rystem.authentication.social.client';

<SocialLoginButtons buttons={[MicrosoftButton, GoogleButton, GitHubButton]} />
```

### `<SocialLogoutButton>`

```tsx
import { SocialLogoutButton } from 'rystem.authentication.social.client';

<SocialLogoutButton>Sign Out</SocialLogoutButton>
```

### Individual Buttons

```tsx
import {
    MicrosoftButton, GoogleButton, FacebookButton, GitHubButton,
    AmazonButton, LinkedinButton, XButton, TikTokButton, InstagramButton, PinterestButton
} from 'rystem.authentication.social.client';

<MicrosoftButton />
<GoogleButton />
```

---

## Services

### `IStorageService`

```typescript
interface IStorageService {
    get(key: string): string | null;
    set(key: string, value: string): void;
    remove(key: string): void;
    has(key: string): boolean;
    clear?(): void;
}
```

Default: `LocalStorageService` (browser `localStorage`).

### `IRoutingService`

Handles OAuth callback URL parsing and navigation:

```typescript
interface IRoutingService {
    getSearchParam(key: string): string | null;
    getAllSearchParams(): URLSearchParams;
    getCurrentPath(): string;
    navigateTo(url: string): void;
    navigateReplace(path: string): void;
    openPopup(url: string, name: string, features: string): Window | null;
}
```

Default: `WindowRoutingService` (uses `window.location` and `window.history`).

For **React Router** or **Next.js App Router**, implement this interface using the framework's hooks (`useSearchParams`, `useNavigate`, `useRouter`) and inject it via `x.routingService = new MyRoutingService()`.

### `IPlatformService`

Abstracts browser APIs for rendering popups, loading external scripts, and listening to storage events:

```typescript
interface IPlatformService {
    addStorageListener(callback: () => void): void;
    removeStorageListener(callback: () => void): void;
    getScreenWidth(): number;
    getScreenHeight(): number;
    loadScript(id: string, src: string, onLoad: () => void): HTMLScriptElement | null;
    scriptExists(id: string): boolean;
    removeScript(scriptElement: HTMLScriptElement): void;
    isPopup(): boolean;
    closeWindow(): void;
}
```

Default: `BrowserPlatformService`.

---

## Custom Identity Transformer

Map the raw user JSON from the server API to a typed model:

```typescript
interface AppUser {
    username: string;
    email: string;
    role: string;
}

setupSocialLogin(x => {
    x.useBrowserDefaults();
    x.apiUri = 'https://api.example.com';
    x.microsoft.clientId = 'your-client-id';

    x.identityTransformer = {
        fromPlain: (raw: any): AppUser => ({
            username: raw.username,
            email:    raw.email,
            role:     raw.role ?? 'user'
        }),
        toPlain: (user: AppUser) => user,
        retrieveUsername: (user: AppUser) => user.username
    };
});

// In component:
const user = useSocialUser<AppUser>();
console.log(user.role); // typed
```

---

## Error Handling

```typescript
setupSocialLogin(x => {
    x.onLoginFailure = (error: SocialLoginErrorResponse) => {
        console.error(`[${error.code}] ${error.message} (provider: ${error.provider})`);
    };
});

interface SocialLoginErrorResponse {
    code: number;     // 10 = user fetch error, 15 = token exchange error
    message: string;
    provider: ProviderType;
}
```

---

## PKCE (Microsoft)

PKCE is applied automatically for Microsoft OAuth:

1. A `code_verifier` and `code_challenge` are generated on button click
2. `code_challenge` is sent in the OAuth authorization URL
3. `code_verifier` is sent in the `POST /api/Authentication/Social/Token` body
4. After exchange, PKCE data is cleared from storage

No additional configuration is needed.

---

## Storage Keys

| Key | Description |
|---|---|
| `socialUserToken` | JWT access token |
| `socialUserToken_expiry` | Expiration timestamp |
| `socialUser` | Serialized user object |
| `rystem_pkce_{provider}_verifier` | PKCE code verifier |
| `rystem_pkce_{provider}_challenge` | PKCE code challenge |

---

## Related Packages

- **API Server (.NET)**: [`Rystem.Authentication.Social`](https://www.nuget.org/packages/Rystem.Authentication.Social)
- **Blazor Client**: [`Rystem.Authentication.Social.Blazor`](https://www.nuget.org/packages/Rystem.Authentication.Social.Blazor)
- **Abstractions**: [`Rystem.Authentication.Social.Abstractions`](https://www.nuget.org/packages/Rystem.Authentication.Social.Abstractions)
