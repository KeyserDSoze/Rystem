# Budget Limit Feature

## 📊 Overview

The **Budget Limit** feature allows you to control costs by automatically stopping PlayFramework execution when a specified budget threshold is exceeded. This is critical for production environments where cost control is essential.

## 🎯 Key Features

- ✅ **Per-Request Budget**: Set maximum cost limit for each execution
- ✅ **Real-time Monitoring**: Checks budget after every LLM call
- ✅ **Automatic Stop**: Execution halts immediately when limit exceeded
- ✅ **Transparent Reporting**: Detailed cost breakdown in responses
- ✅ **Multi-Currency Support**: Works with any configured currency (USD, EUR, GBP, etc.)
- ✅ **Flexible Configuration**: Can be enabled/disabled per request

## 🚀 Usage

### Basic Setup

```csharp
// Configure cost tracking with budget limit
var settings = new SceneRequestSettings
{
    MaxBudget = 0.50m // Stop execution if cost exceeds $0.50
};

await foreach (var response in sceneManager.ExecuteAsync("Your query", settings))
{
    Console.WriteLine($"Status: {response.Status}");
    Console.WriteLine($"Message: {response.Message}");
    Console.WriteLine($"Cost: ${response.Cost:F6} (Total: ${response.TotalCost:F6})");
    
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        Console.WriteLine("❌ Budget limit reached!");
        break;
    }
}
```

### Configuration Examples

#### Unlimited Budget (Default)
```csharp
var settings = new SceneRequestSettings
{
    MaxBudget = null // No limit - execution continues regardless of cost
};
```

#### Tight Budget for Testing
```csharp
var settings = new SceneRequestSettings
{
    MaxBudget = 0.10m // Only $0.10 - useful for development/testing
};
```

#### Production Budget with Monitoring
```csharp
var settings = new SceneRequestSettings
{
    MaxBudget = 5.00m // $5 limit for complex workflows
};

decimal totalCost = 0;
await foreach (var response in sceneManager.ExecuteAsync("Complex query", settings))
{
    if (response.Cost.HasValue)
    {
        totalCost = response.TotalCost;
        Console.WriteLine($"Running cost: ${totalCost:F6} / ${settings.MaxBudget:F2}");
    }
    
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        // Log to monitoring system
        Logger.Warning($"Budget exceeded: {totalCost:F6} > {settings.MaxBudget:F2}");
        
        // Alert operations team
        await AlertService.SendBudgetAlertAsync(totalCost, settings.MaxBudget.Value);
        
        break;
    }
}
```

## 🧮 How It Works

### Cost Calculation

Budget checks happen **after every LLM call**:

1. **Extract token usage** from LLM response (input, output, cached tokens)
2. **Calculate cost** using configured pricing:
   ```
   InputCost = (InputTokens / 1000) × InputCostPer1K
   OutputCost = (OutputTokens / 1000) × OutputCostPer1K  
   CachedCost = (CachedTokens / 1000) × CachedInputCostPer1K
   TotalCost = InputCost + OutputCost + CachedCost
   ```
3. **Accumulate total cost** across all operations
4. **Check budget**: If `TotalCost > MaxBudget`, stop execution

### Example Flow

```
Request: "Analyze sales data for Q1 2024"
Budget: $0.50

LLM Call 1 (Scene Selection):
  - Input: 200 tokens, Output: 50 tokens
  - Cost: $0.015
  - Total: $0.015 ✅ Continue

LLM Call 2 (Data Analysis):
  - Input: 500 tokens, Output: 300 tokens  
  - Cost: $0.048
  - Total: $0.063 ✅ Continue

LLM Call 3 (Report Generation):
  - Input: 800 tokens, Output: 1200 tokens
  - Cost: $0.096
  - Total: $0.159 ✅ Continue

... (multiple tool calls)

LLM Call 12 (Final Summary):
  - Input: 600 tokens, Output: 900 tokens
  - Cost: $0.090
  - Total: $0.567 ❌ BUDGET EXCEEDED

Result: Execution stopped, returns BudgetExceeded status
```

## 📋 Response Status

When budget is exceeded, you'll receive a response with:

```csharp
{
    Status = AiResponseStatus.BudgetExceeded,
    Message = "Budget limit of 0.500000 USD exceeded. Total cost: 0.567000",
    ErrorMessage = "Maximum budget reached",
    TotalCost = 0.567m,
    Cost = 0.090m // Cost of the call that exceeded the budget
}
```

## 🎨 Integration Patterns

### Pattern 1: Graceful Degradation

