using System.Collections.Concurrent;

namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// In-memory MCP server for testing and development.
/// Provides a complete MCP implementation without external dependencies.
/// </summary>
public sealed class InMemoryMcpServer : IMcpClient
{
    private readonly ConcurrentDictionary<string, McpTool> _tools = new();
    private readonly ConcurrentDictionary<string, McpResource> _resources = new();
    private readonly ConcurrentDictionary<string, McpPrompt> _prompts = new();
    private readonly ConcurrentDictionary<string, Func<string, Task<string>>> _toolExecutors = new();
    private readonly ConcurrentDictionary<string, Func<Task<string>>> _resourceLoaders = new();

    public string Name { get; set; } = "InMemoryMcpServer";
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Registers a tool with this MCP server.
    /// </summary>
    public InMemoryMcpServer AddTool(
        string name,
        string description,
        string? inputSchema = null,
        Func<string, Task<string>>? executor = null)
    {
        var tool = new McpTool
        {
            Name = name,
            Description = description,
            InputSchema = inputSchema,
            ServerUrl = $"inmemory://{Name}",
            FactoryName = Name
        };

        _tools[name] = tool;

        if (executor != null)
        {
            _toolExecutors[name] = executor;
        }

        return this;
    }

    /// <summary>
    /// Registers a resource with this MCP server.
    /// </summary>
    public InMemoryMcpServer AddResource(
        string name,
        string uri,
        string description,
        string? content = null,
        Func<Task<string>>? loader = null)
    {
        var resource = new McpResource
        {
            Name = name,
            Uri = uri,
            Description = description,
            Content = content,
            ServerUrl = $"inmemory://{Name}",
            FactoryName = Name
        };

        _resources[name] = resource;

        if (loader != null)
        {
            _resourceLoaders[uri] = loader;
        }

        return this;
    }

    /// <summary>
    /// Registers a prompt template with this MCP server.
    /// </summary>
    public InMemoryMcpServer AddPrompt(
        string name,
        string description,
        string template,
        List<string>? parameters = null)
    {
        var prompt = new McpPrompt
        {
            Name = name,
            Description = description,
            Template = template,
            Parameters = parameters ?? new List<string>(),
            ServerUrl = $"inmemory://{Name}",
            FactoryName = Name
        };

        _prompts[name] = prompt;

        return this;
    }

    /// <summary>
    /// Clears all registered tools, resources, and prompts.
    /// </summary>
    public InMemoryMcpServer Clear()
    {
        _tools.Clear();
        _resources.Clear();
        _prompts.Clear();
        _toolExecutors.Clear();
        _resourceLoaders.Clear();
        return this;
    }

    /// <summary>
    /// Creates a default test server with common tools, resources, and prompts.
    /// </summary>
    public static InMemoryMcpServer CreateDefault()
    {
        var server = new InMemoryMcpServer();

        // Default tools
        server.AddTool(
            name: "get_weather",
            description: "Get current weather for a location",
            inputSchema: @"{""type"":""object"",""properties"":{""location"":{""type"":""string""}}}",
            executor: async args => await Task.FromResult($"Weather in {args}: Sunny, 22°C"));

        server.AddTool(
            name: "search_database",
            description: "Search in the knowledge database",
            inputSchema: @"{""type"":""object"",""properties"":{""query"":{""type"":""string""}}}",
            executor: async args => await Task.FromResult($"Found 3 results for: {args}"));

        server.AddTool(
            name: "calculate_sum",
            description: "Calculate the sum of numbers",
            inputSchema: @"{""type"":""object"",""properties"":{""numbers"":{""type"":""array""}}}",
            executor: async args => await Task.FromResult("Sum calculated: 42"));

        // Default resources
        server.AddResource(
            name: "company_policy",
            uri: "internal://docs/policy",
            description: "Company policies and guidelines",
            content: "# Company Policy\n\n1. Respect all employees\n2. Follow security protocols\n3. Report incidents immediately");

        server.AddResource(
            name: "api_documentation",
            uri: "internal://docs/api",
            description: "REST API documentation",
            loader: async () => await Task.FromResult("# API Documentation\n\n## Endpoints\n- GET /users\n- POST /users\n- DELETE /users/{id}"));

        // Default prompts
        server.AddPrompt(
            name: "code_review",
            description: "Prompt template for code review",
            template: "Review the following code and provide feedback:\n\n{code}\n\nFocus on: {focus_areas}",
            parameters: new List<string> { "code", "focus_areas" });

        server.AddPrompt(
            name: "summarize_document",
            description: "Summarize a document",
            template: "Summarize the following document in {max_words} words:\n\n{document}",
            parameters: new List<string> { "document", "max_words" });

        return server;
    }

    // IMcpClient implementation
    public async Task<List<McpTool>> GetToolsAsync(McpServerSettings settings, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _tools.Values.ToList();
    }

    public async Task<List<McpResource>> GetResourcesAsync(McpServerSettings settings, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _resources.Values.ToList();
    }

    public async Task<List<McpPrompt>> GetPromptsAsync(McpServerSettings settings, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _prompts.Values.ToList();
    }

    public async Task<string> ExecuteToolAsync(
        McpServerSettings settings,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        if (_toolExecutors.TryGetValue(toolName, out var executor))
        {
            return await executor(argumentsJson);
        }

        return $"Tool '{toolName}' executed with args: {argumentsJson}";
    }

    public async Task<string> ReadResourceAsync(
        McpServerSettings settings,
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        if (_resourceLoaders.TryGetValue(resourceUri, out var loader))
        {
            return await loader();
        }

        var resource = _resources.Values.FirstOrDefault(r => r.Uri == resourceUri);
        if (resource?.Content != null)
        {
            return resource.Content;
        }

        return $"Resource content for: {resourceUri}";
    }
}
