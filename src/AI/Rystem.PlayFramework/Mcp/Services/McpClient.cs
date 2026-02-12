using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// HTTP client implementation for communicating with MCP servers.
/// </summary>
internal sealed class McpClient : IMcpClient
{
    private readonly ILogger<McpClient> _logger;
    private readonly IJsonService _jsonService;

    public McpClient(
        ILogger<McpClient> logger,
        IJsonService jsonService)
    {
        _logger = logger;
        _jsonService = jsonService;
    }

    public async Task<List<McpTool>> GetToolsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tools from MCP server: {ServerUrl} (Factory: {FactoryName})",
            settings.Url, settings.Name);

        try
        {
            var client = CreateHttpClient(settings);
            var response = await client.GetAsync($"{settings.Url}/tools", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var toolsResponse = _jsonService.Deserialize<McpToolsResponse>(json);

            var tools = toolsResponse?.Tools?
                .Select(t => new McpTool
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = t.InputSchema != null ? _jsonService.Serialize(t.InputSchema) : null,
                    ServerUrl = settings.Url,
                    FactoryName = settings.Name.ToString() ?? "default"
                })
                .ToList() ?? [];

            _logger.LogInformation("Fetched {ToolCount} tools from MCP server: {ServerUrl} (Factory: {FactoryName})",
                tools.Count, settings.Url, settings.Name);

            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tools from MCP server: {ServerUrl} (Factory: {FactoryName})",
                settings.Url, settings.Name);
            return [];
        }
    }

    public async Task<List<McpResource>> GetResourcesAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching resources from MCP server: {ServerUrl} (Factory: {FactoryName})",
            settings.Url, settings.Name);

        try
        {
            var client = CreateHttpClient(settings);
            var response = await client.GetAsync($"{settings.Url}/resources", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var resourcesResponse = _jsonService.Deserialize<McpResourcesResponse>(json);

            var resources = resourcesResponse?.Resources?
                .Select(r => new McpResource
                {
                    Name = r.Name,
                    Description = r.Description,
                    Uri = r.Uri,
                    MimeType = r.MimeType,
                    Content = null, // Content loaded on demand
                    ServerUrl = settings.Url,
                    FactoryName = settings.Name.ToString() ?? "default"
                })
                .ToList() ?? [];

            _logger.LogInformation("Fetched {ResourceCount} resources from MCP server: {ServerUrl} (Factory: {FactoryName})",
                resources.Count, settings.Url, settings.Name);

            return resources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch resources from MCP server: {ServerUrl} (Factory: {FactoryName})",
                settings.Url, settings.Name);
            return [];
        }
    }

    public async Task<List<McpPrompt>> GetPromptsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching prompts from MCP server: {ServerUrl} (Factory: {FactoryName})",
            settings.Url, settings.Name);

        try
        {
            var client = CreateHttpClient(settings);
            var response = await client.GetAsync($"{settings.Url}/prompts", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var promptsResponse = _jsonService.Deserialize<McpPromptsResponse>(json);

            var prompts = promptsResponse?.Prompts?
                .Select(p => new McpPrompt
                {
                    Name = p.Name,
                    Description = p.Description,
                    Template = p.Template,
                    Parameters = p.Parameters,
                    ServerUrl = settings.Url,
                    FactoryName = settings.Name.ToString() ?? "default"
                })
                .ToList() ?? [];

            _logger.LogInformation("Fetched {PromptCount} prompts from MCP server: {ServerUrl} (Factory: {FactoryName})",
                prompts.Count, settings.Url, settings.Name);

            return prompts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch prompts from MCP server: {ServerUrl} (Factory: {FactoryName})",
                settings.Url, settings.Name);
            return [];
        }
    }

    public async Task<string> ExecuteToolAsync(
        McpServerSettings settings,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Executing tool {ToolName} on MCP server: {ServerUrl} (Factory: {FactoryName})",
            toolName, settings.Url, settings.Name);

        try
        {
            var client = CreateHttpClient(settings);

            var request = new
            {
                tool = toolName,
                arguments = _jsonService.Deserialize<Dictionary<string, object?>>(argumentsJson)
            };

            var content = new StringContent(
                _jsonService.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{settings.Url}/tools/execute", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation("Tool {ToolName} executed successfully on MCP server: {ServerUrl} (Factory: {FactoryName})",
                toolName, settings.Url, settings.Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute tool {ToolName} on MCP server: {ServerUrl} (Factory: {FactoryName})",
                toolName, settings.Url, settings.Name);
            throw;
        }
    }

    public async Task<string> ReadResourceAsync(
        McpServerSettings settings,
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading resource {ResourceUri} from MCP server: {ServerUrl} (Factory: {FactoryName})",
            resourceUri, settings.Url, settings.Name);

        try
        {
            var client = CreateHttpClient(settings);

            var request = new { uri = resourceUri };
            var content = new StringContent(
                _jsonService.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync($"{settings.Url}/resources/read", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var resourceResponse = _jsonService.Deserialize<McpResourceReadResponse>(json);

            _logger.LogInformation("Resource {ResourceUri} read successfully from MCP server: {ServerUrl} (Factory: {FactoryName})",
                resourceUri, settings.Url, settings.Name);

            return resourceResponse?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read resource {ResourceUri} from MCP server: {ServerUrl} (Factory: {FactoryName})",
                resourceUri, settings.Url, settings.Name);
            throw;
        }
    }

    private HttpClient CreateHttpClient(McpServerSettings settings)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        };

        if (!string.IsNullOrWhiteSpace(settings.AuthorizationHeader))
        {
            client.DefaultRequestHeaders.Add("Authorization", settings.AuthorizationHeader);
        }

        return client;
    }

    // Internal response models for MCP protocol
    private sealed class McpToolsResponse
    {
        public List<ToolDto>? Tools { get; set; }
    }

    private sealed class ToolDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public Dictionary<string, object?>? InputSchema { get; set; }
    }

    private sealed class McpResourcesResponse
    {
        public List<ResourceDto>? Resources { get; set; }
    }

    private sealed class ResourceDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Uri { get; set; }
        public string? MimeType { get; set; }
    }

    private sealed class McpPromptsResponse
    {
        public List<PromptDto>? Prompts { get; set; }
    }

    private sealed class PromptDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Template { get; set; }
        public List<string>? Parameters { get; set; }
    }

    private sealed class McpResourceReadResponse
    {
        public string? Content { get; set; }
    }
}
