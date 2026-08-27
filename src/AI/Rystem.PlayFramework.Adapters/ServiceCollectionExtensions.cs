using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
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
        var endpoint = settings.Endpoint
            ?? throw new InvalidOperationException("AdapterSettings.Endpoint is required.");

        // Share a single credential instance across the chat/Responses client and the
        // optional audio client: DefaultAzureCredential probes multiple credential sources
        // on construction, so building it once per adapter avoids doing that work twice.
        var credential = settings.UseAzureCredential ? new DefaultAzureCredential() : null;
        var openAIClient = CreateOpenAIClient(settings, credential);
        IChatClient chatClient;

        if (settings.UseResponsesApi)
        {
            chatClient = openAIClient.GetResponsesClient().AsIChatClient(settings.Deployment);
        }
        else
        {
            chatClient = openAIClient.GetChatClient(settings.Deployment).AsIChatClient();
        }

        // Wrap with MultiModalChatClient if using Responses API + file upload enabled
        if (settings.UseResponsesApi && settings.EnableFileUpload)
        {
            var fileClient = openAIClient.GetOpenAIFileClient();
            var distributedCache = sp.GetService<IDistributedCache>();
            var memoryCache = sp.GetService<IMemoryCache>();
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<MultiModalChatClient>();
            chatClient = new MultiModalChatClient(chatClient, fileClient, distributedCache, memoryCache, logger);
        }

        // Wrap with SpeechToTextChatClient if audio mode is SpeechToText
        if (settings.AudioMode == AudioMode.SpeechToText)
        {
            var speechToTextDeployment = settings.SpeechToTextDeployment
                ?? throw new InvalidOperationException(
                    "AdapterSettings.SpeechToTextDeployment is required when AudioMode is SpeechToText.");
            var audioClient = OpenAIClientFactory.CreateAudioClient(
                endpoint,
                speechToTextDeployment,
                settings.ApiKey,
                settings.UseAzureCredential,
                configure: null,
                transcriptionApiVersion: settings.SpeechToTextApiVersion,
                tokenProvider: credential);
            var sttLogger = sp.GetService<ILoggerFactory>()?.CreateLogger<SpeechToTextChatClient>();
            chatClient = new SpeechToTextChatClient(chatClient, audioClient, sttLogger);
        }

        // Wrap with CostTrackingChatClient so the adapter owns its own cost calculation.
        // ChatClientManager reads cost from ChatResponse.AdditionalProperties — no DI wiring needed.
        if (settings.CostTracking != null)
            chatClient = new CostTrackingChatClient(chatClient, settings.CostTracking);

        return chatClient;
    }

    #endregion

    #region Azure OpenAI helpers

    private static OpenAIClient CreateOpenAIClient(AdapterSettings settings, AuthenticationTokenProvider? credential)
    {
        var endpoint = settings.Endpoint
            ?? throw new InvalidOperationException("AdapterSettings.Endpoint is required.");
        return OpenAIClientFactory.Create(
            endpoint,
            settings.ApiKey,
            settings.UseAzureCredential,
            configure: null,
            tokenProvider: credential);
    }

    private static void Validate(AdapterSettings settings)
    {
        if (settings.Endpoint is null)
            throw new InvalidOperationException("AdapterSettings.Endpoint is required.");

        NormalizeOrThrowInvalidOperation(settings.Endpoint, nameof(AdapterSettings.Endpoint));

        if (!settings.UseAzureCredential && string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Either ApiKey or UseAzureCredential must be set.");

        if (settings.UseAzureCredential && !string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("AdapterSettings.ApiKey must not be set when UseAzureCredential is enabled.");

        if (string.IsNullOrEmpty(settings.Deployment))
            throw new InvalidOperationException("AdapterSettings.Deployment is required.");

        if (settings.AudioMode == AudioMode.SpeechToText && string.IsNullOrEmpty(settings.SpeechToTextDeployment))
            throw new InvalidOperationException(
                "AdapterSettings.SpeechToTextDeployment is required when AudioMode is SpeechToText. " +
                "Set it to the deployment name of your Whisper model (e.g., \"whisper\").");

        ValidateApiVersion(settings.SpeechToTextApiVersion, nameof(AdapterSettings.SpeechToTextApiVersion));
    }

    /// <summary>
    /// Runs <see cref="AzureOpenAIEndpoint.Normalize"/> purely for validation and surfaces any
    /// rejection as <see cref="InvalidOperationException"/>, consistent with the other configuration
    /// errors thrown by this class, instead of leaking the normalizer's internal <see cref="ArgumentException"/>
    /// (which refers to a parameter named "endpoint" rather than the public settings property).
    /// </summary>
    private static void NormalizeOrThrowInvalidOperation(Uri endpoint, string propertyName)
    {
        try
        {
            AzureOpenAIEndpoint.Normalize(endpoint);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"{propertyName} is not a valid Azure OpenAI endpoint: {exception.Message}", exception);
        }
    }

    private static void ValidateApiVersion(string? apiVersion, string propertyName)
    {
        if (apiVersion is not null && string.IsNullOrWhiteSpace(apiVersion))
            throw new InvalidOperationException($"{propertyName} must not be empty or whitespace.");
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

        if (settings.CostTracking != null)
            services.AddAudioCostTracking(name, settings.CostTracking);

        return services;
    }

    private static IVoiceAdapter CreateVoiceAdapter(IServiceProvider sp, VoiceAdapterSettings settings)
    {
        var endpoint = settings.Endpoint
            ?? throw new InvalidOperationException("VoiceAdapterSettings.Endpoint is required.");

        // Share a single credential instance between the STT and TTS clients (see CreateChatClient).
        var credential = settings.UseAzureCredential ? new DefaultAzureCredential() : null;
        var sttClient = OpenAIClientFactory.CreateAudioClient(
            endpoint, settings.SttDeployment, settings.ApiKey, settings.UseAzureCredential,
            configure: null, transcriptionApiVersion: settings.SttApiVersion, tokenProvider: credential);
        var ttsClient = OpenAIClientFactory.CreateAudioClient(
            endpoint, settings.TtsDeployment, settings.ApiKey, settings.UseAzureCredential,
            configure: null, transcriptionApiVersion: null, tokenProvider: credential);
        var logger = sp.GetService<ILoggerFactory>()?.CreateLogger<AzureOpenAIVoiceAdapter>();

        return new AzureOpenAIVoiceAdapter(sttClient, ttsClient, settings, logger);
    }

    private static void ValidateVoiceSettings(VoiceAdapterSettings settings)
    {
        if (settings.Endpoint is null)
            throw new InvalidOperationException("VoiceAdapterSettings.Endpoint is required.");

        NormalizeOrThrowInvalidOperation(settings.Endpoint, nameof(VoiceAdapterSettings.Endpoint));

        if (!settings.UseAzureCredential && string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("Either ApiKey or UseAzureCredential must be set on VoiceAdapterSettings.");

        if (settings.UseAzureCredential && !string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("VoiceAdapterSettings.ApiKey must not be set when UseAzureCredential is enabled.");

        if (string.IsNullOrEmpty(settings.SttDeployment))
            throw new InvalidOperationException("VoiceAdapterSettings.SttDeployment is required (e.g., \"whisper\").");

        if (string.IsNullOrEmpty(settings.TtsDeployment))
            throw new InvalidOperationException("VoiceAdapterSettings.TtsDeployment is required (e.g., \"tts-1\").");

        ValidateApiVersion(settings.SttApiVersion, nameof(VoiceAdapterSettings.SttApiVersion));
    }

    #endregion
}
