### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Rystem.Authentication.Social

Server-side social authentication library for .NET that provides OAuth 2.0 integration with multiple providers (Microsoft, Google, Facebook, GitHub, Amazon, LinkedIn, X/Twitter, TikTok, Instagram, Pinterest).

### ✨ Key Features

- **🔐 PKCE Support**: Implements RFC 7636 Proof Key for Code Exchange for enhanced security
- **🌐 Multiple Providers**: Support for 10+ OAuth providers
- **🎯 Token Management**: Built-in bearer and refresh token handling
- **🔧 Extensible**: Custom user providers and claim management
- **⚡ Modern APIs**: Minimal API endpoints with automatic OpenAPI documentation

## 📦 Installation

```bash
dotnet add package Rystem.Authentication.Social
```

## 🚀 Quick Start

### 1. Configure Services

```csharp
builder.Services.AddSocialLogin(x =>
{
    // Configure OAuth providers
    x.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
    x.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
    x.Microsoft.RedirectDomain = builder.Configuration["SocialLogin:Microsoft:RedirectDomain"];
    
    x.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
    x.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
    x.Google.RedirectDomain = builder.Configuration["SocialLogin:Google:RedirectDomain"];
    
    x.Facebook.ClientId = builder.Configuration["SocialLogin:Facebook:ClientId"];
    x.Facebook.ClientSecret = builder.Configuration["SocialLogin:Facebook:ClientSecret"];
    x.Facebook.RedirectDomain = builder.Configuration["SocialLogin:Facebook:RedirectDomain"];
    
    // Add other providers as needed (GitHub, Amazon, LinkedIn, X, TikTok, Instagram, Pinterest)
},
x =>
{
    // Configure token expiration
    x.BearerTokenExpiration = TimeSpan.FromHours(1);
    x.RefreshTokenExpiration = TimeSpan.FromDays(10);
});
```

### 2. Register Endpoints

```csharp
app.UseSocialLoginEndpoints();
```

This registers the following endpoints:
- `POST /api/Authentication/Social/Token` - Exchange OAuth code for JWT token (with PKCE support)
- `GET /api/Authentication/Social/Token` - Legacy endpoint (backward compatibility)
- `GET /api/Authentication/Social/User` - Get authenticated user information

### 3. Custom User Provider

```csharp
builder.Services.AddSocialUserProvider<SocialUserProvider>();
```

Implement `ISocialUserProvider` to integrate with your database:

```csharp
internal sealed class SocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User> _userRepository;
    
    public SocialUserProvider(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<SocialUser> GetAsync(string username, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        // Fetch user from your database
        var user = await _userRepository.Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (user == null)
        {
            // Create new user on first login
            user = new User
            {
                Email = username,
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            };
            await _userRepository.InsertAsync(user, cancellationToken);
        }
        
        return new CustomSocialUser
        {
            Username = user.Email,
            DisplayName = user.Name,
            UserId = user.Id
        };
    }

    public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
    {
        var user = await _userRepository.Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (user != null)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
            yield return new Claim(ClaimTypes.Name, user.Name);
            yield return new Claim(ClaimTypes.Email, user.Email);
            yield return new Claim(ClaimTypes.Role, user.Role);
        }
    }
}

public sealed class CustomSocialUser : DefaultSocialUser
{
    public string DisplayName { get; set; }
    public Guid UserId { get; set; }
}
```

## 🔐 PKCE (Proof Key for Code Exchange)

### What is PKCE?

PKCE (RFC 7636) enhances OAuth 2.0 security by preventing authorization code interception attacks. It's **required** for:
- Single-Page Applications (SPAs)
- Mobile applications
- Public clients (where client secrets cannot be safely stored)

### How PKCE Works

1. **Client generates `code_verifier`**: Random 43-128 character string
2. **Client creates `code_challenge`**: SHA256 hash of code_verifier, base64url encoded
3. **Authorization request**: Client sends `code_challenge` to OAuth provider
4. **Token exchange**: Client sends original `code_verifier` to your API
5. **API validates**: Verifies code_verifier matches code_challenge with OAuth provider

### PKCE Implementation

The library automatically handles PKCE when clients send `code_verifier`:

```csharp
// Endpoint accepts code_verifier in request body
POST /api/Authentication/Social/Token?provider=Microsoft&code={oauth_code}&redirectPath=/account/login
Content-Type: application/json

{
    "code_verifier": "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
}
```

The `TokenCheckerSettings` class encapsulates all parameters:

```csharp
public sealed class TokenCheckerSettings
{
    public string? Domain { get; set; }                          // OAuth redirect domain
    public string RedirectPath { get; set; } = "/";              // Redirect path (for exact URI matching)
    public Dictionary<string, string>? AdditionalParameters { get; set; }  // Includes code_verifier for PKCE
    
    public string GetRedirectUri() => $"{Domain.TrimEnd('/')}{RedirectPath}";
    public string? GetParameter(string key) => AdditionalParameters?.TryGetValue(key, out var val) == true ? val : null;
}
```

