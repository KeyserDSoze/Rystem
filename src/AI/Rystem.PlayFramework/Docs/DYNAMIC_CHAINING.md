# Dynamic Scene Chaining

**Dynamic Scene Chaining** is the third execution mode in PlayFramework, providing a **"live planning"** approach that combines the flexibility of direct execution with the power of multi-scene orchestration.

## Overview

Unlike static planning (which decides all scenes upfront) or direct execution (which runs a single scene), dynamic chaining allows the LLM to **decide on-the-fly** whether to execute additional scenes based on the results of previous ones.

## Execution Modes Comparison

| Mode | When to Use | Scenes | Planning | Decision Point |
|------|-------------|--------|----------|----------------|
| **Direct** | Simple queries, single data source | 1 | No | Upfront |
| **Planning** | Complex tasks, known dependencies | Multiple | Yes | Upfront |
| **Dynamic Chaining** | Adaptive workflows, unknown dependencies | Multiple | No | After each scene |

## How It Works

```
┌─────────────────────────────────────────────────────────┐
│                   User Query                             │
│             "Analyze sales and create report"            │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Scene Selection #1                          │
│         LLM chooses: "SalesAnalysis"                     │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         Execute SalesAnalysis Scene                      │
│    • Call GetSalesData() → "$1.5M, +20%"               │
│    • Call AnalyzeTrends() → "Strong growth"            │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         LLM Decision: "Continue?"                        │
│         ✓ YES - Need to correlate with weather          │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         Scene Selection #2                               │
│         Available: WeatherData, ReportGenerator          │
│         (SalesAnalysis excluded - already executed)      │
│         LLM chooses: "WeatherData"                       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         Execute WeatherData Scene                        │
│    • Call GetWeather() → "Sunny periods in Q1"         │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         LLM Decision: "Continue?"                        │
│         ✓ YES - Ready to generate report                │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         Scene Selection #3                               │
│         Available: ReportGenerator                       │
│         LLM chooses: "ReportGenerator"                   │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         Execute ReportGenerator Scene                    │
│    • Call GenerateReport() → "## Q1 Report..."         │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│         LLM Decision: "Continue?"                        │
│         ✗ NO - Report complete                          │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Final Response                              │
│    Synthesizes all results:                             │
│    • Sales data + trends                                │
│    • Weather correlation                                │
│    • Formatted report                                   │
└─────────────────────────────────────────────────────────┘
```

## Key Features

### 1. Adaptive Execution
The LLM decides **after each scene** whether to continue, based on:
- Results from executed scenes
- Remaining available scenes
- Original user query

### 2. Context Accumulation
Each scene's results are accumulated and provided to subsequent scenes:

```csharp
context.SceneResults["SalesAnalysis"] = "Q1: $1.5M, +20%";
context.SceneResults["WeatherData"] = "Sunny periods in Q1";
context.SceneResults["ReportGenerator"] = "## Complete Report...";
```

### 3. Duplicate Prevention
The framework tracks:
- **Executed scenes**: Excluded from future selections
- **Tool calls**: `SceneName.ToolName(arguments)` to detect duplicates
- **Execution order**: For context building

### 4. Loop Prevention
- `MaxDynamicScenes`: Hard limit on scene executions (default: 5)
- Automatic stop when no more scenes available
- Budget limit enforcement (if enabled)

## Usage

### Basic Setup

```csharp
services.AddPlayFramework(chatClient, builder =>
{
    builder
        .AddScene("DataFetcher", scene =>
        {
            scene.AddTool<DataService>(nameof(DataService.FetchData));
        })
        .AddScene("Analyzer", scene =>
        {
            scene.AddTool<AnalysisService>(nameof(AnalysisService.Analyze));
        })
        .AddScene("ReportGenerator", scene =>
        {
            scene.AddTool<ReportService>(nameof(ReportService.Generate));
        });
});
```

### Enable Dynamic Chaining

