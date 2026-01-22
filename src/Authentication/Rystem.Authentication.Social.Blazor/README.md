### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 🎨 Rystem.Authentication.Social.Blazor

Complete **Blazor UI components** for social login - ready-to-use buttons and authentication flow for Blazor Server and WebAssembly applications.

### 📦 What's Included

✅ **Pre-built Login Buttons** - Google, Microsoft, Facebook, GitHub, Amazon, LinkedIn, X, TikTok, Pinterest, Instagram  
✅ **OAuth Flow Handler** - Automatic authorization code exchange  
✅ **Cascading Parameters** - `SocialUser` and `SocialToken` for authenticated pages  
✅ **Logout Component** - One-line logout button  
✅ **Token Management** - Auto-refresh before expiration  
✅ **Error Handling** - Built-in error display and recovery  

---

## 🔧 Step 1: Installation

Add JavaScript to your `App.razor` or `Layout.razor` component:

```html
<!-- In head or end of body in App.razor -->
<script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
```

---

## 🚀 Step 2: Service Configuration

```csharp
// Program.cs

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add Razor components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Social Login UI
services.AddSocialLoginUI(x =>
{
    // API server URL (where OAuth endpoints are hosted)
    x.ApiUrl = "https://localhost:7017";
    
    // Provider Client IDs (from app registrations)
    x.Google.ClientId = configuration["SocialLogin:Google:ClientId"];
    x.Microsoft.ClientId = configuration["SocialLogin:Microsoft:ClientId"];
    x.Facebook.ClientId = configuration["SocialLogin:Facebook:ClientId"];
    x.GitHub.ClientId = configuration["SocialLogin:GitHub:ClientId"];
    x.Amazon.ClientId = configuration["SocialLogin:Amazon:ClientId"];
    x.LinkedIn.ClientId = configuration["SocialLogin:LinkedIn:ClientId"];
    x.X.ClientId = configuration["SocialLogin:X:ClientId"];
    x.TikTok.ClientId = configuration["SocialLogin:TikTok:ClientId"];
    x.Pinterest.ClientId = configuration["SocialLogin:Pinterest:ClientId"];
    x.Instagram.ClientId = configuration["SocialLogin:Instagram:ClientId"];
    
    // Optional: Automatic token refresh
    x.AutomaticRefresh = true;
});

// Optional: Add Repository API Client if using repositories
services.AddRepository<YourEntity, TKey>(builder =>
{
    builder.WithApiClient(apiBuilder =>
    {
        apiBuilder.WithHttpClient(configuration["ApiUrl"]);
    });
});

// Add default authorization interceptor for API calls
services.AddDefaultAuthorizationInterceptorForApiHttpClient();

await builder.Build().RunAsync();
```

### Configuration Breakdown

**ApiUrl**: Your .NET server where OAuth endpoints are hosted

**ClientId**: Only the public Client ID (NOT Client Secret - never share that!)

**AutomaticRefresh**: Automatically refresh tokens before they expire

---

## 📄 Step 3: Router Setup

In your `Routes.razor`:

```razor
@* Routes.razor *@
@using System.Reflection
@using Rystem.Authentication.Social.Blazor

<SocialAuthenticationRouter AppAssembly="typeof(Program).Assembly" 
                            DefaultLayout="typeof(Layout.MainLayout)">
</SocialAuthenticationRouter>
```

This component:
- Wraps your regular `<Router>` component
- Handles OAuth callback redirects
- Manages authentication state
- Provides cascading parameters to all pages

---

## 🔐 Step 4: Protect Pages

### Require Authentication

```razor
@page "/dashboard"
@attribute [Authorize]

@using Rystem.Authentication.Social.Blazor

<h1>Dashboard</h1>

@if (SocialUser != null)
{
    <p>Welcome, @SocialUser.User?.Username!</p>
    <p>Email: @SocialUser.User?.Email</p>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
}
```

