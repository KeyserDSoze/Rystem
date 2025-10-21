# Social Authentication - TypeScript/React Client

Add **social login** to your React, Vue, or Angular application with automatic **JWT token management**.

**Features:**
- Pre-built social login buttons
- React hooks for token and user
- Automatic token refresh
- TypeScript support
- Framework agnostic core

---

## Installation

```bash
npm install rystem.authentication.social.react
```

---

## Setup

Configure social login in your app's entry point (`main.tsx` or `App.tsx`):

```typescript
import { setupSocialLogin, SocialLoginWrapper } from 'rystem.authentication.social.react';

setupSocialLogin(config => {
    config.apiUri = "https://localhost:7017"; // Your API URL
    config.google.clientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";
    config.microsoft.clientId = "YOUR_MICROSOFT_CLIENT_ID";
    config.facebook.clientId = "YOUR_FACEBOOK_APP_ID";
    config.github.clientId = "YOUR_GITHUB_CLIENT_ID";
    config.automaticRefresh = true; // Auto-refresh expired tokens
    config.onLoginFailure = (error) => {
        console.error(`Login failed: ${error.message}`, error);
    };
});

function App() {
    return (
        <SocialLoginWrapper>
            <YourApp />
        </SocialLoginWrapper>
    );
}

export default App;
```

---

## Login Buttons

### Default Buttons (All Providers)

```typescript
import { SocialLoginButtons } from 'rystem.authentication.social.react';

export const LoginPage = () => {
    return (
        <div>
            <h1>Sign In</h1>
            <SocialLoginButtons />
        </div>
    );
};
```

### Custom Button Order

```typescript
import { 
    SocialLoginButtons,
    GoogleButton,
    MicrosoftButton,
    FacebookButton,
    GitHubButton,
    LinkedinButton,
    XButton,
    InstagramButton,
    PinterestButton,
    TikTokButton,
    AmazonButton
} from 'rystem.authentication.social.react';

const customOrder = [
    GoogleButton,
    MicrosoftButton,
    GitHubButton,
    FacebookButton,
    LinkedinButton,
    XButton,
    InstagramButton,
    PinterestButton,
    TikTokButton,
    AmazonButton
];

export const LoginPage = () => {
    return (
        <div>
            <h1>Choose Provider</h1>
            <SocialLoginButtons buttons={customOrder} />
        </div>
    );
};
```

---

## Access Token

Use the `useSocialToken()` hook to get the current JWT token:

```typescript
import { useSocialToken } from 'rystem.authentication.social.react';

export const Dashboard = () => {
    const token = useSocialToken();
    
    // Check if token is expired
    if (token.isExpired) {
        return <LoginPage />;
    }
    
    return (
        <div>
            <h1>Dashboard</h1>
            <p>Token: {token.accessToken}</p>
            <p>Expires at: {token.expiresAt.toLocaleString()}</p>
        </div>
    );
};
```

### Use Token in API Requests

```typescript
import { useSocialToken } from 'rystem.authentication.social.react';
import { useEffect, useState } from 'react';

export const UserList = () => {
    const token = useSocialToken();
    const [users, setUsers] = useState([]);
    
    useEffect(() => {
        if (!token.isExpired) {
            fetch('https://localhost:7017/api/users', {
                headers: {
                    'Authorization': `Bearer ${token.accessToken}`
                }
            })
            .then(res => res.json())
            .then(data => setUsers(data));
        }
    }, [token]);
    
    return (
        <ul>
            {users.map(user => <li key={user.id}>{user.email}</li>)}
        </ul>
    );
};
```

---

## Access User

Use the `useSocialUser()` hook to get current user information:

```typescript
import { useSocialUser } from 'rystem.authentication.social.react';

export const Profile = () => {
    const user = useSocialUser();
    
    if (!user.isAuthenticated) {
        return <p>Not logged in</p>;
    }
    
    return (
        <div>
            <h1>Profile</h1>
            <p>Username: {user.username}</p>
            <p>Email: {user.email}</p>
            <p>Provider: {user.provider}</p>
        </div>
    );
};
```