### Backward Compatibility

If `code_verifier` is not provided, the library falls back to a default value for backward compatibility:

```csharp
// Legacy GET request (no PKCE)
GET /api/Authentication/Social/Token?provider=Microsoft&code={oauth_code}
```

⚠️ **Security Note**: For production SPAs and mobile apps, always use PKCE.

## 🎯 Token Checker Interface

All OAuth providers implement `ITokenChecker`:

```csharp
public interface ITokenChecker
{
    Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(
        string code, 
        TokenCheckerSettings settings, 
        CancellationToken cancellationToken = default);
}
```

### Supported Providers

| Provider | PKCE Support | Token Checker |
|----------|--------------|---------------|
| Microsoft | ✅ Required | `MicrosoftTokenChecker` |
| Google | ✅ Optional | `GoogleTokenChecker` |
| GitHub | ❌ Not supported | `GithubTokenChecker` |
| Facebook | ❌ Not supported | `FacebookTokenChecker` |
| Amazon | ❌ Not supported | `AmazonTokenChecker` |
| LinkedIn | ❌ Not supported | `LinkedinTokenChecker` |
| X (Twitter) | ❌ Not supported | `XTokenChecker` |
| TikTok | ❌ Not supported | `TikTokTokenChecker` |
| Instagram | ❌ Not supported | `InstagramTokenChecker` |
| Pinterest | ❌ Not supported | `PinterestTokenChecker` |

## 📝 Configuration Example

### appsettings.json

```json
{
  "SocialLogin": {
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret",
      "RedirectDomain": "https://yourdomain.com"
    },
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret",
      "RedirectDomain": "https://yourdomain.com"
    }
  }
}
```

### OAuth Provider Setup

#### Microsoft Entra ID (Azure AD)
1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Create new registration, set redirect URI: `https://yourdomain.com/account/login`
3. Enable "ID tokens" under Authentication
4. Copy Application (client) ID and create a client secret

#### Google
1. Go to [Google Cloud Console](https://console.cloud.google.com) → APIs & Services → Credentials
2. Create OAuth 2.0 Client ID
3. Add authorized redirect URI: `https://yourdomain.com/account/login`
4. Copy Client ID and Client Secret

#### Facebook
1. Go to [Facebook Developers](https://developers.facebook.com) → My Apps → Create App
2. Add Facebook Login product
3. Set Valid OAuth Redirect URI: `https://yourdomain.com/account/login`
4. Copy App ID and App Secret

## 🔧 Advanced Configuration

### Custom Token Validation

Implement custom `ITokenChecker` for additional providers:

```csharp
public class CustomTokenChecker : ITokenChecker
{
    public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(
        string code, 
        TokenCheckerSettings settings, 
        CancellationToken cancellationToken)
    {
        var codeVerifier = settings.GetParameter("code_verifier");
        var redirectUri = settings.GetRedirectUri();
        
        // Your custom OAuth token exchange logic
        // ...
        
        return new TokenResponse
        {
            Username = "user@example.com",
            Claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, "user@example.com")
            }
        };
    }
}
```

### Custom Claim Transformation

```csharp
public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
{
    // Add custom claims based on user roles from database
    var userRoles = await _roleRepository.GetUserRolesAsync(username, cancellationToken);
    
    foreach (var role in userRoles)
    {
        yield return new Claim(ClaimTypes.Role, role.Name);
    }
    
    // Add custom application-specific claims
    yield return new Claim("tenant_id", "tenant-123");
    yield return new Claim("subscription_level", "premium");
}
```

## 🌐 API Endpoints Reference

### POST /api/Authentication/Social/Token

**Query Parameters:**
- `provider` (required): OAuth provider (Microsoft, Google, Facebook, etc.)
- `code` (required): Authorization code from OAuth provider
- `redirectPath` (optional): OAuth redirect path (default: `/`)

**Request Body** (JSON):
```json
{
    "code_verifier": "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
}
```

**Response** (200 OK):
```json
{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiresIn": 3600
}
```

### GET /api/Authentication/Social/User

**Headers:**
- `Authorization: Bearer {accessToken}`

**Response** (200 OK):
```json
{
    "username": "user@example.com",
    "displayName": "John Doe",
    "userId": "guid-here"
}
```

## 🔗 Related Packages

- **Blazor Client**: `Rystem.Authentication.Social.Blazor` - UI components for Blazor Server/WASM
- **React Client**: `rystem.authentication.social.react` - React hooks and components with TypeScript
- **Abstractions**: `Rystem.Authentication.Social.Abstractions` - Shared models and interfaces

## 📚 More Information

- **Complete Docs**: [https://rystem.net/mcp/tools/auth-social-server.md](https://rystem.net/mcp/tools/auth-social-server.md)
- **PKCE RFC**: [RFC 7636](https://tools.ietf.org/html/rfc7636)
- **OAuth 2.0 Flow**: [https://rystem.net/mcp/prompts/auth-flow.md](https://rystem.net/mcp/prompts/auth-flow.md)