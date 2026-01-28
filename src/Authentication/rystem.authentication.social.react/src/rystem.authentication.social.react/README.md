### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# rystem.authentication.social.react

React/TypeScript library for social authentication with built-in PKCE support for secure OAuth 2.0 flows.

### ✨ Key Features

- **🔐 PKCE Built-in**: Automatic code_verifier generation for Microsoft OAuth (RFC 7636)
- **⚛️ React Hooks**: Type-safe hooks for token and user management
- **🎨 Ready-to-Use Components**: Login buttons, logout, authentication wrapper
- **🔄 Automatic Token Refresh**: Handles token expiration seamlessly
- **📱 SPA Optimized**: Designed for Single-Page Applications with security best practices
- **📱 Mobile Support**: Full React Native support with deep link OAuth flows

## 🆕 What's New - Mobile Platform Support

**All social providers now support mobile platforms!** Configure platform-specific OAuth redirect URIs for seamless authentication across Web, React Native iOS, and React Native Android.

### Supported Platforms & Providers

| Provider | Web (Popup) | Web (Redirect) | React Native iOS | React Native Android | PKCE Support |
|----------|-------------|----------------|------------------|---------------------|--------------|
| Microsoft | ✅ | ✅ | ✅ | ✅ | ✅ |
| Google | ✅ | ✅ | ✅ | ✅ | - |
| Facebook | ✅ | ✅ | ✅ | ✅ | - |
| GitHub | ✅ | ✅ | ✅ | ✅ | - |
| Amazon | ✅ | ✅ | ✅ | ✅ | - |
| LinkedIn | ✅ | ✅ | ✅ | ✅ | - |
| X (Twitter) | ✅ | ✅ | ✅ | ✅ | - |
| TikTok | ✅ | ✅ | ✅ | ✅ | - |
| Instagram | ✅ | ✅ | ✅ | ✅ | - |
| Pinterest | ✅ | ✅ | ✅ | ✅ | - |

### How It Works

1. **Auto-Detection**: Library automatically detects platform (Web/iOS/Android) from navigator.userAgent
2. **Platform-Specific URIs**: Configure custom redirect URIs per platform (e.g., `msauth://` for iOS, `myapp://` for Android)
3. **Login Modes**: Choose Popup (web) or Redirect (mobile) behavior
4. **Deep Links**: All buttons support mobile deep link OAuth callbacks
5. **No Breaking Changes**: Existing web apps work without modification

### Quick Example

```typescript
import { setupSocialLogin, PlatformType, LoginMode } from 'rystem.authentication.social.react';
import { Platform } from 'react-native'; // Only in React Native projects

setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    
    // Platform configuration (auto-detects if not specified)
    x.platform = {
        type: PlatformType.Auto,
        
        // Smart redirect path (auto-detects domain for web)
        redirectPath: Platform.select({
            ios: 'msauth://com.yourapp.bundle/auth',      // Complete URI for mobile
            android: 'myapp://oauth/callback',             // Complete URI for mobile
            web: '/account/login'                          // Path only (auto-detects domain)
        }),
        
        // Login mode (auto-set based on platform if not specified)
        loginMode: Platform.select({
            ios: LoginMode.Redirect,
            android: LoginMode.Redirect,
            web: LoginMode.Popup
        })
    };
    
    x.microsoft.clientId = "your-client-id";
    x.google.clientId = "your-client-id";
});
```

📖 **Full Migration Guide**: See [`PLATFORM_SUPPORT.md`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Authentication/PLATFORM_SUPPORT.md) for detailed setup instructions, OAuth provider configuration, and troubleshooting.

## 📦 Installation

```bash
npm install rystem.authentication.social.react
```

## 🚀 Quick Start

### 1. Setup Configuration (main.tsx)

```typescript
import { SocialLoginWrapper, setupSocialLogin } from 'rystem.authentication.social.react';
import App from './App';

setupSocialLogin(x => {
    // API server URL
    x.apiUri = "https://localhost:7017";
    
    // Optional: Custom redirect path (default: "/account/login")
    x.platform = {
        redirectPath: "/account/login"  // Auto-detects domain
    };
    
    // Configure OAuth providers (only clientId needed for client-side)
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    x.facebook.clientId = "345885718092912";
    x.github.clientId = "97154d062f2bb5d28620";
    x.amazon.clientId = "amzn1.application-oa2-client.dffbc466d62c44e49d71ad32f4aecb62";
    
    // Error handling callback
    x.onLoginFailure = (error) => {
        console.error(`Login failed: ${error.message} (Code: ${error.code})`);
        alert(`Authentication error: ${error.message}`);
    };
    
    // Automatic token refresh when expired
    x.automaticRefresh = true;
});

function Root() {
    return (
        <SocialLoginWrapper>
            <App />
        </SocialLoginWrapper>
    );
}

export default Root;
```