```csharp
var settings = new SceneRequestSettings
{
    EnableDynamicSceneChaining = true,
    MaxDynamicScenes = 5  // Optional: default is 5
};

await foreach (var response in sceneManager.ExecuteAsync(
    "Fetch data, analyze it, and create a report",
    settings))
{
    Console.WriteLine($"[{response.Status}] {response.Message}");
}
```

### With Budget Limit

```csharp
var settings = new SceneRequestSettings
{
    EnableDynamicSceneChaining = true,
    MaxDynamicScenes = 5,
    MaxBudget = 0.50m  // Stop if cost exceeds $0.50
};
```

### With Streaming

```csharp
var settings = new SceneRequestSettings
{
    EnableDynamicSceneChaining = true,
    EnableStreaming = true  // Stream final responses
};
```

## Real-World Example

### E-Commerce Order Analysis

```csharp
services.AddPlayFramework(chatClient, builder =>
{
    builder
        .AddScene("OrderLookup", s => s
            .WithDescription("Look up customer orders")
            .AddTool<OrderService>(nameof(OrderService.GetOrder)))
        
        .AddScene("InventoryCheck", s => s
            .WithDescription("Check product inventory")
            .AddTool<InventoryService>(nameof(InventoryService.CheckStock)))
        
        .AddScene("ShippingStatus", s => s
            .WithDescription("Check shipping status")
            .AddTool<ShippingService>(nameof(ShippingService.GetStatus)))
        
        .AddScene("RefundProcessor", s => s
            .WithDescription("Process refunds")
            .AddTool<RefundService>(nameof(RefundService.ProcessRefund)));
});

// User query: "I want to check my order #12345 and return item XYZ"
var settings = new SceneRequestSettings
{
    EnableDynamicSceneChaining = true
};

await foreach (var response in sceneManager.ExecuteAsync(
    "Check order #12345 and return item XYZ",
    settings))
{
    // Dynamic flow:
    // 1. OrderLookup → Get order details
    // 2. LLM decides: "Need to check if item is eligible for return"
    // 3. InventoryCheck → Check item status
    // 4. LLM decides: "Item is eligible, proceed with refund"
    // 5. RefundProcessor → Process the refund
    // 6. LLM decides: "Done"
    
    Console.WriteLine(response.Message);
}
```

### Execution Flow:

```
User: "Check order #12345 and return item XYZ"

Scene 1: OrderLookup
  ├─ GetOrder(12345)
  └─ Result: "Order found. Item XYZ purchased on 2024-01-15. Status: Delivered"

LLM Decision: "YES - Need to verify return eligibility"

Scene 2: InventoryCheck
  ├─ CheckStock("XYZ")
  └─ Result: "Item XYZ: In stock, return window: 30 days"

LLM Decision: "YES - Eligible for return, process refund"

Scene 3: RefundProcessor
  ├─ ProcessRefund(12345, "XYZ")
  └─ Result: "Refund initiated: $49.99. Expected in 5-7 days"

LLM Decision: "NO - Task complete"

Final Response: "Your return for item XYZ from order #12345 has been processed. 
You will receive a refund of $49.99 in 5-7 business days."
```

## Scene Selection Intelligence

The LLM receives rich context for scene selection:

```
Previously executed scenes and their results:

Scene: SalesAnalysis
Result: Q1 sales were $1.5M, representing 20% growth from Q4.
        Peak sales occurred in March.

Executed tools:
- SalesAnalysis.GetSalesData(quarter=Q1)
- SalesAnalysis.AnalyzeTrends(no args)

Available scenes for further execution:
- WeatherData
- CompetitorAnalysis
- ReportGenerator

Do you need to execute another scene to complete the user's request?
```

The LLM can make informed decisions based on:
- **What has been done**: Executed scenes and tools
- **What was learned**: Results from each scene
- **What remains**: Available scenes (excluding executed ones)
- **Original goal**: User's query

## Benefits Over Static Planning

