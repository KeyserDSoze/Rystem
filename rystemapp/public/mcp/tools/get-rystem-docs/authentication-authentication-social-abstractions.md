### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Authentication.Social.Abstractions

Shared models, interfaces, and abstractions for Rystem social authentication libraries.

### 📦 Installation

```bash
dotnet add package Rystem.Authentication.Social.Abstractions
```

## 🔑 Core Interfaces

### ISocialUser

Base interface for social user models:

```csharp
public interface ISocialUser
{
    string? Username { get; set; }
}
```

### DefaultSocialUser

Default implementation provided by the library:

```csharp
internal sealed class DefaultSocialUser : ISocialUser
{
    public string? Username { get; set; }
}
```

### Custom Social User

Extend with your application-specific properties:

```csharp
public sealed class CustomSocialUser : ISocialUser
{
    public string? Username { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public Guid UserId { get; set; }
    public string Avatar { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime LastLogin { get; set; }
}
```

## 🔐 TokenCheckerSettings

Unified settings class for OAuth token validation (with PKCE support):

```csharp
public sealed class TokenCheckerSettings
{
    /// <summary>
    /// Domain for OAuth redirect (e.g., https://yourdomain.com)
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Redirect path after OAuth callback (default: /)
    /// Used to construct full redirect_uri: {Domain}{RedirectPath}
    /// </summary>
    public string RedirectPath { get; set; } = "/";

    /// <summary>
    /// Additional parameters for token exchange
    /// Example: { "code_verifier": "abc123..." } for PKCE
    /// </summary>
    public Dictionary<string, string>? AdditionalParameters { get; set; }

    /// <summary>
    /// Get full redirect URI by combining Domain and RedirectPath
    /// </summary>
    public string GetRedirectUri()
    {
        if (string.IsNullOrWhiteSpace(Domain))
            return RedirectPath;

        return $"{Domain.TrimEnd('/')}{RedirectPath}";
    }

    /// <summary>
    /// Get additional parameter by key (e.g., "code_verifier")
    /// </summary>
    public string? GetParameter(string key)
    {
        if (AdditionalParameters == null || string.IsNullOrWhiteSpace(key))
            return null;

        return AdditionalParameters.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Set additional parameter (fluent API)
    /// </summary>
    public TokenCheckerSettings WithParameter(string key, string value)
    {
        AdditionalParameters ??= new Dictionary<string, string>();
        AdditionalParameters[key] = value;
        return this;
    }
}
```

### Usage Examples

#### Basic Usage

```csharp
var settings = new TokenCheckerSettings
{
    Domain = "https://yourdomain.com",
    RedirectPath = "/account/login"
};

var fullUri = settings.GetRedirectUri();  // "https://yourdomain.com/account/login"
```

#### With PKCE (Microsoft OAuth)

```csharp
var settings = new TokenCheckerSettings
{
    Domain = "https://yourdomain.com",
    RedirectPath = "/account/login",
    AdditionalParameters = new Dictionary<string, string>
    {
        { "code_verifier", "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk" }
    }
};

var codeVerifier = settings.GetParameter("code_verifier");
```

#### Fluent API

```csharp
var settings = new TokenCheckerSettings
    .WithParameter("code_verifier", codeVerifier)
    .WithParameter("custom_param", "value");
```

## 📝 TokenResponse

Response from OAuth token validation:

```csharp
public sealed class TokenResponse
{
    /// <summary>
    /// Username/email from OAuth provider
    /// </summary>
    public required string Username { get; init; }
    
    /// <summary>
    /// Claims extracted from OAuth provider
    /// </summary>
    public required List<Claim> Claims { get; init; }
    
    /// <summary>
    /// Empty response for failed validations
    /// </summary>
    public static TokenResponse Empty => new TokenResponse 
    { 
        Username = string.Empty, 
        Claims = new List<Claim>() 
    };
}
```

## 🌐 ProviderType Enum

Supported OAuth providers:

```csharp
public enum ProviderType
{
    DotNet = 0,      // Internal .NET bearer token
    Microsoft = 1,   // Microsoft Entra ID (Azure AD)
    Google = 2,      // Google OAuth
    Facebook = 3,    // Facebook Login
    GitHub = 4,      // GitHub OAuth
    Amazon = 5,      // Amazon Login
    Linkedin = 6,    // LinkedIn OAuth
    X = 7,           // X (Twitter) OAuth
    TikTok = 8,      // TikTok Login
    Instagram = 9,   // Instagram Basic Display
    Pinterest = 10   // Pinterest OAuth
}
```

## 🔧 ITokenChecker Interface

Core interface for OAuth token validation:

```csharp
public interface ITokenChecker
{
    /// <summary>
    /// Validate OAuth authorization code and exchange for user information
    /// </summary>
    /// <param name="code">Authorization code from OAuth provider</param>
    /// <param name="settings">Token checker settings (domain, redirectPath, code_verifier, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TokenResponse with username and claims, or error message</returns>
    Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(
        string code, 
        TokenCheckerSettings settings, 
        CancellationToken cancellationToken = default);
}
```

### Implementation Example

```csharp
public class CustomTokenChecker : ITokenChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CustomOAuthSettings _settings;
    
    public CustomTokenChecker(
        IHttpClientFactory httpClientFactory, 
        CustomOAuthSettings settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
    }
    
    public async Task<AnyOf<TokenResponse?, string>> CheckTokenAndGetUsernameAsync(
        string code, 
        TokenCheckerSettings settings, 
        CancellationToken cancellationToken)
    {
        var codeVerifier = settings.GetParameter("code_verifier");
        var redirectUri = settings.GetRedirectUri();
        
        var client = _httpClientFactory.CreateClient("CustomOAuth");
        
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };
        
        // Add PKCE if provided
        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            tokenRequest["code_verifier"] = codeVerifier;
        }
        
        var response = await client.PostAsync(
            "https://oauth.provider.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return error;
        }
        
        var tokenData = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken);
        
        // Validate ID token and extract claims
        var claims = ExtractClaims(tokenData.IdToken);
        var username = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        
        return new TokenResponse
        {
            Username = username ?? string.Empty,
            Claims = claims
        };
    }
}
```

## 🔗 Related Packages

- **Server Implementation**: `Rystem.Authentication.Social` - ASP.NET Core OAuth endpoints
- **Blazor Client**: `Rystem.Authentication.Social.Blazor` - Blazor UI components
- **React Client**: `rystem.authentication.social.react` - React/TypeScript hooks

## 📚 More Information

- **Authentication Flow**: [https://rystem.net/mcp/prompts/auth-flow.md](https://rystem.net/mcp/prompts/auth-flow.md)
- **PKCE RFC 7636**: [https://tools.ietf.org/html/rfc7636](https://tools.ietf.org/html/rfc7636)