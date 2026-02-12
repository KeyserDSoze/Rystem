using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Manager implementation for MCP server connections.
/// Implements IFactoryName to work with Rystem's factory pattern.
/// </summary>
internal sealed class McpServerManager : IMcpServerManager, IFactoryName
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<McpServerManager> _logger;
    private readonly IFactory<McpServerSettings> _settingsFactory;
    private McpServerSettings? _settings;
    private string? _factoryName;

    public McpServerManager(
        IMcpClient mcpClient,
        ILogger<McpServerManager> logger,
        IFactory<McpServerSettings> settingsFactory)
    {
        _mcpClient = mcpClient;
        _logger = logger;
        _settingsFactory = settingsFactory;
    }

    public void SetFactoryName(AnyOf<string?, Enum>? name)
    {
        _factoryName = name?.ToString() ?? "default";
        _settings = _settingsFactory.Create(name);

        if (_settings != null)
        {
            _logger.LogInformation("MCP server configured - URL: {ServerUrl}, Factory: {FactoryName}",
                _settings.Url, _factoryName);
        }
        else
        {
            _logger.LogWarning("MCP server settings not found for factory: {FactoryName}", _factoryName);
        }
    }

    public async Task<List<McpTool>> GetToolsAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Fetching tools from MCP server (Factory: {FactoryName})", _factoryName);

        var allTools = await _mcpClient.GetToolsAsync(_settings!, cancellationToken);

        if (filterSettings == null)
        {
            _logger.LogDebug("No filter applied - returning all {ToolCount} tools (Factory: {FactoryName})",
                allTools.Count, _factoryName);
            return allTools;
        }

        var filteredTools = allTools
            .Where(t => filterSettings.MatchesTool(t.Name))
            .ToList();

        _logger.LogInformation("Filtered {FilteredCount} of {TotalCount} tools (Factory: {FactoryName})",
            filteredTools.Count, allTools.Count, _factoryName);

        return filteredTools;
    }

    public async Task<List<McpResource>> GetResourcesAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Fetching resources from MCP server (Factory: {FactoryName})", _factoryName);

        var allResources = await _mcpClient.GetResourcesAsync(_settings!, cancellationToken);

        if (filterSettings == null)
        {
            _logger.LogDebug("No filter applied - returning all {ResourceCount} resources (Factory: {FactoryName})",
                allResources.Count, _factoryName);
            return allResources;
        }

        var filteredResources = allResources
            .Where(r => filterSettings.MatchesResource(r.Name))
            .ToList();

        _logger.LogInformation("Filtered {FilteredCount} of {TotalCount} resources (Factory: {FactoryName})",
            filteredResources.Count, allResources.Count, _factoryName);

        return filteredResources;
    }

    public async Task<List<McpPrompt>> GetPromptsAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Fetching prompts from MCP server (Factory: {FactoryName})", _factoryName);

        var allPrompts = await _mcpClient.GetPromptsAsync(_settings!, cancellationToken);

        if (filterSettings == null)
        {
            _logger.LogDebug("No filter applied - returning all {PromptCount} prompts (Factory: {FactoryName})",
                allPrompts.Count, _factoryName);
            return allPrompts;
        }

        var filteredPrompts = allPrompts
            .Where(p => filterSettings.MatchesPrompt(p.Name))
            .ToList();

        _logger.LogInformation("Filtered {FilteredCount} of {TotalCount} prompts (Factory: {FactoryName})",
            filteredPrompts.Count, allPrompts.Count, _factoryName);

        return filteredPrompts;
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Executing tool {ToolName} on MCP server (Factory: {FactoryName})",
            toolName, _factoryName);

        return await _mcpClient.ExecuteToolAsync(_settings!, toolName, argumentsJson, cancellationToken);
    }

    public async Task<string> ReadResourceAsync(
        string resourceUri,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Reading resource {ResourceUri} from MCP server (Factory: {FactoryName})",
            resourceUri, _factoryName);

        return await _mcpClient.ReadResourceAsync(_settings!, resourceUri, cancellationToken);
    }

    public async Task<string> BuildSystemMessageAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        _logger.LogDebug("Building system message from MCP server resources and prompts (Factory: {FactoryName})",
            _factoryName);

        var resources = await GetResourcesAsync(filterSettings, cancellationToken);
        var prompts = await GetPromptsAsync(filterSettings, cancellationToken);

        var builder = new StringBuilder();

        // Add resources
        if (resources.Count > 0)
        {
            builder.AppendLine("# Available Resources");
            builder.AppendLine();

            foreach (var resource in resources)
            {
                builder.AppendLine($"## {resource.Name}");
                builder.AppendLine($"**Description**: {resource.Description}");
                builder.AppendLine($"**URI**: {resource.Uri}");

                if (!string.IsNullOrWhiteSpace(resource.Content))
                {
                    builder.AppendLine($"**Content**:");
                    builder.AppendLine(resource.Content);
                }
                else
                {
                    // Load content on demand
                    try
                    {
                        var content = await ReadResourceAsync(resource.Uri, cancellationToken);
                        builder.AppendLine($"**Content**:");
                        builder.AppendLine(content);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load content for resource {ResourceUri} (Factory: {FactoryName})",
                            resource.Uri, _factoryName);
                        builder.AppendLine($"**Content**: (Failed to load: {ex.Message})");
                    }
                }

                builder.AppendLine();
            }
        }

        // Add prompts
        if (prompts.Count > 0)
        {
            builder.AppendLine("# Available Prompt Templates");
            builder.AppendLine();

            foreach (var prompt in prompts)
            {
                builder.AppendLine($"## {prompt.Name}");
                builder.AppendLine($"**Description**: {prompt.Description}");

                if (prompt.Parameters != null && prompt.Parameters.Count > 0)
                {
                    builder.AppendLine($"**Parameters**: {string.Join(", ", prompt.Parameters)}");
                }

                builder.AppendLine($"**Template**:");
                builder.AppendLine(prompt.Template);
                builder.AppendLine();
            }
        }

        var systemMessage = builder.ToString();

        _logger.LogInformation("Built system message with {ResourceCount} resources and {PromptCount} prompts (Factory: {FactoryName}, Length: {MessageLength} chars)",
            resources.Count, prompts.Count, _factoryName, systemMessage.Length);

        return systemMessage;
    }

    private void EnsureConfigured()
    {
        if (_settings == null)
        {
            throw new InvalidOperationException(
                $"MCP server manager '{_factoryName}' is not configured. " +
                "Ensure AddMcpServer() was called during service registration with the correct factory name.");
        }
    }
}
