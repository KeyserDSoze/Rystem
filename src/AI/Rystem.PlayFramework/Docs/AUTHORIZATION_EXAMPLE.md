# Authorization Policy Support

PlayFramework API endpoints support **ASP.NET Core Authorization Policies** for fine-grained access control.

## Features

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

---

## Policy Evaluation Order

Policies are evaluated in this order:

1. `RequireAuthentication` (if `true`)
2. Global `AuthorizationPolicies` (all must pass)
3. Factory-specific `FactoryPolicies[factoryName]` (all must pass)

**All policies must succeed** for the request to proceed.

---

## Unity/MAUI Compatibility

When using PlayFramework in **Unity/MAUI/WPF**:
- The HTTP API endpoints **are not available**
- Use `ISceneManager.ExecuteAsync()` directly
- Implement your own authorization logic in the app layer

Authorization policies are **ASP.NET Core-only** features.
