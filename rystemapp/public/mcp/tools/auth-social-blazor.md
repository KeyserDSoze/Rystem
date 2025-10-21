# Social Authentication - Blazor Client

Add **social login UI** to your Blazor Server or Blazor WebAssembly application.

**Features:**
- Pre-built social login buttons (Google, Microsoft, Facebook, GitHub, etc.)
- Automatic JWT token management
- Protected routing with authentication
- Cascading user context
- Auto-refresh tokens

---

## Installation

```bash
dotnet add package Rystem.Authentication.Social.Blazor --version 9.1.3
```

---

## Setup JavaScript

Add the JavaScript library in `App.razor` (in the `<head>` or before `</body>`):

```razor
<script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
```

---

## Configuration

### Program.cs (Blazor Server)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Social Login UI
builder.Services.AddSocialLoginUI(options =>
{
    options.ApiUrl = "https://localhost:7017"; // Your API URL
    options.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    options.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    options.Facebook.ClientId = builder.Configuration["SocialLogin:Facebook:ClientId"];
    options.GitHub.ClientId = builder.Configuration["SocialLogin:GitHub:ClientId"];
});

// Optional: Repository with authorization interceptor
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithApiClient(apiBuilder =>
    {
        apiBuilder
            .WithHttpClient("https://localhost:7017")
            .WithDefaultRetryPolicy();
    });
});

builder.Services.AddDefaultAuthorizationInterceptorForApiHttpClient();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app
    .MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### appsettings.json

```json
{
  "SocialLogin": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
    },
    "Microsoft": {
      "ClientId": "YOUR_MICROSOFT_CLIENT_ID"
    },
    "Facebook": {
      "ClientId": "YOUR_FACEBOOK_APP_ID"
    },
    "GitHub": {
      "ClientId": "YOUR_GITHUB_CLIENT_ID"
    }
  }
}
```

---

## Router Setup

Replace the default router in `Routes.razor` with `SocialAuthenticationRouter`:

```razor
<SocialAuthenticationRouter 
    AppAssembly="typeof(Program).Assembly" 
    DefaultLayout="typeof(Layout.MainLayout)">
</SocialAuthenticationRouter>
```

**Features:**
- Automatically redirects unauthenticated users to login page
- Cascades `SocialUserWrapper` to all components
- Manages token refresh automatically

---

## Login Page

Create a login page with pre-built social buttons:

```razor
@page "/login"
@using Rystem.Authentication.Social.Blazor

<h3>Login</h3>

<SocialLoginButtons></SocialLoginButtons>
```

### Custom Button Order

```razor
@page "/login"
@using Rystem.Authentication.Social.Blazor

<h3>Login with Social</h3>

<SocialLoginButtons Buttons="@customOrder"></SocialLoginButtons>

@code {
    private Type[] customOrder = new[]
    {
        typeof(GoogleButton),
        typeof(MicrosoftButton),
        typeof(FacebookButton),
        typeof(GitHubButton),
        typeof(LinkedinButton),
        typeof(XButton),
        typeof(InstagramButton),
        typeof(PinterestButton)
    };
}
```

---

## Logout

### Pre-built Logout Button

```razor
<SocialLogout></SocialLogout>
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
            await SocialUser.LogoutAsync(forceRefresh: false);
        }
    }
}

<button @onclick="LogoutAsync">Logout</button>
```

---

## Access User Information

Use the cascading `SocialUserWrapper` parameter:

```razor
@page "/profile"

<h3>Profile</h3>

@if (SocialUser?.IsAuthenticated == true)
{
    <p>Username: @SocialUser.Username</p>
    <p>Email: @SocialUser.Email</p>
    <p>Token Expires: @SocialUser.ExpiresAt</p>
}
else
{
    <p>Not authenticated</p>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
}
```

---

## Protected Routes

Use `@attribute [Authorize]` to protect pages:

```razor
@page "/dashboard"
@attribute [Authorize]

<h3>Dashboard</h3>

<p>Welcome, @SocialUser?.Username!</p>

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
}
```

---

## Complete Example

### App.razor

```razor
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="app.css" />
    <HeadOutlet />
    <script src="_content/Rystem.Authentication.Social.Blazor/socialauthentications.js"></script>
</head>
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

### Routes.razor

```razor
<SocialAuthenticationRouter 
    AppAssembly="typeof(Program).Assembly" 
    DefaultLayout="typeof(Layout.MainLayout)">
</SocialAuthenticationRouter>
```

### MainLayout.razor

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>
    
    <main>
        <div class="top-row px-4">
            @if (SocialUser?.IsAuthenticated == true)
            {
                <span>Hello, @SocialUser.Username</span>
                <SocialLogout></SocialLogout>
            }
        </div>
        
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
}
```

