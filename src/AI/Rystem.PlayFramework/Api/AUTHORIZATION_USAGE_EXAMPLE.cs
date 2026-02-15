using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework;
using Rystem.PlayFramework.Api;

var builder = WebApplication.CreateBuilder(args);

// Add authentication (example: JWT Bearer)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://your-identity-server.com";
        options.Audience = "playframework-api";
    });

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Policy 1: Require authenticated user
    options.AddPolicy("Authenticated", policy =>
        policy.RequireAuthenticatedUser());

    // Policy 2: Premium subscription required
    options.AddPolicy("PremiumUser", policy =>
        policy.RequireClaim("subscription", "premium", "enterprise"));

    // Policy 3: Admin role required
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Policy 4: Custom policy for PlayFramework access
    options.AddPolicy("PlayFrameworkAccess", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => 
                c.Type == "permissions" && 
                c.Value.Contains("playframework:use"))));

    // Policy 5: Rate limit approved (custom requirement)
    options.AddPolicy("RateLimitApproved", policy =>
        policy.Requirements.Add(new RateLimitRequirement()));
});

// Add PlayFramework services
builder.Services
    .AddPlayFramework()
    .WithFactory("default", factory =>
    {
        factory.UseChatClient<MockChatClient>();
    })
    .WithFactory("premium", factory =>
    {
        factory.UseChatClient<PremiumChatClient>();
    })
    .WithFactory("admin", factory =>
    {
        factory.UseChatClient<AdminChatClient>();
    });

var app = builder.Build();

// Configure middleware
app.UseAuthentication();
app.UseAuthorization();

// Example 1: Multi-factory endpoint with global policies
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess"
    };
});
// Result: POST /api/ai/{factoryName} - requires Authenticated + PlayFrameworkAccess

// Example 2: Public endpoint (no authentication)
app.MapPlayFramework(settings =>
{
    settings.BasePath = "/api/ai/public";
    settings.RequireAuthentication = false;
});
// Result: POST /api/ai/public/{factoryName} - no authentication required

// Example 3: Premium factory with factory-specific policy
app.MapPlayFramework("premium", settings =>
{
    settings.BasePath = "/api/ai/premium";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated"
    };
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "premium", new List<string> { "PremiumUser" } }
    };
});
// Result: POST /api/ai/premium - requires Authenticated + PremiumUser

// Example 4: Admin factory with multiple policies
app.MapPlayFramework("admin", settings =>
{
    settings.BasePath = "/api/ai/admin";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string>
    {
        "Authenticated",
        "PlayFrameworkAccess"
    };
    settings.FactoryPolicies = new Dictionary<string, List<string>>
    {
        { "admin", new List<string> { "AdminOnly", "RateLimitApproved" } }
    };
});
// Result: POST /api/ai/admin - requires all 4 policies

// Example 5: Custom base path per factory
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/chat";
    settings.RequireAuthentication = true;
    settings.AuthorizationPolicies = new List<string> { "Authenticated" };
});
// Result: POST /api/chat - requires Authenticated only

app.Run();

// Mock implementations (replace with your actual implementations)
public class MockChatClient : IChatClient
{
    // Implementation...
}

public class PremiumChatClient : IChatClient
{
    // Implementation...
}

public class AdminChatClient : IChatClient
{
    // Implementation...
}

public class RateLimitRequirement : IAuthorizationRequirement
{
    // Custom requirement implementation
}