```csharp
var settings = new SceneRequestSettings { MaxBudget = 1.00m };

var partialResults = new List<string>();
bool budgetExceeded = false;

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Status == AiResponseStatus.Running && response.Message != null)
    {
        partialResults.Add(response.Message);
    }
    
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        budgetExceeded = true;
        break;
    }
}

if (budgetExceeded)
{
    // Return partial results with disclaimer
    return new
    {
        Results = partialResults,
        Warning = "Budget limit reached - results may be incomplete",
        Cost = partialResults.Last().TotalCost
    };
}
```

### Pattern 2: Dynamic Budget Adjustment

```csharp
decimal initialBudget = 0.50m;
var settings = new SceneRequestSettings { MaxBudget = initialBudget };

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        // User approval for additional budget
        bool approved = await RequestBudgetIncreaseAsync(initialBudget, response.TotalCost);
        
        if (approved)
        {
            // Restart with higher budget
            settings.MaxBudget = initialBudget * 2;
            // Continue execution...
        }
    }
}
```

### Pattern 3: Cost Estimation Before Execution

```csharp
// Dry-run with very low budget to estimate cost
var estimationSettings = new SceneRequestSettings
{
    MaxBudget = 0.01m,
    EnablePlanning = true // Planning phase gives cost indication
};

await foreach (var response in sceneManager.ExecuteAsync(query, estimationSettings))
{
    if (response.Status == AiResponseStatus.Planning)
    {
        // Estimate full cost based on plan complexity
        decimal estimatedCost = EstimateCostFromPlan(response);
        
        if (estimatedCost > userBudget)
        {
            await NotifyUserOfEstimatedCostAsync(estimatedCost);
            return;
        }
    }
}

// If estimation looks good, run with full budget
var actualSettings = new SceneRequestSettings { MaxBudget = userBudget };
await foreach (var response in sceneManager.ExecuteAsync(query, actualSettings))
{
    // Process results...
}
```

## ⚙️ Configuration with Cost Tracking

Budget limit works seamlessly with cost tracking:

```csharp
services.AddPlayFramework(builder =>
{
    builder
        // Enable cost tracking with specific pricing
        .WithCostTracking(
            currency: "USD",
            inputCostPer1K: 0.03m,      // GPT-4 input pricing
            outputCostPer1K: 0.06m,     // GPT-4 output pricing
            cachedInputCostPer1K: 0.003m // 10% cost for cached tokens
        )
        
        // Add model-specific pricing
        .WithModelCosts("gpt-4", 0.03m, 0.06m)
        .WithModelCosts("gpt-3.5-turbo", 0.0015m, 0.002m) // Cheaper fallback
        
        .AddScene(/* ... */);
});

// Use in request
var settings = new SceneRequestSettings
{
    MaxBudget = 2.00m,        // $2 budget
    ModelId = "gpt-3.5-turbo" // Use cheaper model to stay under budget
};
```

## 📊 Monitoring & Analytics

### Track Budget Usage

```csharp
public class BudgetMonitor
{
    private readonly Dictionary<string, BudgetUsage> _usage = new();

    public async Task<ExecutionResult> ExecuteWithMonitoringAsync(
        ISceneManager sceneManager,
        string query,
        string userId,
        decimal maxBudget)
    {
        var settings = new SceneRequestSettings { MaxBudget = maxBudget };
        var responses = new List<AiSceneResponse>();

        await foreach (var response in sceneManager.ExecuteAsync(query, settings))
        {
            responses.Add(response);
            
            if (response.Status == AiResponseStatus.BudgetExceeded)
            {
                // Log budget exceeded event
                _usage[userId] = new BudgetUsage
                {
                    TotalCost = response.TotalCost,
                    Budget = maxBudget,
                    ExceededAt = DateTime.UtcNow,
                    Query = query
                };
                
                await LogBudgetExceededAsync(userId, response);
                break;
            }
        }

        return new ExecutionResult
        {
            Responses = responses,
            TotalCost = responses.LastOrDefault()?.TotalCost ?? 0,
            BudgetExceeded = responses.Any(r => r.Status == AiResponseStatus.BudgetExceeded)
        };
    }
}
```

### Daily Budget Aggregation

```csharp
public class DailyBudgetService
{
    private decimal _dailySpent = 0;
    private readonly decimal _dailyLimit = 100.00m; // $100/day limit

    public async Task<AiSceneResponse?> ExecuteWithDailyLimitAsync(
        ISceneManager sceneManager,
        string query)
    {
        // Check if we've already hit daily limit
        if (_dailySpent >= _dailyLimit)
        {
            return new AiSceneResponse
            {
                Status = AiResponseStatus.BudgetExceeded,
                Message = $"Daily budget of ${_dailyLimit} exceeded. Spent: ${_dailySpent}",
                ErrorMessage = "Daily limit reached"
            };
        }

        // Calculate remaining budget for this request
        var remainingBudget = _dailyLimit - _dailySpent;
        var settings = new SceneRequestSettings { MaxBudget = remainingBudget };

        AiSceneResponse? lastResponse = null;
        await foreach (var response in sceneManager.ExecuteAsync(query, settings))
        {
            lastResponse = response;
            
            if (response.Cost.HasValue)
            {
                _dailySpent += response.Cost.Value;
            }
        }

        return lastResponse;
    }
}
```