---

## Logout

### Pre-built Logout Button

```typescript
import { SocialLogoutButton } from 'rystem.authentication.social.react';

export const Header = () => {
    return (
        <header>
            <h1>My App</h1>
            <SocialLogoutButton>
                Sign Out
            </SocialLogoutButton>
        </header>
    );
};
```

### Custom Logout

```typescript
import { useContext } from 'react';
import { SocialLoginContextLogout } from 'rystem.authentication.social.react';

export const Header = () => {
    const logout = useContext(SocialLoginContextLogout);
    
    return (
        <header>
            <h1>My App</h1>
            <button onClick={() => logout()}>
                Logout
            </button>
        </header>
    );
};
```

---

## Force Refresh Token

```typescript
import { useContext } from 'react';
import { SocialLoginContextRefresh } from 'rystem.authentication.social.react';

export const Settings = () => {
    const forceRefresh = useContext(SocialLoginContextRefresh);
    
    return (
        <div>
            <h1>Settings</h1>
            <button onClick={() => forceRefresh()}>
                Refresh Token
            </button>
        </div>
    );
};
```

---

## Complete Example

```typescript
import { useState, useContext } from 'react';
import {
    setupSocialLogin,
    SocialLoginWrapper,
    SocialLoginButtons,
    SocialLoginContextLogout,
    SocialLoginContextRefresh,
    SocialLogoutButton,
    useSocialToken,
    useSocialUser,
    GoogleButton,
    MicrosoftButton,
    FacebookButton,
    GitHubButton,
    LinkedinButton,
    XButton,
    InstagramButton,
    PinterestButton
} from 'rystem.authentication.social.react';

// Setup
setupSocialLogin(config => {
    config.apiUri = "https://localhost:7017";
    config.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    config.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    config.facebook.clientId = "345885718092912";
    config.github.clientId = "97154d062f2bb5d28620";
    config.automaticRefresh = true;
    config.onLoginFailure = (error) => {
        alert(`Login failed: ${error.message}`);
    };
});

// Custom button order
const buttonOrder = [
    MicrosoftButton,
    GoogleButton,
    LinkedinButton,
    FacebookButton,
    GitHubButton,
    XButton,
    InstagramButton,
    PinterestButton
];

// Main component
const AppContent = () => {
    const token = useSocialToken();
    const user = useSocialUser();
    const forceRefresh = useContext(SocialLoginContextRefresh);
    const logout = useContext(SocialLoginContextLogout);
    
    if (token.isExpired) {
        return (
            <div className="login-page">
                <h1>Welcome</h1>
                <p>Sign in to continue</p>
                <SocialLoginButtons buttons={buttonOrder} />
            </div>
        );
    }
    
    return (
        <div className="dashboard">
            <header>
                <h1>Dashboard</h1>
                <div>
                    {user.isAuthenticated && <span>Hello, {user.username}</span>}
                    <button onClick={() => forceRefresh()}>Refresh</button>
                    <SocialLogoutButton>Logout</SocialLogoutButton>
                </div>
            </header>
            
            <main>
                <div className="user-info">
                    <h2>User Information</h2>
                    <p>Username: {user.username}</p>
                    <p>Email: {user.email}</p>
                    <p>Provider: {user.provider}</p>
                </div>
                
                <div className="token-info">
                    <h2>Token</h2>
                    <p>Access Token: {token.accessToken.substring(0, 50)}...</p>
                    <p>Expires At: {token.expiresAt.toLocaleString()}</p>
                    <p>Is Expired: {token.isExpired ? 'Yes' : 'No'}</p>
                </div>
            </main>
        </div>
    );
};

function App() {
    return (
        <SocialLoginWrapper>
            <AppContent />
        </SocialLoginWrapper>
    );
}

export default App;
```

