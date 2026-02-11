# Multi-Scene Testing Guide

## Overview

`MultiSceneTests.cs` demonstrates the PlayFramework's advanced capabilities for orchestrating complex workflows involving multiple scenes, dynamic scene selection, planning, and tool calling.

## Test Architecture

### Configured Scenes

The test suite configures **4 different scenes**, each with specialized capabilities:

#### 1. **Calculator Scene**
- **Purpose**: Mathematical operations
- **Tools**: `add`, `subtract`, `multiply`, `divide`
- **Use Case**: Precise numeric calculations

#### 2. **Weather Scene**
- **Purpose**: Weather information retrieval
- **Tools**: 
  - `getCurrentWeather` - Get current conditions
  - `getTemperature` - Get temperature in Celsius
  - `getForecast` - Get N-day forecast
- **Use Case**: Weather data queries

#### 3. **DataAnalysis Scene**
- **Purpose**: Statistical analysis
- **Tools**:
  - `calculateAverage` - Average of number array
  - `findMinimum` - Minimum value
  - `findMaximum` - Maximum value
  - `calculateSum` - Sum of values
- **Use Case**: Data processing and statistics

#### 4. **ReportGenerator Scene**
- **Purpose**: Document generation
- **Tools**:
  - `generateSummary` - Text summarization
  - `formatAsTable` - Markdown table generation
  - `createMarkdownReport` - Full report creation
- **Use Case**: Report formatting and documentation

## Test Categories

### ✅ Unit Tests (Passing - 4 tests)

These tests validate core functionality without requiring a real LLM:

1. **`SceneManager_ShouldRegisterAllScenes`**
   - Verifies all 4 scenes are registered
   - Tests scene factory configuration

2. **`SceneFactory_ShouldCreateAllScenesWithTools`**
   - Validates each scene has correct number of tools
   - Ensures tool registration works properly

3. **`WeatherService_ShouldReturnMockData`**
   - Tests mock weather service functionality
   - Validates temperature data for multiple cities

4. **`DataAnalysisService_ShouldCalculateStatistics`**
   - Tests statistical calculations (average, min, max, sum)
   - Validates data analysis logic

### 🔄 Integration Tests (Skipped - 6 tests)

These tests require Azure OpenAI and demonstrate real multi-scene orchestration:

#### Test 1: Multi-Scene with Planning
**`MultiScene_WithPlanning_ShouldCalculateAndAnalyze`**

```csharp
"Calculate these values: 10+5, 20+8, 30+12. Then find the average of the three results."
```

**Expected Flow:**
1. ✅ Planning phase creates execution plan
2. ✅ Calculator scene executes: `10+5=15`, `20+8=28`, `30+12=42`
3. ✅ DataAnalysis scene calculates average: `(15+28+42)/3 = 28.33`
4. ✅ Final response includes the average

**Demonstrates:**
- Multi-scene workflow orchestration
- Planning system usage
- Context propagation between scenes
- Tool calling within scenes

---

#### Test 2: Dynamic Scene Selection
**`MultiScene_WithDynamicSelection_ShouldChooseWeatherScene`**

```csharp
"What's the temperature in Paris?"
```

**Expected Flow:**
1. ✅ LLM analyzes request
2. ✅ Dynamically selects Weather scene
3. ✅ Calls `getTemperature` tool with "Paris"
4. ✅ Returns temperature result

**Demonstrates:**
- Automatic scene selection
- Single-scene execution
- Tool calling within selected scene

---

#### Test 3: Complex Workflow
**`MultiScene_ComplexWorkflow_WeatherThenCalculate`**

```csharp
"Get the temperatures for Rome, Milan, and Naples. Calculate the average temperature and create a summary report."
```

**Expected Flow:**
1. ✅ Weather scene: Get temperatures for 3 cities
2. ✅ DataAnalysis scene: Calculate average
3. ✅ ReportGenerator scene: Create summary report
4. ✅ At least 2 different scenes executed

**Demonstrates:**
- Complex multi-step workflow
- Multiple scene orchestration
- Data flow between scenes
- Report generation from analyzed data

---

#### Test 4: Multiple Tool Calls
**`MultiScene_WithToolCalling_ShouldExecuteMultipleTools`**

```csharp
"Calculate: (15 + 25) * 2 - 10"
```

**Expected Flow:**
1. ✅ Call `add(15, 25)` → 40
2. ✅ Call `multiply(40, 2)` → 80
3. ✅ Call `subtract(80, 10)` → 70
4. ✅ Final answer: 70

**Demonstrates:**
- Multiple tool calls in sequence
- Tool result propagation
- Complex mathematical expressions
- Multi-turn conversation

---

#### Test 5: Planning Verification
**`MultiScene_PlanningEnabled_ShouldCreateExecutionPlan`**

```csharp
"Get weather for London, then calculate if the temperature is above 20 degrees."
```

**Expected Flow:**
1. ✅ Planning phase activated
2. ✅ Execution plan created
3. ✅ Weather scene executed
4. ✅ Calculator scene for comparison

**Demonstrates:**
- Planning system activation
- Conditional logic in plans
- Multi-scene coordination via planner

---

#### Test 6: Response Stream Details
**`MultiScene_ResponseStream_ShouldProvideDetailedStatus`**

```csharp
"Calculate 100 / 4"
```

**Expected Status Sequence:**
1. ✅ `Initializing` - Context setup
2. ✅ `ExecutingScene` - Scene entry
3. ✅ `FunctionRequest` - About to call tool
4. ✅ `FunctionCompleted` - Tool executed
5. ✅ `Completed` - Execution finished

