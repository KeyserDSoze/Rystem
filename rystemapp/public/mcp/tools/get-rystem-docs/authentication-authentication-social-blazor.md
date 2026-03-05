### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Rystem.Authentication.Social.Blazor

Blazor UI components for social authentication (Blazor Server and WebAssembly) with built-in PKCE support for secure OAuth 2.0 flows.

### ✨ Key Features

- **🔐 PKCE Built-in**: Automatic code_verifier generation for Microsoft OAuth (RFC 7636)
- **🎨 Ready-to-Use Components**: Login buttons, logout, authentication router
- **⚡ Blazor Server & WASM**: Works with both hosting models
- **🔄 Automatic Token Refresh**: Handles token expiration seamlessly
- **🎯 Type-Safe**: Strongly-typed user models with C# generics
- **📱 MAUI Hybrid Support**: Full .NET MAUI Blazor Hybrid support with deep link OAuth flows

## 🆕 What's New - Mobile Platform Support

**All social providers now support MAUI Blazor Hybrid!** Configure platform-specific OAuth redirect URIs for seamless authentication across Web (Blazor Server/WASM), iOS (MAUI), and Android (MAUI).

### Supported Platforms & Providers

| Provider | Blazor Server | Blazor WASM | MAUI iOS | MAUI Android | PKCE Support |
|----------|---------------|-------------|----------|--------------|--------------|
| Microsoft | ✅ | ✅ | ✅ | ✅ | ✅ |
| Google | ✅ | ✅ | ✅ | ✅ | - |

*More providers coming soon for Blazor*

### How It Works

1. **Auto-Detection**: Library automatically detects platform (Web/iOS/Android) via JSInterop and compile-time symbols
2. **Platform-Specific URIs**: Configure custom redirect URIs per platform (e.g., `msauth://` for iOS, `myapp://` for Android)
3. **Deep Links**: All buttons support mobile deep link OAuth callbacks through MAUI's `AppActions`
4. **No Breaking Changes**: Existing Blazor Server/WASM apps work without modification

### Quick Example

```csharp
// Program.cs or MauiProgram.cs
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://api.yourdomain.com";
    
    // Platform configuration (auto-detects if not specified)
    x.Platform = new PlatformConfig
    {
        Type = PlatformType.Auto,  // Auto-detect Web/iOS/Android
        
        // Smart redirect path detection:
        // - Contains "://" -> Complete URI (mobile deep links: msauth://, myapp://)
        // - Starts with "/" -> Relative path (web, auto-detects domain)
        // - Empty/null -> Default "/account/login"
#if IOS
        RedirectPath = "msauth://com.yourapp.bundle/auth",  // Complete URI for iOS
#elif ANDROID
        RedirectPath = "myapp://oauth/callback",  // Complete URI for Android
#else
        RedirectPath = "/account/login",  // Relative path for web
#endif
        
        LoginMode = LoginMode.Redirect
    };
    
    x.Microsoft.ClientId = builder.Configuration["Microsoft:ClientId"];
    x.Google.ClientId = builder.Configuration["Google:ClientId"];
});
```

**iOS Deep Link Configuration** (`Platforms/iOS/Info.plist`):
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array><string>msauth</string></array>
        <key>CFBundleURLName</key>
        <string>com.yourapp.bundle</string>
    </dict>
</array>
```

**Android Deep Link Configuration** (`Platforms/Android/AndroidManifest.xml`):
```xml
<intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="myapp" android:host="oauth" />
</intent-filter>
```

📖 **Full Migration Guide**: See [`PLATFORM_SUPPORT.md`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Authentication/PLATFORM_SUPPORT.md) for detailed setup instructions, OAuth provider configuration, and troubleshooting.

## 📦 Installation

```bash
dotnet add package Rystem.Authentication.Social.Blazor
```

## 🚀 Quick Start

### 1. Install JavaScript Interop

Add to your `App.razor` (in `<head>` or before `</body>`):

```html
<script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
```

### 2. Configure Services (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // or .AddInteractiveWebAssemblyComponents()

// Configure social login UI
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://localhost:7017";  // Your API server URL
    
    // Configure OAuth providers (only ClientId needed for client-side)
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Facebook.ClientId = builder.Configuration["SocialLogin:Facebook:ClientId"];
    x.GitHub.ClientId = builder.Configuration["SocialLogin:GitHub:ClientId"];
    // Add other providers as needed
});

// Optional: Configure repository with automatic authorization headers
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient("https://localhost:7017")
                  .WithDefaultRetryPolicy();
    });
});

// Add authorization interceptor to inject Bearer tokens automatically
builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();  // or .AddInteractiveWebAssemblyRenderMode()

app.Run();
```

