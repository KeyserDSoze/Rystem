namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// HTTP client for communicating with MCP (Model Context Protocol) servers.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Retrieves all tools exposed by the MCP server.
    /// </summary>
    /// <param name="settings">MCP server connection settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available tools.</returns>
    Task<List<McpTool>> GetToolsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all resources exposed by the MCP server.
    /// </summary>
    /// <param name="settings">MCP server connection settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available resources.</returns>
    Task<List<McpResource>> GetResourcesAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all prompts exposed by the MCP server.
    /// </summary>
    /// <param name="settings">MCP server connection settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available prompts.</returns>
    Task<List<McpPrompt>> GetPromptsAsync(
        McpServerSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a tool on the MCP server.
    /// </summary>
    /// <param name="settings">MCP server connection settings.</param>
    /// <param name="toolName">Name of the tool to execute.</param>
    /// <param name="argumentsJson">Tool arguments as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tool execution result as JSON string.</returns>
    Task<string> ExecuteToolAsync(
        McpServerSettings settings,
        string toolName,
        string argumentsJson,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the content of a resource from the MCP server.
    /// </summary>
    /// <param name="settings">MCP server connection settings.</param>
    /// <param name="resourceUri">URI of the resource to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resource content as string.</returns>
    Task<string> ReadResourceAsync(
        McpServerSettings settings,
        string resourceUri,
        CancellationToken cancellationToken = default);
}
