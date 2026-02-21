# Tool Calling Implementation in PlayFramework

## Overview

PlayFramework now includes **full tool calling support**, enabling AI agents to dynamically call C# methods as functions during conversation flow. This implementation bridges the gap between Azure OpenAI's function calling capabilities and the PlayFramework's scene-based orchestration.

## Architecture

### Components

1. **AzureOpenAIChatClientAdapter** (`Test\Infrastructure\AzureOpenAIChatClientAdapter.cs`)
   - Bridges Azure.AI.OpenAI SDK with Microsoft.Extensions.AI.IChatClient
   - Converts AIFunction → ChatTool for Azure OpenAI
   - Parses ChatCompletion → FunctionCallContent with JSON argument deserialization
   - Handles both text responses and function calls in same response

2. **SceneManager Tool Execution** (`Services\SceneManager.cs`)
   - **RequestAsync**: Scene selection via function calling
   - **ExecuteSceneAsync**: Multi-turn conversation loop with tool execution

3. **ServiceMethodTool** (`Services\Tools\ServiceMethodTool.cs`)
   - Wraps C# methods as ISceneTool
   - Converts method signature → AIFunction using AIFunctionFactory
   - Executes methods via reflection with JSON argument deserialization

## How It Works

### 1. Tool Registration

Tools are registered via fluent API during scene configuration:

```csharp
services.AddPlayFramework(options => { })
    .AddScene("Calculator", "Performs mathematical calculations")
    .WithService<ICalculatorService>(tool =>
    {
        tool.WithMethod(x => x.Add(default, default), "Add", "Adds two numbers");
        tool.WithMethod(x => x.Multiply(default, default), "Multiply", "Multiplies two numbers");
    });
```

### 2. Scene Selection Flow (RequestAsync)

**Step 1**: User sends a request → "What is 15 + 27?"

**Step 2**: SceneManager calls LLM with all available scenes as tools:
```csharp
var sceneTools = _sceneFactory.GetSceneNames()
    .Select(name => _sceneFactory.Create(name))
    .Select(scene => CreateSceneSelectionTool(scene))
    .ToList();

var chatOptions = new ChatOptions { Tools = sceneTools.Cast<AITool>().ToList() };
var response = await context.ChatClient.GetResponseAsync(new[] { userMessage }, chatOptions, ct);
```

**Step 3**: LLM responds with FunctionCallContent indicating selected scene:
```json
{
  "callId": "call_123",
  "name": "Calculator",
  "arguments": {}
}
```

**Step 4**: SceneManager extracts scene name and executes the scene:
```csharp
if (content is FunctionCallContent functionCall)
{
    var selectedSceneName = functionCall.Name;
    var scene = FindSceneByFuzzyMatch(selectedSceneName);
    await foreach (var sceneResponse in ExecuteSceneAsync(context, scene, ct))
    {
        yield return sceneResponse;
    }
}
```

### 3. Tool Execution Loop (ExecuteSceneAsync)

**Step 1**: Scene actors are executed to provide dynamic context:
```csharp
await scene.ExecuteActorsAsync(context, cancellationToken);
```

**Step 2**: Scene tools are registered with ChatClient:
```csharp
var sceneTools = scene.GetTools().ToList();
var chatOptions = new ChatOptions 
{ 
    Tools = sceneTools.Select(t => t.ToAIFunction()).Cast<AITool>().ToList() 
};
```

**Step 3**: Multi-turn conversation loop (max 10 iterations):

```csharp
while (iteration < MaxToolCallIterations)
{
    // Call LLM
    var response = await context.ChatClient.GetResponseAsync(conversationMessages, chatOptions, ct);
    
    // Check for function calls
    var functionCalls = responseMessage.Contents?.OfType<FunctionCallContent>().ToList();
    
    if (functionCalls.Count == 0)
    {
        // No more function calls → return final text response
        yield return finalResponse;
        yield break;
    }
    
    // Execute each function call
    foreach (var functionCall in functionCalls)
    {
        var tool = sceneTools.FirstOrDefault(t => t.Name == functionCall.Name);
        
        // Serialize arguments
        var argsJson = JsonSerializer.Serialize(functionCall.Arguments);
        
        // Execute tool
        var toolResult = await tool.ExecuteAsync(argsJson, context, ct);
        
        // Send result back to LLM
        var functionResult = new FunctionResultContent(functionCall.CallId, functionCall.Name) 
        { 
            Result = toolResult 
        };
        conversationMessages.Add(new ChatMessage(ChatRole.Tool, [functionResult]));
    }
    
    // Loop continues → LLM processes tool results and may call more tools or return final answer
}
```

