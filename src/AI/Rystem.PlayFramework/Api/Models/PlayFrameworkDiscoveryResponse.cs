namespace Rystem.PlayFramework.Api.Models;

/// <summary>
/// Discovery payload exposed by the PlayFramework HTTP API.
/// </summary>
public sealed class PlayFrameworkDiscoveryResponse
{
    /// <summary>
    /// Factory name that generated this discovery payload.
    /// </summary>
    public string FactoryName { get; set; } = "default";

    /// <summary>
    /// Registered scenes for the selected factory.
    /// </summary>
    public List<PlayFrameworkSceneInfo> Scenes { get; set; } = [];

    /// <summary>
    /// DI services exposed as tools.
    /// </summary>
    public List<PlayFrameworkToolSourceInfo> Services { get; set; } = [];

    /// <summary>
    /// Client-side tools exposed by scenes.
    /// </summary>
    public List<PlayFrameworkToolSourceInfo> Clients { get; set; } = [];

    /// <summary>
    /// MCP servers referenced by scenes.
    /// </summary>
    public List<PlayFrameworkToolSourceInfo> McpServers { get; set; } = [];

    /// <summary>
    /// Tools that do not belong to the standard source buckets.
    /// </summary>
    public List<PlayFrameworkToolInfo> Others { get; set; } = [];
}

/// <summary>
/// Scene metadata exposed by the discovery endpoint.
/// </summary>
public sealed class PlayFrameworkSceneInfo
{
    /// <summary>
    /// Normalized scene name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Scene description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tools currently exposed by the scene.
    /// </summary>
    public List<PlayFrameworkToolInfo> Tools { get; set; } = [];
}

/// <summary>
/// Grouped tool source information used by the discovery endpoint.
/// </summary>
public sealed class PlayFrameworkToolSourceInfo
{
    /// <summary>
    /// Source name (service type, MCP server name, or client marker).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Source category.
    /// </summary>
    public PlayFrameworkToolSourceType SourceType { get; set; }

    /// <summary>
    /// Indicates whether the source is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Optional source error, typically used for MCP discovery failures.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Tools exposed by the source.
    /// </summary>
    public List<PlayFrameworkToolInfo> Tools { get; set; } = [];
}

/// <summary>
/// Tool metadata exposed by the discovery endpoint.
/// </summary>
public sealed class PlayFrameworkToolInfo
{
    /// <summary>
    /// Normalized scene name that owns this tool.
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// Normalized tool name that can be sent back in ForcedTools.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Tool description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tool source category.
    /// </summary>
    public PlayFrameworkToolSourceType SourceType { get; set; }

    /// <summary>
    /// Source name (service type, MCP server name, or client marker).
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Method name or original external tool name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// Indicates whether this tool is a fire-and-forget client command.
    /// </summary>
    public bool IsCommand { get; set; }

    /// <summary>
    /// Optional input schema when available.
    /// </summary>
    public string? JsonSchema { get; set; }
}