### 2. Use in Components

```typescript
import { useSocialToken, useSocialUser, SocialLoginButtons, SocialLogoutButton } from 'rystem.authentication.social.react';

export const App = () => {
    const token = useSocialToken();
    const user = useSocialUser();
    
    return (
        <div>
            {token.isExpired ? (
                <div>
                    <h3>Please login</h3>
                    <SocialLoginButtons />
                </div>
            ) : (
                <div>
                    <h3>Welcome, {user.username}</h3>
                    <p>Access Token: {token.accessToken}</p>
                    <SocialLogoutButton>Logout</SocialLogoutButton>
                </div>
            )}
        </div>
    );
};
```

## 🔐 PKCE Support (Microsoft OAuth)

### Automatic PKCE Implementation

The library **automatically** implements PKCE for Microsoft OAuth:

1. **Code Verifier Generation**: When user clicks Microsoft login button
   ```typescript
   const codeVerifier = await generateCodeVerifier();  // 43-128 chars random string
   const codeChallenge = await generateCodeChallenge(codeVerifier);  // SHA256 hash
   ```

2. **Session Storage**: Stores `code_verifier` for callback retrieval
   ```typescript
   storeCodeVerifier('microsoft', codeVerifier);
   ```

3. **OAuth Request**: Sends `code_challenge` with S256 method
   ```
   https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize
     ?client_id={clientId}
     &response_type=code
     &redirect_uri={redirectUri}
     &code_challenge={codeChallenge}
     &code_challenge_method=S256
   ```

4. **Token Exchange**: Sends `code_verifier` to API server
   ```typescript
   POST /api/Authentication/Social/Token?provider=Microsoft&code={code}&redirectPath=/account/login
   Body: { "code_verifier": "original-verifier" }
   ```

5. **Cleanup**: Removes verifier from sessionStorage after use

### Manual PKCE Usage

For custom implementations:

```typescript
import { generateCodeVerifier, generateCodeChallenge, storeCodeVerifier, getAndRemoveCodeVerifier } from 'rystem.authentication.social.react';

// Generate PKCE values
const codeVerifier = await generateCodeVerifier();
const codeChallenge = await generateCodeChallenge(codeVerifier);

// Store for later retrieval
storeCodeVerifier('custom-provider', codeVerifier);

// Build OAuth URL with code_challenge
const authUrl = `https://oauth.provider.com/authorize?code_challenge=${codeChallenge}&code_challenge_method=S256`;
window.location.href = authUrl;

// After OAuth callback, retrieve and remove verifier
const storedVerifier = getAndRemoveCodeVerifier('custom-provider');
```

## 🎣 React Hooks

### useSocialToken

Get current JWT token for API requests:

```typescript
const token = useSocialToken();

interface Token {
    accessToken: string;    // JWT bearer token
    refreshToken: string;   // Refresh token for renewal
    isExpired: boolean;     // True if token expired
    expiresIn: Date;        // Token expiration timestamp
}

// Usage in API calls
if (!token.isExpired) {
    const response = await fetch('/api/orders', {
        headers: {
            'Authorization': `Bearer ${token.accessToken}`
        }
    });
}
```

### useSocialUser

Get authenticated user information:

```typescript
const user = useSocialUser();

interface SocialUser {
    username: string;         // User's email/username
    isAuthenticated: boolean; // True if user is logged in
    // Add custom properties from your API
}

if (user.isAuthenticated) {
    console.log(`Logged in as: ${user.username}`);
}
```

### useContext(SocialLoginContextRefresh)

Force token refresh:

```typescript
import { useContext } from 'react';
import { SocialLoginContextRefresh } from 'rystem.authentication.social.react';

const forceRefresh = useContext(SocialLoginContextRefresh);

