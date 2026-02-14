using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for registering chat clients with PlayFramework.
/// </summary>
public static class ServiceCollectionExtensions_ChatClient
{
    /// <summary>
    /// Registers a chat client with associated token cost settings for the factory pattern.
    /// Clients are created lazily on-demand to optimize startup time and memory usage.
    /// </summary>
    /// <typeparam name="TChatClient">The IChatClient implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">
    /// Factory key for this client (e.g., "gpt-4o", "gpt-4o-mini", "llama3").
    /// Use null or empty string for the default unnamed registration.
    /// </param>
    /// <param name="clientFactory">
    /// Factory function to create the chat client instance.
    /// Called lazily when the client is first needed.
    /// </param>
    /// <param name="costSettings">
    /// Token cost settings for calculating API costs.
    /// If null, cost tracking is disabled for this client.
    /// </param>
    /// <param name="serviceLifetime">
    /// Service lifetime for the chat client. Default: Singleton (recommended for stateless clients).
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Register primary GPT-4o with cost tracking
    /// services.AddChatClient&lt;IChatClient&gt;(
    ///     name: "gpt-4o",
    ///     clientFactory: sp =>
    ///     {
    ///         var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///         var azureClient = new AzureOpenAIClient(
    ///             new Uri(config["AzureOpenAI:Endpoint"]!),
    ///             new AzureKeyCredential(config["AzureOpenAI:ApiKey"]!));
    ///         return azureClient.AsChatClient("gpt-4o");
    ///     },
    ///     costSettings: new TokenCostSettings
    ///     {
    ///         Enabled = true,
    ///         Currency = "USD",
    ///         InputCostPer1MTokens = 2.50m,
    ///         OutputCostPer1MTokens = 10.00m,
    ///         CachedInputCostPer1MTokens = 1.25m
    ///     });
    ///
    /// // Register fallback GPT-4o-mini (cheaper model)
    /// services.AddChatClient&lt;IChatClient&gt;(
    ///     name: "gpt-4o-mini",
    ///     clientFactory: sp =>
    ///     {
    ///         var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///         var azureClient = new AzureOpenAIClient(
    ///             new Uri(config["AzureOpenAI:Endpoint"]!),
    ///             new AzureKeyCredential(config["AzureOpenAI:ApiKey"]!));
    ///         return azureClient.AsChatClient("gpt-4o-mini");
    ///     },
    ///     costSettings: new TokenCostSettings
    ///     {
    ///         Enabled = true,
    ///         Currency = "USD",
    ///         InputCostPer1MTokens = 0.15m,
    ///         OutputCostPer1MTokens = 0.60m
    ///     });
    ///
    /// // Configure PlayFramework to use fallback chain
    /// services.AddPlayFramework(builder =>
    /// {
    ///     builder.WithChatClients(["gpt-4o", "gpt-4o-mini"])
    ///            .WithFallbackMode(FallbackMode.Sequential);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddChatClient<TChatClient>(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<TokenCostSettings>? costSettings = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TChatClient : class, IChatClient
    {
        // Register the chat client with factory pattern
        services.AddFactory<IChatClient, TChatClient>(
            name: name,
            lifetime: serviceLifetime);

        var costSettingsValue = new TokenCostSettings { Enabled = false };
        if (costSettings != null)
            costSettings.Invoke(costSettingsValue);

        // Register token cost settings with same factory key
        services.AddFactory(
            costSettingsValue,
            name: name,
            lifetime: ServiceLifetime.Singleton);

        return services;
    }
}
