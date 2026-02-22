# Authorization: HTTP Policies & Business Logic

PlayFramework supports **two levels of authorization**:

1. **HTTP Endpoint Authorization** - ASP.NET Core policies (token validation, claims, roles)
2. **Business Logic Authorization** - Custom `IAuthorizationLayer` (quotas, feature flags, budgets)

## HTTP Endpoint Authorization (ASP.NET Core Policies)

### Features

- **Global Policies**: Apply to all factories
- **Factory-Specific Policies**: Apply to specific factories (only in single-factory endpoints)
- **Combined Policies**: Global + factory-specific policies are enforced together

---

## Setup Authorization Policies

Register policies in your ASP.NET Core app:

```csharp
builder.Services.AddAuthorization(options =>
{
    // Basic policy: require authenticated user
    options.AddPolicy("Authenticated", policy => 
        policy.RequireAuthenticatedUser());

    // Role-based policy
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));

    // Claim-based policy
    options.AddPolicy("PremiumUser", policy => 
        policy.RequireClaim("subscription", "premium"));

    // Custom requirement
    options.AddPolicy("PlayFrameworkAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "feature" && c.Value == "ai")));
});
```

---

## Multi-Factory Endpoint (Dynamic Factory)

Use **global policies only** for endpoints that accept any factory name:

```csharp
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    
    // Require authentication for all factories
    settings.RequireAuthentication = true;
    
    // Apply global policies to all factories
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess"
    };
    
    // ⚠️ Factory-specific policies are NOT applied in multi-factory endpoints
});
```

**Result**: All requests to `/api/ai/{factoryName}` require:
- Authenticated user
- `PlayFrameworkAccess` policy

---

## Single-Factory Endpoint (Fixed Factory)

Use **global + factory-specific policies** for dedicated factory endpoints:

```csharp
// Premium factory with premium-only access
app.MapPlayFramework("premium", settings =>
{
    settings.BasePath = "/api/ai/premium";
    settings.RequireAuthentication = true;
    
    // Global policies (apply to all factories when used elsewhere)
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated"
    };
    
    // Factory-specific policies (only for "premium" factory)
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "premium", new List<string> { "PremiumUser" } }
    };
});

// Admin factory with admin-only access
app.MapPlayFramework("admin", settings =>
{
    settings.BasePath = "/api/ai/admin";
    settings.RequireAuthentication = true;
    
    settings.AuthorizationPolicies = new List<string> { "Authenticated" };
    
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "admin", new List<string> { "AdminOnly" } }
    };
});
```

**Result**:
- `/api/ai/premium` requires: `Authenticated` + `PremiumUser`
- `/api/ai/admin` requires: `Authenticated` + `AdminOnly`

---

## Multiple Policies

Combine multiple policies for complex authorization:

```csharp
app.MapPlayFramework("secure", settings =>
{
    settings.BasePath = "/api/ai/secure";
    settings.RequireAuthentication = true;
    
    // All of these policies must pass
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess",
        "RateLimitApproved"
    };
    
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "secure", new List<string> { "AdminOnly", "AuditLogged" } }
    };
});
```

**Result**: `/api/ai/secure` requires ALL of:
- Authenticated
- PlayFrameworkAccess
- RateLimitApproved
- AdminOnly
- AuditLogged

---

## No Authentication

Public endpoints without authentication:

```csharp
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai/public";
    settings.RequireAuthentication = false; // Default
    // No policies applied
});
```

---

## Testing Authorization

### Postman / HTTP Client

```http
POST https://localhost:5001/api/ai/premium HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "prompt": "Hello",
  "sceneName": "MainScene"
}
```

### C# Client

```csharp
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var request = new PlayFrameworkRequest
{
    Prompt = "Hello",
    SceneName = "MainScene"
};

var response = await httpClient.PostAsJsonAsync(
    "https://localhost:5001/api/ai/premium", 
    request);
```

---

## Response Codes

| Code | Meaning |
|------|---------|
| `200` | Success (authorized + executed) |
| `401` | Unauthorized (no authentication) |
| `403` | Forbidden (authenticated but policy failed) |
| `500` | Internal server error |

---

## Best Practices

