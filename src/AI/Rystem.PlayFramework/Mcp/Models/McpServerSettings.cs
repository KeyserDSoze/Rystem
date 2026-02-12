namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Settings for configuring an MCP (Model Context Protocol) server connection.
/// </summary>
public sealed class McpServerSettings
{
    /// <summary>
    /// MCP server base URL (e.g., "https://mcp-server.example.com").
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Factory name to identify this MCP server (used with IFactory pattern).
    /// </summary>
    public required AnyOf<string?, Enum> Name { get; set; }

    /// <summary>
    /// Optional authentication header (e.g., "Bearer token123").
    /// </summary>
    public string? AuthorizationHeader { get; set; }

    /// <summary>
    /// Timeout for MCP server requests in seconds. Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Filter settings for selecting specific MCP tools, resources, and prompts.
/// </summary>
public sealed class McpFilterSettings
{
    /// <summary>
    /// Specific tool names to include (exact match).
    /// If null or empty, all tools are included (unless ToolsRegex is specified).
    /// </summary>
    public List<string>? Tools { get; set; }

    /// <summary>
    /// Regex pattern to filter tool names. Only tools matching this pattern will be included.
    /// </summary>
    public string? ToolsRegex { get; set; }

    /// <summary>
    /// Specific resource names to include (exact match).
    /// If null or empty, all resources are included (unless ResourcesRegex is specified).
    /// </summary>
    public List<string>? Resources { get; set; }

    /// <summary>
    /// Regex pattern to filter resource names. Only resources matching this pattern will be included.
    /// </summary>
    public string? ResourcesRegex { get; set; }

    /// <summary>
    /// Specific prompt names to include (exact match).
    /// If null or empty, all prompts are included (unless PromptsRegex is specified).
    /// </summary>
    public List<string>? Prompts { get; set; }

    /// <summary>
    /// Regex pattern to filter prompt names. Only prompts matching this pattern will be included.
    /// </summary>
    public string? PromptsRegex { get; set; }

    /// <summary>
    /// Checks if a tool name matches the filter criteria.
    /// </summary>
    public bool MatchesTool(string toolName)
    {
        if (Tools != null && Tools.Count > 0)
        {
            if (Tools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(ToolsRegex))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(toolName, ToolsRegex);
        }

        // If no filters specified, include all
        return Tools == null || Tools.Count == 0;
    }

    /// <summary>
    /// Checks if a resource name matches the filter criteria.
    /// </summary>
    public bool MatchesResource(string resourceName)
    {
        if (Resources != null && Resources.Count > 0)
        {
            if (Resources.Contains(resourceName, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(ResourcesRegex))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(resourceName, ResourcesRegex);
        }

        return Resources == null || Resources.Count == 0;
    }

    /// <summary>
    /// Checks if a prompt name matches the filter criteria.
    /// </summary>
    public bool MatchesPrompt(string promptName)
    {
        if (Prompts != null && Prompts.Count > 0)
        {
            if (Prompts.Contains(promptName, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(PromptsRegex))
        {
            return System.Text.RegularExpressions.Regex.IsMatch(promptName, PromptsRegex);
        }

        return Prompts == null || Prompts.Count == 0;
    }
}
