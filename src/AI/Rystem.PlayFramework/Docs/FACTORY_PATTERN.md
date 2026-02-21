# Factory Pattern Integration

PlayFramework is now **fully integrated with Rystem's IFactory pattern**, allowing you to create **multiple configurations** identified by keys and resolve them dynamically at runtime.

## Quick Example

```csharp
// Setup multiple configurations
services.AddPlayFramework("basic", builder => { ... });
services.AddPlayFramework("premium", builder => { ... });

// Resolve by key
var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();
var basicManager = factory.Create("basic");
var premiumManager = factory.Create("premium");
```

## Why Factory Pattern?

The factory pattern enables:
- **Multi-tenant scenarios**: Different configurations per tenant
- **Environment-specific setups**: Dev, staging, production configs
- **Feature flags**: Enable advanced features for premium users
- **A/B testing**: Run different AI strategies simultaneously
- **Dynamic configuration**: Switch between configs at runtime

## Execution Modes (Enum-Based)

Instead of multiple boolean flags, use the `SceneExecutionMode` enum:

```csharp
public enum SceneExecutionMode
{
    Direct = 0,           // Single scene, fast
    Planning = 1,         // Multi-scene with upfront plan
    DynamicChaining = 2   // Multi-scene with live decisions
}
```

### Setting Execution Mode

**Per-Request**:
```csharp
var settings = new SceneRequestSettings
{
    ExecutionMode = SceneExecutionMode.DynamicChaining,
    MaxDynamicScenes = 5
};

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    // ...
}
```

**Default in Configuration**:
```csharp
services.AddPlayFramework(builder =>
{
    builder.WithExecutionMode(SceneExecutionMode.Planning);
    // This becomes the default, can be overridden per-request
});
```

## IFactory<T> Interface

```csharp
public interface IFactory<out TService>
{
    TService? Create(AnyOf<string?, Enum>? name = null);
    TService? CreateWithoutDecoration(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAll(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAllWithoutDecoration(AnyOf<string?, Enum>? name = null);
    bool Exists(AnyOf<string?, Enum>? name = null);
}
```

Keys can be:
- **String**: `"basic"`, `"premium"`, `"tenant-123"`
- **Enum**: `Environment.Development`, `Region.Europe`
- **Null**: Resolves default (no-key) registration

## Usage Examples

### 1. Multiple Configurations with Different Keys

```csharp
// Startup/Program.cs
services.AddPlayFramework("free", builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Direct)
        .AddScene(s => s.WithName("BasicSearch").WithDescription("Simple search"));
});

services.AddPlayFramework("pro", builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.DynamicChaining)
        .WithCostTracking("USD", 0.03m, 0.06m)
        .AddScene(s => s.WithName("AdvancedSearch").WithDescription("AI-powered search"))
        .AddScene(s => s.WithName("DataAnalysis").WithDescription("Deep analysis"));
});

// In your service/controller
public class SearchService
{
    private readonly IFactory<ISceneManager> _factory;

    public SearchService(IFactory<ISceneManager> factory)
    {
        _factory = factory;
    }

    public async Task<string> SearchAsync(string query, bool isPremiumUser)
    {
        var key = isPremiumUser ? "pro" : "free";
        var sceneManager = _factory.Create(key)!;

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(query))
        {
            results.Add(response);
        }

        return results.Last().Message ?? "No results";
    }
}
```

### 2. Enum-Based Keys (Environment)

```csharp
public enum AppEnvironment
{
    Development,
    Staging,
    Production
}

// Startup
services.AddPlayFramework(AppEnvironment.Development, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Direct)
        .AddScene(s => s.WithName("DevTool").WithDescription("Debugging tools"));
});

services.AddPlayFramework(AppEnvironment.Production, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Planning)
        .WithPlanning()
        .WithCostTracking("USD", 0.03m, 0.06m)
        .AddScene(s => s.WithName("ProdSearch").WithDescription("Production search"));
});

// Usage
var env = configuration.GetValue<AppEnvironment>("Environment");
var sceneManager = factory.Create(env)!;
```

### 3. Multi-Tenant SaaS Application

