### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## ⚛️ rystem.authentication.social.react

Complete **React hooks and components** for social login - seamless OAuth 2.0 integration for React applications with automatic token management and provider support.

### 📦 What's Included

✅ **Pre-built Login Buttons** - Google, Microsoft, Facebook, GitHub, Amazon, LinkedIn, X, TikTok, Pinterest, Instagram  
✅ **React Hooks** - `useSocialUser()`, `useSocialToken()`, `useContext()` integration  
✅ **OAuth Flow Handler** - Automatic authorization code exchange  
✅ **Token Management** - Auto-refresh, expiration checking  
✅ **Error Handling** - Built-in error callback support  
✅ **Type-Safe** - Full TypeScript support  

---

## 📦 Installation

```bash
npm install rystem.authentication.social.react
```

---

## 🚀 Step 1: Setup

In your main `App.tsx`:

```typescript
import { SocialLoginWrapper, setupSocialLogin } from 'rystem.authentication.social.react';

// 1. Configure social login
setupSocialLogin(config => {
    // API server URL (where OAuth endpoints are hosted)
    config.apiUri = "https://localhost:7017";
    
    // Client IDs from app registrations (public only - never share secrets!)
    config.google.clientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";
    config.microsoft.clientId = "YOUR_MICROSOFT_CLIENT_ID";
    config.facebook.clientId = "YOUR_FACEBOOK_APP_ID";
    config.github.clientId = "YOUR_GITHUB_CLIENT_ID";
    config.amazon.clientId = "YOUR_AMAZON_CLIENT_ID";
    config.linkedin.clientId = "YOUR_LINKEDIN_CLIENT_ID";
    config.x.clientId = "YOUR_X_CLIENT_ID";
    config.tiktok.clientId = "YOUR_TIKTOK_CLIENT_ID";
    config.pinterest.clientId = "YOUR_PINTEREST_CLIENT_ID";
    config.instagram.clientId = "YOUR_INSTAGRAM_CLIENT_ID";
    
    // Optional: Auto-refresh token before expiration
    config.automaticRefresh = true;
    
    // Optional: Error callback
    config.onLoginFailure = (data) => {
        console.error(`Login failed (${data.code}): ${data.message}`);
        console.error(`Provider: ${data.provider}`);
    };
});

// 2. Wrap app with provider
function App() {
    return (
        <SocialLoginWrapper>
            <MainApp />
        </SocialLoginWrapper>
    );
}

export default App;
```

---

## 🔐 App Registration ClientIds (by Provider)

