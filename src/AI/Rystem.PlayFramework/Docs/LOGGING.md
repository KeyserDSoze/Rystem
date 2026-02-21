# Logging in PlayFramework

PlayFramework uses **standard .NET `ILogger<T>`** with **structured logging** for observability and Application Insights integration.

## Key Features

✅ **Standard .NET Logging** - Uses `ILogger<SceneManager>` from `Microsoft.Extensions.Logging`  
✅ **Structured Logging** - All logs include `{FactoryName}` for filtering  
✅ **Application Insights Ready** - Works with any logging provider  
✅ **Single Logger** - One logger logs all factory instances (not factory-scoped)  
✅ **Comprehensive Logging** - Initialization, execution flow, LLM calls, tool execution, costs, errors  

## Setup

### 1. Configure Logging in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure logging (example with Application Insights)
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddApplicationInsights(); // Optional: for Azure Application Insights
    logging.SetMinimumLevel(LogLevel.Information); // Set minimum level globally
});

// Register PlayFramework
builder.Services.AddPlayFramework("premium", pf =>
{
    pf.AddScene(s => s
        .WithName("Weather")
        .WithDescription("Get weather information"));
});

builder.Services.AddPlayFramework("free", pf =>
{
    pf.AddScene(s => s
        .WithName("SimpleCalculator")
        .WithDescription("Basic calculations")));
```

### 2. Configure via `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Rystem.PlayFramework": "Debug"  // ← Set PlayFramework log level
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information",
        "Rystem.PlayFramework.SceneManager": "Trace"  // ← Detailed logs for SceneManager
      }
    }
  }
}
```

## Log Examples

### Initialization Logs

```
[Information] SceneManager initialized successfully - Scenes: 3, Planner: True, Summarizer: True, Director: False, Cache: True (Factory: premium)
```

**Structured properties** available in Application Insights:
- `FactoryName`: "premium"
- `SceneCount`: 3
- `HasPlanner`: true
- `HasSummarizer`: true
- `HasDirector`: false
- `HasCache`: true

### Execution Logs

```
[Information] === ExecuteAsync START === (Factory: premium)
[Information] User Message: 'What's the weather in Rome?' (Factory: premium)
[Debug] Execution mode selected: Direct (Factory: premium)
[Information] Executing 2 main actors... (Factory: premium)
[Information] Selected scene: Weather (Factory: premium)
[Debug] Calling LLM for scene: Weather (Factory: premium)
[Debug] LLM returned 1 tool call(s) (Factory: premium)
[Information] Executing tool: GetWeather with args: {"city":"Rome"} (Factory: premium)
[Information] Tool result: Temperature 25°C, Sunny (Factory: premium)
[Debug] Cost tracking - InputTokens: 150, OutputTokens: 50, Cost: 0.00045 USD (Factory: premium)
[Information] === ExecuteAsync COMPLETED === Total cost: 0.00045 USD (Factory: premium)
```

## Querying Logs in Application Insights

### Filter by Factory Name

```kusto
traces
| where customDimensions.FactoryName == "premium"
| where message contains "ExecuteAsync"
| order by timestamp desc
```

### Track Tool Executions

```kusto
traces
| where message contains "Executing tool"
| extend Tool = extract(@"tool: (\w+)", 1, message)
| extend Factory = tostring(customDimensions.FactoryName)
| summarize Count=count() by Tool, Factory
```

### Track Costs by Factory

```kusto
traces
| where message contains "Total cost"
| extend Factory = tostring(customDimensions.FactoryName)
| extend Cost = todouble(extract(@"cost: ([\d\.]+)", 1, message))
| summarize TotalCost=sum(Cost) by Factory
```

### Error Tracking

```kusto
traces
| where severityLevel >= 3  // Warning and above
| where customDimensions.FactoryName != ""
| extend Factory = tostring(customDimensions.FactoryName)
| project timestamp, severityLevel, message, Factory
| order by timestamp desc
```

## Log Levels

| Level | Usage |
|-------|-------|
| **Trace** | SceneFactory/ChatClient types, detailed initialization |
| **Debug** | Settings, actors count, execution mode, LLM calls, tool args |
| **Information** | Initialization complete, scene selection, tool execution, costs |
| **Warning** | Fallbacks, cache misses, budget warnings |
| **Error** | Exceptions, tool failures, missing services |
| **Critical** | Unrecoverable errors |

## Best Practices

### ✅ DO

```csharp
// Use structured logging with named parameters
_logger.LogInformation("Scene selected: {SceneName} for factory: {FactoryName}", 
    sceneName, _factoryName);

// Include factory name in all important logs
_logger.LogWarning("Cache miss for key: {CacheKey} (Factory: {FactoryName})", 
    cacheKey, _factoryName);
```

### ❌ DON'T

```csharp
// Don't use string interpolation (loses structured data)
_logger.LogInformation($"Scene selected: {sceneName}");

// Don't omit factory name from important logs
_logger.LogError("Tool execution failed");
```

## Integration with Serilog

```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/playframework-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), 
        TelemetryConverter.Traces));
```

## Why This Approach?

✅ **No custom abstractions** - Uses standard .NET logging  
✅ **Provider-agnostic** - Works with any `ILogger` provider  
✅ **Structured data** - `{FactoryName}` and other properties queryable in App Insights  
✅ **Centralized** - Single logger for all factories, no factory-scoped logger instances  
✅ **Observable** - Full visibility into execution flow, costs, and errors  

## Next Steps

- [Budget Tracking](BUDGET_LIMIT.md) - Track costs with structured logging
- [Cost Calculation](../Services/Cost/CostCalculator.cs) - See cost tracking implementation
- [Factory Pattern](FACTORY_PATTERN.md) - Understand multi-configuration setups