### Check Authentication in Code

```razor
@page "/profile"
@using Rystem.Authentication.Social.Blazor

@if (IsAuthenticated)
{
    <p>Logged in as: @SocialUser?.User?.Username</p>
}
else
{
    <p>Please log in</p>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private bool IsAuthenticated => SocialUser?.IsAuthenticated ?? false;
}
```

---

## 🔘 Step 5: Display Login Buttons

### Minimal Button Bar

```razor
@page "/"

@using Rystem.Authentication.Social.Blazor

<div class="login-container">
    <h2>Sign In</h2>
    <SocialLoginButtons />
</div>
```

This displays all 10 providers with default styling.

### Custom Button Ordering

```razor
@page "/"

@using Rystem.Authentication.Social.Blazor

<div class="login-container">
    <h2>Sign In With</h2>
    
    <SocialLoginButtons 
        Providers="@(new[] { 
            typeof(MicrosoftButton), 
            typeof(GoogleButton), 
            typeof(FacebookButton) 
        })" />
</div>

@code {
    // Control which providers and in what order they appear
}
```

### Individual Buttons

```razor
@page "/"

@using Rystem.Authentication.Social.Blazor

<div class="button-group">
    <GoogleButton />
    <MicrosoftButton />
    <FacebookButton />
    <GitHubButton />
</div>
```

---

## 🚪 Step 6: Logout

### Simple Logout Button

```razor
@page "/account"

@using Rystem.Authentication.Social.Blazor

<h1>Account Settings</h1>

<SocialLogout />
```

### Custom Logout Button

```razor
@page "/account"

@using Rystem.Authentication.Social.Blazor

<button class="btn btn-danger" @onclick="LogoutAsync">
    Sign Out
</button>

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private async Task LogoutAsync()
    {
        if (SocialUser != null)
        {
            // redirectToHome: false = stay on current page
            await SocialUser.LogoutAsync(redirectToHome: true);
        }
    }
}
```

---

## 🔑 Step 7: Use Token for API Calls

### Access Current Token

```razor
@page "/api-example"

@using Rystem.Authentication.Social.Blazor
@inject HttpClient Http

<h1>API Example</h1>

<button @onclick="GetUserDataAsync">Fetch User Data</button>

@if (userData != null)
{
    <p>Data: @userData</p>
}

@code {
    private string? userData;
    
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private async Task GetUserDataAsync()
    {
        if (SocialUser?.Token?.IsExpired == true)
        {
            // Token expired - refresh
            await SocialUser.RefreshTokenAsync();
        }
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/my-data");
        
        // Add token to Authorization header
        request.Headers.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", 
                SocialUser?.Token?.BearerToken);
        
        var response = await Http.SendAsync(request);
        userData = await response.Content.ReadAsStringAsync();
    }
}
```

### Check Token Expiration

```csharp
// SocialUserWrapper.Token properties:
SocialUser.Token.IsExpired          // bool - is token expired?
SocialUser.Token.ExpiresAt          // DateTime - expiration time
SocialUser.Token.BearerToken        // string - JWT token
SocialUser.Token.RefreshToken       // string - refresh token
```

---

## 📊 Complete Example

```razor
@* Pages/Login.razor *@
@page "/login"

@using Rystem.Authentication.Social.Blazor

@if (!IsAuthenticated)
{
    <div class="login-container">
        <h1>Welcome to MyApp</h1>
        <p>Sign in with your social account:</p>
        
        <div class="button-group">
            <SocialLoginButtons />
        </div>
    </div>
}
else
{
    <div class="welcome-container">
        <h1>Welcome, @SocialUser?.User?.Username!</h1>
        
        @if (!string.IsNullOrEmpty(SocialUser?.User?.Email))
        {
            <p>Email: @SocialUser.User.Email</p>
        }
        
        <img src="@SocialUser?.User?.ProfilePictureUrl" alt="Profile" />
        
        <button class="btn btn-primary" @onclick="GoToDashboard">
            Go to Dashboard
        </button>
        
        <SocialLogout />
    </div>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    [Inject]
    public NavigationManager Navigation { get; set; } = null!;
    
    private bool IsAuthenticated => SocialUser?.IsAuthenticated ?? false;
    
    private void GoToDashboard() => Navigation.NavigateTo("/dashboard");
}
```