### Microsoft Azure AD
1. Go to **[Azure Portal](https://portal.azure.com)** → Azure Active Directory
2. **App registrations** → **New registration**
3. **Supported account types**: Accounts in any organizational directory (Multi-tenant)
4. **Redirect URI**: Select "Single Page Application (SPA)"
5. Enter: `https://yourdomain.com/auth/microsoft/callback`
6. Copy **Application (client) ID**

```typescript
config.microsoft.clientId = "your-application-client-id";
```

### Google OAuth
1. Go to **[Google Cloud Console](https://console.cloud.google.com)**
2. **APIs & Services** → **Credentials**
3. **Create credentials** → **OAuth client ID**
4. **Application type**: Web application
5. Add **Authorized JavaScript origins**: `https://yourdomain.com`
6. Add **Authorized redirect URIs**: `https://yourdomain.com/auth/google/callback`

```typescript
config.google.clientId = "your-client-id.apps.googleusercontent.com";
```

### Facebook
1. Go to **[Facebook Developers](https://developers.facebook.com)**
2. **My Apps** → Select or create app
3. **Settings** → **Basic** → Copy **App ID**
4. **Products** → **Facebook Login** → **Settings**
5. **Valid OAuth Redirect URIs**: `https://yourdomain.com/auth/facebook/callback`

```typescript
config.facebook.clientId = "your-app-id";
```

### GitHub
1. Go to **GitHub Settings** → **Developer settings** → **OAuth Apps**
2. **New OAuth App**
3. **Authorization callback URL**: `https://yourdomain.com/auth/github/callback`

```typescript
config.github.clientId = "your-client-id";
```

---

## 🔘 Step 2: Display Login Buttons

### All Providers

```typescript
import { SocialLoginButtons } from 'rystem.authentication.social.react';

function LoginPage() {
    return (
        <div className="login-container">
            <h1>Sign In</h1>
            <SocialLoginButtons />
        </div>
    );
}
```

### Custom Provider Order

```typescript
import {
    MicrosoftButton,
    GoogleButton,
    FacebookButton,
    GitHubButton,
    LinkedinButton
} from 'rystem.authentication.social.react';

function LoginPage() {
    const buttons = [
        MicrosoftButton,
        GoogleButton,
        LinkedinButton,
        FacebookButton,
        GitHubButton
    ];
    
    return <SocialLoginButtons buttons={buttons} />;
}
```

### Individual Buttons

```typescript
import {
    MicrosoftButton,
    GoogleButton,
    FacebookButton
} from 'rystem.authentication.social.react';

function LoginPage() {
    return (
        <div className="button-group">
            <GoogleButton />
            <MicrosoftButton />
            <FacebookButton />
        </div>
    );
}
```

---

## 👤 Step 3: Access User Data

Use the `useSocialUser()` hook:

```typescript
import { useSocialUser } from 'rystem.authentication.social.react';

function Dashboard() {
    const user = useSocialUser();
    
    if (!user) {
        return <p>Not logged in</p>;
    }
    
    return (
        <div className="user-profile">
            <h1>Welcome, {user.username}!</h1>
            <p>Email: {user.email}</p>
            {user.profilePictureUrl && (
                <img src={user.profilePictureUrl} alt="Profile" />
            )}
        </div>
    );
}
```

---

## 🔑 Step 4: Access Token

Use the `useSocialToken()` hook:

```typescript
import { useSocialToken } from 'rystem.authentication.social.react';

function ApiConsumer() {
    const token = useSocialToken();
    
    async function fetchUserData() {
        if (token?.isExpired) {
            console.log("Token expired");
            return;
        }
        
        const response = await fetch('https://localhost:7017/api/my-data', {
            headers: {
                'Authorization': `Bearer ${token?.bearerToken}`
            }
        });
        
        return response.json();
    }
    
    return (
        <div>
            <p>Expires: {token?.expiresAt?.toLocaleString()}</p>
            <p>Expired: {token?.isExpired ? "Yes" : "No"}</p>
            <button onClick={fetchUserData}>Fetch Data</button>
        </div>
    );
}
```

---

## 🔄 Step 5: Token Refresh

### Automatic Refresh

```typescript
setupSocialLogin(config => {
    config.automaticRefresh = true;  // Refresh before expiration
});
```

### Manual Refresh

```typescript
import { useContext } from 'react';
import { SocialLoginContextRefresh } from 'rystem.authentication.social.react';

function TokenManager() {
    const forceRefresh = useContext(SocialLoginContextRefresh);
    
    return (
        <button onClick={() => forceRefresh?.()}>
            Refresh Token
        </button>
    );
}
```

---

## 🚪 Step 6: Logout

### Using SocialLogout Component

```typescript
import { SocialLogoutButton } from 'rystem.authentication.social.react';

function Header() {
    return (
        <header>
            <h1>My App</h1>
            <SocialLogoutButton />
        </header>
    );
}
```

### Using Logout Context

```typescript
import { useContext } from 'react';
import { SocialLoginContextLogout } from 'rystem.authentication.social.react';

function CustomLogout() {
    const logout = useContext(SocialLoginContextLogout);
    
    return (
        <button onClick={() => logout?.()}>
            Sign Out
        </button>
    );
}
```

---

## ❌ Error Handling

### Error Codes

```typescript
setupSocialLogin(config => {
    config.onLoginFailure = (error) => {
        switch (error.code) {
            case 3:
                // Social provider button click error
                console.error("Failed to initialize provider");
                break;
            case 15:
                // Token retrieval error
                console.error("Failed to get access token");
                break;
            case 10:
                // User data retrieval error
                console.error("Failed to load user data");
                break;
            default:
                console.error("Unknown error:", error.message);
        }
        
        console.log("Provider:", error.provider);  // "Google", "Microsoft", etc.
    };
});
```

---

## 📋 Complete Example

```typescript
// src/App.tsx
import { SocialLoginWrapper, setupSocialLogin } from 'rystem.authentication.social.react';
import { LoginPage } from './pages/LoginPage';
import { Dashboard } from './pages/Dashboard';

setupSocialLogin(config => {
    config.apiUri = "https://localhost:7017";
    config.google.clientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";
    config.microsoft.clientId = "YOUR_MICROSOFT_CLIENT_ID";
    config.automaticRefresh = true;
    config.onLoginFailure = (error) => {
        console.error(`${error.provider}: ${error.message}`);
    };
});

function App() {
    return (
        <SocialLoginWrapper>
            <MainApp />
        </SocialLoginWrapper>
    );
}

function MainApp() {
    const user = useSocialUser();
    return (
        <div className="app">
            {!user ? <LoginPage /> : <Dashboard />}
        </div>
    );
}

export default App;
```

```typescript
// src/pages/Dashboard.tsx
import { useSocialUser, useSocialToken } from 'rystem.authentication.social.react';
import { useContext } from 'react';
import { SocialLoginContextLogout } from 'rystem.authentication.social.react';

export function Dashboard() {
    const user = useSocialUser();
    const token = useSocialToken();
    const logout = useContext(SocialLoginContextLogout);
    
    return (
        <div className="dashboard">
            <h1>Welcome, {user?.username}!</h1>
            <p>Email: {user?.email}</p>
            <p>Token expires: {token?.expiresAt?.toLocaleString()}</p>
            <button onClick={() => logout?.()}>Sign Out</button>
        </div>
    );
}
```

---

## 📚 Related Packages

- **Rystem.Authentication.Social** - Server-side OAuth implementation
- **Rystem.Authentication.Social.Blazor** - Blazor alternative  
- **Rystem.Authentication.Social.Abstractions** - Interfaces and models

---

## References

- [OAuth 2.0 Specification](https://tools.ietf.org/html/rfc6749)
- [React Hooks](https://react.dev/reference/react)
- [Server Implementation](../Rystem.Authentication.Social/README.md)
```