### Login.razor

```razor
@page "/login"
@using Rystem.Authentication.Social.Blazor

<div class="login-container">
    <h3>Sign In</h3>
    <p>Choose a social provider to continue</p>
    
    <SocialLoginButtons></SocialLoginButtons>
</div>

<style>
    .login-container {
        max-width: 400px;
        margin: 100px auto;
        padding: 20px;
        text-align: center;
    }
</style>
```

### Dashboard.razor

```razor
@page "/dashboard"
@attribute [Authorize]
@inject IRepository<User, Guid> UserRepository

<h3>Dashboard</h3>

@if (SocialUser?.IsAuthenticated == true)
{
    <div class="user-info">
        <h4>Welcome, @SocialUser.Username!</h4>
        <p>Email: @SocialUser.Email</p>
        <p>Token expires: @SocialUser.ExpiresAt.ToString("g")</p>
    </div>
    
    @if (users != null)
    {
        <h5>Users (@users.Count)</h5>
        <ul>
            @foreach (var user in users)
            {
                <li>@user.Email</li>
            }
        </ul>
    }
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private List<User>? users;
    
    protected override async Task OnInitializedAsync()
    {
        // Fetch users from API (with automatic auth token)
        users = await UserRepository.Query().ToListAsync();
    }
}
```

---

## Real-World Examples

### Admin Panel with Role Check

```razor
@page "/admin"
@attribute [Authorize(Roles = "Admin")]

<h3>Admin Panel</h3>

@if (SocialUser?.HasRole("Admin") == true)
{
    <p>You have admin access</p>
    
    <button @onclick="DeleteAllUsers">Delete All Users</button>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private async Task DeleteAllUsers()
    {
        // Admin action
    }
}
```

### Token Refresh on API Call

```razor
@inject IRepository<Order, Guid> OrderRepository

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private async Task LoadOrdersAsync()
    {
        try
        {
            // Automatic token refresh if expired
            var orders = await OrderRepository.Query().ToListAsync();
        }
        catch (UnauthorizedAccessException)
        {
            // Token expired and refresh failed
            await SocialUser!.LogoutAsync(false);
        }
    }
}
```

### Multi-Tenant Dashboard

```razor
@page "/tenant-dashboard"
@attribute [Authorize]
@inject IRepository<Tenant, Guid> TenantRepository

<h3>Tenant Dashboard</h3>

@if (tenant != null)
{
    <h4>@tenant.Name</h4>
    <p>Domain: @tenant.EmailDomain</p>
    <p>Users: @tenant.UserCount</p>
}

@code {
    [CascadingParameter(Name = "SocialUser")]
    public SocialUserWrapper? SocialUser { get; set; }
    
    private Tenant? tenant;
    
    protected override async Task OnInitializedAsync()
    {
        var tenantId = SocialUser?.GetClaim("TenantId");
        
        if (Guid.TryParse(tenantId, out var id))
        {
            tenant = await TenantRepository.GetAsync(id);
        }
    }
}
```

---

## SocialUserWrapper API

```csharp
public class SocialUserWrapper
{
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsAuthenticated { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string AccessToken { get; set; }
    
    public bool HasRole(string role);
    public string? GetClaim(string claimType);
    public Task LogoutAsync(bool forceRefresh);
    public Task RefreshTokenAsync();
}
```

---

## Available Social Buttons

- `GoogleButton`
- `MicrosoftButton`
- `FacebookButton`
- `GitHubButton`
- `LinkedinButton`
- `XButton` (Twitter)
- `InstagramButton`
- `PinterestButton`
- `TikTokButton`
- `AmazonButton`

---

## Benefits

- ✅ **Pre-built UI Components**: Social buttons, logout, router
- ✅ **Automatic Token Management**: Refresh tokens automatically
- ✅ **Protected Routing**: `[Authorize]` attribute support
- ✅ **Cascading User Context**: Access user from any component
- ✅ **Repository Integration**: Auto-inject auth headers

---

## Related Tools

- **[Social Authentication - Server Setup](https://rystem.net/mcp/tools/auth-social-server.md)** - API configuration
- **[Social Authentication - TypeScript Client](https://rystem.net/mcp/tools/auth-social-typescript.md)** - React/Vue integration
- **[Repository API Client - .NET](https://rystem.net/mcp/tools/repository-api-client-dotnet.md)** - Repository with auth

---

## References

- **NuGet Package**: [Rystem.Authentication.Social.Blazor](https://www.nuget.org/packages/Rystem.Authentication.Social.Blazor) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