| Aspect | Static Planning | Dynamic Chaining |
|--------|----------------|------------------|
| **Flexibility** | Rigid - all scenes decided upfront | Adaptive - decisions based on results |
| **Efficiency** | May execute unnecessary scenes | Only executes needed scenes |
| **Context** | Limited inter-scene context | Rich accumulated context |
| **Discovery** | Cannot discover new needs | Can discover needs during execution |
| **Complexity** | Better for predictable workflows | Better for exploratory workflows |

### Example: Why Dynamic is Better

**User query**: "Analyze our Q1 performance"

**Static Planning** would decide upfront:
```
Plan:
  1. Get sales data
  2. Get expense data
  3. Get competitor data
  4. Generate report
```
→ Executes all 4 scenes even if unnecessary

**Dynamic Chaining** adapts:
```
Scene 1: Get sales data → "Sales were excellent, +50%"
LLM: "Excellent sales! Let me check expenses to see profitability"
Scene 2: Get expense data → "Expenses stable"
LLM: "Great! No need for competitor data. Generate report"
Scene 3: Generate report → "## Q1 Outstanding Performance..."
```
→ Skips competitor analysis (scene 3) because strong performance made it unnecessary

## Configuration Options

```csharp
public class SceneRequestSettings
{
    /// <summary>
    /// Enable dynamic scene chaining mode.
    /// </summary>
    public bool EnableDynamicSceneChaining { get; set; }

    /// <summary>
    /// Maximum number of scenes to execute in chain.
    /// Default: 5. Prevents infinite loops.
    /// </summary>
    public int MaxDynamicScenes { get; set; } = 5;

    /// <summary>
    /// Maximum budget in your configured currency.
    /// Stops execution if exceeded.
    /// </summary>
    public decimal? MaxBudget { get; set; }

    /// <summary>
    /// Enable streaming for text responses.
    /// </summary>
    public bool EnableStreaming { get; set; }
}
```

## Response Statuses

During dynamic chaining, you'll receive these statuses:

```csharp
AiResponseStatus.Initializing         // Starting up
AiResponseStatus.ExecutingMainActors  // Running main actors
AiResponseStatus.Running              // Scene selection
AiResponseStatus.ExecutingScene       // Entering a scene
AiResponseStatus.FunctionRequest      // Calling a tool
AiResponseStatus.FunctionCompleted    // Tool completed
AiResponseStatus.Running              // "Evaluating if more scenes needed"
AiResponseStatus.GeneratingFinalResponse  // Creating final answer
AiResponseStatus.Completed            // All done
```

## Tracking Execution

### Monitor Scene Chain

```csharp
var executedScenes = new List<string>();

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Status == AiResponseStatus.ExecutingScene)
    {
        executedScenes.Add(response.SceneName!);
        Console.WriteLine($"Executing scene #{executedScenes.Count}: {response.SceneName}");
    }
}

Console.WriteLine($"Total scenes executed: {executedScenes.Count}");
```

### Track Costs

```csharp
decimal totalCost = 0;

await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Cost.HasValue)
    {
        totalCost = response.TotalCost;
        Console.WriteLine($"Current cost: ${totalCost:F6}");
    }
}

Console.WriteLine($"Final cost: ${totalCost:F6}");
```

## Best Practices

### 1. Scene Descriptions Matter
The LLM uses scene descriptions to decide which to execute next:

```csharp
.AddScene("WeatherData", s => s
    .WithDescription("Retrieves weather data for specified dates and locations") // ✓ Good
    // .WithDescription("Weather") // ✗ Too vague
)
```

### 2. Set Reasonable Limits
```csharp
MaxDynamicScenes = 5  // ✓ Good - allows flexibility but prevents runaway
MaxDynamicScenes = 50 // ✗ Too high - risk of excessive costs
MaxDynamicScenes = 1  // ✗ Too low - defeats the purpose
```

### 3. Combine with Budget Limits
```csharp
var settings = new SceneRequestSettings
{
    EnableDynamicSceneChaining = true,
    MaxDynamicScenes = 10,
    MaxBudget = 1.00m  // Hard stop at $1.00
};
```

### 4. Use Meaningful Tool Names
The LLM sees executed tools when making decisions:

```csharp
public string GetSalesData()  // ✓ Clear
public string DoStuff()       // ✗ Unclear
```

