### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Authentication.Social.Abstractions

Core **interfaces and models** for Rystem Social Authentication - defines the contract for server-side OAuth 2.0 implementation.

### 📦 What's Included

This package provides the **abstractions** used by the server implementation and should be referenced when:
- Creating custom `SocialUser` implementations
- Implementing `ISocialUserProvider` for user provisioning
- Using token validation services
- Building custom authentication flows

---

## 🏛️ Core Interfaces

### ISocialUser - User Identity

The base interface for social user data:

```csharp
public interface ISocialUser
{
    string? Username { get; set; }
}
```

**Minimal implementation** - only provides username from social provider. Extend this interface in your application.

### ILocalizedSocialUser - With Localization

For multi-language applications:

```csharp
public interface ILocalizedSocialUser : ISocialUser
{
    string? Language { get; set; }
}
```

### ProviderType - Supported Providers

Enum of all supported OAuth providers:

```csharp
public enum ProviderType
{
    Google,
    Microsoft,
    Facebook,
    GitHub,
    Amazon,
    LinkedIn,
    X,           // Twitter
    TikTok,
    Pinterest,
    Instagram
}
```

---

## 🔑 Token Models

### TokenResponse - JWT Response

```csharp
public class TokenResponse
{
    public string? BearerToken { get; set; }        // JWT for API calls
    public string? RefreshToken { get; set; }       // Refresh token
    public int ExpiresIn { get; set; }              // Seconds until expiration
    public ISocialUser? User { get; set; }          // User data
}
```

Returned by `/auth/{provider}/login` endpoint.

---

## 👤 Creating Custom SocialUser

### Option 1: Extend ISocialUser Only

```csharp
using Rystem.Authentication.Social;

public sealed class AppSocialUser : ISocialUser
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? CreatedAt { get; set; }
}
```

### Option 2: Extend Both ISocialUser and ILocalizedSocialUser

```csharp
using Rystem.Authentication.Social;
using System.Text.Json.Serialization;

public sealed class GlobalAppSocialUser : ISocialUser, ILocalizedSocialUser
{
    [JsonPropertyName("u")]
    public string? Username { get; set; }
    
    [JsonPropertyName("e")]
    public string? Email { get; set; }
    
    [JsonPropertyName("p")]
    public string? ProfilePictureUrl { get; set; }
    
    [JsonPropertyName("l")]
    public string? Language { get; set; }
    
    [JsonPropertyName("r")]
    public List<string>? Roles { get; set; }
    
    [JsonPropertyName("c")]
    public DateTime CreatedAt { get; set; }
}
```

**Notes:**
- Use `[JsonPropertyName]` to keep JSON payload small
- This example uses 1-letter property names for network efficiency
- Roles can be loaded from user provider if needed

### Option 3: DefaultSocialUser (Internal)

For simple cases, Rystem provides `DefaultSocialUser` (internal):

```csharp
// This is what Rystem uses if no custom implementation provided
internal sealed class DefaultSocialUser : ISocialUser
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
```

**When to use Default:**
- Simple demo applications
- Quick prototyping
- No additional user data needed

**When to create Custom:**
- Production applications
- Need email, profile picture, roles, etc.
- Multi-language support required
- Want to minimize JSON payload

---

## 🔌 ISocialUserProvider - User Provisioning

Interface for loading/creating users from social login data:

```csharp
public interface ISocialUserProvider
{
    /// <summary>
    /// Called on social login - get or create user
    /// </summary>
    /// <param name="username">Username from social provider</param>
    /// <param name="claims">Claims extracted from social provider token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User data to return to client</returns>
    Task<SocialUser> GetAsync(
        string username, 
        IEnumerable<Claim> claims, 
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Load claims for authorization (roles, permissions, etc)
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Claims to include in JWT token</returns>
    IAsyncEnumerable<Claim> GetClaimsAsync(
        string? username, 
        CancellationToken cancellationToken);
}
```

### Example Implementation

