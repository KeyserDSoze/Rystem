namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Represents a pre-configured prompt template exposed by an MCP server.
/// </summary>
public sealed record McpPrompt
{
    /// <summary>
    /// Prompt name (unique identifier).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the prompt's purpose.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Prompt template text (may contain placeholders like {variable}).
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// List of parameter names used in the template.
    /// </summary>
    public List<string>? Parameters { get; init; }

    /// <summary>
    /// MCP server URL that provides this prompt.
    /// </summary>
    public required string ServerUrl { get; init; }

    /// <summary>
    /// Factory name identifying the MCP server connection.
    /// </summary>
    public required string FactoryName { get; init; }
}
