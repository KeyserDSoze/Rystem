# Social Authentication - Server Setup

Add **social login** (OAuth 2.0) to your API with automatic **JWT token management**.

**Supported Providers:**
- Google
- Microsoft (Azure AD)
- Facebook
- GitHub
- LinkedIn
- X (Twitter)
- Instagram
- Pinterest
- Amazon

---

## Installation

```bash
dotnet add package Rystem.Authentication.Social --version 9.1.3
```

---

## Configuration

### Basic Setup

```csharp
builder.Services.AddSocialLogin(
    socialProviders =>
    {
        // Google OAuth
        socialProviders.Google.ClientId = configuration["SocialLogin:Google:ClientId"];
        socialProviders.Google.ClientSecret = configuration["SocialLogin:Google:ClientSecret"];
        socialProviders.Google.AddUris(configuration["SocialLogin:Google:AllowedDomains"]!.Split(','));
        
        // Microsoft OAuth
        socialProviders.Microsoft.ClientId = configuration["SocialLogin:Microsoft:ClientId"];
        socialProviders.Microsoft.ClientSecret = configuration["SocialLogin:Microsoft:ClientSecret"];
        socialProviders.Microsoft.AddUris(configuration["SocialLogin:Microsoft:AllowedDomains"]!.Split(','));
        
        // Facebook OAuth
        socialProviders.Facebook.ClientId = configuration["SocialLogin:Facebook:ClientId"];
        socialProviders.Facebook.ClientSecret = configuration["SocialLogin:Facebook:ClientSecret"];
        socialProviders.Facebook.AddUris(configuration["SocialLogin:Facebook:AllowedDomains"]!.Split(','));
    },
    tokenSettings =>
    {
        tokenSettings.BearerTokenExpiration = TimeSpan.FromHours(1);
        tokenSettings.RefreshTokenExpiration = TimeSpan.FromDays(10);
    }
);
```

### appsettings.json

```json
{
  "SocialLogin": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
      "AllowedDomains": "http://localhost:5173,https://yourdomain.com"
    },
    "Microsoft": {
      "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
      "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET",
      "AllowedDomains": "http://localhost:5173,https://yourdomain.com"
    },
    "Facebook": {
      "ClientId": "YOUR_FACEBOOK_APP_ID",
      "ClientSecret": "YOUR_FACEBOOK_APP_SECRET",
      "AllowedDomains": "http://localhost:5173,https://yourdomain.com"
    },
    "GitHub": {
      "ClientId": "YOUR_GITHUB_CLIENT_ID",
      "ClientSecret": "YOUR_GITHUB_CLIENT_SECRET",
      "AllowedDomains": "http://localhost:5173,https://yourdomain.com"
    }
  }
}
```

---

## Add Endpoints

```csharp
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Add social login endpoints
app.UseSocialLoginEndpoints();

app.Run();
```

**Endpoints Added:**
- `POST /api/social/google` - Google OAuth callback
- `POST /api/social/microsoft` - Microsoft OAuth callback
- `POST /api/social/facebook` - Facebook OAuth callback
- `POST /api/social/github` - GitHub OAuth callback
- `POST /api/social/refresh` - Refresh JWT token
- `GET /api/social/user` - Get current user info

---

## User Provider

Create a **user provider** to integrate with your database or storage:

```csharp
public class SocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User, Guid> _userRepository;
    
    public SocialUserProvider(IRepository<User, Guid> userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<SocialUser> GetAsync(
        string username, 
        IEnumerable<Claim> claims, 
        CancellationToken cancellationToken)
    {
        // Find user by email/username
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
        {
            // Create new user on first social login
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = username,
                Name = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value,
                CreatedAt = DateTime.UtcNow
            };
            
            await _userRepository.InsertAsync(user, cancellationToken);
        }
        
        // Return SocialUser (or custom subclass)
        return new AppSocialUser
        {
            Username = user.Email,
            Email = user.Email,
            UserId = user.Id,
            Roles = user.Roles
        };
    }
    
    public async IAsyncEnumerable<Claim> GetClaimsAsync(
        string? username, 
        CancellationToken cancellationToken)
    {
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user != null)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
            yield return new Claim(ClaimTypes.Name, user.Email);
            yield return new Claim(ClaimTypes.Email, user.Email);
            
            foreach (var role in user.Roles)
            {
                yield return new Claim(ClaimTypes.Role, role);
            }
        }
    }
}

// Custom SocialUser with additional properties
public class AppSocialUser : SocialUser
{
    public Guid UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}
```

