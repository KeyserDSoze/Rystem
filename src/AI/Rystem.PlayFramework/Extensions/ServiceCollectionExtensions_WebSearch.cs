using Microsoft.Extensions.DependencyInjection;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for registering web search services.
/// </summary>
public static class ServiceCollectionExtensionsWebSearch
{
    /// <summary>
    /// Registers an IWebSearchService implementation with optional cost configuration and factory key.
    /// </summary>
    /// <typeparam name="TService">Web search service implementation type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <param name="configureCost">Optional action to configure web search cost settings (pricing per search).</param>
    /// <param name="name">Factory key (string or enum). Use null for default implementation.</param>
    /// <returns>Service collection for chaining.</returns>
    /// <example>
    /// // Register Bing Search with cost configuration
    /// services.AddWebSearchService&lt;BingSearchService&gt;(settings =>
    /// {
    ///     settings.CostPerSearch = 0.003m;  // Bing pricing
    ///     settings.MonthlyQuota = 1000;     // Track quota
    /// }, "bing");
    /// 
    /// // Register with enum key
    /// services.AddWebSearchService&lt;GoogleSearchService&gt;(settings =>
    /// {
    ///     settings.CostPerSearch = 0.005m;
    /// }, WebSearchProvider.Google);
    /// 
    /// // Register as default (no cost config)
    /// services.AddWebSearchService&lt;CustomSearchService&gt;();
    /// </example>
    public static IServiceCollection AddWebSearchService<TService>(
        this IServiceCollection services,
        Action<WebSearchCostSettings>? configureCost = null,
        AnyOf<string?, Enum>? name = null)
        where TService : class, IWebSearchService
    {
        var key = name?.Value?.ToString() ?? string.Empty;

        // Register with factory
        services.AddFactory<IWebSearchService, TService>(name);

        // Configure and save cost settings using PostConfigure
        if (configureCost != null)
        {
            services.PostConfigure<PlayFrameworkSettings>(options =>
            {
                if (!options.WebSearchCostSettings.ContainsKey(key))
                {
                    var costSettings = new WebSearchCostSettings();
                    configureCost(costSettings);
                    options.WebSearchCostSettings[key] = costSettings;
                }
            });
        }

        return services;
    }
}
