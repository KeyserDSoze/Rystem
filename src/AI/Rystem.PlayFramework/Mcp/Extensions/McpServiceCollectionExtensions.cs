using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Rystem.PlayFramework.Mcp;

/// <summary>
/// Extension methods for registering MCP server connections.
/// </summary>
public static class McpServiceCollectionExtensions
{
    /// <summary>
    /// Registers an MCP (Model Context Protocol) server connection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="url">MCP server base URL (e.g., "https://mcp-server.example.com").</param>
    /// <param name="factoryName">Factory name to identify this MCP server connection.</param>
    /// <param name="configure">Optional configuration action for additional settings.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMcpServer(
        this IServiceCollection services,
        string url,
        AnyOf<string?, Enum> factoryName,
        Action<McpServerSettings>? configure = null)
    {
        // Register IMcpClient as singleton (shared across all MCP servers)
        services.TryAddSingleton<IMcpClient, McpClient>();

        // Register IJsonService if not already registered
        services.TryAddSingleton<IJsonService, DefaultJsonService>();

        // Build settings
        var settings = new McpServerSettings
        {
            Url = url,
            Name = factoryName
        };

        // Apply configuration
        configure?.Invoke(settings);

        // Register settings with factory pattern (like PlayFrameworkSettings)
        services.AddFactory(settings, factoryName, ServiceLifetime.Singleton);

        // Register manager with factory pattern (standard approach)
        services.AddFactory<IMcpServerManager, McpServerManager>(factoryName, ServiceLifetime.Singleton);

        return services;
    }

    /// <summary>
    /// Registers an MCP server connection with detailed configuration.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="settings">Complete MCP server settings.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddMcpServer(
        this IServiceCollection services,
        McpServerSettings settings)
    {
        return services.AddMcpServer(settings.Url, settings.Name, s =>
        {
            s.AuthorizationHeader = settings.AuthorizationHeader;
            s.TimeoutSeconds = settings.TimeoutSeconds;
        });
    }

    /// <summary>
    /// Registers an in-memory MCP server for testing and development.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="factoryName">Factory name to identify this MCP server connection.</param>
    /// <param name="configure">Configuration action to add tools, resources, and prompts.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryMcpServer(
        this IServiceCollection services,
        AnyOf<string?, Enum> factoryName,
        Action<InMemoryMcpServer>? configure = null)
    {
        // Create and configure in-memory server (unique instance per factory name)
        var inMemoryServer = InMemoryMcpServer.CreateDefault();
        inMemoryServer.Name = factoryName.ToString() ?? "default";
        configure?.Invoke(inMemoryServer);

        // Register as keyed singleton IMcpClient with the server instance
        // This ensures each factory name gets its own isolated instance
        var key = $"InMemoryMcp_{factoryName}";
        services.AddKeyedSingleton<IMcpClient>(key, inMemoryServer);

        // Register IJsonService if not already registered
        services.TryAddSingleton<IJsonService, DefaultJsonService>();

        // Build settings (URL doesn't matter for in-memory)
        var settings = new McpServerSettings
        {
            Url = $"inmemory://{factoryName}",
            Name = factoryName
        };

        // Register settings with factory pattern
        services.AddFactory(settings, factoryName, ServiceLifetime.Singleton);

        // Register manager with factory pattern - use custom factory to inject keyed IMcpClient
        services.AddFactory<IMcpServerManager>((sp, name) =>
        {
            var keyedClient = sp.GetRequiredKeyedService<IMcpClient>(key);
            var logger = sp.GetRequiredService<ILogger<McpServerManager>>();
            var settingsFactory = sp.GetRequiredService<IFactory<McpServerSettings>>();

            var manager = new McpServerManager(keyedClient, logger, settingsFactory);

            // Convert name to AnyOf if needed
            AnyOf<string?, Enum>? anyOfName = name switch
            {
                string s => s,
                Enum e => e,
                _ => null
            };
            manager.SetFactoryName(anyOfName);

            return manager;
        }, factoryName, ServiceLifetime.Singleton);

        return services;
    }
}
