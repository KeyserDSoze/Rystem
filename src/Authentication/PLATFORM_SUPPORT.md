# Platform & Login Mode Support - Migration Guide

## 🎯 New Features

### 1. **Platform Support** (Web, iOS, Android, React Native, MAUI)
Configure platform-specific redirect URIs for mobile apps

### 2. **Login Mode** (Popup vs Redirect)
Choose between popup windows (web) or full-page redirects (mobile)

---

## 📱 React/TypeScript Examples

### Basic Web Setup (Popup)

```typescript
import { setupSocialLogin, LoginMode } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://yourdomain.com";
    
    // Optional: Configure platform-specific redirect path
    x.platform = {
        redirectPath: "/account/login",  // Relative path (auto-detects domain)
        loginMode: LoginMode.Popup  // Opens OAuth in popup window (web only)
    };
    
    x.microsoft.clientId = "your-client-id";
    x.google.clientId = "your-client-id";
});
```

### Web with Redirect Mode

```typescript
setupSocialLogin(x => {
    x.apiUri = "https://yourdomain.com";
    
    x.platform = {
        loginMode: LoginMode.Redirect  // Navigate to OAuth provider (full-page redirect)
    };
    
    x.microsoft.clientId = "your-client-id";
});
```

### React Native (Mobile App)

```typescript
import { Platform } from 'react-native';
import { setupSocialLogin, PlatformType, LoginMode } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    
    // Platform-specific configuration
    x.platform = {
        type: PlatformType.Auto,  // Auto-detects iOS/Android
        
        // Smart redirect path detection:
        // - Contains "://" -> Complete URI (mobile deep links)
        // - Starts with "/" -> Relative path (web, auto-detects domain)
        redirectPath: Platform.select({
            ios: 'msauth://com.keyserdsoze.fantasoccer/auth',  // Complete URI for iOS
            android: 'fantasoccer://oauth/callback',  // Complete URI for Android
            default: '/account/login'  // Relative path for web
        }),
        
        loginMode: LoginMode.Redirect  // Always redirect on mobile
    };
    
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.google.clientId = "23769141170-xxx.apps.googleusercontent.com";
});
```

**Configure Deep Links:**

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

### Advanced: Custom Platform Detection

```typescript
import { detectPlatform, isMobilePlatform, PlatformType, LoginMode } from 'rystem.authentication.social.react';

const platform = detectPlatform();

setupSocialLogin(x => {
    x.apiUri = "https://api.yourdomain.com";
    
    x.platform = {
        type: platform,
        
        // Smart detection handles this automatically:
        // - Complete URI (contains "://") for mobile
        // - Relative path (starts with "/") for web
        redirectPath: (() => {
            switch (platform) {
                case PlatformType.iOS:
                    return 'msauth://com.yourapp.bundle/auth';  // Complete URI
                case PlatformType.Android:
                    return 'yourapp://oauth/callback';  // Complete URI
                default:
                    return '/account/login';  // Relative path (auto-detects domain)
            }
        })(),
        
        loginMode: isMobilePlatform(platform) ? LoginMode.Redirect : LoginMode.Popup
    };
    
    x.microsoft.clientId = "your-client-id";
});
```

---

## 🔷 Blazor/MAUI Examples

### Basic Blazor Web (Server/WASM)

```csharp
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://yourdomain.com";
    
    // No platform config needed for web - uses NavigationManager.BaseUri
    x.Microsoft.ClientId = builder.Configuration["Microsoft:ClientId"];
    x.Google.ClientId = builder.Configuration["Google:ClientId"];
});
```

### MAUI Hybrid (iOS & Android)

```csharp
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://api.yourdomain.com";
    
    // Platform-specific configuration
    x.Platform = new PlatformConfig
    {
        Type = PlatformType.Auto,  // Auto-detects iOS/Android/Web
        
        // Smart redirect path detection:
        // - Contains "://" -> Complete URI (mobile deep links: msauth://, fantasoccer://)
        // - Starts with "/" -> Relative path (web, uses NavigationManager.BaseUri)
        // - Empty/null -> Default "/account/login"
#if IOS
        RedirectPath = "msauth://com.keyserdsoze.fantasoccer/auth",  // Complete URI for iOS
#elif ANDROID
        RedirectPath = "fantasoccer://oauth/callback",  // Complete URI for Android
#else
        RedirectPath = "/account/login",  // Relative path for web
#endif
        
        LoginMode = LoginMode.Redirect  // Only mode supported currently
    };
    
    x.Microsoft.ClientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
});
```

**Configure Deep Links:**

**iOS** (`Platforms/iOS/Info.plist`):
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

**Android** (`Platforms/Android/AndroidManifest.xml`):
```xml
<activity android:name="com.microsoft.identity.client.BrowserTabActivity">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data
            android:scheme="fantasoccer"
            android:host="oauth"
            android:path="/callback" />
    </intent-filter>
</activity>
```

### Advanced: Runtime Platform Detection

