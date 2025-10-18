# Authentication Flow Setup

> Configure authentication and authorization in your Rystem application

## Context

You are implementing authentication and authorization in a .NET application using Rystem. This prompt will guide you through setting up social authentication, JWT tokens, and role-based access control.

## Prerequisites

- .NET 6.0 or higher
- Rystem packages installed
- Authentication provider credentials (Google, Facebook, Microsoft, etc.)

## Configuration Steps

### 1. Install Required Packages

```bash
dotnet add package Rystem.Authentication.Social
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2. Configure Social Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication()
    .AddRystemSocialAuthentication(social =>
    {
        social.AddGoogle(google =>
        {
            google.ClientId = builder.Configuration["Authentication:Google:ClientId"];
            google.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        });

        social.AddMicrosoft(microsoft =>
        {
            microsoft.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
            microsoft.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
        });

        social.AddFacebook(facebook =>
        {
            facebook.AppId = builder.Configuration["Authentication:Facebook:AppId"];
            facebook.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
        });
    });
```

### 3. Configure JWT

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
```

### 4. Add Authorization Policies

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("UserOrAdmin", policy => 
        policy.RequireRole("User", "Admin"));
    
    options.AddPolicy("VerifiedEmail", policy => 
        policy.RequireClaim("email_verified", "true"));
});
```

### 5. Create Authentication Service

```csharp
public class AuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;

    public async Task<AuthResult> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null || !VerifyPassword(password, user.PasswordHash))
        {
            return AuthResult.Failed("Invalid credentials");
        }

        var token = GenerateJwtToken(user);
        
        return AuthResult.Success(token, user);
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("email_verified", user.EmailVerified.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### 6. Protect Endpoints

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "UserOrAdmin")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Return user profile
    }

    [HttpPost("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminAction()
    {
        // Admin-only action
    }
}
```

### 7. Configuration File

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    },
    "Facebook": {
      "AppId": "your-facebook-app-id",
      "AppSecret": "your-facebook-app-secret"
    }
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "Issuer": "your-app-name",
    "Audience": "your-app-users"
  }
}
```

## Testing

### Login Request
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123"}'
```

### Authenticated Request
```bash
curl -X GET https://localhost:5001/api/users/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Security Best Practices

1. **Use HTTPS** - Always in production
2. **Secure Secrets** - Use Azure Key Vault or similar
3. **Token Expiration** - Set appropriate expiration times
4. **Refresh Tokens** - Implement for long-lived sessions
5. **Rate Limiting** - Protect against brute force
6. **CORS** - Configure properly for your frontend

## See Also

- [Rystem.Authentication.Social Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Authentication)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [OAuth 2.0 Specification](https://oauth.net/2/)