### 4. Example Flow: Calculator Request

**User Request**: "What is 15 + 27?"

**Execution Steps**:

1. **Scene Selection**:
   - LLM receives: User message + Scene tools (Calculator, Weather, Database, etc.)
   - LLM calls: `Calculator()` function
   - SceneManager executes: Calculator scene

2. **Tool Execution (Iteration 1)**:
   - LLM receives: User message + Calculator tools (Add, Subtract, Multiply, Divide)
   - LLM calls: `Add(a: 15, b: 27)`
   - ServiceMethodTool executes: `calculatorService.Add(15, 27)`
   - Tool returns: `"42"`
   - Function result sent back to LLM

3. **Final Response (Iteration 2)**:
   - LLM receives: Tool result "42"
   - LLM responds: "The result of 15 + 27 is 42."
   - No more function calls → Execution completes

**Response Stream**:
```
1. Status: ExecutingScene, Message: "Entering scene: Calculator"
2. Status: FunctionRequest, Message: "Executing tool: Add"
3. Status: FunctionCompleted, Message: "Tool Add executed: 42", FunctionName: "Add"
4. Status: Running, Message: "The result of 15 + 27 is 42."
5. Status: Completed
```

## Key Features

### ✅ Multi-Turn Tool Calling
The implementation supports multiple rounds of tool execution:
```
User: "Calculate (10 + 5) * 3"
→ LLM calls Add(10, 5) → Returns 15
→ LLM calls Multiply(15, 3) → Returns 45
→ LLM responds: "The result is 45"
```

### ✅ Error Handling
Robust error handling for tool execution failures:
```csharp
try
{
    var toolResult = await tool.ExecuteAsync(argsJson, context, ct);
    // Send success result
}
catch (Exception ex)
{
    var errorResult = new FunctionResultContent(callId, name) 
    { 
        Result = $"Error executing tool: {ex.Message}" 
    };
    // LLM receives error and can retry or respond with error message
}
```

### ✅ Tool Tracking
Prevents infinite loops and tracks executed tools:
```csharp
var toolKey = $"{scene.Name}.{functionCall.Name}";
context.ExecutedTools.Add(toolKey);
```

### ✅ Response Streaming
All tool execution steps are streamed via `IAsyncEnumerable<AiSceneResponse>`:
```csharp
await foreach (var response in sceneManager.ExecuteAsync(request, ct))
{
    Console.WriteLine($"[{response.Status}] {response.Message}");
    if (response.FunctionName != null)
        Console.WriteLine($"  Function: {response.FunctionName}");
}
```

## Testing

### Unit Tests (MockChatClient)
12 unit tests validate core functionality without real LLM:
```bash
dotnet test src\AI\Test\Rystem.PlayFramework.Test\Rystem.PlayFramework.Test.csproj
```

### Integration Tests (Azure OpenAI)
4 integration tests validate real Azure OpenAI tool calling:

1. **AzureOpenAI_ShouldConnect**: Validates Azure OpenAI connectivity
2. **PlayFramework_WithAzureOpenAI_ShouldExecuteCalculation**: Tests "15 + 27" with tool calling
3. **PlayFramework_WithAzureOpenAI_ShouldHandleMultipleOperations**: Tests "(10 + 5) * 3" with multiple tool calls
4. **PlayFramework_ShouldTrackCostsAccurately**: Validates token and cost tracking

To run integration tests:
1. Configure user secrets (see `README_TESTING.md`)
2. Remove `[Skip]` attributes from tests
3. Run: `dotnet test --filter "FullyQualifiedName~AzureOpenAIIntegrationTests"`

## Configuration

### User Secrets (for testing)
```json
{
  "OpenAi": {
    "ApiKey": "your-azure-openai-key",
    "EndpointBase": "your-resource-name",
    "ModelName": "gpt-4"
  }
}
```

### Production Configuration
```csharp
// Register Azure OpenAI Chat Client
services.AddSingleton<IChatClient>(sp => 
    new AzureOpenAIChatClientAdapter(
        endpoint: "https://your-resource.openai.azure.com/",
        apiKey: configuration["AzureOpenAI:ApiKey"],
        deploymentName: "gpt-4"));

// Register PlayFramework
services.AddPlayFramework(options => 
{
    options.Planning.EnablePlanning = true;
    options.Cache.EnableCaching = true;
})
.AddScene("Calculator", "Performs mathematical calculations")
    .WithService<ICalculatorService>(tool =>
    {
        tool.WithMethod(x => x.Add(default, default), "Add", "Adds two numbers");
        tool.WithMethod(x => x.Multiply(default, default), "Multiply", "Multiplies two numbers");
    });
```

