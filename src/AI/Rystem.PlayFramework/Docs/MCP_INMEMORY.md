# In-Memory MCP Server - Testing and Development

The **InMemoryMcpServer** provides a complete Model Context Protocol (MCP) implementation without external dependencies, perfect for testing and local development.

## Table of Contents
- [Overview](#overview)
- [Quick Start](#quick-start)
- [Creating Custom Servers](#creating-custom-servers)
- [Integration with PlayFramework](#integration-with-playframework)
- [Testing Patterns](#testing-patterns)
- [API Reference](#api-reference)

---

## Overview

InMemoryMcpServer is an in-memory implementation of the MCP protocol that:

✅ **No External Dependencies** - Runs entirely in-memory  
✅ **Isolated Instances** - Each factory name gets its own isolated server  
✅ **Fluent API** - Easy configuration with method chaining  
✅ **Custom Executors** - Define tool and resource behavior with delegates  
✅ **Default Content** - Comes with pre-configured tools, resources, and prompts  

---

## Quick Start

### 1. Register with Dependency Injection

```csharp
services.AddInMemoryMcpServer("TestServer", server =>
{
    // Use default content (weather, database, calculator tools)
    // server is pre-configured with 3 tools, 2 resources, 2 prompts
});
```

### 2. Use in PlayFramework Scene

```csharp
services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("TestScene")
        .WithDescription("Scene using in-memory MCP")
        .WithMcpServer("TestServer"));
});
```

### 3. Execute Scene

```csharp
var sceneManager = serviceProvider
    .GetRequiredService<IFactory<ISceneManager>>()
    .Create(null);

await foreach (var response in sceneManager.ExecuteAsync("What is the weather in Rome?"))
{
    Console.WriteLine(response.Message);
}
```

---

## Creating Custom Servers

### Empty Server with Custom Tools

```csharp
services.AddInMemoryMcpServer("CustomServer", server =>
{
    // Clear default content
    server.Clear();

    // Add custom tool
    server.AddTool(
        name: "calculate_taxes",
        description: "Calculate taxes for a given amount",
        inputSchema: @"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" },
                ""rate"": { ""type"": ""number"" }
            }
        }",
        executor: async (argsJson) =>
        {
            // Parse JSON arguments
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argsJson);
            var amount = args["amount"].GetDouble();
            var rate = args["rate"].GetDouble();
            
            var tax = amount * (rate / 100);
            return await Task.FromResult($"Tax: ${tax:F2}");
        });
});
```

### Server with Custom Resources

```csharp
services.AddInMemoryMcpServer("DocsServer", server =>
{
    server.Clear();

    // Static resource (content provided immediately)
    server.AddResource(
        name: "user_manual",
        uri: "internal://docs/manual",
        description: "User manual documentation",
        content: "# User Manual\n\n## Getting Started\n..."
    );

    // Dynamic resource (loaded on demand)
    server.AddResource(
        name: "api_spec",
        uri: "internal://docs/api",
        description: "REST API specification",
        loader: async () =>
        {
            // Simulate loading from database or file system
            await Task.Delay(10);
            return "# API Specification\n\n...";
        }
    );
});
```

### Server with Prompt Templates

```csharp
services.AddInMemoryMcpServer("TemplateServer", server =>
{
    server.Clear();

    server.AddPrompt(
        name: "bug_report",
        description: "Generate a bug report template",
        template: @"
## Bug Report

**Title**: {title}
**Severity**: {severity}
**Affected Version**: {version}

### Description
{description}

### Steps to Reproduce
{steps}

### Expected Behavior
{expected}

### Actual Behavior
{actual}
",
        parameters: new List<string> 
        { 
            "title", "severity", "version", 
            "description", "steps", "expected", "actual" 
        }
    );
});
```

---

## Integration with PlayFramework

### Basic Integration

```csharp
// 1. Register in-memory MCP server
services.AddInMemoryMcpServer("WeatherServer", server =>
{
    server.Clear()
        .AddTool("get_forecast", "Get weather forecast",
            executor: async _ => await Task.FromResult("Sunny, 25°C"));
});

// 2. Add PlayFramework scene using the server
services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("WeatherScene")
        .WithMcpServer("WeatherServer"));
});

// 3. Execute
var sceneManager = serviceProvider
    .GetRequiredService<IFactory<ISceneManager>>()
    .Create(null);

await foreach (var response in sceneManager.ExecuteAsync("What's the weather?"))
{
    Console.WriteLine(response.Message);
}
```

### Multiple Isolated Servers

```csharp
// Each server has its own isolated tool set
services.AddInMemoryMcpServer("ServerA", server =>
{
    server.Clear()
        .AddTool("tool_a", "Tool from server A");
});

services.AddInMemoryMcpServer("ServerB", server =>
{
    server.Clear()
        .AddTool("tool_b", "Tool from server B");
});

// Scenes use different servers
services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("SceneA")
        .WithMcpServer("ServerA"));  // Only has tool_a

    builder.AddScene(scene => scene
        .WithName("SceneB")
        .WithMcpServer("ServerB"));  // Only has tool_b
});
```

### With Filtering

```csharp
services.AddInMemoryMcpServer("AllToolsServer");  // Uses defaults

services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("WeatherOnlyScene")
        .WithMcpServer("AllToolsServer", filter =>
        {
            // Only include weather-related tools
            filter.Tools = new List<string> { "get_weather" };
        }));

    builder.AddScene(scene => scene
        .WithName("DatabaseOnlyScene")
        .WithMcpServer("AllToolsServer", filter =>
        {
            // Only include database tools
            filter.ToolsRegex = ".*database.*";
        }));
});
```

---

## Testing Patterns

### Unit Test Example

```csharp
[Fact]
public async Task InMemoryMcpServer_ShouldExecuteCustomTool()
{
    // Arrange
    var server = new InMemoryMcpServer();
    var executorCalled = false;

    server.AddTool(
        name: "test_tool",
        description: "Test tool",
        executor: async (args) =>
        {
            executorCalled = true;
            return await Task.FromResult("Success");
        });

    var settings = new McpServerSettings 
    { 
        Url = "test", 
        Name = "test" 
    };

    // Act
    var result = await server.ExecuteToolAsync(settings, "test_tool", "{}");

    // Assert
    Assert.True(executorCalled);
    Assert.Equal("Success", result);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task Scene_WithInMemoryMcp_ShouldUseTools()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging();

    // Mock chat client (required by PlayFramework)
    var mockChatClient = new MockChatClient("Test response");
    services.AddSingleton<IChatClient>(mockChatClient);

    // In-memory MCP server
    services.AddInMemoryMcpServer("TestServer", server =>
    {
        server.Clear()
            .AddTool("custom_tool", "Custom test tool",
                executor: async _ => await Task.FromResult("Tool result"));
    });

    // PlayFramework with scene
    services.AddPlayFramework(builder =>
    {
        builder.AddScene(scene => scene
            .WithName("TestScene")
            .WithMcpServer("TestServer"));
    });

    var provider = services.BuildServiceProvider();
    var sceneManager = provider
        .GetRequiredService<IFactory<ISceneManager>>()
        .Create(null);

    // Act
    var responses = new List<AiSceneResponse>();
    await foreach (var response in sceneManager.ExecuteAsync("Use custom tool"))
    {
        responses.Add(response);
    }

    // Assert
    Assert.NotEmpty(responses);
    var completed = responses.FirstOrDefault(r => r.Status == AiResponseStatus.Completed);
    Assert.NotNull(completed);
}
```

---

## API Reference

### InMemoryMcpServer Class

#### Constructor

```csharp
var server = new InMemoryMcpServer();
```

#### Properties

```csharp
public string Name { get; set; } = "InMemoryMcpServer";
public string Version { get; set; } = "1.0.0";
```

#### Methods

##### AddTool
```csharp
InMemoryMcpServer AddTool(
    string name,
    string description,
    string? inputSchema = null,
    Func<string, Task<string>>? executor = null)
```

**Parameters:**
- `name`: Unique tool identifier
- `description`: Human-readable description
- `inputSchema`: JSON Schema for tool parameters (optional)
- `executor`: Async function to execute the tool (optional)

**Returns:** `InMemoryMcpServer` for method chaining

##### AddResource
```csharp
InMemoryMcpServer AddResource(
    string name,
    string uri,
    string description,
    string? content = null,
    Func<Task<string>>? loader = null)
```

**Parameters:**
- `name`: Unique resource identifier
- `uri`: Resource URI (e.g., `"internal://docs/policy"`)
- `description`: Human-readable description
- `content`: Static content (optional)
- `loader`: Async function to load content on demand (optional)

**Returns:** `InMemoryMcpServer` for method chaining

##### AddPrompt
```csharp
InMemoryMcpServer AddPrompt(
    string name,
    string description,
    string template,
    List<string>? parameters = null)
```

**Parameters:**
- `name`: Unique prompt identifier
- `description`: Human-readable description
- `template`: Prompt template string (use `{param}` for placeholders)
- `parameters`: List of parameter names

**Returns:** `InMemoryMcpServer` for method chaining

##### Clear
```csharp
InMemoryMcpServer Clear()
```

Removes all tools, resources, and prompts from the server.

**Returns:** `InMemoryMcpServer` for method chaining

##### CreateDefault (static)
```csharp
static InMemoryMcpServer CreateDefault()
```

Creates a new server with pre-configured default content:
- **Tools**: get_weather, search_database, calculate_sum
- **Resources**: company_policy, api_documentation
- **Prompts**: code_review, summarize_document

**Returns:** New `InMemoryMcpServer` instance with default content

---

### Extension Method

##### AddInMemoryMcpServer
```csharp
IServiceCollection AddInMemoryMcpServer(
    this IServiceCollection services,
    AnyOf<string?, Enum> factoryName,
    Action<InMemoryMcpServer>? configure = null)
```

**Parameters:**
- `services`: Service collection
- `factoryName`: Factory name to identify this server
- `configure`: Configuration action (optional)

**Returns:** `IServiceCollection` for chaining

**Example:**
```csharp
services.AddInMemoryMcpServer("MyServer", server =>
{
    server.Clear()
        .AddTool("my_tool", "My custom tool")
        .AddResource("my_resource", "internal://docs", "My docs");
});
```

---

## Default Content

When using `CreateDefault()` or not clearing the server, you get:

### Tools
| Name | Description | Input Schema |
|------|-------------|--------------|
| `get_weather` | Get current weather for a location | `{ location: string }` |
| `search_database` | Search in the knowledge database | `{ query: string }` |
| `calculate_sum` | Calculate the sum of numbers | `{ numbers: array }` |

### Resources
| Name | URI | Description |
|------|-----|-------------|
| `company_policy` | `internal://docs/policy` | Company policies and guidelines |
| `api_documentation` | `internal://docs/api` | REST API documentation (loaded dynamically) |

### Prompts
| Name | Description | Parameters |
|------|-------------|------------|
| `code_review` | Prompt template for code review | `code`, `focus_areas` |
| `summarize_document` | Summarize a document | `document`, `max_words` |

---

## Best Practices

### ✅ DO

- **Clear for Tests**: Use `.Clear()` when creating custom test scenarios
- **Use Executors**: Define executors for tools to control test behavior
- **Isolate Instances**: Use unique factory names for isolated servers
- **Test Tools First**: Test tool executors independently before integration
- **Mock ChatClient**: Always provide a mock IChatClient for PlayFramework tests

### ❌ DON'T

- **Don't Share Servers**: Don't try to reuse the same server instance across tests
- **Don't Forget Async**: Tool executors and resource loaders must return `Task<string>`
- **Don't Skip Validation**: Always validate that tools/resources are actually loaded
- **Don't Use in Production**: InMemoryMcpServer is for testing only - use real MCP servers for production

---

## Examples

### Example 1: Weather Service Mock

```csharp
services.AddInMemoryMcpServer("WeatherMock", server =>
{
    server.Clear();

    var weatherData = new Dictionary<string, string>
    {
        ["Rome"] = "Sunny, 22°C",
        ["London"] = "Rainy, 15°C",
        ["Tokyo"] = "Cloudy, 18°C"
    };

    server.AddTool(
        name: "get_weather",
        description: "Get weather for a city",
        inputSchema: @"{""type"":""object"",""properties"":{""city"":{""type"":""string""}}}",
        executor: async (argsJson) =>
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argsJson);
            var city = args["city"];
            var weather = weatherData.TryGetValue(city, out var w) ? w : "Unknown";
            return await Task.FromResult($"Weather in {city}: {weather}");
        });
});
```

### Example 2: Database Mock

```csharp
services.AddInMemoryMcpServer("DatabaseMock", server =>
{
    server.Clear();

    var database = new List<string> 
    { 
        "User: Alice", 
        "User: Bob", 
        "Product: Widget" 
    };

    server.AddTool(
        name: "search",
        description: "Search database",
        executor: async (query) =>
        {
            var results = database.Where(item => item.Contains(query)).ToList();
            return await Task.FromResult($"Found {results.Count} results: {string.Join(", ", results)}");
        });
});
```

### Example 3: Testing Error Handling

```csharp
services.AddInMemoryMcpServer("ErrorMock", server =>
{
    server.Clear();

    server.AddTool(
        name: "failing_tool",
        description: "Tool that always fails",
        executor: async (_) =>
        {
            throw new InvalidOperationException("Simulated failure");
        });
});

// Test that your scene handles tool failures gracefully
```

---

## See Also

- [MCP Integration Guide](./MCP_INTEGRATION.md) - Full MCP integration overview
- [MCP Testing Guide](../Test/Rystem.PlayFramework.Test/MCP_TESTING.md) - Testing strategies
- [PlayFramework Documentation](./README.md) - Main PlayFramework docs