### 3. Update Routes.razor

Replace default router with `SocialAuthenticationRouter`:

```razor
<SocialAuthenticationRouter 
    AppAssembly="typeof(Program).Assembly" 
    DefaultLayout="typeof(Layout.MainLayout)">
</SocialAuthenticationRouter>
```

This automatically:
- Handles OAuth callbacks (`/account/login?code=...`)
- Manages token storage in localStorage
- Provides cascading `SocialUserWrapper` parameter to all pages

### 4. Add Login Page

```razor
@page "/login"
@using Rystem.Authentication.Social.Blazor

<h3>Login</h3>

<SocialLogin />

@code {
    // Component automatically renders all configured provider buttons
}
```

### 5. Access User in Components

```razor
@page "/dashboard"

<h3>Welcome, @SocialUser?.User?.Username</h3>

@if (SocialUser?.User != null)
{
    <p>You are logged in as @SocialUser.User.Username</p>
    <SocialLogout />
}
else
{
    <p>Please <a href="/login">login</a></p>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
}
```

## 🔐 PKCE Support (Microsoft OAuth)

### Automatic PKCE Implementation

The library **automatically** implements PKCE for Microsoft OAuth:

1. **Code Verifier Generation**: When user clicks Microsoft login button
   ```csharp
   var codeVerifier = PkceGenerator.GenerateCodeVerifier();  // 43-128 chars random string
   var codeChallenge = PkceGenerator.GenerateCodeChallenge(codeVerifier);  // SHA256 hash
   ```

2. **Local Storage**: Stores `code_verifier` for callback retrieval
   ```csharp
   await LocalStorage.SetItemAsync("microsoft_code_verifier", codeVerifier);
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
   ```csharp
   POST /api/Authentication/Social/Token?provider=Microsoft&code={code}
   Body: { "code_verifier": "original-verifier" }
   ```

5. **Cleanup**: Removes verifier from localStorage after use

### Manual PKCE Usage

For custom implementations:

```csharp
@inject SocialLoginLocalStorageService LocalStorage

private async Task CustomLoginAsync()
{
    // Generate PKCE values
    var codeVerifier = PkceGenerator.GenerateCodeVerifier();
    var codeChallenge = PkceGenerator.GenerateCodeChallenge(codeVerifier);
    
    // Store for later retrieval
    await LocalStorage.SetItemAsync("custom_code_verifier", codeVerifier);
    
    // Build OAuth URL with code_challenge
    var authUrl = $"https://oauth.provider.com/authorize?code_challenge={codeChallenge}&code_challenge_method=S256";
    NavigationManager.NavigateTo(authUrl);
}
```

## 🎨 UI Components

### SocialLogin

Renders all configured provider buttons:

```razor
<SocialLogin />
```

### Individual Provider Buttons

```razor
<MicrosoftButton />
<GoogleButton />
<FacebookButton />
<GitHubButton />
```

### Custom Button Order

```razor
<SocialLogin>
    <MicrosoftButton />
    <GoogleButton />
    <GitHubButton />