```csharp
public sealed class MyAppSocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User, string> _users;
    private readonly IRepository<UserRole, Guid> _roles;
    
    public MyAppSocialUserProvider(
        IRepository<User, string> users,
        IRepository<UserRole, Guid> roles)
    {
        _users = users;
        _roles = roles;
    }
    
    public async Task<SocialUser> GetAsync(
        string username, 
        IEnumerable<Claim> claims, 
        CancellationToken cancellationToken)
    {
        // Get or create user from database
        var user = await _users.GetByKeyAsync(username, cancellationToken);
        
        if (user == null)
        {
            // Create new user from social data
            var email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
            
            user = new User
            {
                Id = username,
                Email = email,
                Name = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                ProfilePicture = claims.FirstOrDefault(x => x.Type == "picture")?.Value,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            await _users.InsertAsync(user, cancellationToken);
        }
        
        // Return user data
        return new AppSocialUser
        {
            Username = user.Id,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePicture,
            FirstName = user.Name,
            CreatedAt = user.CreatedAt
        };
    }
    
    public async IAsyncEnumerable<Claim> GetClaimsAsync(
        string? username, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (username == null)
            yield break;
        
        var user = await _users.GetByKeyAsync(username, cancellationToken);
        if (user == null)
            yield break;
        
        // Standard claims
        yield return new Claim(ClaimTypes.NameIdentifier, user.Id);
        yield return new Claim(ClaimTypes.Name, user.Name ?? "");
        yield return new Claim(ClaimTypes.Email, user.Email ?? "");
        
        // Load user roles
        var userRoles = await _roles.QueryAsync(
            r => r.UserId == user.Id,
            cancellationToken: cancellationToken);
        
        foreach (var role in userRoles)
        {
            yield return new Claim(ClaimTypes.Role, role.RoleName);
        }
        
        // Custom claims
        yield return new Claim("profile_complete", (!string.IsNullOrEmpty(user.Name)).ToString());
    }
}

// Register in DI
builder.Services.AddSocialUserProvider<MyAppSocialUserProvider>();
```

---

## 📋 Common Claims Used

Standard claim types from `System.Security.Claims.ClaimTypes`:

```csharp
ClaimTypes.NameIdentifier   // User unique ID (primary key)
ClaimTypes.Name             // User name/display name
ClaimTypes.Email            // Email address
ClaimTypes.Role             // User role
ClaimTypes.Upn              // User principal name
ClaimTypes.GivenName        // First name
ClaimTypes.Surname          // Last name
```

Custom claims are also supported:

```csharp
yield return new Claim("email_verified", "true");
yield return new Claim("subscription_level", "pro");
yield return new Claim("org_id", organizationId.ToString());
```

---

## 🔐 Token Security

### Bearer Token (JWT)

Short-lived token (1 hour default) for API requests:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**When it expires:**
- Client refreshes using Refresh Token
- Or user logs in again

### Refresh Token

Long-lived token (7 days default) for obtaining new Bearer Tokens:

```http
POST /auth/refresh
Content-Type: application/json

{
    "refreshToken": "refresh-token-value"
}
```

**Best Practice:** Store refresh tokens in:
- HttpOnly cookies (most secure)
- Secure local storage
- IndexedDB with encryption

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────┐
│  Application Layer (Your App)       │
│  - API Controllers                  │
│  - Business Logic                   │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│  Rystem.Authentication.Social       │
│  - OAuth Token Exchange             │
│  - Endpoint: /auth/{provider}/login │
│  - Uses ISocialUserProvider         │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────────────┐
│  Rystem.Authentication.Social.Abstractions  │
│  - ISocialUser (interfaces)                 │
│  - ISocialUserProvider (interface)          │
│  - TokenResponse (models)                   │
│  - This Package                             │
└─────────────────────────────────────────────┘
```

---

## 📚 Related Packages

- **Rystem.Authentication.Social** - Server implementation
- **Rystem.Authentication.Social.Blazor** - Blazor components for login
- **rystem.authentication.social.react** - React hooks for login

---

## 💡 Usage Pattern

1. **Server Setup** (Rystem.Authentication.Social):
   - Configure OAuth providers
   - Register ISocialUserProvider
   - Call `app.UseSocialLoginEndpoints()`

2. **Custom SocialUser** (Your Application):
   - Extend `ISocialUser` with your properties
   - Register in DI

3. **User Provisioning** (ISocialUserProvider):
   - Get/create user from database
   - Return SocialUser with populated data
   - Populate claims for authorization

4. **Client** (Blazor or React):
   - Call `/auth/{provider}/login` with code
   - Receive JWT and user data
   - Use JWT for subsequent API calls

---

## References

- [OAuth 2.0 Specification](https://tools.ietf.org/html/rfc6749)
- [JWT RFC 7519](https://tools.ietf.org/html/rfc7519)
- [System.Security.Claims](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims)
- [Rystem.Authentication.Social](../Rystem.Authentication.Social/README.md) - Server implementation