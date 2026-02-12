using Microsoft.Extensions.DependencyInjection;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for registering RAG services.
/// </summary>
public static class ServiceCollectionExtensionsRag
{
    /// <summary>
    /// Registers an IRagService implementation with optional cost configuration and factory key.
    /// </summary>
    /// <typeparam name="TService">RAG service implementation type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="configureCost">Optional action to configure RAG cost settings (pricing per token).</param>
    /// <param name="name">Factory key (string or enum). Use null for default implementation.</param>
    /// <returns>Service collection for chaining.</returns>
    /// <example>
    /// // Register with cost configuration and string key
    /// services.AddRagService&lt;AzureAISearchRagService&gt;(settings =>
    /// {
    ///     settings.CostPerThousandEmbeddingTokens = 0.0001m; // text-embedding-ada-002
    /// }, "azure");
    /// 
    /// // Register with enum key (use default cost)
    /// services.AddRagService&lt;PineconeRagService&gt;(name: RagProvider.Pinecone);
    /// 
    /// // Register as default with custom pricing
    /// services.AddRagService&lt;CustomRagService&gt;(settings =>
    /// {
    ///     settings.CostPerThousandEmbeddingTokens = 0.00002m; // text-embedding-3-small
    ///     settings.FixedCostPerSearch = 0.0001m; // Pinecone query cost
    /// });
    /// </example>
    public static IServiceCollection AddRagService<TService>(
        this IServiceCollection services,
        Action<RagCostSettings>? configureCost = null,
        AnyOf<string?, Enum>? name = null)
        where TService : class, IRagService
    {
        var key = name?.Value?.ToString() ?? string.Empty;

        // Register with factory
        services.AddFactory<IRagService, TService>(name);

        // Configure and save cost settings using PostConfigure to ensure it runs after Configure
        if (configureCost != null)
        {
            services.PostConfigure<PlayFrameworkSettings>(options =>
            {
                if (!options.RagCostSettings.ContainsKey(key))
                {
                    var costSettings = new RagCostSettings();
                    configureCost(costSettings);
                    options.RagCostSettings[key] = costSettings;
                }
            });
        }

        return services;
    }
}
