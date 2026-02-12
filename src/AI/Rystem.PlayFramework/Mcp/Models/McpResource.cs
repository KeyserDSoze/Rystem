namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Represents a resource (data/context) exposed by an MCP server.
/// </summary>
public sealed record McpResource
{
    /// <summary>
    /// Resource name (unique identifier).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the resource content.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Resource URI (e.g., "file:///path/to/file", "db://table/record").
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// MIME type of the resource content (e.g., "text/plain", "application/json").
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Resource content (text or base64-encoded binary data).
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// MCP server URL that provides this resource.
    /// </summary>
    public required string ServerUrl { get; init; }

    /// <summary>
    /// Factory name identifying the MCP server connection.
    /// </summary>
    public required string FactoryName { get; init; }
}