1. **Use multi-factory endpoints** for general-purpose AI APIs
2. **Use single-factory endpoints** when you need different authorization per factory
3. **Combine policies** for defense-in-depth (e.g., `Authenticated` + `RateLimitApproved`)
4. **Document policies** in your API documentation (Swagger/OpenAPI)
5. **Test policy evaluation** in integration tests
6. **Use IAuthorizationLayer** for complex business logic (quotas, feature flags)
7. **Separate HTTP auth from business auth** - Use ASP.NET policies for tokens/roles, IAuthorizationLayer for user-specific logic

---

## Policy Evaluation Order

Policies are evaluated in this order:

1. **HTTP Request** → ASP.NET Core Authentication Middleware
2. **Endpoint Authorization** → `RequireAuthentication` (if `true`)
3. **Global Policies** → `AuthorizationPolicies` (all must pass)
4. **Factory-Specific Policies** → `FactoryPolicies[factoryName]` (all must pass)
5. **Scene Initialization** → Load cache, initialize context, execute main actors
6. **Business Authorization** → `IAuthorizationLayer.AuthorizeAsync()` (if registered)
7. **Scene Execution** → Execute selected scenes (if authorized)

**All policies must succeed** for the request to proceed.

---

## Business Logic Authorization (IAuthorizationLayer)

For **complex authorization logic** that depends on business rules (quotas, feature flags, budgets), implement `IAuthorizationLayer`:

```csharp
public class QuotaAuthorizationLayer : IAuthorizationLayer
{
    private readonly IUserService _userService;
    private readonly ILogger<QuotaAuthorizationLayer> _logger;

    public QuotaAuthorizationLayer(
        IUserService userService,
        ILogger<QuotaAuthorizationLayer> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(
        SceneContext context,
        SceneRequestSettings settings,
        CancellationToken cancellationToken)
    {
        // Extract userId from metadata
        if (!context.Metadata.TryGetValue("userId", out var userIdObj) || userIdObj is not string userId)
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "User ID not found in request metadata"
            };
        }

        // Check user quota
        var user = await _userService.GetUserAsync(userId, cancellationToken);
        if (user.MonthlyQuota <= 0)
        {
            _logger.LogWarning("User {UserId} exceeded monthly quota", userId);
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = $"Monthly quota exceeded. Resets on {user.QuotaResetDate:yyyy-MM-dd}"
            };
        }

        // Check feature flag for specific scene
        if (settings.SceneName == "PremiumScene" && !user.HasFeature("premium-scenes"))
        {
            return new AuthorizationResult
            {
                IsAuthorized = false,
                Reason = "Premium subscription required for this feature"
            };
        }

        // All checks passed
        _logger.LogInformation("User {UserId} authorized (Quota: {Quota})", userId, user.MonthlyQuota);
        return new AuthorizationResult { IsAuthorized = true };
    }
}
```

**Register in PlayFramework:**
```csharp
builder.Services.AddPlayFramework("default", pb => pb
    .AddChatClient(...)
    .AddScene(...)
    .AddAuthorizationLayer<QuotaAuthorizationLayer>());

// Register dependencies
builder.Services.AddSingleton<IUserService, UserService>();
```

**When authorization fails:**
```json
{
  "status": "error",
  "errorMessage": "Authorization failed: Monthly quota exceeded. Resets on 2025-03-01",
  "message": "You are not authorized to perform this action."
}
```

**Use Cases for IAuthorizationLayer:**
- ✅ User-specific quotas (requests per month, tokens per day)
- ✅ Feature flags (beta features, premium scenes)
- ✅ Budget limits (max cost per request/user)
- ✅ Time-based restrictions (business hours only)
- ✅ Content filtering (block specific inputs based on user tier)
- ✅ Multi-tenancy (tenant-specific permissions)
- ✅ Dynamic pricing (adjust maxBudget based on user plan)

**Why use IAuthorizationLayer instead of ASP.NET policies?**

| ASP.NET Core Policies | IAuthorizationLayer |
|----------------------|---------------------|
| HTTP-level authorization | Business-level authorization |
| JWT validation, roles, claims | Quotas, feature flags, budgets |
| Static configuration | Dynamic database queries |
| Runs before execution | Runs after initialization |
| Returns HTTP 401/403 | Returns custom error message |
| No access to SceneContext | Full access to context, settings, metadata |

---

## Unity/MAUI Compatibility

When using PlayFramework in **Unity/MAUI/WPF**:
- The HTTP API endpoints **are not available**
- Use `ISceneManager.ExecuteAsync()` directly
- Implement your own authorization logic in the app layer
- `IAuthorizationLayer` **is available** in non-HTTP scenarios

Authorization policies are **ASP.NET Core-only** features.