## API Reference

### FunctionCallContent
Represents a function call from the LLM:
```csharp
public class FunctionCallContent : AIContent
{
    public string CallId { get; }           // Unique call identifier
    public string Name { get; }             // Function/tool name
    public IDictionary<string, object?> Arguments { get; }  // Parsed JSON arguments
}
```

### FunctionResultContent
Represents the result sent back to the LLM:
```csharp
public class FunctionResultContent : AIContent
{
    public FunctionResultContent(string callId, string name);
    public string CallId { get; }           // Matches FunctionCallContent.CallId
    public string Name { get; }             // Function/tool name
    public object? Result { get; set; }     // Serialized result (string, JSON, etc.)
}
```

### AiSceneResponse (Enhanced)
```csharp
public sealed class AiSceneResponse
{
    public AiResponseStatus Status { get; set; }        // Current status
    public string? SceneName { get; set; }              // Scene being executed
    public string? FunctionName { get; set; }           // Function/tool called
    public string? Message { get; set; }                // Response message
    public string? ErrorMessage { get; set; }           // Error details
    public int? InputTokens { get; set; }               // Tokens used
    public int? OutputTokens { get; set; }              // Tokens generated
    public decimal? Cost { get; set; }                  // Cost for this operation
    public decimal TotalCost { get; set; }              // Total accumulated cost
}
```

### AiResponseStatus (Relevant Values)
```csharp
public enum AiResponseStatus
{
    FunctionRequest,      // About to execute a tool
    FunctionCompleted,    // Tool execution succeeded
    Running,              // Normal execution (text response)
    Error,                // Error occurred
    Completed             // Execution finished
}
```

## Performance Considerations

### Token Optimization
- **Caching**: Enable caching to avoid re-executing identical tool calls
- **Summarization**: Enable summarization to compress long conversation histories
- **Max Iterations**: Default limit of 10 tool call iterations prevents runaway costs

### Cost Tracking
Every response includes token counts and estimated costs:
```csharp
response.InputTokens = completion.Usage.InputTokenCount;
response.OutputTokens = completion.Usage.OutputTokenCount;
response.Cost = CalculateCost(inputTokens, outputTokens, modelName);
response.TotalCost = context.TotalCost; // Accumulated across entire execution
```

## Troubleshooting

### Common Issues

**Issue**: Tool not found during execution
```
Status: Error, Message: "Tool 'Add' not found"
```
**Solution**: Ensure tool is registered in scene:
```csharp
.WithService<ICalculatorService>(tool =>
{
    tool.WithMethod(x => x.Add(default, default), "Add", "Adds two numbers");
});
```

**Issue**: Arguments deserialization fails
```
Status: Error, Message: "Error executing tool: Cannot deserialize arguments"
```
**Solution**: Ensure method parameters match function call arguments. Check argument types in function description.

**Issue**: Max iterations reached
```
Status: Error, Message: "Maximum tool call iterations (10) reached"
```
**Solution**: 
- Check if tools are returning useful results to the LLM
- Verify tool descriptions are clear and unambiguous
- Consider increasing MaxToolCallIterations if genuinely needed

**Issue**: Azure OpenAI returns error
```
Status: Error, Message: "Azure OpenAI API error: 401 Unauthorized"
```
**Solution**: 
- Verify API key is correct in user secrets or configuration
- Check endpoint URL format: `https://your-resource.openai.azure.com/`
- Ensure model deployment name matches configured value

## Future Enhancements

- [ ] **Parallel tool execution**: Execute multiple independent tool calls concurrently
- [ ] **Streaming tool results**: Stream partial tool results back to LLM
- [ ] **Tool call caching**: Cache tool results by arguments hash
- [ ] **Dynamic tool registration**: Allow tools to be added/removed during execution
- [ ] **MCP integration**: Support Model Context Protocol for external tool providers
- [ ] **Cost limits**: Abort execution when cost threshold exceeded
- [ ] **Rate limiting**: Throttle tool execution to prevent API overload

## References

- **Microsoft.Extensions.AI Documentation**: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai
- **Azure OpenAI Function Calling**: https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling
- **PlayFramework Testing Guide**: [README_TESTING.md](../Test/Rystem.PlayFramework.Test/README_TESTING.md)

---

**Implementation Date**: January 2026  
**Framework Version**: Rystem.PlayFramework 10.1.0.beta-1  
**Azure OpenAI SDK**: 2.1.0  
**Microsoft.Extensions.AI**: 10.3.0
