# MCP Integration Guide

PlayFramework supports integration with **MCP (Model Context Protocol)** servers, allowing scenes to dynamically load tools, resources, and prompts from external servers.

## What is MCP?

MCP (Model Context Protocol) is a standard protocol for AI applications to access external tools, data, and prompts. An MCP server exposes:

- **Tools** - Functions that the AI can call (e.g., database queries, API calls, calculations)
- **Resources** - Data/documents that provide context (e.g., company policies, API docs)
- **Prompts** - Reusable prompt templates for common tasks

## Quick Start

### 1. Register an MCP Server

```csharp
services.AddMcpServer(
    url: "https://mcp-server.example.com",
    factoryName: "CompanyMcpServer",
    configure: settings =>
    {
        settings.AuthorizationHeader = "Bearer your-token-here";
        settings.TimeoutSeconds = 60;
    });
```

### 2. Attach MCP Server to a Scene

```csharp
services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("CustomerSupport")
        .WithDescription("Handle customer support requests")
        .WithMcpServer("CompanyMcpServer", filter =>
        {
            // Optional: filter which tools/resources to include
            filter.Tools = new List<string> 
            { 
                "query_database", 
                "send_email" 
            };
            
            filter.Resources = new List<string> 
            { 
                "company_policy", 
                "faq_database" 
            };
        }));
});
```

### 3. Execute Scene

When the scene executes:
- MCP tools are automatically added to `ChatOptions.Tools`
- MCP resources and prompts are combined into a system message
- The LLM can call MCP tools just like regular scene tools

```csharp
var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

await foreach (var response in sceneManager.ExecuteAsync("How do I reset my password?"))
{
    Console.WriteLine($"{response.Status}: {response.Message}");
}
```

## Filtering MCP Capabilities

### Filter by Exact Name (Case-Insensitive)

```csharp
.WithMcpServer("MyServer", filter =>
{
    // Include only specific tools
    filter.Tools = new List<string> 
    { 
        "query_database", 
        "send_email",
        "create_ticket" 
    };
    
    // Include only specific resources
    filter.Resources = new List<string> 
    { 
        "company_policy",
        "api_documentation" 
    };
    
    // Include only specific prompts
    filter.Prompts = new List<string> 
    { 
        "code_review",
        "bug_report" 
    };
})
```

### Filter by Regex Pattern

```csharp
.WithMcpServer("MyServer", filter =>
{
    // Include all tools starting with "database_" or "query_"
    filter.ToolsRegex = "^(database|query)_.*";
    
    // Include all JSON resources
    filter.ResourcesRegex = ".*\\.json$";
    
    // Include all review-related prompts
    filter.PromptsRegex = ".*_review$";
})
```

### Combined Filtering (OR Logic)

Exact name match **OR** regex match:

```csharp
.WithMcpServer("MyServer", filter =>
{
    // Include "query_database" OR anything matching the regex
    filter.Tools = new List<string> { "query_database" };
    filter.ToolsRegex = "^send_.*"; // send_email, send_sms, etc.
})
```

### Default Behavior (No Filter)

If no filter is specified, **all** tools/resources/prompts from the MCP server are included:

```csharp
.WithMcpServer("MyServer") // All capabilities included
```

## Multiple MCP Servers

A scene can use multiple MCP servers:

```csharp
builder.AddScene(scene => scene
    .WithName("AdvancedAnalysis")
    .WithDescription("Perform advanced data analysis")
    .WithMcpServer("DatabaseServer", filter =>
    {
        filter.ToolsRegex = "^query_.*";
    })
    .WithMcpServer("VisualizationServer", filter =>
    {
        filter.ToolsRegex = "^chart_.*";
    })
    .WithMcpServer("DocumentationServer", filter =>
    {
        filter.Resources = new List<string> { "data_dictionary" };
    }));
```

## MCP Server Settings

```csharp
services.AddMcpServer(
    url: "https://mcp-server.example.com",
    factoryName: "MyServer",
    configure: settings =>
    {
        // Required
        settings.Url = "https://mcp-server.example.com";
        settings.Name = "MyServer";
        
        // Optional
        settings.AuthorizationHeader = "Bearer token123"; // For secured servers
        settings.TimeoutSeconds = 30; // Default: 30 seconds
    });
```