---

## Error Handling

The `onLoginFailure` callback receives error codes:

```typescript
setupSocialLogin(config => {
    config.onLoginFailure = (error) => {
        switch (error.code) {
            case 3:
                console.error('Error clicking social button', error);
                break;
            case 15:
                console.error('Error retrieving token', error);
                break;
            case 10:
                console.error('Error retrieving user', error);
                break;
            default:
                console.error('Unknown error', error);
        }
        
        // Show user-friendly message
        alert(`Login failed: ${error.message}`);
    };
});
```

**Error Codes:**
- `3`: Error during social button click
- `15`: Error during token retrieval
- `10`: Error during user retrieval
- `0` or `"DotNet"`: Integration error with .NET API

---

## Real-World Examples

### Protected Routes with React Router

```typescript
import { Navigate } from 'react-router-dom';
import { useSocialToken } from 'rystem.authentication.social.react';

const ProtectedRoute = ({ children }) => {
    const token = useSocialToken();
    
    if (token.isExpired) {
        return <Navigate to="/login" replace />;
    }
    
    return children;
};

// Usage
<Route path="/dashboard" element={
    <ProtectedRoute>
        <Dashboard />
    </ProtectedRoute>
} />
```

### Axios Interceptor

```typescript
import axios from 'axios';
import { useSocialToken } from 'rystem.authentication.social.react';

export const useApiClient = () => {
    const token = useSocialToken();
    
    const apiClient = axios.create({
        baseURL: 'https://localhost:7017/api'
    });
    
    apiClient.interceptors.request.use(config => {
        if (!token.isExpired) {
            config.headers.Authorization = `Bearer ${token.accessToken}`;
        }
        return config;
    });
    
    return apiClient;
};

// Usage
const apiClient = useApiClient();
const users = await apiClient.get('/users');
```

### Multi-Tenant Dashboard

```typescript
import { useSocialUser } from 'rystem.authentication.social.react';

export const TenantDashboard = () => {
    const user = useSocialUser();
    const tenantId = user.claims?.find(c => c.type === 'TenantId')?.value;
    
    return (
        <div>
            <h1>Tenant Dashboard</h1>
            <p>Tenant ID: {tenantId}</p>
        </div>
    );
};
```

---

## TypeScript Types

```typescript
interface SocialLoginConfig {
    apiUri: string;
    automaticRefresh: boolean;
    onLoginFailure?: (error: LoginError) => void;
    google: { clientId: string };
    microsoft: { clientId: string };
    facebook: { clientId: string };
    github: { clientId: string };
}

interface SocialToken {
    accessToken: string;
    refreshToken: string;
    expiresAt: Date;
    isExpired: boolean;
}

interface SocialUser {
    username: string;
    email: string;
    provider: string;
    isAuthenticated: boolean;
    claims?: Claim[];
}

interface LoginError {
    code: number;
    message: string;
    provider: string;
}
```

---

## Benefits

- ✅ **React Hooks**: `useSocialToken()`, `useSocialUser()`
- ✅ **Pre-built Components**: Login buttons, logout button
- ✅ **Automatic Refresh**: Tokens refresh automatically
- ✅ **TypeScript**: Full type support
- ✅ **Framework Agnostic**: Core library works with any framework

---

## Related Tools

- **[Social Authentication - Server Setup](https://rystem.net/mcp/tools/auth-social-server.md)** - API configuration
- **[Social Authentication - Blazor Client](https://rystem.net/mcp/tools/auth-social-blazor.md)** - Blazor integration
- **[Repository API Client - TypeScript](https://rystem.net/mcp/tools/repository-api-client-typescript.md)** - Repository with auth

---

## References

- **NPM Package**: [rystem.authentication.social.react](https://www.npmjs.com/package/rystem.authentication.social.react)
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
