using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Mcp;
using Rystem.PlayFramework.Configuration;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for configuring a scene.
/// </summary>
public sealed class SceneBuilder
{
    private readonly SceneConfiguration _config;
    private readonly IServiceCollection _services;

    internal SceneBuilder(SceneConfiguration config, IServiceCollection services)
    {
        _config = config;
        _services = services;
    }

    /// <summary>
    /// Adds service methods as tools.
    /// </summary>
    public SceneBuilder WithService<TService>(Action<ServiceToolBuilder<TService>> configure) where TService : class
    {
        var builder = new ServiceToolBuilder<TService>(_config);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Configures actors for this scene.
    /// </summary>
    public SceneBuilder WithActors(Action<ActorBuilder> configure)
    {
        var builder = new ActorBuilder(_config);
        configure(builder);
        return this;
    }

    /// <summary>
    /// Adds an MCP server to this scene with optional filtering.
    /// </summary>
    /// <param name="factoryName">Factory name of the registered MCP server.</param>
    /// <param name="configure">Optional action to configure filter settings.</param>
    public SceneBuilder WithMcpServer(AnyOf<string?, Enum> factoryName, Action<McpFilterSettings>? configure = null)
    {
        var filterSettings = new McpFilterSettings();
        configure?.Invoke(filterSettings);

        _config.McpServerReferences.Add(new McpServerReference
        {
            FactoryName = factoryName,
            FilterSettings = filterSettings
        });

        return this;
    }

    /// <summary>
    /// Configures client-side tools that execute in browser/mobile app.
    /// Client receives tool request, executes it, and returns result with continuation token.
    /// Requires distributed cache configuration (in-memory or Redis).
    /// </summary>
    /// <param name="configure">Action to configure client interaction tools.</param>
    public SceneBuilder OnClient(Action<ClientInteractionBuilder> configure)
    {
        var builder = new ClientInteractionBuilder();
        configure(builder);

        _config.ClientInteractionDefinitions = builder.Build();
        _config.RequiresCache = true;

        return this;
    }

    /// <summary>
    /// Sets cache expiration time for continuation tokens.
    /// Only relevant when OnClient() is used.
    /// Default is 5 minutes.
    /// </summary>
    /// <param name="expiration">Time before continuation token expires in cache.</param>
    public SceneBuilder WithCacheExpiration(TimeSpan expiration)
    {
        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Cache expiration must be positive", nameof(expiration));

        _config.CacheExpiration = expiration;
        return this;
    }
}

/// <summary>
/// Internal configuration for a scene.
/// </summary>
internal sealed class SceneConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ServiceToolConfiguration> ServiceTools { get; set; } = [];
    public List<ActorConfiguration> Actors { get; set; } = [];
    public List<McpServerReference> McpServerReferences { get; set; } = [];

    /// <summary>
    /// Scene-specific RAG configurations (key = factory key or empty for default).
    /// </summary>
    public Dictionary<string, RagSettings> RagSettings { get; set; } = new();

    /// <summary>
    /// Scene-specific web search configurations (key = factory key or empty for default).
    /// </summary>
    public Dictionary<string, WebSearchSettings> WebSearchSettings { get; set; } = new();

    /// <summary>
    /// Client-side tool definitions registered via OnClient().
    /// </summary>
    internal IReadOnlyList<ClientInteractionDefinition>? ClientInteractionDefinitions { get; set; }

    /// <summary>
    /// Whether this scene requires cache (set to true when OnClient() is used).
    /// </summary>
    internal bool RequiresCache { get; set; }

    /// <summary>
    /// Cache expiration for continuation tokens.
    /// Default is 5 minutes.
    /// </summary>
    internal TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Reference to an MCP server with filter settings.
/// </summary>
public sealed class McpServerReference
{
    public required AnyOf<string?, Enum> FactoryName { get; init; }
    public required McpFilterSettings FilterSettings { get; init; }
}
