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

### Custom Social User Model

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