```csharp
public class TenantService
{
    private readonly IFactory<ISceneManager> _factory;
    private readonly ITenantProvider _tenantProvider;

    public TenantService(IFactory<ISceneManager> factory, ITenantProvider tenantProvider)
    {
        _factory = factory;
        _tenantProvider = tenantProvider;
    }

    public async Task<AiResponse> ProcessRequestAsync(string query)
    {
        var tenant = _tenantProvider.GetCurrentTenant();
        var configKey = $"tenant-{tenant.Id}";

        // Each tenant has their own configuration
        var sceneManager = _factory.Create(configKey);

        if (sceneManager == null)
        {
            // Fallback to default config
            sceneManager = _factory.Create("default")!;
        }

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(query))
        {
            results.Add(response);
        }

        return new AiResponse(results);
    }
}

// Startup - Register tenant configurations
foreach (var tenant in tenantRepository.GetAllTenants())
{
    services.AddPlayFramework($"tenant-{tenant.Id}", builder =>
    {
        builder
            .WithExecutionMode(tenant.PlanType == "Enterprise" 
                ? SceneExecutionMode.DynamicChaining 
                : SceneExecutionMode.Direct)
            .WithCostTracking("USD", tenant.CostPerInput, tenant.CostPerOutput)
            .Configure(settings =>
            {
                settings.DefaultMaxTokens = tenant.MaxTokens;
            });

        // Add tenant-specific scenes
        foreach (var scene in tenant.EnabledScenes)
        {
            builder.AddScene(s => ConfigureScene(s, scene));
        }
    });
}
```

### 4. Feature Flags / A/B Testing

```csharp
public class ExperimentService
{
    private readonly IFactory<ISceneManager> _factory;

    public ExperimentService(IFactory<ISceneManager> factory)
    {
        _factory = factory;
    }

    public async Task<string> RunExperimentAsync(string query, string userId)
    {
        // A/B test: 50% users get variant A, 50% get variant B
        var variant = userId.GetHashCode() % 2 == 0 ? "variant-a" : "variant-b";

        var sceneManager = _factory.Create(variant)!;

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(query))
        {
            results.Add(response);
        }

        // Log for analysis
        await LogExperimentResult(userId, variant, results);

        return results.Last().Message ?? "";
    }
}

// Startup
services.AddPlayFramework("variant-a", builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Direct)
        .AddScene(s => s.WithName("FastSearch").WithDescription("Quick results"));
});

services.AddPlayFramework("variant-b", builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.DynamicChaining)
        .AddScene(s => s.WithName("DeepSearch").WithDescription("Comprehensive results"));
});
```

### 5. Check Configuration Existence

```csharp
public class ConfigurationManager
{
    private readonly IFactory<ISceneManager> _factory;

    public ConfigurationManager(IFactory<ISceneManager> factory)
    {
        _factory = factory;
    }

    public ISceneManager GetSceneManager(string configKey)
    {
        if (!_factory.Exists(configKey))
        {
            throw new InvalidOperationException($"Configuration '{configKey}' not found");
        }

        return _factory.Create(configKey)!;
    }

    public IEnumerable<string> GetAllConfigurationKeys()
    {
        // Factory returns all registered configurations
        return _factory.CreateAll()
            .Select(manager => /* extract key somehow */)
            .ToList();
    }
}
```

## Mixing Factory and Default Registration

You can have **both** a default (no-key) registration **and** keyed registrations:

```csharp
// Default (no key)
services.AddPlayFramework(builder =>
{
    builder.WithExecutionMode(SceneExecutionMode.Direct);
    // ... basic config
});

// Keyed registrations
services.AddPlayFramework("advanced", builder =>
{
    builder.WithExecutionMode(SceneExecutionMode.Planning);
    // ... advanced config
});

// Resolve default via ISceneManager
var defaultManager = serviceProvider.GetRequiredService<ISceneManager>();

// Resolve keyed via IFactory
var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();
var advancedManager = factory.Create("advanced");
```

## Advanced: Custom Key Types

You can use any object as a key (converted to string):

```csharp
public record TenantKey(string Region, string Tier);

var key = new TenantKey("EU", "Premium");
services.AddPlayFramework(key, builder => { ... });

// Resolve
var sceneManager = factory.Create(key.ToString()); // "TenantKey { Region = EU, Tier = Premium }"
```

## Per-Request vs Per-Configuration Execution Mode

| Setting | Scope | Override Priority |
|---------|-------|-------------------|
| `PlayFrameworkSettings.DefaultExecutionMode` | Global (per configuration) | Lowest |
| `SceneRequestSettings.ExecutionMode` | Per-request | Highest |

```csharp
// Configuration: default is Direct
services.AddPlayFramework("myconfig", builder =>
{
    builder.WithExecutionMode(SceneExecutionMode.Direct);
});

// Request: override to DynamicChaining
var settings = new SceneRequestSettings
{
    ExecutionMode = SceneExecutionMode.DynamicChaining
};

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    // Uses DynamicChaining (request override wins)
}

// Request: use default
var defaultSettings = new SceneRequestSettings();
// ExecutionMode is null, so uses config default (Direct)

await foreach (var response in sceneManager.ExecuteAsync(query, defaultSettings))
{
    // Uses Direct (from config)
}
```

## Migration from Boolean Flags

**Before**:
```csharp
var settings = new SceneRequestSettings
{
    EnablePlanning = true,
    EnableDynamicSceneChaining = false
};
```