</SocialLogin>
```

### SocialLogout

```razor
<SocialLogout />
```

### Custom Logout

```razor
@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }

    private async Task LogoutAsync()
    {
        if (SocialUser != null)
        {
            await SocialUser.LogoutAsync(refreshPage: false);
            NavigationManager.NavigateTo("/login");
        }
    }
}
```

## 🔧 Advanced Configuration

### Platform Support (Web & Mobile - MAUI Hybrid)

The library now supports **platform-specific configuration** for Web, iOS (MAUI), and Android (MAUI):

```csharp
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://yourdomain.com";
    
    // Platform configuration
    x.Platform = new PlatformConfig
    {
        Type = PlatformType.Auto,  // Auto-detect (Web/iOS/Android)
        
        // Platform-specific redirect URIs
        RedirectUri = null,  // null = use NavigationManager.BaseUri for web
        
        // For mobile apps (MAUI), set explicit redirect URI:
        // RedirectUri = "msauth://com.yourapp.bundle/auth",  // iOS
        // RedirectUri = "myapp://oauth/callback",  // Android
        
        RedirectPath = "/account/login",  // Path appended to RedirectUri
        
        LoginMode = LoginMode.Redirect  // Only Redirect supported currently
    };
    
    // OAuth providers
    x.Microsoft.ClientId = "your-client-id";
    x.Google.ClientId = "your-client-id";
});
```

#### MAUI Hybrid Example (iOS & Android)

For **Blazor Hybrid** in **.NET MAUI**, configure deep links:

```csharp
// Program.cs or MauiProgram.cs
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://api.yourdomain.com";
    
    // Detect platform and configure accordingly
    x.Platform = new PlatformConfig
    {
        Type = PlatformType.Auto,  // Auto-detects iOS or Android
        
#if IOS
        RedirectUri = "msauth://com.keyserdsoze.fantasoccer/auth",
#elif ANDROID
        RedirectUri = "fantasoccer://oauth/callback",
#else
        RedirectUri = null,  // Web: use NavigationManager.BaseUri
#endif
        
        RedirectPath = "/account/login",
        LoginMode = LoginMode.Redirect
    };
    
    x.Microsoft.ClientId = builder.Configuration["Microsoft:ClientId"];
});
```

**iOS Configuration** (`Platforms/iOS/Info.plist`):

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

**Android Configuration** (`Platforms/Android/AndroidManifest.xml`):

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

**MAUI App Deep Link Handler** (`MauiProgram.cs`):

```csharp
builder.Services.AddSingleton<IDeepLinkHandler, DeepLinkHandler>();

// DeepLinkHandler.cs
public class DeepLinkHandler : IDeepLinkHandler
{
    private readonly NavigationManager _navigationManager;
    
    public DeepLinkHandler(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }
    
    public void HandleDeepLink(string uri)
    {
        // OAuth callback from mobile OAuth flow
        if (uri.Contains("code=") && uri.Contains("state="))
        {
            // Extract query parameters and navigate to callback page
            var parsedUri = new Uri(uri);
            var code = HttpUtility.ParseQueryString(parsedUri.Query).Get("code");
            var state = HttpUtility.ParseQueryString(parsedUri.Query).Get("state");
            
            _navigationManager.NavigateTo($"/account/login?code={code}&state={state}");
        }
    }
}
```

### Platform Detection Utilities

Use built-in utilities for platform detection:

```csharp
@inject IJSRuntime JSRuntime

@code {
    private PlatformType _currentPlatform;
    
    protected override async Task OnInitializedAsync()
    {
        // Async platform detection
        _currentPlatform = await PlatformDetector.DetectPlatformAsync(JSRuntime);
        
        // Or synchronous (compile-time detection)
        _currentPlatform = PlatformDetector.DetectPlatformSync();
        
        // Check if mobile
        if (PlatformDetector.IsMobilePlatform(_currentPlatform))
        {
            // Configure mobile-specific behavior
        }
        
        // Check if Blazor Hybrid (MAUI)
        if (PlatformDetector.IsBlazorHybrid())
        {
            // MAUI-specific initialization
        }
    }
}
```

### Login Mode (Redirect)

Currently, only **Redirect mode** is supported in Blazor:

```csharp
x.LoginMode = LoginMode.Redirect;  // Default
```

**Note**: Popup mode will be added in a future release. For now, all OAuth flows use redirect navigation.

### Complete MAUI Setup Example

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
        
        // Blazor components
        builder.Services.AddMauiBlazorWebView();
        
        // Detect platform at startup
        var currentPlatform = PlatformDetector.DetectPlatformSync();
        
        // Social authentication with platform detection
        builder.Services.AddSocialLoginUI(x =>
        {
            x.ApiUrl = "https://api.fantasoccer.com";
            
            x.Platform = new PlatformConfig
            {
                Type = currentPlatform,
                
                RedirectUri = currentPlatform switch
                {
                    PlatformType.iOS => "msauth://com.keyserdsoze.fantasoccer/auth",
                    PlatformType.Android => "fantasoccer://oauth/callback",
                    _ => null  // Web: use NavigationManager.BaseUri
                },
                
                RedirectPath = "/account/login",
                LoginMode = LoginMode.Redirect
            };
            
            // Read from configuration
            x.Microsoft.ClientId = builder.Configuration["Microsoft:ClientId"];
            x.Google.ClientId = builder.Configuration["Google:ClientId"];
        });
        
        // Repository with automatic authorization
        builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
        {
            repositoryBuilder.WithApiClient(apiBuilder =>
            {
                apiBuilder.WithHttpClient("https://api.fantasoccer.com")
                          .WithDefaultRetryPolicy();
            });
        });
        
        builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();
        
        return builder.Build();
    }
}
```

