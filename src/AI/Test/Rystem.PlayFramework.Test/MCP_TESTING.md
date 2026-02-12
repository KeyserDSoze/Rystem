# MCP (Model Context Protocol) Integration Tests

This document describes the MCP integration testing strategy and mock infrastructure.

## Overview

MCP integration is tested using a **MockMcpClient** that simulates an MCP server without requiring real HTTP calls. This approach provides:

- ✅ **Fast execution** - No network latency
- ✅ **Reliable results** - No external dependencies
- ✅ **Reproducible tests** - Consistent test data
- ✅ **Easy debugging** - Full control over responses

## Test Infrastructure

### MockMcpClient

Located at: `src/AI/Test/Rystem.PlayFramework.Test/Infrastructure/MockMcpClient.cs`

The `MockMcpClient` implements `IMcpClient` and provides:

```csharp
public sealed class MockMcpClient : IMcpClient
{
    // Load tools, resources, prompts
    Task<List<McpTool>> GetToolsAsync(...);
    Task<List<McpResource>> GetResourcesAsync(...);
    Task<List<McpPrompt>> GetPromptsAsync(...);
    
    // Execute tools
    Task<string> ExecuteToolAsync(...);
    Task<string> ReadResourceAsync(...);
    
    // Test setup helpers
    MockMcpClient AddTool(...);
    MockMcpClient AddResource(...);
    MockMcpClient AddPrompt(...);
    MockMcpClient WithToolExecutor(...);
}
```

### Default Test Data

The mock client comes pre-configured with:

**Tools:**
- `get_weather` - Get current weather for a location
- `search_database` - Search database for information
- `calculate_sum` - Calculate sum of two numbers

**Resources:**
- `company_policy` - Company policy documentation
- `api_documentation` - API reference documentation

**Prompts:**
- `code_review` - Template for code review
- `summarize_document` - Template for document summarization

## Test Categories

### 1. Basic MCP Server Tests

Test that MCP server manager correctly loads capabilities:

```csharp
[Fact]
public async Task McpServer_ShouldLoadTools()
{
    var mockMcpClient = new MockMcpClient();
    services.AddSingleton<IMcpClient>(mockMcpClient);
    services.AddMcpServer("https://test-mcp-server.local", "TestMcpServer");
    
    var manager = factory.Create("TestMcpServer");
    var tools = await manager.GetToolsAsync();
    
    Assert.Contains(tools, t => t.Name == "get_weather");
}
```

**Covered scenarios:**
- ✅ Loading tools from MCP server
- ✅ Loading resources from MCP server
- ✅ Loading prompts from MCP server
- ✅ Executing tools
- ✅ Reading resource content

### 2. Filtering Tests

Test that MCP filtering works correctly:

```csharp
[Fact]
public async Task McpServer_ShouldFilterToolsByName()
{
    var filter = new McpFilterSettings
    {
        Tools = new List<string> { "get_weather", "calculate_sum" }
    };
    
    var tools = await manager.GetToolsAsync(filter);
    
    Assert.Equal(2, tools.Count);
}
```

**Covered scenarios:**
- ✅ Filter tools by exact name match (case-insensitive)
- ✅ Filter tools by regex pattern
- ✅ Filter resources by name/regex
- ✅ Filter prompts by name/regex
- ✅ Filter system message content

### 3. Scene Integration Tests

Test that scenes correctly integrate MCP tools:

```csharp
[Fact]
public async Task Scene_WithMcpServer_ShouldLoadMcpTools()
{
    services.AddMcpServer("https://test-mcp-server.local", "TestMcpServer");
    
    services.AddPlayFramework(builder =>
    {
        builder.AddScene(scene => scene
            .WithName("TestScene")
            .WithMcpServer("TestMcpServer", filter =>
            {
                filter.Tools = new List<string> { "get_weather" };
            }));
    });
    
    await foreach (var response in sceneManager.ExecuteAsync("What is the weather?"))
    {
        // MCP tools should be available to LLM
    }
}
```

**Covered scenarios:**
- ✅ Scene loads MCP tools into ChatOptions.Tools
- ✅ Scene includes MCP resources/prompts in system message
- ✅ Multiple scenes with different MCP servers remain isolated

### 4. Multi-Server Tests

Test that multiple MCP servers work independently:

```csharp
[Fact]
public async Task MultipleScenes_WithDifferentMcpServers_ShouldIsolateTools()
{
    services.AddMcpServer("https://server-a.local", "ServerA");
    services.AddMcpServer("https://server-b.local", "ServerB");
    
    builder.AddScene(scene => scene
        .WithName("SceneA")
        .WithMcpServer("ServerA"));
        
    builder.AddScene(scene => scene
        .WithName("SceneB")
        .WithMcpServer("ServerB"));
    
    // Each scene only sees its own MCP server's tools
}
```

**Covered scenarios:**
- ✅ Multiple MCP servers registered with different factory names
- ✅ Each scene only accesses its configured MCP server
- ✅ Tool isolation between servers

## Creating Custom Test Data

### Add Custom Tools

```csharp
var mockMcpClient = new MockMcpClient()
    .ClearAll() // Remove default data
    .AddTool(
        name: "my_custom_tool",
        description: "Does something custom",
        serverUrl: "https://my-server.local",
        factoryName: "MyServer",
        inputSchema: """{"type": "object", "properties": {"param": {"type": "string"}}}""");
```

### Add Custom Tool Executor

```csharp
mockMcpClient.WithToolExecutor("my_custom_tool", async args =>
{
    // Parse args and execute custom logic
    var parsedArgs = JsonSerializer.Deserialize<Dictionary<string, object>>(args);
    return JsonSerializer.Serialize(new { result = "custom result" });
});
```

### Add Custom Resources

```csharp
mockMcpClient.AddResource(
    name: "custom_doc",
    description: "Custom documentation",
    uri: "file:///docs/custom.md",
    serverUrl: "https://my-server.local",
    factoryName: "MyServer",
    content: "# Custom Documentation\n\nContent here...");
```

### Add Custom Prompts

```csharp
mockMcpClient.AddPrompt(
    name: "custom_prompt",
    description: "Custom prompt template",
    template: "Analyze the following: {input}",
    serverUrl: "https://my-server.local",
    factoryName: "MyServer",
    parameters: new List<string> { "input" });
```

## Running Tests

```bash
# Run all MCP tests
dotnet test --filter "FullyQualifiedName~McpIntegrationTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~McpIntegrationTests.McpServer_ShouldLoadTools"
```

## Future Enhancements

### Real HTTP Server Tests (Optional)

For more realistic integration testing, we could add:

```csharp
// In-memory ASP.NET Core MCP server for tests
public class RealMcpServerTests
{
    [Fact]
    public async Task RealMcpServer_ShouldWork()
    {
        using var server = new TestMcpServer(); // Starts HTTP server
        
        services.AddMcpServer(server.Url, "RealServer");
        
        // Test with real HTTP calls
    }
}
```

This would be useful for:
- Testing HTTP client error handling
- Testing timeout behavior
- Testing authentication headers
- Testing real network conditions

### Load Testing

For performance testing:

```csharp
[Fact]
public async Task McpServer_ShouldHandleHighLoad()
{
    var tasks = Enumerable.Range(0, 1000)
        .Select(i => manager.GetToolsAsync())
        .ToList();
        
    await Task.WhenAll(tasks);
    
    // Verify no failures under load
}
```

## Best Practices

1. **Use MockMcpClient for unit tests** - Fast and reliable
2. **Customize test data per test** - Clear what is being tested
3. **Test filtering thoroughly** - Both exact match and regex
4. **Test scene integration** - Verify tools reach ChatOptions
5. **Test isolation** - Multiple MCP servers don't interfere
6. **Add real HTTP tests later** - Only if needed for specific scenarios

## Troubleshooting

### Test fails with "McpServerManager not configured"

Make sure you register the MCP server before creating the manager:

```csharp
services.AddMcpServer("https://test-mcp-server.local", "TestMcpServer");
var manager = factory.Create("TestMcpServer");
```

### Tools not appearing in scene execution

Check that:
1. MCP server is registered: `services.AddMcpServer(...)`
2. Scene references MCP server: `.WithMcpServer("TestMcpServer")`
3. MockMcpClient has tools for that server URL and factory name

### Filter not working

Verify:
- Exact name match is **case-insensitive**
- Regex pattern is valid .NET regex
- Filter is passed to `GetToolsAsync(filter)`

## Related Documentation

- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [PlayFramework MCP Integration](../src/AI/Rystem.PlayFramework/MCP_INTEGRATION.md)
- [Factory Pattern](../src/AI/Rystem.PlayFramework/FACTORY_PATTERN.md)