## 🧪 Testing

Example test demonstrating budget limit:

```csharp
[Fact]
public async Task ExecuteAsync_WithBudgetLimit_StopsWhenExceeded()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddPlayFramework(builder =>
    {
        builder
            .WithCostTracking("USD", 0.03m, 0.06m)
            .AddScene(/* ... */);
    });

    var serviceProvider = services.BuildServiceProvider();
    var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

    // Expected cost per call: $0.09
    // Budget: $0.15 - should stop after 2nd LLM call
    var settings = new SceneRequestSettings { MaxBudget = 0.15m };

    // Act
    var responses = new List<AiSceneResponse>();
    await foreach (var response in sceneManager.ExecuteAsync("Query", settings))
    {
        responses.Add(response);
    }

    // Assert
    Assert.Contains(responses, r => r.Status == AiResponseStatus.BudgetExceeded);
    Assert.True(responses.Last().TotalCost > 0.15m);
}
```

## 🛡️ Best Practices

### 1. Set Reasonable Budgets
```csharp
// ❌ Too tight - may fail on first call
MaxBudget = 0.01m

// ✅ Reasonable for simple queries
MaxBudget = 0.25m

// ✅ Good for complex multi-step workflows
MaxBudget = 2.00m

// ✅ Enterprise scenarios with multiple scenes
MaxBudget = 10.00m
```

### 2. Monitor Partial Costs
```csharp
await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Cost.HasValue)
    {
        var percentUsed = (response.TotalCost / settings.MaxBudget.Value) * 100;
        
        if (percentUsed > 80)
        {
            Logger.Warning($"Budget 80% consumed: {response.TotalCost:F6}");
        }
    }
}
```

### 3. Provide User Feedback
```csharp
if (response.Status == AiResponseStatus.BudgetExceeded)
{
    return new ApiResponse
    {
        Success = false,
        Message = $"Your request exceeded the ${settings.MaxBudget:F2} budget limit. " +
                  $"Actual cost: ${response.TotalCost:F4}. " +
                  "Please simplify your query or upgrade your plan.",
        PartialResults = collectedResults
    };
}
```

### 4. Use Model Selection for Cost Optimization
```csharp
// Try with cheaper model first
var cheapSettings = new SceneRequestSettings
{
    MaxBudget = 0.50m,
    ModelId = "gpt-3.5-turbo" // Cheaper model
};

await foreach (var response in sceneManager.ExecuteAsync(query, cheapSettings))
{
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        // Fallback: use partial results or ask user to pay for GPT-4
        break;
    }
}
```

## 🔗 Related Features

- **Cost Tracking**: [COST_TRACKING.md](./COST_TRACKING.md) - Track token usage and costs
- **Model Selection**: Configure different models with specific pricing
- **Caching**: Reduce costs with conversation caching (90% discount on cached tokens)
- **Planning**: Estimate costs before execution with planning phase

## 📚 API Reference

### SceneRequestSettings.MaxBudget

```csharp
/// <summary>
/// Maximum budget for this request (in the configured currency).
/// If set, execution will stop when total cost exceeds this value.
/// Set to null for unlimited budget (default).
/// </summary>
public decimal? MaxBudget { get; set; }
```

### AiResponseStatus.BudgetExceeded

```csharp
/// <summary>
/// Budget limit exceeded - execution stopped.
/// </summary>
BudgetExceeded
```

### Example Response

```csharp
{
    "status": "BudgetExceeded",
    "message": "Budget limit of 0.500000 USD exceeded. Total cost: 0.567000",
    "errorMessage": "Maximum budget reached",
    "totalCost": 0.567,
    "cost": 0.090,
    "timestamp": "2024-01-15T10:30:45Z"
}
```

## 🎯 Summary

The Budget Limit feature provides:

- ✅ **Cost Control**: Never exceed predefined budgets
- ✅ **Real-time Monitoring**: Track costs at every step
- ✅ **Graceful Handling**: Execution stops cleanly with clear feedback
- ✅ **Production Ready**: Essential for cost-sensitive production environments

Perfect for SaaS applications, enterprise deployments, and any scenario where cost predictability is critical!
