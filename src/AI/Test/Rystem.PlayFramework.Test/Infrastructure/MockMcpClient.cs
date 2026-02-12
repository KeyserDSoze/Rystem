using Rystem.PlayFramework.Mcp;

namespace Rystem.PlayFramework.Test.Infrastructure;

/// <summary>
/// Mock MCP client for testing MCP integration without real HTTP calls.
/// </summary>
public sealed class MockMcpClient : IMcpClient
{
    private readonly List<McpTool> _tools = [];
    private readonly List<McpResource> _resources = [];
    private readonly List<McpPrompt> _prompts = [];
    private readonly Dictionary<string, Func<string, Task<string>>> _toolExecutors = new();

    public MockMcpClient()
    {
        // Default test data
        SetupDefaultTestData();
    }

    public Task<List<McpTool>> GetToolsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        var serverTools = _tools
            .Where(t => t.ServerUrl == settings.Url && t.FactoryName == settings.Name.ToString())
            .ToList();
        return Task.FromResult(serverTools);
    }

    public Task<List<McpResource>> GetResourcesAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        var serverResources = _resources
            .Where(r => r.ServerUrl == settings.Url && r.FactoryName == settings.Name.ToString())
            .ToList();
        return Task.FromResult(serverResources);
    }

    public Task<List<McpPrompt>> GetPromptsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        var serverPrompts = _prompts
            .Where(p => p.ServerUrl == settings.Url && p.FactoryName == settings.Name.ToString())
            .ToList();
        return Task.FromResult(serverPrompts);
    }

    public Task<string> ExecuteToolAsync(
        McpServerSettings settings,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (_toolExecutors.TryGetValue(toolName, out var executor))
        {
            return executor(argumentsJson);
        }

        return Task.FromResult($"Mock execution result for tool: {toolName}");
    }

    public Task<string> ReadResourceAsync(
        McpServerSettings settings,
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        var resource = _resources.FirstOrDefault(r => r.Uri == resourceUri);
        return Task.FromResult(resource?.Content ?? $"Mock content for resource: {resourceUri}");
    }

    // ===== Test Setup Helpers =====

    public MockMcpClient AddTool(string name, string description, string serverUrl, string factoryName, string? inputSchema = null)
    {
        _tools.Add(new McpTool
        {
            Name = name,
            Description = description,
            ServerUrl = serverUrl,
            FactoryName = factoryName,
            InputSchema = inputSchema
        });
        return this;
    }

    public MockMcpClient AddResource(string name, string description, string uri, string serverUrl, string factoryName, string? content = null)
    {
        _resources.Add(new McpResource
        {
            Name = name,
            Description = description,
            Uri = uri,
            ServerUrl = serverUrl,
            FactoryName = factoryName,
            Content = content,
            MimeType = "text/plain"
        });
        return this;
    }

    public MockMcpClient AddPrompt(string name, string description, string template, string serverUrl, string factoryName, List<string>? parameters = null)
    {
        _prompts.Add(new McpPrompt
        {
            Name = name,
            Description = description,
            Template = template,
            ServerUrl = serverUrl,
            FactoryName = factoryName,
            Parameters = parameters
        });
        return this;
    }

    public MockMcpClient WithToolExecutor(string toolName, Func<string, Task<string>> executor)
    {
        _toolExecutors[toolName] = executor;
        return this;
    }

    public MockMcpClient ClearAll()
    {
        _tools.Clear();
        _resources.Clear();
        _prompts.Clear();
        _toolExecutors.Clear();
        return this;
    }

    private void SetupDefaultTestData()
    {
        // Default test tools
        AddTool(
            "get_weather",
            "Get current weather for a location",
            "https://test-mcp-server.local",
            "TestMcpServer",
            """{"type": "object", "properties": {"location": {"type": "string"}}}""");

        AddTool(
            "search_database",
            "Search database for information",
            "https://test-mcp-server.local",
            "TestMcpServer",
            """{"type": "object", "properties": {"query": {"type": "string"}}}""");

        AddTool(
            "calculate_sum",
            "Calculate sum of two numbers",
            "https://test-mcp-server.local",
            "TestMcpServer",
            """{"type": "object", "properties": {"a": {"type": "number"}, "b": {"type": "number"}}}""");

        // Default test resources
        AddResource(
            "company_policy",
            "Company policy documentation",
            "file:///docs/company-policy.md",
            "https://test-mcp-server.local",
            "TestMcpServer",
            "# Company Policy\n\nAll employees must follow these guidelines...");

        AddResource(
            "api_documentation",
            "API reference documentation",
            "file:///docs/api-reference.md",
            "https://test-mcp-server.local",
            "TestMcpServer",
            "# API Documentation\n\n## Endpoints\n- GET /api/users\n- POST /api/users");

        // Default test prompts
        AddPrompt(
            "code_review",
            "Template for code review",
            "Review the following code and provide feedback:\n\n{code}\n\nFocus on: {focus_areas}",
            "https://test-mcp-server.local",
            "TestMcpServer",
            new List<string> { "code", "focus_areas" });

        AddPrompt(
            "summarize_document",
            "Template for document summarization",
            "Summarize the following document in {max_sentences} sentences:\n\n{document}",
            "https://test-mcp-server.local",
            "TestMcpServer",
            new List<string> { "document", "max_sentences" });

        // Setup default tool executors
        WithToolExecutor("get_weather", async args =>
        {
            await Task.CompletedTask;
            return """{"temperature": 22, "condition": "sunny", "location": "Test City"}""";
        });

        WithToolExecutor("search_database", async args =>
        {
            await Task.CompletedTask;
            return """{"results": [{"id": 1, "name": "Test Result"}], "count": 1}""";
        });

        WithToolExecutor("calculate_sum", async args =>
        {
            await Task.CompletedTask;
            // Parse args and calculate (mock)
            return """{"result": 42}""";
        });
    }
}