```csharp
@inject IJSRuntime JSRuntime

@code {
    protected override async Task OnInitializedAsync()
    {
        var platform = await PlatformDetector.DetectPlatformAsync(JSRuntime);
        
        if (PlatformDetector.IsMobilePlatform(platform))
        {
            // Configure mobile-specific behavior
            Console.WriteLine($"Running on {platform}");
        }
    }
}
```

---

## 🆚 Comparison: Popup vs Redirect

### Popup Mode (Web Only)

**✅ Advantages:**
- User stays on your page
- Better UX (no full-page navigation)
- Session preserved during OAuth flow

**❌ Limitations:**
- Only works in web browsers
- May be blocked by popup blockers
- Not supported on mobile

**Usage:**
```typescript
x.loginMode = LoginMode.Popup;
```

### Redirect Mode (Web & Mobile)

**✅ Advantages:**
- Works everywhere (web + mobile)
- No popup blocker issues
- Required for mobile apps
- Better for mobile UX

**❌ Limitations:**
- Full-page navigation (user leaves your app temporarily)
- May need to restore session state

**Usage:**
```typescript
x.loginMode = LoginMode.Redirect;
```

---

## 🔐 OAuth Provider Configuration

### Microsoft Entra ID (Azure AD)

**For Web:**
- Redirect URI: `https://yourdomain.com/account/login`
- Platform: **Web**

**For Mobile:**
- iOS Redirect URI: `msauth://com.yourapp.bundle/auth`
- Android Redirect URI: `yourapp://oauth/callback`
- Platform: **Mobile and desktop applications**

**Both need:**
- ✅ Enable "ID tokens"
- ✅ Enable "Access tokens"
- ✅ PKCE enabled (automatic with library)

### Google

**For Web:**
- Authorized redirect URI: `https://yourdomain.com/account/login`
- Application type: **Web application**

**For Mobile:**
- **iOS**: Use reverse client ID format
  - `com.googleusercontent.apps.YOUR_CLIENT_ID:/oauth2redirect`
- **Android**: Use package name + SHA-1
  - Package: `com.yourapp`
  - SHA-1 from keystore

---

## 📋 Quick Reference

### React/TypeScript Exports

```typescript
import {
    // Setup
    setupSocialLogin,
    
    // Enums
    PlatformType,  // Web, iOS, Android, Auto
    LoginMode,     // Popup, Redirect
    
    // Types
    PlatformConfig,
    PlatformSelector,
    
    // Utilities
    detectPlatform,
    isMobilePlatform,
    isReactNative,
    getDefaultRedirectUri,
    selectByPlatform
} from 'rystem.authentication.social.react';
```

### Blazor/C# Types

```csharp
using Rystem.Authentication.Social.Blazor;

// Enums
PlatformType.Web | iOS | Android | Auto
LoginMode.Redirect | Popup

// Classes
PlatformConfig
PlatformDetector
```

---

## 🎯 Migration Checklist

### For Existing Web Apps

- [ ] **No changes needed** - default behavior is popup (web)
- [ ] Optional: Set `x.loginMode = LoginMode.Popup` explicitly

### For New Mobile Apps

- [ ] Install `Rystem.Authentication.Social.Blazor` or `.react`
- [ ] Configure `x.platform.redirectUri` with deep link
- [ ] Set `x.platform.loginMode = LoginMode.Redirect`
- [ ] Add deep link configuration to iOS/Android manifests
- [ ] Register deep link URI with OAuth providers
- [ ] Test OAuth flow on physical devices

### For React Native

- [ ] Import platform utilities: `detectPlatform`, `PlatformType`, `LoginMode`
- [ ] Use `Platform.select()` for platform-specific URIs
- [ ] Configure deep links in `Info.plist` and `AndroidManifest.xml`
- [ ] Update OAuth provider with mobile redirect URIs
- [ ] Test on both iOS and Android

---

## 🐛 Troubleshooting

### Issue: "Redirect URI mismatch"

**Solution:** Ensure your `PlatformConfig.RedirectUri` + `RedirectPath` exactly matches the URI registered in your OAuth provider.

```typescript
// Must match EXACTLY (including trailing slashes, paths)
x.platform.redirectUri = "msauth://com.keyserdsoze.fantasoccer/auth";
```

### Issue: Deep link not working on mobile

**Solution:**
1. Verify scheme registered in `Info.plist` (iOS) or `AndroidManifest.xml` (Android)
2. Test deep link with: `xcrun simctl openurl booted "msauth://com.yourapp.bundle/auth?code=test"`
3. Ensure app handles deep link and navigates to callback page

### Issue: Popup blocked on web

**Solution:**
1. Use `LoginMode.Redirect` instead
2. Or inform users to allow popups for your domain

---

## 📚 Documentation Links

- **React Platform Support**: `src/Authentication/rystem.authentication.social.react/README.md`
- **Blazor Platform Support**: `src/Authentication/Rystem.Authentication.Social.Blazor/README.md`
- **PKCE Implementation**: Both READMEs contain PKCE sections
- **OAuth Providers Setup**: Both READMEs contain provider-specific guides

---

**Questions?** Join our Discord: https://discord.gg/tkWvy4WPjt