const handleRefresh = async () => {
    await forceRefresh();
    console.log('Token refreshed!');
};
```

### useContext(SocialLoginContextLogout)

Programmatic logout:

```typescript
import { useContext } from 'react';
import { SocialLoginContextLogout } from 'rystem.authentication.social.react';

const logout = useContext(SocialLoginContextLogout);

const handleLogout = async () => {
    await logout();
    window.location.href = '/login';
};
```

## 🎨 UI Components

### SocialLoginButtons

Renders all configured provider buttons:

```typescript
import { SocialLoginButtons } from 'rystem.authentication.social.react';

<SocialLoginButtons />
```

### Custom Button Order

```typescript
import { 
    SocialLoginButtons,
    MicrosoftButton, 
    GoogleButton, 
    FacebookButton,
    GitHubButton,
    AmazonButton,
    LinkedinButton,
    XButton,
    TikTokButton,
    InstagramButton,
    PinterestButton
} from 'rystem.authentication.social.react';

const customOrder = [
    MicrosoftButton,  // Show Microsoft first
    GoogleButton,
    GitHubButton,
    LinkedinButton,
    FacebookButton,
    AmazonButton,
    XButton,
    TikTokButton,
    InstagramButton,
    PinterestButton
];

<SocialLoginButtons buttons={customOrder} />
```

### Individual Provider Buttons

```typescript
import { MicrosoftButton, GoogleButton } from 'rystem.authentication.social.react';

<div>
    <MicrosoftButton />
    <GoogleButton />
</div>
```

### SocialLogoutButton

```typescript
import { SocialLogoutButton } from 'rystem.authentication.social.react';

<SocialLogoutButton>Sign Out</SocialLogoutButton>
```

## 🔧 Advanced Configuration

### Platform Support (Web & Mobile)

The library now supports **platform-specific configuration** for Web, iOS, and Android (including React Native):

```typescript
import { setupSocialLogin, PlatformType, LoginMode } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://yourdomain.com";
    
    // Platform configuration
    x.platform = {
        type: PlatformType.Auto,  // Auto-detect platform (Web/iOS/Android)
        
        // Smart redirect path (detects if complete URI or relative path)
        redirectPath: Platform.select({
            web: '/account/login',                          // Relative path (auto-detects domain)
            ios: 'msauth://com.yourapp.fantasoccer/auth',   // Complete URI
            android: 'myapp://oauth/callback',              // Complete URI
            default: '/account/login'
        }),
        
        // Login mode (popup for web, redirect for mobile)
        loginMode: Platform.select({
            web: LoginMode.Popup,
            ios: LoginMode.Redirect,
            android: LoginMode.Redirect,
            default: LoginMode.Redirect
        })
    };
    
    // OAuth providers
    x.microsoft.clientId = "your-client-id";
    x.google.clientId = "your-client-id";
});
```

#### React Native Example

For **React Native** apps, use platform-specific deep links:

```typescript
import { Platform } from 'react-native';
import { setupSocialLogin, PlatformType, LoginMode } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://yourdomain.com";
    
    x.platform = {
        type: PlatformType.Auto,  // Will detect iOS/Android automatically
        
        // Deep link redirect paths for mobile
        redirectPath: Platform.select({
            ios: 'msauth://com.keyserdsoze.fantasoccer/auth',   // Complete URI
            android: 'fantasoccer://oauth/callback',            // Complete URI
            default: '/account/login'                           // Relative path for web
        }),
        
        loginMode: LoginMode.Redirect  // Always use redirect for mobile
    };
    
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
});
```

**Important**: Configure deep links in your app:

**iOS** (`Info.plist`):
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>msauth</string>
        </array>
        <key>CFBundleURLName</key>
        <string>com.keyserdsoze.fantasoccer</string>
    </dict>
</array>
```

**Android** (`AndroidManifest.xml`):
```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="fantasoccer" android:host="oauth" />
</intent-filter>
```

### Login Mode (Popup vs Redirect)

Choose between **popup** and **redirect** modes:

```typescript
// Popup mode (default for web - opens in new window)
setupSocialLogin(x => {
    x.loginMode = LoginMode.Popup;  // or x.platform.loginMode
});

// Redirect mode (default for mobile - navigates in same window)
setupSocialLogin(x => {
    x.loginMode = LoginMode.Redirect;
});
```

