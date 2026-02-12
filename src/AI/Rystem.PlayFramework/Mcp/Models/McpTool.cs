namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Represents a tool exposed by an MCP server.
/// </summary>
public sealed record McpTool
{
    /// <summary>
    /// Tool name (unique identifier).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// JSON schema describing the tool's input parameters.
    /// </summary>
    public string? InputSchema { get; init; }

    /// <summary>
    /// MCP server URL that provides this tool.
    /// </summary>
    public required string ServerUrl { get; init; }

    /// <summary>
    /// Factory name identifying the MCP server connection.
    /// </summary>
    public required string FactoryName { get; init; }
}
