using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem;
using System.Reflection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring RAG (Retrieval-Augmented Generation) at scene level.
/// </summary>
public static class SceneBuilderRagExtensions
{
    /// <summary>
    /// Enables RAG (Retrieval-Augmented Generation) for this specific scene.
    /// Overrides global configuration if present.
    /// </summary>
    /// <param name="builder">Scene builder.</param>
    /// <param name="configure">Configuration action for RAG settings.</param>
    /// <param name="name">Factory key (string or enum) to identify the IRagService. Use null for default.</param>
    /// <returns>Builder for chaining.</returns>
    /// <example>
    /// builder.AddScene(scene => scene
    ///     .WithName("CustomerSupport")
    ///     .WithRag(settings =>
    ///     {
    ///         settings.TopK = 5;  // Override global TopK
    ///         settings.MinimumScore = 0.8;
    ///     }, "azure")
    /// );
    /// 
    /// // With enum
    /// builder.AddScene(scene => scene
    ///     .WithName("TechDocs")
    ///     .WithRag(settings =>
    ///     {
    ///         settings.TopK = 3;
    ///     }, RagProvider.Pinecone)
    /// );
    /// </example>
    public static SceneBuilder WithRag(
        this SceneBuilder builder,
        Action<RagSettings> configure,
        AnyOf<string?, Enum>? name = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var key = name?.Value?.ToString() ?? string.Empty;

        // Access private _config field via reflection
        var configField = typeof(SceneBuilder).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = (SceneConfiguration)configField!.GetValue(builder)!;

        // Create or update settings for this scene
        if (!config.RagSettings.TryGetValue(key, out var ragSettings))
        {
            ragSettings = new RagSettings { FactoryKey = key };
            config.RagSettings[key] = ragSettings;
        }

        // Apply configuration
        configure(ragSettings);

        // Note: RagTool will be registered automatically during scene resolution
        // based on the presence of RagSettings in SceneConfiguration

        return builder;
    }

    /// <summary>
    /// Disables RAG for this specific scene, even if enabled globally.
    /// </summary>
    /// <param name="builder">Scene builder.</param>
    /// <param name="name">Factory key to disable. Use null to disable default RAG.</param>
    /// <returns>Builder for chaining.</returns>
    /// <example>
    /// builder.AddScene(scene => scene
    ///     .WithName("NoRagScene")
    ///     .WithoutRag("azure")  // Disable Azure RAG for this scene
    /// );
    /// </example>
    public static SceneBuilder WithoutRag(
        this SceneBuilder builder,
        AnyOf<string?, Enum>? name = null)
    {
        var key = name?.Value?.ToString() ?? string.Empty;

        // Access private _config field via reflection
        var configField = typeof(SceneBuilder).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
        var config = (SceneConfiguration)configField!.GetValue(builder)!;

        // Mark as disabled for this scene
        config.RagSettings[key] = new RagSettings
        {
            Enabled = false,
            FactoryKey = key
        };

        return builder;
    }
}
