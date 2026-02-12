namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Manager for MCP server connections and capabilities.
/// Uses IFactory pattern for named MCP server instances.
/// </summary>
public interface IMcpServerManager
{
    /// <summary>
    /// Gets all tools from the specified MCP server, applying filter settings.
    /// </summary>
    /// <param name="filterSettings">Optional filter settings for tools.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered list of MCP tools.</returns>
    Task<List<McpTool>> GetToolsAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources from the specified MCP server, applying filter settings.
    /// </summary>
    /// <param name="filterSettings">Optional filter settings for resources.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered list of MCP resources.</returns>
    Task<List<McpResource>> GetResourcesAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all prompts from the specified MCP server, applying filter settings.
    /// </summary>
    /// <param name="filterSettings">Optional filter settings for prompts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Filtered list of MCP prompts.</returns>
    Task<List<McpPrompt>> GetPromptsAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a tool on the MCP server.
    /// </summary>
    /// <param name="toolName">Name of the tool to execute.</param>
    /// <param name="argumentsJson">Tool arguments as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool execution result as JSON string.</returns>
    Task<string> ExecuteToolAsync(
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the content of a resource from the MCP server.
    /// </summary>
    /// <param name="resourceUri">URI of the resource to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resource content as string.</returns>
    Task<string> ReadResourceAsync(
        string resourceUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a system message combining all filtered resources and prompts.
    /// </summary>
    /// <param name="filterSettings">Optional filter settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined system message text.</returns>
    Task<string> BuildSystemMessageAsync(
        McpFilterSettings? filterSettings = null,
        CancellationToken cancellationToken = default);
}