### Register User Provider

```csharp
builder.Services.AddSocialLogin<SocialUserProvider>(
    socialProviders => { /* ... */ },
    tokenSettings => { /* ... */ }
);
```

---

## Complete Example

```csharp
using Rystem.Authentication.Social;
using RepositoryFramework.InMemory;

var builder = WebApplication.CreateBuilder(args);

// Social Login with User Provider
builder.Services.AddSocialLogin<SocialUserProvider>(
    socialProviders =>
    {
        // Google
        socialProviders.Google.ClientId = builder.Configuration["SocialLogin:Google:ClientId"];
        socialProviders.Google.ClientSecret = builder.Configuration["SocialLogin:Google:ClientSecret"];
        socialProviders.Google.AddUris([.. builder.Configuration["SocialLogin:Google:AllowedDomains"]!.Split(',')]);
        
        // Microsoft
        socialProviders.Microsoft.ClientId = builder.Configuration["SocialLogin:Microsoft:ClientId"];
        socialProviders.Microsoft.ClientSecret = builder.Configuration["SocialLogin:Microsoft:ClientSecret"];
        socialProviders.Microsoft.AddUris([.. builder.Configuration["SocialLogin:Microsoft:AllowedDomains"]!.Split(',')]);
        
        // GitHub
        socialProviders.GitHub.ClientId = builder.Configuration["SocialLogin:GitHub:ClientId"];
        socialProviders.GitHub.ClientSecret = builder.Configuration["SocialLogin:GitHub:ClientSecret"];
        
        // LinkedIn
        socialProviders.Linkedin.ClientId = builder.Configuration["SocialLogin:Linkedin:ClientId"];
        socialProviders.Linkedin.ClientSecret = builder.Configuration["SocialLogin:Linkedin:ClientSecret"];
        socialProviders.Linkedin.AddUris([.. builder.Configuration["SocialLogin:Linkedin:AllowedDomains"]!.Split(',')]);
        
        // X (Twitter)
        socialProviders.X.ClientId = builder.Configuration["SocialLogin:X:ClientId"];
        socialProviders.X.ClientSecret = builder.Configuration["SocialLogin:X:ClientSecret"];
        socialProviders.X.AddUris([.. builder.Configuration["SocialLogin:X:AllowedDomains"]!.Split(',')]);
        
        // Instagram
        socialProviders.Instagram.ClientId = builder.Configuration["SocialLogin:Instagram:ClientId"];
        socialProviders.Instagram.ClientSecret = builder.Configuration["SocialLogin:Instagram:ClientSecret"];
        socialProviders.Instagram.AddUris([.. builder.Configuration["SocialLogin:Instagram:AllowedDomains"]!.Split(',')]);
        
        // Pinterest
        socialProviders.Pinterest.ClientId = builder.Configuration["SocialLogin:Pinterest:ClientId"];
        socialProviders.Pinterest.ClientSecret = builder.Configuration["SocialLogin:Pinterest:ClientSecret"];
        socialProviders.Pinterest.AddUris([.. builder.Configuration["SocialLogin:Pinterest:AllowedDomains"]!.Split(',')]);
    },
    tokenSettings =>
    {
        tokenSettings.BearerTokenExpiration = TimeSpan.FromHours(1);
        tokenSettings.RefreshTokenExpiration = TimeSpan.FromDays(10);
    },
    ServiceLifetime.Transient
);

// Repository for users
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUsers", policy => 
        policy.RequireClaim(ClaimTypes.NameIdentifier));
    
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://yourdomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Social login endpoints
app.UseSocialLoginEndpoints();

// Protected API endpoints
app.MapGet("/api/profile", (ClaimsPrincipal user) =>
{
    return new
    {
        UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        Email = user.FindFirst(ClaimTypes.Email)?.Value,
        Name = user.FindFirst(ClaimTypes.Name)?.Value
    };
})
.RequireAuthorization("AuthenticatedUsers");

app.Run();
```

---

## Real-World Examples

### Multi-Tenant SaaS