**After**:
```csharp
var settings = new SceneRequestSettings
{
    ExecutionMode = SceneExecutionMode.Planning
};
```

## Benefits Summary

✅ **Cleaner API**: One enum instead of multiple booleans  
✅ **Multi-configuration**: Different setups for different scenarios  
✅ **Type-safe keys**: Use enums for compile-time safety  
✅ **Runtime flexibility**: Switch configs dynamically  
✅ **Multi-tenancy**: Isolate tenant configurations  
✅ **A/B testing**: Run experiments with different strategies  
✅ **Environment-specific**: Dev, staging, prod configs  
✅ **Feature flags**: Enable features per user tier  

## Full Example: E-Commerce Platform

```csharp
public enum UserTier
{
    Free,
    Standard,
    Premium,
    Enterprise
}

// Startup
services.AddPlayFramework(UserTier.Free, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Direct)
        .AddScene(s => s.WithName("ProductSearch").WithDescription("Basic search"));
});

services.AddPlayFramework(UserTier.Standard, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Direct)
        .WithCostTracking("USD", 0.01m, 0.02m)
        .AddScene(s => s.WithName("ProductSearch").WithDescription("Enhanced search"))
        .AddScene(s => s.WithName("Recommendations").WithDescription("Product recommendations"));
});

services.AddPlayFramework(UserTier.Premium, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.DynamicChaining)
        .WithCostTracking("USD", 0.02m, 0.04m)
        .AddScene(s => s.WithName("AdvancedSearch").WithDescription("AI-powered search"))
        .AddScene(s => s.WithName("PersonalShopper").WithDescription("Personal shopping assistant"))
        .AddScene(s => s.WithName("TrendAnalysis").WithDescription("Trend forecasting"));
});

services.AddPlayFramework(UserTier.Enterprise, builder =>
{
    builder
        .WithExecutionMode(SceneExecutionMode.Planning)
        .WithPlanning()
        .WithCostTracking("USD", 0.03m, 0.06m)
        .WithCostTracking(costs =>
        {
            costs.WithModelCosts("gpt-4", 0.03m, 0.06m);
            costs.WithModelCosts("gpt-3.5-turbo", 0.001m, 0.002m);
        })
        .AddScene(s => s.WithName("EnterpriseSearch").WithDescription("Full-featured search"))
        .AddScene(s => s.WithName("InventoryOptimizer").WithDescription("Inventory optimization"))
        .AddScene(s => s.WithName("DemandForecasting").WithDescription("Demand prediction"))
        .AddScene(s => s.WithName("CompetitorAnalysis").WithDescription("Market intelligence"));
});

// Usage in controller
public class ProductController : ControllerBase
{
    private readonly IFactory<ISceneManager> _factory;

    public ProductController(IFactory<ISceneManager> factory)
    {
        _factory = factory;
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchAsync([FromBody] SearchRequest request)
    {
        var user = await GetCurrentUserAsync();
        var sceneManager = _factory.Create(user.Tier)!;

        var results = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            request.Query,
            new SceneRequestSettings
            {
                // Can still override per-request
                ExecutionMode = request.UseAdvancedMode 
                    ? SceneExecutionMode.DynamicChaining 
                    : null, // Use config default
                MaxBudget = user.MonthlyBudget
            }))
        {
            results.Add(response);
        }

        return Ok(new
        {
            Results = results.Where(r => r.Status == AiResponseStatus.Running).Select(r => r.Message),
            TotalCost = results.Last().TotalCost,
            ScenesExecuted = results.Count(r => r.Status == AiResponseStatus.ExecutingScene)
        });
    }
}
```

## Testing with Factory

```csharp
[Fact]
public async Task Test_DifferentConfigurations()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IChatClient, MockChatClient>();

    services.AddPlayFramework("config-a", builder => { /* ... */ });
    services.AddPlayFramework("config-b", builder => { /* ... */ });

    var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IFactory<ISceneManager>>();

    // Act
    var managerA = factory.Create("config-a")!;
    var managerB = factory.Create("config-b")!;

    var resultsA = new List<AiSceneResponse>();
    await foreach (var response in managerA.ExecuteAsync("test"))
    {
        resultsA.Add(response);
    }

    var resultsB = new List<AiSceneResponse>();
    await foreach (var response in managerB.ExecuteAsync("test"))
    {
        resultsB.Add(response);
    }

    // Assert
    Assert.NotEqual(resultsA.Count, resultsB.Count); // Different behavior
}
```

## Summary

The factory pattern integration makes PlayFramework **production-ready** for:
- **Multi-tenant SaaS** applications
- **Enterprise** platforms with role-based features
- **A/B testing** and experimentation
- **Environment-specific** configurations
- **Dynamic** configuration selection at runtime

Combined with the **enum-based execution mode**, the API is now **cleaner, more maintainable, and more powerful**.