## Using IFactory Pattern

MCP servers use Rystem's IFactory pattern for named instances:

```csharp
// Register multiple MCP servers with different names
services.AddMcpServer("https://server-a.com", "ServerA");
services.AddMcpServer("https://server-b.com", "ServerB");
services.AddMcpServer("https://server-c.com", "ServerC");

// Retrieve specific server manager
var factory = serviceProvider.GetRequiredService<IFactory<IMcpServerManager>>();
var managerA = factory.Create("ServerA");

// Get capabilities
var tools = await managerA.GetToolsAsync();
var resources = await managerA.GetResourcesAsync();
```

## How MCP Integration Works

### 1. Scene Execution Start

When a scene with MCP server references starts:

```
SceneManager.ExecuteSceneAsync()
  └─> Load MCP capabilities
       ├─> GetToolsAsync() for each MCP server
       ├─> GetResourcesAsync() for each MCP server
       └─> GetPromptsAsync() for each MCP server
```

### 2. Apply Filters

```
Tools: [tool_a, tool_b, tool_c]
Filter: { Tools: ["tool_a"], ToolsRegex: "tool_c" }
Result: [tool_a, tool_c]
```

### 3. Build Chat Options

```csharp
ChatOptions
{
    Tools = 
    [
        ...scene.GetTools(),      // Scene's own tools
        ...mcpTools               // MCP tools (converted to AIFunction)
    ]
}
```

### 4. Build System Message

```markdown
# Available Resources

## company_policy
**Description**: Company policy documentation
**URI**: file:///docs/policy.md
**Content**:
# Company Policy
...

# Available Prompt Templates

## code_review
**Description**: Template for code review
**Parameters**: code, focus_areas
**Template**:
Review the following code...
```

### 5. LLM Interaction

The LLM can now:
- Call MCP tools (just like scene tools)
- Reference MCP resources in its reasoning
- Use MCP prompt templates

## Real-World Example

### Scenario: Customer Support Scene

```csharp
// 1. Register MCP server with company tools
services.AddMcpServer(
    url: "https://company-tools.internal",
    factoryName: "CompanyTools",
    configure: settings =>
    {
        settings.AuthorizationHeader = $"Bearer {apiKey}";
    });

// 2. Configure scene
services.AddPlayFramework(builder =>
{
    builder.AddScene(scene => scene
        .WithName("CustomerSupport")
        .WithDescription("Handle customer support tickets")
        .WithMcpServer("CompanyTools", filter =>
        {
            // Only include customer-related tools
            filter.ToolsRegex = "^customer_.*";
            
            // Include support documentation
            filter.Resources = new List<string> 
            { 
                "support_guidelines",
                "escalation_policy",
                "known_issues" 
            };
            
            // Include support prompt templates
            filter.Prompts = new List<string> 
            { 
                "ticket_response",
                "escalation_email" 
            };
        }));
});

// 3. Execute
var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

await foreach (var response in sceneManager.ExecuteAsync(
    "Customer reports they can't login. Ticket #12345"))
{
    if (response.Status == AiResponseStatus.FunctionRequest)
    {
        Console.WriteLine($"Calling MCP tool: {response.FunctionName}");
    }
    else if (response.Status == AiResponseStatus.Running)
    {
        Console.WriteLine(response.Message);
    }
}
```

**MCP Tools Available:**
- `customer_lookup(customer_id)` - Get customer information
- `customer_tickets(customer_id)` - Get customer ticket history
- `customer_reset_password(customer_id)` - Reset customer password

**MCP Resources Available:**
- `support_guidelines` - Standard support procedures
- `escalation_policy` - When and how to escalate
- `known_issues` - Current system issues

**LLM Execution Flow:**
1. LLM reads system message with support guidelines and known issues
2. LLM calls `customer_lookup("12345")` to get customer info
3. LLM calls `customer_tickets("12345")` to check history
4. LLM finds login issue matches a known issue from resources
5. LLM calls `customer_reset_password("12345")` to resolve
6. LLM uses `ticket_response` prompt template to format reply

## Architecture Diagram