**Demonstrates:**
- Response streaming
- Status tracking
- Detailed execution visibility

## Running the Tests

### Run Unit Tests Only
```bash
dotnet test "src\AI\Test\Rystem.PlayFramework.Test\Rystem.PlayFramework.Test.csproj" \
  --filter "FullyQualifiedName~MultiSceneTests&FullyQualifiedName!~Skip"
```

### Run All Tests (Including Integration)
1. Configure Azure OpenAI credentials in user secrets
2. Remove `[Skip]` attributes from integration tests
3. Run:
```bash
dotnet test "src\AI\Test\Rystem.PlayFramework.Test\Rystem.PlayFramework.Test.csproj" \
  --filter "FullyQualifiedName~MultiSceneTests"
```

### Run Specific Integration Test
```bash
dotnet test --filter "FullyQualifiedName~MultiScene_WithPlanning_ShouldCalculateAndAnalyze"
```

## Mock Services Implementation

### MockWeatherService
Simulates weather API with predefined data for 10 cities:
- Milan: 18.5°C
- Rome: 22.3°C
- Paris: 15.2°C
- London: 12.8°C
- *etc.*

### MockDataAnalysisService
Implements standard statistical functions:
- Average, Min, Max, Sum
- Handles empty array validation

### MockReportService
Generates formatted outputs:
- Text summarization (first 20 words)
- Markdown table formatting
- Complete markdown reports with timestamps

## Configuration Details

### Planning Configuration
```csharp
.Configure(settings =>
{
    settings.Planning.Enabled = true; // Enable planning for multi-scene
    settings.Summarization.Enabled = false;
})
```

### Main Actor Context
The main actor provides high-level guidance:
```
"You are an intelligent assistant that can:
- Perform mathematical calculations
- Get weather information
- Analyze data
- Generate reports
When a task requires multiple operations, plan the execution carefully."
```

This helps the LLM understand available capabilities and make intelligent scene selection decisions.

## Expected Costs (Integration Tests)

Each integration test makes multiple LLM calls:

| Test | Estimated Calls | Estimated Cost* |
|------|----------------|----------------|
| WithPlanning | 3-5 | $0.01-0.02 |
| DynamicSelection | 2-3 | $0.005-0.01 |
| ComplexWorkflow | 4-6 | $0.02-0.03 |
| WithToolCalling | 3-5 | $0.01-0.02 |
| PlanningEnabled | 2-4 | $0.01-0.015 |
| ResponseStream | 2-3 | $0.005-0.01 |

*Based on GPT-4 pricing, actual costs may vary

## Common Issues

### Issue: Scene Not Found
**Symptom:**
```
Status: Error, Message: "Scene 'Calculator' not found"
```
**Solution:** Ensure scene name matches exactly (case-insensitive matching is supported)

### Issue: Tool Not Found
**Symptom:**
```
Status: Error, Message: "Tool 'add' not found"
```
**Solution:** Verify tool is registered with `.WithMethod()` in scene configuration

### Issue: Planning Not Activated
**Symptom:** Tests pass but planning status never appears
**Solution:** 
- Enable planning: `settings.Planning.Enabled = true`
- Use prompts that require multiple scenes
- Check planner is registered: `.WithPlanning()`

### Issue: Mock Data vs Real LLM
**Symptom:** Test expects real LLM behavior but gets mock responses
**Solution:** Remove `[Skip]` attribute and configure Azure OpenAI credentials

## Advanced Scenarios

### Custom Scene with Database Access
```csharp
.AddScene(sceneBuilder =>
{
    sceneBuilder
        .WithName("Database")
        .WithDescription("Query and update database records")
        .WithService<IDatabaseService>(serviceBuilder =>
        {
            serviceBuilder
                .WithMethod(x => x.QueryAsync(default!), "query", "Execute SQL query")
                .WithMethod(x => x.InsertAsync(default!), "insert", "Insert record");
        });
})
```

### Scene with Async Dynamic Actors
```csharp
.WithActors(actorBuilder =>
{
    actorBuilder.AddAsyncActor(async (context, ct) =>
    {
        var dbService = context.ServiceProvider.GetRequiredService<IDatabaseService>();
        var recentData = await dbService.GetRecentDataAsync();
        return $"Recent data context: {recentData}";
    });
})
```

### Conditional Scene Execution
The planner can create conditional flows:
```
User: "If temperature in London > 15°C, calculate days until vacation, else suggest indoor activities"

Plan:
1. Weather.getTemperature(London)
2. IF temp > 15: Calculator scene
   ELSE: ReportGenerator.generateSummary(indoor activities)
```

## Performance Tips

1. **Disable Planning for Simple Tasks**: Single-scene tasks don't need planning overhead
2. **Use Caching**: Enable caching for repeated queries
3. **Limit Scene Count**: More scenes = longer planning time
4. **Clear Scene Descriptions**: Better descriptions = better scene selection

## Next Steps

- [ ] Add streaming response tests for long-running operations
- [ ] Test error recovery across multiple scenes
- [ ] Implement scene timeout mechanisms
- [ ] Add performance benchmarks for multi-scene workflows
- [ ] Test concurrent scene execution (when implemented)

---

**Related Documentation:**
- [Main Testing Guide](README_TESTING.md)
- [Tool Calling Guide](../../Rystem.PlayFramework/TOOL_CALLING.md)
- [PlayFramework Configuration](../../Rystem.PlayFramework/Configuration/PlayFrameworkSettings.cs)