```razor
@* Pages/Dashboard.razor *@
@page "/dashboard"
@attribute [Authorize]

@using Rystem.Authentication.Social.Blazor

<h1>Dashboard</h1>

<div class="user-profile">
    <img src="@SocialUser?.User?.ProfilePictureUrl" alt="Profile" class="avatar" />
    <h2>@SocialUser?.User?.Username</h2>
    <p>@SocialUser?.User?.Email</p>
</div>

<div class="token-info">
    <h3>Token Status</h3>
    <p>Expires: @SocialUser?.Token?.ExpiresAt?.ToString("g")</p>
    <p>Is Expired: @(SocialUser?.Token?.IsExpired == true ? "Yes" : "No")</p>
</div>

<button class="btn btn-danger" @onclick="LogoutAsync">
    Sign Out
</button>

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    [Inject]
    public NavigationManager Navigation { get; set; } = null!;
    
    private async Task LogoutAsync()
    {
        if (SocialUser != null)
        {
            await SocialUser.LogoutAsync(redirectToHome: true);
        }
    }
}
```

---

## 🎨 CSS Customization

Default styling uses Bootstrap. To customize:

```css
/* Override button styles */
.social-login-button {
    padding: 12px 24px;
    font-size: 16px;
    border-radius: 8px;
}

.social-login-button.google {
    background-color: #4285F4;
}

.social-login-button.microsoft {
    background-color: #0078D4;
}

.social-login-button.facebook {
    background-color: #1877F2;
}

/* Container styling */
.login-container {
    max-width: 500px;
    margin: 0 auto;
    padding: 20px;
    border-radius: 8px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}
```

---

## 🔄 Cascading Parameters

Available in all child components via `[CascadingParameter]`:

```csharp
[CascadingParameter(Name = "SocialUser")]
public SocialUserWrapper? SocialUser { get; set; }

[CascadingParameter(Name = "SocialToken")]
public SocialTokenWrapper? SocialToken { get; set; }
```

**SocialUserWrapper:**
- `IsAuthenticated` - bool
- `User` - ISocialUser (username, email, roles, etc.)
- `LogoutAsync()` - Logout method
- `RefreshTokenAsync()` - Refresh token

**SocialTokenWrapper:**
- `BearerToken` - JWT string
- `RefreshToken` - Refresh token string
- `ExpiresAt` - Expiration DateTime
- `IsExpired` - bool

---

## ⚠️ Common Issues

### Issue: "OAuth redirect failed"
- Check `ApiUrl` matches your server
- Verify server is running and accessible
- Check browser console for CORS errors

### Issue: "Token is null"
- Ensure component is within `<SocialAuthenticationRouter>`
- Verify user is authenticated (`IsAuthenticated == true`)
- Check `AddSocialLoginUI()` was called in Program.cs

### Issue: "Automatic refresh not working"
- Set `x.AutomaticRefresh = true` in configuration
- Ensure refresh token is not expired
- Check server has `/auth/refresh` endpoint implemented

---

## 📚 Related Packages

- **Rystem.Authentication.Social** - Server-side OAuth implementation
- **rystem.authentication.social.react** - React alternative
- **Rystem.Authentication.Social.Abstractions** - Interfaces and models

---

## References

- [Blazor Authentication Docs](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)
- [OAuth 2.0 Flow](https://tools.ietf.org/html/rfc6749)
- [JWT Best Practices](https://tools.ietf.org/html/rfc7519)
- [Server Implementation Guide](../Rystem.Authentication.Social/README.md)