## Comparison with Other Modes

### When to Use Each Mode

**Direct Execution** (`EnablePlanning = false`, `EnableDynamicSceneChaining = false`):
- Single data source needed
- Simple queries: "What's the weather?"
- Known scene upfront
- Fastest execution

**Planning Mode** (`EnablePlanning = true`):
- Multiple scenes required
- Dependencies known upfront
- Complex workflows: "Analyze sales, check inventory, generate forecast"
- Predictable execution path

**Dynamic Chaining** (`EnableDynamicSceneChaining = true`):
- Multiple scenes may be needed
- Dependencies discovered during execution
- Exploratory workflows: "Help me understand our Q1 performance"
- Adaptive execution path

## Performance Considerations

### LLM Calls per Scene

```
Direct Mode:
  1. Scene selection
  2. Tool calls (N iterations)
  3. Final response
  = 2 + N calls

Dynamic Chaining:
  1. Scene selection #1
  2. Tool calls scene #1 (N1 iterations)
  3. Continue decision
  4. Scene selection #2
  5. Tool calls scene #2 (N2 iterations)
  6. Continue decision
  ...
  7. Final response
  = (scenes × (2 + N)) + (scenes - 1) + 1 calls
```

### Cost Implications

3 scenes with 2 tool calls each:
```
Direct: 2 + 2 = 4 LLM calls
Planning: 1 + (3 × (1 + 2)) + 1 = 11 LLM calls
Dynamic: (3 × (1 + 2)) + 2 + 1 = 12 LLM calls
```

Dynamic chaining is slightly more expensive than planning due to continuation decisions, but can be more efficient than planning if it skips unnecessary scenes.

## Error Handling

### Budget Exceeded

```csharp
await foreach (var response in sceneManager.ExecuteAsync(query, settings))
{
    if (response.Status == AiResponseStatus.BudgetExceeded)
    {
        Console.WriteLine($"Stopped: Budget of ${settings.MaxBudget} exceeded");
        Console.WriteLine($"Scenes executed: {response.SceneName}");
        break;
    }
}
```

### Max Scenes Reached

```csharp
// The framework automatically stops at MaxDynamicScenes
// You'll see a status message:
"Maximum scene limit (5) reached"
```

## Testing

Example test demonstrating dynamic chaining:

```csharp
[Fact]
public async Task DynamicChaining_AdaptiveFlow_ExecutesOnlyNeededScenes()
{
    // Arrange
    var services = new ServiceCollection();
    
    services.AddPlayFramework(mockChatClient, builder =>
    {
        builder
            .AddScene("DataFetcher", s => s.AddTool<DataService>(nameof(DataService.Fetch)))
            .AddScene("Analyzer", s => s.AddTool<DataService>(nameof(DataService.Analyze)))
            .AddScene("Reporter", s => s.AddTool<ReportService>(nameof(ReportService.Generate)));
    });

    var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

    // Act
    var settings = new SceneRequestSettings
    {
        EnableDynamicSceneChaining = true,
        MaxDynamicScenes = 3
    };

    var results = new List<AiSceneResponse>();
    await foreach (var response in sceneManager.ExecuteAsync("Analyze data", settings))
    {
        results.Add(response);
    }

    // Assert
    var executedScenes = results
        .Where(r => r.Status == AiResponseStatus.ExecutingScene)
        .Select(r => r.SceneName)
        .ToList();

    Assert.True(executedScenes.Count > 1, "Should chain multiple scenes");
    Assert.True(executedScenes.Count <= 3, "Should respect MaxDynamicScenes");
}
```

## Summary

Dynamic Scene Chaining provides a **"live planning"** execution mode that:

✓ **Adapts** to results discovered during execution  
✓ **Accumulates** context across scenes  
✓ **Prevents** infinite loops and duplicates  
✓ **Optimizes** by skipping unnecessary scenes  
✓ **Combines** with budget limits and streaming  

It's the ideal choice for **exploratory workflows** where the path forward depends on what you learn along the way.
