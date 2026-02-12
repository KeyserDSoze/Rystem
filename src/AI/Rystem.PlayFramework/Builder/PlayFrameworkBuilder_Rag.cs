using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring RAG (Retrieval-Augmented Generation) globally.
/// </summary>
public static class PlayFrameworkBuilderRagExtensions
{
    /// <summary>
    /// Enables RAG (Retrieval-Augmented Generation) for all scenes globally.
    /// Can be overridden at scene level.
    /// </summary>
    /// <param name="builder">PlayFramework builder.</param>
    /// <param name="configure">Configuration action for RAG settings.</param>
    /// <param name="name">Factory key (string or enum) to identify the IRagService. Use null for default.</param>
    /// <returns>Builder for chaining.</returns>
    /// <example>
    /// // Configure global RAG with Azure AI Search
    /// builder.WithRag(settings =>
    /// {
    ///     settings.TopK = 10;
    ///     settings.SearchAlgorithm = VectorSearchAlgorithm.CosineSimilarity;
    ///     settings.MinimumScore = 0.7;
    /// }, "azure");
    /// 
    /// // Configure with enum
    /// builder.WithRag(settings =>
    /// {
    ///     settings.TopK = 5;
    /// }, RagProvider.Pinecone);
    /// 
    /// // Configure default (no key)
    /// builder.WithRag(settings =>
    /// {
    ///     settings.TopK = 10;
    /// });
    /// </example>
    public static PlayFrameworkBuilder WithRag(
        this PlayFrameworkBuilder builder,
        Action<RagSettings> configure,
        AnyOf<string?, Enum>? name = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var key = name?.Value?.ToString() ?? string.Empty;

        // Create or update settings
        if (!builder.Settings.GlobalRagSettings.TryGetValue(key, out var ragSettings))
        {
            ragSettings = new RagSettings { FactoryKey = key };
            builder.Settings.GlobalRagSettings[key] = ragSettings;
        }
        // Apply configuration
        configure(ragSettings);

        return builder;
    }
}