## 📱 Mobile OAuth Configuration (MAUI)

### Microsoft Entra ID (Azure AD) for MAUI

1. Go to Azure Portal → App registrations → Your app
2. Under **Authentication**, add **Mobile and desktop applications** platform
3. Add redirect URI:
   - iOS: `msauth://com.yourapp.bundle/auth`
   - Android: `yourapp://oauth/callback`
4. Enable **ID tokens** and **Access tokens**
5. Configure PKCE (library handles automatically)

### Google for MAUI

1. Go to Google Cloud Console → Credentials
2. Create **iOS OAuth client ID**:
   - Bundle ID: `com.yourapp.bundle`
   - Redirect URI: Reverse client ID format
3. Create **Android OAuth client ID**:
   - Package name: `com.yourapp`
   - SHA-1 fingerprint: From your keystore

### Deep Link Best Practices

**iOS Bundle ID Format:**
```
msauth://com.yourcompany.yourapp/auth
```

**Android Package Name Format:**
```
yourapp://oauth/callback
```

**Important**: Deep links must match exactly between:
- OAuth provider configuration
- Platform manifest files (Info.plist, AndroidManifest.xml)
- `PlatformConfig.RedirectUri` in your code

## 🔍 How Platform Configuration Works

### Understanding Redirect URI Resolution

When a user clicks a social login button, the library determines the OAuth redirect URI using this **priority order**:

```csharp
// SocialLoginManager.cs - GetFullRedirectUri() method

// Priority 1: Explicit platform.RedirectUri (highest priority)
if (!string.IsNullOrEmpty(_settings.Platform?.RedirectUri))
{
    redirectUri = _settings.Platform.RedirectUri;
}
// Priority 2: NavigationManager.BaseUri (Blazor default)
else
{
    redirectUri = _navigationManager.BaseUri.TrimEnd('/');
}

// Append path
var path = _settings.Platform?.RedirectPath ?? "/account/login";
return $"{redirectUri}{path}";
```

### Example Flow (Microsoft Login on MAUI iOS)

1. **Setup Configuration** (`MauiProgram.cs`):
```csharp
builder.Services.AddSocialLoginUI(x =>
{
    x.ApiUrl = "https://api.yourdomain.com";
    
    x.Platform = new PlatformConfig
    {
        Type = PlatformType.iOS,
        RedirectUri = "msauth://com.yourapp.bundle/auth",  // Mobile deep link
        RedirectPath = "/account/login"
    };
    
    x.Microsoft.ClientId = "your-client-id";
});
```

2. **User Clicks MicrosoftButton.razor**:
   - Calls `Manager.GetFullRedirectUri()` → Returns: `msauth://com.yourapp.bundle/auth/account/login`
   - Generates PKCE code_verifier and code_challenge
   - Constructs OAuth URL with `Uri.EscapeDataString(redirectUri)`
   - Navigates: `NavigationManager.NavigateTo(oauthUrl)`

3. **OAuth Provider Redirects**:
   - Microsoft redirects: `msauth://com.yourapp.bundle/auth?code=ABC123&state=XYZ`
   - MAUI deep link handler catches URL
   - Navigates: `/account/login?code=ABC123&state=XYZ`

4. **Token Exchange**:
   - `SocialAuthenticationRouter` detects callback
   - Calls API: `POST /api/Authentication/Social/Token?provider=Microsoft&code=ABC123&redirectPath=/account/login`
   - Returns JWT token

### Platform Auto-Detection Logic

```csharp
// Compile-time detection (recommended for MAUI)
public static PlatformType DetectPlatformSync()
{
#if IOS
    return PlatformType.iOS;
#elif ANDROID
    return PlatformType.Android;
#else
    return PlatformType.Web;
#endif
}
```

### Configuration Best Practices

✅ **DO**:
- Use `PlatformType.Auto` for automatic detection
- Set `Platform.RedirectUri` explicitly for MAUI
- Use `#if IOS / #elif ANDROID` compiler directives
- Register redirect URIs in OAuth provider consoles
- Configure Info.plist and AndroidManifest.xml

