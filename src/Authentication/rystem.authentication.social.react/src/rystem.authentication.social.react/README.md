### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# rystem.authentication.social.react

React/TypeScript library for social authentication with built-in PKCE support for secure OAuth 2.0 flows.

### ✨ Key Features

- **🔐 PKCE Built-in**: Automatic code_verifier generation for Microsoft OAuth (RFC 7636)
- **⚛️ React Hooks**: Type-safe hooks for token and user management
- **🎨 Ready-to-Use Components**: Login buttons, logout, authentication wrapper
- **🔄 Automatic Token Refresh**: Handles token expiration seamlessly
- **📱 SPA Optimized**: Designed for Single-Page Applications with security best practices

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
    
    // Optional: OAuth redirect path (default: "/account/login")
    x.redirectPath = "/account/login";
    
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

### Error Handling

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
