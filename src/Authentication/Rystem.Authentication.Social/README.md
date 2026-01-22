### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 🔐 Rystem.Authentication.Social - Server-Side OAuth 2.0 Integration

Complete **OAuth 2.0 server-side** integration for social login providers (Google, Microsoft, Facebook, GitHub, Amazon, LinkedIn, X, TikTok, Pinterest, Instagram) with automatic token management and user provisioning.

### 🎯 What This Package Does

✅ **OAuth 2.0 Token Exchange** - Converts authorization codes to access tokens  
✅ **Multi-Provider Support** - 10+ social providers built-in  
✅ **Automatic Token Refresh** - Keep tokens valid without user intervention  
✅ **User Provisioning** - Auto-create users from social provider data  
✅ **Claims-Based Authorization** - Role and permission mapping  
✅ **REST API Endpoints** - Ready-to-use `/login` and `/token` endpoints  

### ⚠️ Prerequisites

Before using this package, ensure you have:
1. **App Registrations** created for each provider you want to support
2. **Client IDs and Client Secrets** from each provider
3. **Redirect URLs** configured in your app registrations
4. **.NET 10** or later

---

## OAuth 2.0 Flow Overview

```
┌─────────────┐              ┌──────────────┐              ┌────────────┐
│   Browser   │              │  Your Server │              │  Provider  │
│   (React)   │              │   (Rystem)   │              │ (Google)   │
└──────┬──────┘              └──────┬───────┘              └──────┬─────┘
       │                             │                             │
       │ 1. User clicks "Login"      │                             │
       ├────────────────────────────>│                             │
       │                             │                             │
       │ 2. Redirect to Provider     │                             │
       │<────────────────────────────┤                             │
       │                             │                             │
       │ 3. Provider Login           │                             │
       ├─────────────────────────────────────────────────────────>│
       │                             │                             │
       │                             │ 4. Authorization Code       │
       │<─────────────────────────────────────────────────────────┤
       │                             │                             │
       │ 5. Send Code to Server      │                             │
       ├────────────────────────────>│                             │
       │                             │                             │
       │                             │ 6. Code + Secret → Token   │
       │                             ├────────────────────────────>
       │                             │                             │
       │                             │ 7. Access Token            │
       │                             │<────────────────────────────
       │                             │                             │
       │ 8. JWT Token (Server)       │                             │
       │<────────────────────────────┤                             │
       │                             │                             │
```

**Key Points:**
- Only the **authorization code** is sent from client to server (never the secret)
- Server exchanges code + secret for access token with provider
- Server issues its own JWT to client for session management
- Client uses JWT for subsequent API calls

---

## 🔧 Step 1: Create App Registrations

Each provider requires an app registration with specific configurations:

### Microsoft / Azure AD

