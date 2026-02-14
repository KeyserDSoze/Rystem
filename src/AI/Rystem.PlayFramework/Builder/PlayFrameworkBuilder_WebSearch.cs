namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring Web Search globally.
/// </summary>
public static class PlayFrameworkBuilderWebSearchExtensions
{
    /// <summary>
    /// Enables Web Search for all scenes globally.
    /// Can be overridden at scene level.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="configure">Configuration action for web search settings.</param>
    /// <param name="name">Factory key (string or enum) to identify the IWebSearchService. Use null for default.</param>
    /// <returns>Builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configure is null.</exception>
    /// <example>
    /// // Configure global web search with Bing
    /// builder.WithWebSearch(settings =>
    /// {
    ///     settings.MaxResults = 5;
    ///     settings.SafeSearch = true;
    ///     settings.Freshness = WebSearchFreshness.Week;
    ///     settings.Market = "en-US";
    /// }, "bing");
    /// 
    /// // Configure with enum
    /// builder.WithWebSearch(settings =>
    /// {
    ///     settings.MaxResults = 10;
    /// }, WebSearchProvider.Google);
    /// 
    /// // Configure default (no key)
    /// builder.WithWebSearch(settings =>
    /// {
    ///     settings.MaxResults = 5;
    /// });
    /// </example>
    /// <remarks>
    /// ⚠️ Important: Before calling WithWebSearch(), you must register an IWebSearchService:
    /// <code>
    /// services.AddWebSearchService&lt;YourWebSearchService&gt;(cost =>
    /// {
    ///     cost.CostPerSearch = 0.005m;
    ///     cost.CostPerResult = 0.0001m;
    /// }, name: "bing");
    /// </code>
    /// Otherwise, you'll get a runtime error when the scene tries to use web search.
    /// </remarks>
    public static PlayFrameworkBuilder WithWebSearch(
        this PlayFrameworkBuilder builder,
        Action<WebSearchSettings> configure,
        AnyOf<string?, Enum>? name = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var key = name?.Value?.ToString() ?? string.Empty;

        // Create or update settings
        if (!builder.Settings.GlobalWebSearchSettings.TryGetValue(key, out var webSearchSettings))
        {
            webSearchSettings = new WebSearchSettings { FactoryKey = key };
            builder.Settings.GlobalWebSearchSettings[key] = webSearchSettings;
        }

        // Apply configuration
        configure(webSearchSettings);

        return builder;
    }
}
