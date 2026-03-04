using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ClientModel;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// Extension methods for configuring LLM adapters for PlayFramework.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region Azure OpenAI

    /// <summary>
    /// Adds an Azure OpenAI adapter as the default <see cref="IChatClient"/>.
    /// </summary>
    public static IServiceCollection AddAdapterForAzureOpenAI(
        this IServiceCollection services,
        Action<AdapterSettings> configure)
    {
        return AddAdapterForAzureOpenAI(services, null, configure);
    }

    /// <summary>
    /// Adds an Azure OpenAI adapter as a named <see cref="IChatClient"/> using the factory pattern.
    /// Use a factory name matching your <c>AddPlayFramework("name", ...)</c> call.
    /// </summary>
    public static IServiceCollection AddAdapterForAzureOpenAI(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<AdapterSettings> configure)
    {
        var settings = new AdapterSettings();
        configure(settings);

        Validate(settings);

        services.AddFactory<IChatClient>(
            (sp, _) => CreateChatClient(sp, settings),
            name,
            ServiceLifetime.Singleton);

        return services;
    }

    private static IChatClient CreateChatClient(IServiceProvider sp, AdapterSettings settings)
    {
        var azureClient = CreateAzureOpenAIClient(settings);
        IChatClient chatClient;

        if (settings.UseResponsesApi)
        {
            chatClient = azureClient.GetResponsesClient(settings.Deployment).AsIChatClient();
        }
        else
        {
            chatClient = azureClient.GetChatClient(settings.Deployment).AsIChatClient();
        }

        // Wrap with MultiModalChatClient if using Responses API + file upload enabled
        if (settings.UseResponsesApi && settings.EnableFileUpload)
        {
            var fileClient = azureClient.GetOpenAIFileClient();
            var distributedCache = sp.GetService<IDistributedCache>();
            var memoryCache = sp.GetService<IMemoryCache>();
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<MultiModalChatClient>();
            chatClient = new MultiModalChatClient(chatClient, fileClient, distributedCache, memoryCache, logger);
        }

        return chatClient;
    }

    #endregion

    #region Azure OpenAI helpers

    private static AzureOpenAIClient CreateAzureOpenAIClient(AdapterSettings settings)
    {
        if (settings.UseAzureCredential)
        {
            return new AzureOpenAIClient(settings.Endpoint!, new DefaultAzureCredential());
        }

        return new AzureOpenAIClient(settings.Endpoint!, new ApiKeyCredential(settings.ApiKey!));
    }

    private static void Validate(AdapterSettings settings)
    {
        if (settings.Endpoint is null)
            throw new InvalidOperationException("AdapterSettings.Endpoint is required.");

        if (!settings.UseAzureCredential && string.IsNullOrEmpty(settings.ApiKey))
            throw new InvalidOperationException("Either ApiKey or UseAzureCredential must be set.");

        if (string.IsNullOrEmpty(settings.Deployment))
            throw new InvalidOperationException("AdapterSettings.Deployment is required.");
    }

    #endregion
}