1. Go to **[Azure Portal](https://portal.azure.com)** → Azure Active Directory
2. **App registrations** → **New registration**
3. **Name**: Your App Name
4. **Supported account types**: Accounts in this organizational directory only
5. **Redirect URI**: Select "Web" and enter: `https://yourdomain.com/auth/microsoft/callback`
6. **Certificate & secrets** → **New client secret**
7. Copy **Application (client) ID** and **Client Secret Value**

**Configuration:**
```csharp
x.Microsoft.ClientId = "your-client-id";
x.Microsoft.ClientSecret = "your-client-secret";
x.Microsoft.RedirectDomain = "https://yourdomain.com";
```

### Google

1. Go to **[Google Cloud Console](https://console.cloud.google.com)**
2. Create new project or select existing
3. **APIs & Services** → **Credentials**
4. **Create credentials** → **OAuth client ID**
5. **Application type**: Web application
6. **Authorized redirect URIs**: Add `https://yourdomain.com/auth/google/callback`
7. Copy **Client ID** and **Client Secret**

**Configuration:**
```csharp
x.Google.ClientId = "your-client-id.apps.googleusercontent.com";
x.Google.ClientSecret = "your-client-secret";
x.Google.RedirectDomain = "https://yourdomain.com";
```

### Facebook

1. Go to **[Facebook Developers](https://developers.facebook.com)**
2. **My Apps** → **Create App**
3. **App Type**: Consumer
4. **Settings** → **Basic** → Copy **App ID** and **App Secret**
5. **Settings** → **Basic** → Add **App Domains**: `yourdomain.com`
6. **Products** → **Facebook Login** → **Settings**
7. **Valid OAuth Redirect URIs**: `https://yourdomain.com/auth/facebook/callback`

**Configuration:**
```csharp
x.Facebook.ClientId = "your-app-id";
x.Facebook.ClientSecret = "your-app-secret";
x.Facebook.RedirectDomain = "https://yourdomain.com";
```

### GitHub

1. Go to **[GitHub Settings](https://github.com/settings/developers)**
2. **OAuth Apps** → **New OAuth App**
3. **Authorization callback URL**: `https://yourdomain.com/auth/github/callback`
4. Copy **Client ID** and generate **Client Secret**

**Configuration:**
```csharp
x.GitHub.ClientId = "your-client-id";
x.GitHub.ClientSecret = "your-client-secret";
x.GitHub.RedirectDomain = "https://yourdomain.com";
```

### Amazon

1. Go to **[Amazon Developer Console](https://developer.amazon.com)**
2. **Login with Amazon** → Create Security Profile
3. **General** → Copy **Client ID** and **Client Secret**
4. **Web Settings** → Add `https://yourdomain.com/auth/amazon/callback`

**Configuration:**
```csharp
x.Amazon.ClientId = "your-client-id";
x.Amazon.ClientSecret = "your-client-secret";
x.Amazon.RedirectDomain = "https://yourdomain.com";
```

---

## 📦 Step 2: Server Configuration

### Basic Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Social Login services
builder.Services.AddSocialLogin(
    // Step 1: Configure providers
    socialSettings =>
    {
        socialSettings.Google.ClientId = configuration["SocialLogin:Google:ClientId"];
        socialSettings.Google.ClientSecret = configuration["SocialLogin:Google:ClientSecret"];
        socialSettings.Google.RedirectDomain = configuration["SocialLogin:Google:RedirectDomain"];
        
        socialSettings.Microsoft.ClientId = configuration["SocialLogin:Microsoft:ClientId"];
        socialSettings.Microsoft.ClientSecret = configuration["SocialLogin:Microsoft:ClientSecret"];
        socialSettings.Microsoft.RedirectDomain = configuration["SocialLogin:Microsoft:RedirectDomain"];
        
        socialSettings.Facebook.ClientId = configuration["SocialLogin:Facebook:ClientId"];
        socialSettings.Facebook.ClientSecret = configuration["SocialLogin:Facebook:ClientSecret"];
        socialSettings.Facebook.RedirectDomain = configuration["SocialLogin:Facebook:RedirectDomain"];
        
        // Add more providers as needed...
    },
    // Step 2: Configure token settings
    tokenSettings =>
    {
        tokenSettings.BearerTokenExpiration = TimeSpan.FromHours(1);
        tokenSettings.RefreshTokenExpiration = TimeSpan.FromDays(7);
    }
);

// Step 3: Add user provider (custom implementation)
builder.Services.AddSocialUserProvider<YourSocialUserProvider>();

var app = builder.Build();

// Step 4: Map OAuth endpoints
app.UseSocialLoginEndpoints();

app.Run();
```

### appsettings.json Configuration

```json
{
  "SocialLogin": {
    "Google": {
      "ClientId": "xxx.apps.googleusercontent.com",
      "ClientSecret": "your-secret",
      "RedirectDomain": "https://yourdomain.com"
    },
    "Microsoft": {
      "ClientId": "your-client-id",
      "ClientSecret": "your-secret",
      "RedirectDomain": "https://yourdomain.com"
    },
    "Facebook": {
      "ClientId": "your-app-id",
      "ClientSecret": "your-secret",
      "RedirectDomain": "https://yourdomain.com"
    }
  }
}
```

---

## 👤 Step 3: Custom SocialUser Class

By default, Rystem provides a minimal `SocialUser` with just `Username`. For most apps, you need to extend it:

### Understanding ISocialUser

```csharp
// This is the base interface (Abstractions)
public interface ISocialUser
{
    string? Username { get; set; }
}

// Optional: For localized applications
public interface ILocalizedSocialUser : ISocialUser
{
    string? Language { get; set; }
}
```

### Create Your Custom SocialUser

```csharp
// In your API project Models folder
using Rystem.Authentication.Social;

public sealed class MyAppSocialUser : ISocialUser, ILocalizedSocialUser
{
    public string? Username { get; set; }
    public string? Email { get; set; }                    // Add email
    public string? ProfilePictureUrl { get; set; }        // Add profile pic
    public string? Language { get; set; }                 // Localization
    public DateTime? CreatedAt { get; set; }
    public List<string>? Roles { get; set; }              // User roles
}
```

### Register Custom SocialUser

```csharp
// This tells Rystem which SocialUser implementation to use
builder.Services.AddSocialLogin<MyAppSocialUser>(socialSettings => 
{
    // ... provider configuration
});
```

---

## 🔌 Step 4: ISocialUserProvider - Custom User Logic

This interface handles **user provisioning** - creating/updating users from social provider data:

```csharp
public sealed class DatabaseSocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<AppUser, string> _userRepository;
    
    public DatabaseSocialUserProvider(IRepository<AppUser, string> userRepository)
    {
        _userRepository = userRepository;
    }
    
    // Called when user logs in via social provider
    public async Task<SocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        // 1. Try to find existing user
        var existingUser = await _userRepository.QueryAsync(
            x => x.Username == username,
            cancellationToken: cancellationToken
        );
        
        var user = existingUser.FirstOrDefault();
        
        // 2. If new user, create it
        if (user == null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                ProfileUrl = claims.FirstOrDefault(x => x.Type == "picture")?.Value,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            await _userRepository.InsertAsync(user, cancellationToken);
        }
        
        // 3. Return user data
        return new MyAppSocialUser
        {
            Username = user.Username,
            Email = user.Email,
            ProfilePictureUrl = user.ProfileUrl,
            Language = user.Language
        };
    }
    
    // Populate claims from user data (roles, permissions, etc)
    public async IAsyncEnumerable<Claim> GetClaimsAsync(
        string? username, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByKeyAsync(username, cancellationToken);
        
        if (user != null)
        {
            // Add identity claims
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id);
            yield return new Claim(ClaimTypes.Name, user.Username);
            yield return new Claim(ClaimTypes.Email, user.Email ?? "");
            
            // Add role claims
            if (user.Roles != null)
            {
                foreach (var role in user.Roles)
                {
                    yield return new Claim(ClaimTypes.Role, role);
                }
            }
            
            // Add custom claims
            yield return new Claim("email_verified", user.EmailVerified.ToString());
            yield return new Claim("profile_url", user.ProfileUrl ?? "");
        }
    }
}

// Register it
builder.Services.AddSocialUserProvider<DatabaseSocialUserProvider>();
```

---

## 🔑 REST API Endpoints (Auto-Generated)

Once configured, these endpoints are automatically available:

### Login Endpoint

```http
POST /auth/{provider}/login
Content-Type: application/json

{
    "code": "authorization-code-from-provider",
    "state": "random-state-value"
}
```

**Response:**
```json
{
    "bearerToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh-token-value",
    "expiresIn": 3600,
    "user": {
        "username": "user@example.com",
        "email": "user@example.com",
        "language": "en"
    }
}
```

### Token Refresh Endpoint

```http
POST /auth/refresh
Content-Type: application/json

{
    "refreshToken": "refresh-token-value"
}
```

**Response:**
```json
{
    "bearerToken": "new-jwt-token",
    "expiresIn": 3600
}
```

---

## Complete Server Example

```csharp
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// 1. Add Database (if using user provisioning)
builder.Services.AddRepository<AppUser, string>(repoBuilder =>
{
    repoBuilder.WithInMemory();  // or your actual database
});

// 2. Configure Social Login
builder.Services.AddSocialLogin(
    socialSettings =>
    {
        // Google
        socialSettings.Google.ClientId = config["SocialLogin:Google:ClientId"]!;
        socialSettings.Google.ClientSecret = config["SocialLogin:Google:ClientSecret"]!;
        socialSettings.Google.RedirectDomain = config["SocialLogin:Google:RedirectDomain"]!;
        
        // Microsoft
        socialSettings.Microsoft.ClientId = config["SocialLogin:Microsoft:ClientId"]!;
        socialSettings.Microsoft.ClientSecret = config["SocialLogin:Microsoft:ClientSecret"]!;
        socialSettings.Microsoft.RedirectDomain = config["SocialLogin:Microsoft:RedirectDomain"]!;
        
        // Facebook
        socialSettings.Facebook.ClientId = config["SocialLogin:Facebook:ClientId"]!;
        socialSettings.Facebook.ClientSecret = config["SocialLogin:Facebook:ClientSecret"]!;
        socialSettings.Facebook.RedirectDomain = config["SocialLogin:Facebook:RedirectDomain"]!;
    },
    tokenSettings =>
    {
        tokenSettings.BearerTokenExpiration = TimeSpan.FromHours(1);
        tokenSettings.RefreshTokenExpiration = TimeSpan.FromDays(7);
    }
);

// 3. Custom user provider
builder.Services.AddSocialUserProvider<DatabaseSocialUserProvider>();

// 4. Add Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Map OAuth endpoints
app.UseSocialLoginEndpoints();

// 6. Custom endpoint example
app.MapGet("/api/me", (HttpContext context) =>
{
    var username = context.User.FindFirst(ClaimTypes.Name)?.Value;
    return Results.Ok(new { message = $"Hello, {username}!" });
})
.RequireAuthorization();

app.Run();
```

---

## 📚 Related Packages

- **Rystem.Authentication.Social.Blazor** - Blazor UI components for login
- **rystem.authentication.social.react** - React hooks for login
- **Rystem.Authentication.Social.Abstractions** - Interfaces and models

---

## 💡 Best Practices

✅ **Always use HTTPS** in production  
✅ **Never expose Client Secret** to frontend  
✅ **Validate State parameter** to prevent CSRF  
✅ **Implement Token Refresh** before expiration  
✅ **Store Refresh Tokens securely** (HttpOnly cookies or secure storage)  
✅ **Map all user claims** for authorization  
✅ **Monitor token expiration** and refresh proactively  

---

## References

- [OAuth 2.0 Official Spec](https://tools.ietf.org/html/rfc6749)
- [Rystem.Authentication.Social.Blazor](../Rystem.Authentication.Social.Blazor/README.md) - Blazor UI
- [rystem.authentication.social.react](../rystem.authentication.social.react/src/rystem.authentication.social.react/README.md) - React UI
- [Rystem.Authentication.Social.Abstractions](../Rystem.Authentication.Social.Abstractions/README.md) - Interfaces