**Use Cases:**
- ✅ **Popup**: Best for desktop web apps (better UX, user stays on page)
- ✅ **Redirect**: Required for mobile apps, some browsers block popups

### Platform Detection Utilities

Use built-in utilities for platform detection:

```typescript
import { 
    detectPlatform, 
    isMobilePlatform, 
    isReactNative,
    PlatformType 
} from 'rystem.authentication.social.react';

// Detect current platform
const platform = detectPlatform();  // Returns: PlatformType.Web | iOS | Android

// Check if mobile
if (isMobilePlatform(platform)) {
    console.log('Running on mobile');
}

// Check if React Native
if (isReactNative()) {
    console.log('Running in React Native');
}
```

### Complete Mobile Setup Example

```typescript
import { setupSocialLogin, PlatformType, LoginMode, detectPlatform } from 'rystem.authentication.social.react';

// Detect platform automatically
const currentPlatform = detectPlatform();

setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    
    // Configure based on detected platform
    x.platform = {
        type: currentPlatform,
        
        redirectUri: (() => {
            switch (currentPlatform) {
                case PlatformType.iOS:
                    return 'msauth://com.yourapp.bundle/auth';
                case PlatformType.Android:
                    return 'yourapp://oauth/callback';
                default:
                    return typeof window !== 'undefined' 
                        ? window.location.origin 
                        : 'http://localhost:3000';
            }
        })(),
        
        loginMode: currentPlatform === PlatformType.Web 
            ? LoginMode.Popup 
            : LoginMode.Redirect
    };
    
    // OAuth providers
    x.microsoft.clientId = "your-microsoft-client-id";
    x.google.clientId = "your-google-client-id";
    
    // Error handling
    x.onLoginFailure = (error) => {
        if (currentPlatform === PlatformType.Web) {
            alert(`Login failed: ${error.message}`);
        } else {
            // Use React Native Alert or Toast
            console.error('Login error:', error);
        }
    };
    
    x.automaticRefresh = true;
});
```

## 📱 Mobile OAuth Configuration

### Microsoft Entra ID (for Mobile)

1. Register your mobile app redirect URI in Azure Portal
2. For iOS: `msauth://com.yourapp.bundle/auth`
3. For Android: `yourapp://oauth/callback`
4. Enable "Mobile and desktop applications" platform
5. Make sure PKCE is enabled (library handles this automatically)

### Google (for Mobile)

1. Configure OAuth consent screen for mobile
2. Add redirect URI: Use reverse client ID for iOS
3. Example: `com.googleusercontent.apps.YOUR_CLIENT_ID:/oauth2redirect`

### Deep Link Best Practices

**iOS Bundle ID Format:**
```
msauth://com.yourcompany.yourapp/auth
```

**Android Package Name Format:**
```
yourapp://oauth/callback
```

## 🔍 How Platform Configuration Works

### Understanding Redirect URI Resolution

When a user clicks a social login button, the library determines the OAuth redirect URI using this **priority order**:

```typescript
// Priority 1: Explicit platform.redirectUri (highest priority)
if (settings.platform?.redirectUri) {
    redirectUri = settings.platform.redirectUri;
}
// Priority 2: Fallback to redirectDomain + redirectPath
else {
    redirectUri = `${settings.redirectDomain}${settings.redirectPath || ''}`;
}
```

### Example Flow (Microsoft Login on React Native iOS)

1. **Setup Configuration**:
```typescript
setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    x.redirectDomain = "https://web.yourdomain.com";
    x.redirectPath = "/account/login";
    
    x.platform = {
        type: PlatformType.iOS,
        redirectUri: "msauth://com.yourapp.bundle/auth"  // Mobile deep link
    };
    
    x.microsoft.clientId = "your-client-id";
});
```

2. **User Clicks MicrosoftButton**:
   - Library detects `platform.redirectUri` is set
   - Uses `msauth://com.yourapp.bundle/auth` (NOT `https://web.yourdomain.com/account/login`)
   - Generates PKCE code_verifier and code_challenge
   - Constructs OAuth URL:
     ```
     https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize
       ?client_id=your-client-id
       &redirect_uri=msauth%3A%2F%2Fcom.yourapp.bundle%2Fauth
       &code_challenge=<generated>
       &code_challenge_method=S256
     ```

