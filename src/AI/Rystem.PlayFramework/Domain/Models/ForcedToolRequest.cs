namespace Rystem.PlayFramework;

/// <summary>
/// Identifies a tool that should be constrained or forced for a specific scene execution.
/// </summary>
public sealed class ForcedToolRequest
{
    /// <summary>
    /// Normalized scene name that owns the tool.
    /// </summary>
    public required string SceneName { get; set; }

    /// <summary>
    /// Normalized tool name exposed to the LLM.
    /// </summary>
    public required string ToolName { get; set; }

    /// <summary>
    /// Optional source type used to disambiguate tools with the same name.
    /// </summary>
    public PlayFrameworkToolSourceType? SourceType { get; set; }

    /// <summary>
    /// Optional source name.
    /// For DI tools this is usually the service type name; for MCP tools this is the MCP factory name.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Optional source member or original tool name.
    /// For DI tools this is the method name; for MCP tools this is the original MCP tool name.
    /// </summary>
    public string? MemberName { get; set; }
}

/// <summary>
/// Source category for a PlayFramework tool.
/// </summary>
public enum PlayFrameworkToolSourceType
{
    /// <summary>
    /// Tool backed by a DI service method.
    /// </summary>
    Service,

    /// <summary>
    /// Tool executed on the client.
    /// </summary>
    Client,

    /// <summary>
    /// Tool exposed by an MCP server.
    /// </summary>
    Mcp,

    /// <summary>
    /// Any other tool category.
    /// </summary>
    Other
}