```csharp
public class MultiTenantSocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IRepository<Tenant, Guid> _tenantRepository;
    
    public async Task<SocialUser> GetAsync(
        string username, 
        IEnumerable<Claim> claims, 
        CancellationToken cancellationToken)
    {
        var email = username;
        var domain = email.Split('@')[1];
        
        // Find tenant by email domain
        var tenant = await _tenantRepository
            .Query()
            .Where(x => x.EmailDomain == domain)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                EmailDomain = domain,
                Name = domain,
                CreatedAt = DateTime.UtcNow
            };
            await _tenantRepository.InsertAsync(tenant, cancellationToken);
        }
        
        // Find or create user
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == email && x.TenantId == tenant.Id)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                TenantId = tenant.Id,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.InsertAsync(user, cancellationToken);
        }
        
        return new TenantSocialUser
        {
            Username = email,
            UserId = user.Id,
            TenantId = tenant.Id
        };
    }
    
    public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
    {
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user != null)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
            yield return new Claim(ClaimTypes.Email, user.Email);
            yield return new Claim("TenantId", user.TenantId.ToString());
        }
    }
}
```

### Role-Based Access Control

```csharp
public class RbacSocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IRepository<UserRole, Guid> _roleRepository;
    
    public async IAsyncEnumerable<Claim> GetClaimsAsync(string? username, CancellationToken cancellationToken)
    {
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user != null)
        {
            yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
            yield return new Claim(ClaimTypes.Name, user.Email);
            
            // Get roles from database
            var roles = await _roleRepository
                .Query()
                .Where(x => x.UserId == user.Id)
                .ToListAsync(cancellationToken);
            
            foreach (var role in roles)
            {
                yield return new Claim(ClaimTypes.Role, role.RoleName);
                
                // Add permissions as claims
                foreach (var permission in role.Permissions)
                {
                    yield return new Claim("Permission", permission);
                }
            }
        }
    }
}

// Usage in controller
[Authorize(Policy = "RequireAdminRole")]
[HttpGet("admin/dashboard")]
public IActionResult AdminDashboard()
{
    return Ok(new { Message = "Admin Dashboard" });
}

// Policy setup
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("CanDeleteUsers", policy =>
        policy.RequireClaim("Permission", "users.delete"));
});
```

### Audit Trail

```csharp
public class AuditSocialUserProvider : ISocialUserProvider
{
    private readonly IRepository<User, Guid> _userRepository;
    private readonly IRepository<LoginAudit, Guid> _auditRepository;
    
    public async Task<SocialUser> GetAsync(
        string username, 
        IEnumerable<Claim> claims, 
        CancellationToken cancellationToken)
    {
        var provider = claims.FirstOrDefault(x => x.Type == "provider")?.Value ?? "Unknown";
        
        var user = await _userRepository
            .Query()
            .Where(x => x.Email == username)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = username,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepository.InsertAsync(user, cancellationToken);
        }
        
        // Log login attempt
        await _auditRepository.InsertAsync(new LoginAudit
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            LoginAt = DateTime.UtcNow,
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        }, cancellationToken);
        
        return new SocialUser
        {
            Username = user.Email
        };
    }
}
```

---

## OAuth Provider Setup

### Google OAuth

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create project → APIs & Services → Credentials
3. Create OAuth 2.0 Client ID
4. Add **Authorized redirect URIs**: `http://localhost:5173`, `https://yourdomain.com`
5. Copy **Client ID** and **Client Secret**

### Microsoft OAuth

1. Go to [Azure Portal](https://portal.azure.com/)
2. Azure Active Directory → App registrations → New registration
3. Add **Redirect URIs** (Web): `http://localhost:5173`, `https://yourdomain.com`
4. Certificates & secrets → New client secret
5. Copy **Application (client) ID** and **Client secret**

### GitHub OAuth

1. Go to [GitHub Settings](https://github.com/settings/developers)
2. OAuth Apps → New OAuth App
3. Set **Authorization callback URL**: `http://localhost:5173`
4. Copy **Client ID** and generate **Client Secret**

---

## Benefits

- ✅ **Multiple Providers**: Google, Microsoft, Facebook, GitHub, LinkedIn, X, Instagram, Pinterest
- ✅ **Automatic Token Management**: JWT with refresh tokens
- ✅ **Customizable User Provider**: Integrate with any database or storage
- ✅ **Claims-Based Auth**: Full support for roles, permissions, custom claims
- ✅ **CORS Support**: Ready for frontend integration

---

## Related Tools

- **[Social Authentication - Blazor Client](https://rystem.net/mcp/tools/auth-social-blazor.md)** - Blazor integration
- **[Social Authentication - TypeScript Client](https://rystem.net/mcp/tools/auth-social-typescript.md)** - React/Vue/Angular integration
- **[Authentication Flow Setup](https://rystem.net/mcp/prompts/auth-flow.md)** - General auth setup guide

---

## References

- **NuGet Package**: [Rystem.Authentication.Social](https://www.nuget.org/packages/Rystem.Authentication.Social) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