3. **OAuth Provider Redirects**:
   - Microsoft redirects to: `msauth://com.yourapp.bundle/auth?code=ABC123&state=XYZ`
   - iOS deep link handler catches this URL
   - React Native navigation extracts `code` and `state`

4. **Token Exchange**:
   - Library calls API: `POST /api/Authentication/Social/Token?provider=Microsoft&code=ABC123&redirectPath=/account/login`
   - API validates code using PKCE code_verifier
   - Returns JWT access token

5. **User Logged In**:
   - Token stored in AsyncStorage (React Native)
   - `useSocialToken()` and `useSocialUser()` hooks update
   - App navigates to `/account/login` (or dashboard)

### Platform Auto-Detection Logic

```typescript
export function detectPlatform(): PlatformType {
    // Check if React Native environment
    if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
        // Detect iOS
        if (/iPad|iPhone|iPod/.test(navigator.userAgent)) {
            return PlatformType.iOS;
        }
        // Detect Android
        if (/Android/.test(navigator.userAgent)) {
            return PlatformType.Android;
        }
    }
    
    // Default to Web
    return PlatformType.Web;
}
```

### When to Use Each Configuration

| Scenario | redirectDomain | redirectPath | platform.redirectUri | platform.type |
|----------|----------------|--------------|---------------------|---------------|
| **Web SPA** | `https://app.com` | `/account/login` | `undefined` | `Web` or `Auto` |
| **React Native iOS** | `https://app.com` (fallback) | `/account/login` | `msauth://com.yourapp.bundle/auth` | `iOS` or `Auto` |
| **React Native Android** | `https://app.com` (fallback) | `/account/login` | `yourapp://oauth/callback` | `Android` or `Auto` |
| **Multi-Platform (Recommended)** | `https://app.com` | `/account/login` | `Platform.select({ ios: '...', android: '...', web: undefined })` | `Auto` |

### Configuration Best Practices

✅ **DO**:
- Use `PlatformType.Auto` for automatic detection
- Set `platform.redirectUri` explicitly for React Native
- Keep `redirectDomain` and `redirectPath` as fallbacks for web
- Use `Platform.select()` for cross-platform apps
- Encode redirect URIs in OAuth URLs (library does this automatically)

❌ **DON'T**:
- Hardcode platform detection (use `detectPlatform()` instead)
- Forget to register redirect URIs in OAuth provider consoles
- Use web redirect URIs (`https://`) for mobile apps
- Skip Info.plist/AndroidManifest.xml configuration for deep links

### Debugging Platform Configuration

Check which redirect URI is being used:

```typescript
import { getSocialLoginSettings } from 'rystem.authentication.social.react';

const settings = getSocialLoginSettings();
const effectiveRedirectUri = settings.platform?.redirectUri 
    || `${settings.redirectDomain}${settings.redirectPath || ''}`;

console.log('Platform Type:', settings.platform?.type);
console.log('Redirect URI:', effectiveRedirectUri);
console.log('Login Mode:', settings.platform?.loginMode || settings.loginMode);
```

## 🆚 Popup vs Redirect Comparison

| Feature | Popup Mode | Redirect Mode |
|---------|-----------|---------------|
| **Platform** | Web only | Web + Mobile |
| **User Experience** | Stays on page | Leaves page temporarily |
| **Browser Support** | May be blocked | Always works |
| **Mobile Apps** | ❌ Not supported | ✅ Required |
| **Session Persistence** | ✅ Maintained | ⚠️ Depends on implementation |
| **Security** | ✅ Same-origin | ✅ PKCE required |

## Error Handling

```typescript
setupSocialLogin(x => {
    x.onLoginFailure = (error) => {
        switch (error.code) {
            case 3:
                // Error during button click (client-side)
                console.error('Client error:', error.message);
                break;
            case 15:
                // Error during token retrieval from API
                console.error('Token exchange failed:', error.message);
                showNotification('Login failed. Please try again.');
                break;
            case 10:
                // Error fetching user information from API
                console.error('User fetch failed:', error.message);
                break;
            default:
                console.error('Unknown error:', error);
        }
    };
});
```

### Custom API Integration

