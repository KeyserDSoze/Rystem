using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.ClientModel;

namespace Rystem.PlayFramework.Adapters.FoundryLocal;

/// <summary>
/// Extension methods for configuring Foundry Local adapter for PlayFramework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Foundry Local adapter as the default <see cref="IChatClient"/>.
    /// Foundry Local runs AI models locally — downloads, loads, and starts the web service automatically.
    /// </summary>
    public static IServiceCollection AddAdapterForFoundryLocal(
        this IServiceCollection services,
        Action<FoundryLocalSettings> configure)
    {
        return AddAdapterForFoundryLocal(services, null, configure);
    }

    /// <summary>
    /// Adds a Foundry Local adapter as a named <see cref="IChatClient"/> using the factory pattern.
    /// Use a factory name matching your <c>AddPlayFramework("name", ...)</c> call.
    /// <para>
    /// Uses the <c>Microsoft.AI.Foundry.Local</c> SDK to automatically download, load,
    /// and start a local web service, then connects via the OpenAI SDK.
    /// </para>
    /// </summary>
    public static IServiceCollection AddAdapterForFoundryLocal(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<FoundryLocalSettings> configure)
    {
        var settings = new FoundryLocalSettings();
        configure(settings);

        Validate(settings);

        services.AddFactory<IChatClient>(
            (sp, _) => CreateChatClient(sp, settings),
            name,
            ServiceLifetime.Singleton);

        return services;
    }

    private static IChatClient CreateChatClient(IServiceProvider sp, FoundryLocalSettings settings)
    {
        var config = new Microsoft.AI.Foundry.Local.Configuration
        {
            AppName = settings.AppName,
            LogLevel = settings.FoundryLogLevel,
            Web = new Microsoft.AI.Foundry.Local.Configuration.WebService
            {
                Urls = settings.WebServiceUrl
            }
        };

        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("FoundryLocal");

        // Initialize the Foundry Local manager (singleton)
        Microsoft.AI.Foundry.Local.FoundryLocalManager
            .CreateAsync(config, logger)
            .GetAwaiter().GetResult();
        var mgr = Microsoft.AI.Foundry.Local.FoundryLocalManager.Instance;

        // Get model from catalog
        var catalog = mgr.GetCatalogAsync().GetAwaiter().GetResult();
        var model = catalog.GetModelAsync(settings.Model).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException(
                $"Model '{settings.Model}' not found in Foundry Local catalog. " +
                $"Run 'foundry model list' to see available models.");

        // Download (skips if already cached)
        model.DownloadAsync(settings.OnDownloadProgress ?? (_ => { }))
            .GetAwaiter().GetResult();

        // Load model into memory
        model.LoadAsync().GetAwaiter().GetResult();

        // Start the OpenAI-compatible web service
        mgr.StartWebServiceAsync().GetAwaiter().GetResult();

        logger?.LogInformation("Foundry Local: model '{Model}' loaded, web service at {Url}/v1",
            settings.Model, settings.WebServiceUrl);

        // Create OpenAI client pointed at the local web service
        var client = new OpenAIClient(
            new ApiKeyCredential("foundry-local"),
            new OpenAIClientOptions { Endpoint = new Uri(settings.WebServiceUrl + "/v1") });

        return client.GetChatClient(model.Id).AsIChatClient();
    }

    private static void Validate(FoundryLocalSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Model))
            throw new InvalidOperationException("FoundryLocalSettings.Model is required.");

        if (string.IsNullOrEmpty(settings.WebServiceUrl))
            throw new InvalidOperationException("FoundryLocalSettings.WebServiceUrl is required.");
    }
}