❌ **DON'T**:
- Use web URIs (`https://`) for mobile apps
- Skip deep link manifest configuration
- Use different `RedirectPath` across platforms

## 🆚 Web vs Mobile Comparison

| Feature | Blazor Server/WASM | Blazor Hybrid (MAUI) |
|---------|-------------------|----------------------|
| **Platform** | Web browsers | iOS + Android |
| **Redirect URI** | `https://yourdomain.com` | Deep link (msauth://, myapp://) |
| **Login Flow** | OAuth redirect | OAuth with deep link callback |
| **Token Storage** | localStorage (JSInterop) | Secure Storage (MAUI) |
| **PKCE** | ✅ Required | ✅ Required |
| **Popup Mode** | ⏳ Coming soon | ❌ Not applicable |

## Custom Social User Model

```csharp
public class CustomSocialUser : DefaultSocialUser
{
    public string DisplayName { get; set; }
    public Guid UserId { get; set; }
    public string Avatar { get; set; }
    public List<string> Roles { get; set; }
}
```

Access in components:

```razor
@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper<CustomSocialUser>? SocialUser { get; set; }
    
    private void ShowUserInfo()
    {
        var displayName = SocialUser?.User?.DisplayName;
        var userId = SocialUser?.User?.UserId;
        var roles = SocialUser?.User?.Roles;
    }
}
```

### Manual Token Management

```csharp
@inject SocialLoginManager LoginManager
@inject SocialLoginLocalStorageService LocalStorage

private async Task<string?> GetAccessTokenAsync()
{
    var token = await LocalStorage.GetTokenAsync();
    return token?.AccessToken;
}

private async Task RefreshTokenAsync()
{
    var token = await LoginManager.FetchTokenAsync();
    // Token automatically refreshed if expired
}
```

### Protected API Calls

With `AddDefaultAuthorizationInterceptorForApiHttpClient()`, all API calls automatically include Bearer token:

```csharp
@inject IRepository<Order, Guid> OrderRepository

private async Task<List<Order>> GetOrdersAsync()
{
    // Authorization header automatically added
    return await OrderRepository.Query()
        .Where(x => x.UserId == currentUserId)
        .ToListAsync();
}
```

## 🌐 OAuth Provider Configuration

### Microsoft Entra ID (Azure AD)

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Create new registration
3. Set **Redirect URI**: `https://yourdomain.com/account/login` (Web platform)
4. Under **Authentication**:
   - Enable "ID tokens"
   - Enable "Access tokens"
   - Set "Supported account types" to "Personal Microsoft accounts only"
5. Copy **Application (client) ID**
6. **Important**: PKCE is automatically handled by the library

### Google

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create OAuth 2.0 Client ID (Web application)
3. Add **Authorized redirect URI**: `https://yourdomain.com/account/login`
4. Copy **Client ID**

### Facebook

1. Go to [Facebook Developers](https://developers.facebook.com)
2. Create App → Add Facebook Login
3. Set **Valid OAuth Redirect URI**: `https://yourdomain.com/account/login`
4. Copy **App ID**

## 📝 appsettings.json Example

```json
{
  "SocialLogin": {
    "Microsoft": {
      "ClientId": "0b90db07-be9f-4b29-b673-9e8ee9265927"
    },
    "Google": {
      "ClientId": "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com"
    },
    "Facebook": {
      "ClientId": "345885718092912"
    }
  }
}
```

⚠️ **Note**: Only `ClientId` is needed on client-side. `ClientSecret` should **only** be configured on the API server.

## 🔗 Related Packages

- **API Server**: `Rystem.Authentication.Social` - Backend OAuth token validation with PKCE
- **React Client**: `rystem.authentication.social.react` - React components with TypeScript
- **Abstractions**: `Rystem.Authentication.Social.Abstractions` - Shared models

## 📚 More Information

- **Complete Docs**: [https://rystem.net/mcp/tools/auth-social-blazor.md](https://rystem.net/mcp/tools/auth-social-blazor.md)
- **OAuth Flow Diagram**: [https://rystem.net/mcp/prompts/auth-flow.md](https://rystem.net/mcp/prompts/auth-flow.md)
- **PKCE RFC**: [RFC 7636](https://tools.ietf.org/html/rfc7636)