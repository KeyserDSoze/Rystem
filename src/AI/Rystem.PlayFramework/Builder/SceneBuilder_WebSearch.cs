using System.Reflection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring Web Search per scene.
/// </summary>
public static class SceneBuilderWebSearchExtensions
{
    /// <summary>
    /// Enables Web Search for this specific scene.
    /// Overrides global web search configuration.
    /// </summary>
    /// <param name="builder">Scene builder.</param>
    /// <param name="configure">Configuration action for web search settings.</param>
    /// <param name="name">Factory key (string or enum) to identify the IWebSearchService. Use null for default.</param>
    /// <returns>Builder for chaining.</returns>
    /// <example>
    /// // Enable web search for Research scene with custom settings
    /// builder.AddScene(scene =>
    /// {
    ///     scene
    ///         .WithName("Research")
    ///         .WithDescription("Research assistant with internet access")
    ///         .WithWebSearch(settings =>
    ///         {
    ///             settings.MaxResults = 10;
    ///             settings.Freshness = WebSearchFreshness.Day;  // Only today's results
    ///             settings.SafeSearch = true;
    ///         }, "bing");
    /// });
    /// </example>
    public static SceneBuilder WithWebSearch(
        this SceneBuilder builder,
        Action<WebSearchSettings> configure,
        AnyOf<string?, Enum>? name = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var key = name?.Value?.ToString() ?? string.Empty;

        // Access private _config field via reflection (same pattern as RAG)
        var configField = typeof(SceneBuilder).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = (SceneConfiguration)configField!.GetValue(builder)!;

        // Create or update web search settings for this scene
        if (!config.WebSearchSettings.TryGetValue(key, out var webSearchSettings))
        {
            webSearchSettings = new WebSearchSettings { FactoryKey = key };
            config.WebSearchSettings[key] = webSearchSettings;
        }

        // Apply configuration
        configure(webSearchSettings);

        return builder;
    }

    /// <summary>
    /// Disables Web Search for this specific scene.
    /// Use this to override global web search configuration.
    /// </summary>
    /// <param name="builder">Scene builder.</param>
    /// <param name="name">Factory key (string or enum) to disable. Use null for default.</param>
    /// <returns>Builder for chaining.</returns>
    /// <example>
    /// // Disable web search for Math scene (doesn't need internet)
    /// builder.AddScene(scene =>
    /// {
    ///     scene
    ///         .WithName("Calculator")
    ///         .WithDescription("Math calculator - no internet needed")
    ///         .WithoutWebSearch("bing");
    /// });
    /// </example>
    public static SceneBuilder WithoutWebSearch(
        this SceneBuilder builder,
        AnyOf<string?, Enum>? name = null)
    {
        var key = name?.Value?.ToString() ?? string.Empty;

        // Access private _config field via reflection
        var configField = typeof(SceneBuilder).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = (SceneConfiguration)configField!.GetValue(builder)!;

        // Mark web search as disabled for this scene
        config.WebSearchSettings[key] = new WebSearchSettings
        {
            Enabled = false,
            FactoryKey = key
        };

        return builder;
    }
}
