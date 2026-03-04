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

        // Wrap with SpeechToTextChatClient if audio mode is SpeechToText
        if (settings.AudioMode == AudioMode.SpeechToText)
        {
            var audioClient = azureClient.GetAudioClient(settings.SpeechToTextDeployment!);
            var sttLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<SpeechToTextChatClient>();
            chatClient = new SpeechToTextChatClient(chatClient, audioClient, sttLogger);
        }

        return chatClient;
    }

    #endregion

    #region Azure OpenAI helpers

    private static AzureOpenAIClient CreateAzureOpenAIClient(AdapterSettings settings)
    {
        return CreateAzureOpenAIClient(settings.Endpoint!, settings.ApiKey, settings.UseAzureCredential);
    }

    private static void Validate(AdapterSettings settings)
    {
        if (settings.Endpoint is null)
            throw new InvalidOperationException("AdapterSettings.Endpoint is required.");

        if (!settings.UseAzureCredential && string.IsNullOrEmpty(settings.ApiKey))
            throw new InvalidOperationException("Either ApiKey or UseAzureCredential must be set.");

        if (string.IsNullOrEmpty(settings.Deployment))
            throw new InvalidOperationException("AdapterSettings.Deployment is required.");

        if (settings.AudioMode == AudioMode.SpeechToText && string.IsNullOrEmpty(settings.SpeechToTextDeployment))
            throw new InvalidOperationException(
                "AdapterSettings.SpeechToTextDeployment is required when AudioMode is SpeechToText. " +
                "Set it to the deployment name of your Whisper model (e.g., \"whisper\").");
    }

    #endregion

    #region Azure OpenAI Voice Adapter

    /// <summary>
    /// Registers an <see cref="IVoiceAdapter"/> backed by Azure OpenAI (Whisper + TTS).
    /// Use the returned factory name in <c>.WithVoice(name)</c>.
    /// </summary>
    public static IServiceCollection AddVoiceAdapterForAzureOpenAI(
        this IServiceCollection services,
        Action<VoiceAdapterSettings> configure)
    {
        return AddVoiceAdapterForAzureOpenAI(services, null, configure);
    }

    /// <summary>
    /// Registers a named <see cref="IVoiceAdapter"/> backed by Azure OpenAI (Whisper + TTS).
    /// </summary>
    public static IServiceCollection AddVoiceAdapterForAzureOpenAI(
        this IServiceCollection services,
        AnyOf<string?, Enum>? name,
        Action<VoiceAdapterSettings> configure)
    {
        var settings = new VoiceAdapterSettings();
        configure(settings);

        ValidateVoiceSettings(settings);

        services.AddFactory<IVoiceAdapter>(
            (sp, _) => CreateVoiceAdapter(sp, settings),
            name,
            ServiceLifetime.Singleton);

        return services;
    }

    private static IVoiceAdapter CreateVoiceAdapter(IServiceProvider sp, VoiceAdapterSettings settings)
    {
        var azureClient = CreateAzureOpenAIClient(settings.Endpoint!, settings.ApiKey, settings.UseAzureCredential);
        var sttClient = azureClient.GetAudioClient(settings.SttDeployment);
        var ttsClient = azureClient.GetAudioClient(settings.TtsDeployment);
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<AzureOpenAIVoiceAdapter>();

        return new AzureOpenAIVoiceAdapter(sttClient, ttsClient, settings, logger);
    }

    private static void ValidateVoiceSettings(VoiceAdapterSettings settings)
    {
        if (settings.Endpoint is null)
            throw new InvalidOperationException("VoiceAdapterSettings.Endpoint is required.");

        if (!settings.UseAzureCredential && string.IsNullOrEmpty(settings.ApiKey))
            throw new InvalidOperationException("Either ApiKey or UseAzureCredential must be set on VoiceAdapterSettings.");

        if (string.IsNullOrEmpty(settings.SttDeployment))
            throw new InvalidOperationException("VoiceAdapterSettings.SttDeployment is required (e.g., \"whisper\").");

        if (string.IsNullOrEmpty(settings.TtsDeployment))
            throw new InvalidOperationException("VoiceAdapterSettings.TtsDeployment is required (e.g., \"tts-1\").");
    }

    #endregion

    #region Shared helpers

    private static AzureOpenAIClient CreateAzureOpenAIClient(Uri endpoint, string? apiKey, bool useAzureCredential)
    {
        if (useAzureCredential)
        {
            return new AzureOpenAIClient(endpoint, new DefaultAzureCredential());
        }

        return new AzureOpenAIClient(endpoint, new ApiKeyCredential(apiKey!));
    }

    #endregion
}