```typescript
import { useSocialToken } from 'rystem.authentication.social.react';

const MyComponent = () => {
    const token = useSocialToken();
    
    const fetchProtectedData = async () => {
        if (token.isExpired) {
            alert('Please login first');
            return;
        }
        
        try {
            const response = await fetch('https://api.example.com/protected', {
                headers: {
                    'Authorization': `Bearer ${token.accessToken}`,
                    'Content-Type': 'application/json'
                }
            });
            
            if (response.status === 401) {
                // Token might be expired, force refresh
                const forceRefresh = useContext(SocialLoginContextRefresh);
                await forceRefresh();
                // Retry request
            }
            
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('API error:', error);
        }
    };
    
    return <button onClick={fetchProtectedData}>Load Data</button>;
};
```

### TypeScript Custom User Model

```typescript
interface CustomSocialUser {
    username: string;
    isAuthenticated: boolean;
    displayName: string;
    avatar: string;
    roles: string[];
}

const MyComponent = () => {
    const user = useSocialUser<CustomSocialUser>();
    
    return (
        <div>
            <img src={user.avatar} alt={user.displayName} />
            <p>{user.displayName}</p>
            <p>Roles: {user.roles.join(', ')}</p>
        </div>
    );
};
```

## 📝 Complete Example

```typescript
import { useState, useContext } from 'react';
import { 
    SocialLoginButtons, 
    SocialLoginContextLogout, 
    SocialLoginContextRefresh, 
    SocialLogoutButton, 
    useSocialToken, 
    useSocialUser,
    MicrosoftButton,
    GoogleButton,
    GitHubButton
} from 'rystem.authentication.social.react';

const customButtons = [MicrosoftButton, GoogleButton, GitHubButton];

export const Dashboard = () => {
    const token = useSocialToken();
    const user = useSocialUser();
    const forceRefresh = useContext(SocialLoginContextRefresh);
    const logout = useContext(SocialLoginContextLogout);
    const [count, setCount] = useState(0);
    
    return (
        <div className="dashboard">
            {token.isExpired ? (
                <div className="login-section">
                    <h2>Welcome! Please login</h2>
                    <SocialLoginButtons buttons={customButtons} />
                </div>
            ) : (
                <div className="user-section">
                    <h2>Welcome back, {user.username}!</h2>
                    
                    <div className="token-info">
                        <p><strong>Access Token:</strong> {token.accessToken.substring(0, 20)}...</p>
                        <p><strong>Expires:</strong> {token.expiresIn.toLocaleString()}</p>
                    </div>
                    
                    <div className="actions">
                        <button onClick={() => setCount(count + 1)}>
                            Counter: {count}
                        </button>
                        <button onClick={() => forceRefresh()}>
                            🔄 Force Refresh Token
                        </button>
                        <SocialLogoutButton>
                            🚪 Logout
                        </SocialLogoutButton>
                    </div>
                </div>
            )}
        </div>
    );
};
```

## 🌐 OAuth Provider Configuration

### Microsoft Entra ID (Azure AD)

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Create new registration (Single-page application)
3. Set **Redirect URI**: `https://yourdomain.com/account/login`
4. Under **Authentication**:
   - Enable "ID tokens"
   - Enable "Access tokens" 
   - Add redirect URI with type "Single-page application"
5. Copy **Application (client) ID**
6. **No client secret needed** - PKCE handles security

### Google

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create OAuth 2.0 Client ID (Web application)
3. Add **Authorized redirect URI**: `https://yourdomain.com/account/login`
4. Copy **Client ID**

### GitHub

1. Go to [GitHub Settings](https://github.com/settings/developers) → OAuth Apps
2. Create new OAuth App
3. Set **Authorization callback URL**: `https://yourdomain.com/account/login`
4. Copy **Client ID**

## 🔗 Related Packages

- **API Server**: `Rystem.Authentication.Social` - Backend OAuth validation with PKCE support
- **Blazor Client**: `Rystem.Authentication.Social.Blazor` - Blazor Server/WASM components
- **Abstractions**: `Rystem.Authentication.Social.Abstractions` - Shared models

## 📚 More Information

- **Complete Docs**: [https://rystem.net/mcp/tools/auth-social-typescript.md](https://rystem.net/mcp/tools/auth-social-typescript.md)
- **OAuth Flow Diagram**: [https://rystem.net/mcp/prompts/auth-flow.md](https://rystem.net/mcp/prompts/auth-flow.md)
- **PKCE RFC**: [RFC 7636](https://tools.ietf.org/html/rfc7636)