```
┌─────────────────────────────────────────────────┐
│  PlayFramework Scene Execution                  │
├─────────────────────────────────────────────────┤
│                                                  │
│  ┌───────────────┐                              │
│  │ SceneManager  │                              │
│  └───────┬───────┘                              │
│          │                                       │
│          ├─> Execute Actors                     │
│          │                                       │
│          ├─> Load MCP Capabilities              │
│          │   ┌──────────────────────┐           │
│          │   │ IMcpServerManager    │           │
│          │   │ (IFactory pattern)   │           │
│          │   └──────────┬───────────┘           │
│          │              │                        │
│          │              ├─> GetToolsAsync()     │
│          │              ├─> GetResourcesAsync() │
│          │              └─> GetPromptsAsync()   │
│          │                     │                 │
│          │                     ▼                 │
│          │              ┌─────────────┐         │
│          │              │ IMcpClient  │         │
│          │              │ (HTTP calls)│         │
│          │              └──────┬──────┘         │
│          │                     │                 │
│          │                     ▼                 │
│          │   ╔═════════════════════════╗        │
│          │   ║   MCP Server            ║        │
│          │   ║   (External HTTP API)   ║        │
│          │   ╚═════════════════════════╝        │
│          │                                       │
│          ├─> Build ChatOptions                  │
│          │   - Add scene tools                  │
│          │   - Add MCP tools (AIFunction)       │
│          │                                       │
│          ├─> Build System Message               │
│          │   - Add actor context                │
│          │   - Add MCP resources                │
│          │   - Add MCP prompts                  │
│          │                                       │
│          └─> Call LLM                            │
│              ┌───────────────┐                  │
│              │ IChatClient   │                  │
│              └───────────────┘                  │
│                                                  │
└─────────────────────────────────────────────────┘
```

## Benefits of MCP Integration

1. **Dynamic Capabilities** - Add tools without code changes
2. **Centralized Management** - MCP server manages tool versions
3. **Reusability** - Multiple scenes/apps can use same MCP server
4. **Separation of Concerns** - Domain experts maintain MCP server, AI engineers use it
5. **Security** - Centralized authorization and access control

## Best Practices

### 1. Use Filtering

Don't load all MCP capabilities if you only need a few:

```csharp
// ❌ Bad - loads everything
.WithMcpServer("LargeServer")

// ✅ Good - only loads what's needed
.WithMcpServer("LargeServer", filter =>
{
    filter.ToolsRegex = "^customer_.*";
})
```

### 2. Organize by Domain

Create separate MCP servers for different domains:

```csharp
services.AddMcpServer("https://customer-tools.internal", "CustomerTools");
services.AddMcpServer("https://inventory-tools.internal", "InventoryTools");
services.AddMcpServer("https://billing-tools.internal", "BillingTools");
```

### 3. Use Factory Names Consistently

Use enum or constants for factory names:

```csharp
public enum McpServers
{
    CustomerTools,
    InventoryTools,
    BillingTools
}

services.AddMcpServer("https://...", McpServers.CustomerTools);
builder.AddScene(s => s.WithMcpServer(McpServers.CustomerTools));
```

### 4. Document Resource Content

MCP resources should have clear, well-structured content:

```markdown
# Good Resource Content

## Overview
Clear explanation of what this resource provides.

## Data Format
- Field 1: Description
- Field 2: Description

## Examples
...
```

### 5. Version MCP Server URLs

Include version in URL or use configuration:

```csharp
services.AddMcpServer(
    url: config["McpServers:CustomerTools:Url"], // https://api.v2.example.com
    factoryName: "CustomerTools");
```

## Troubleshooting

### MCP Server Not Responding

```csharp
services.AddMcpServer("https://mcp-server.com", "MyServer", s =>
{
    s.TimeoutSeconds = 60; // Increase timeout
});
```

### Authentication Issues

```csharp
services.AddMcpServer("https://mcp-server.com", "MyServer", s =>
{
    s.AuthorizationHeader = $"Bearer {apiKey}";
});
```

### Tools Not Appearing

Check:
1. MCP server is registered: `services.AddMcpServer(...)`
2. Scene references MCP server: `.WithMcpServer("MyServer")`
3. Filter isn't too restrictive
4. MCP server is returning tools correctly

## Related Documentation

- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [MCP Testing Guide](../Test/Rystem.PlayFramework.Test/MCP_TESTING.md)
- [Factory Pattern](FACTORY_PATTERN.md)
- [Scene Configuration](README.md)
