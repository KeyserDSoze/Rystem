using Azure.Identity;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace Rystem.PlayFramework.Adapters;

internal static class OpenAIClientFactory
{
    internal const string EntraScope = "https://ai.azure.com/.default";
    internal const string AudioEntraScope = "https://cognitiveservices.azure.com/.default";

    /// <summary>
    /// Default Azure OpenAI Audio Transcriptions api-version. <c>2025-04-01-preview</c> is required to
    /// use <c>gpt-4o-transcribe</c> and <c>gpt-4o-mini-transcribe</c> deployments: the prior GA surface
    /// (<c>2024-10-21</c>) only documents <c>whisper-1</c>-family deployments and can reject requests for
    /// the newer models before the adapter's own <c>verbose_json</c> -&gt; <c>json</c> response-format
    /// fallback ever gets a chance to run. Overridable per adapter via
    /// <see cref="AdapterSettings.SpeechToTextApiVersion"/> / <see cref="VoiceAdapterSettings.SttApiVersion"/>
    /// in case a specific resource or deployment requires a different version.
    /// </summary>
    internal const string AudioTranscriptionApiVersion = "2025-04-01-preview";
    internal const string AudioSpeechApiVersion = "2025-04-01-preview";

    public static OpenAIClient Create(
        Uri endpoint,
        string? apiKey,
        bool useAzureCredential,
        Action<OpenAIClientOptions>? configure = null)
    {
        return Create(endpoint, apiKey, useAzureCredential, configure, null);
    }

    internal static OpenAIClient Create(
        Uri endpoint,
        string? apiKey,
        bool useAzureCredential,
        Action<OpenAIClientOptions>? configure,
        AuthenticationTokenProvider? tokenProvider)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = AzureOpenAIEndpoint.Normalize(endpoint)
        };
        configure?.Invoke(options);

        if (useAzureCredential)
        {
            var authenticationPolicy = new BearerTokenPolicy(
                tokenProvider ?? new DefaultAzureCredential(),
                EntraScope);
            return new OpenAIClient(authenticationPolicy, options);
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("An API key is required when Azure credential authentication is disabled.", nameof(apiKey));

        return new OpenAIClient(new ApiKeyCredential(apiKey), options);
    }

    public static AudioClient CreateAudioClient(
        Uri endpoint,
        string deployment,
        string? apiKey,
        bool useAzureCredential,
        Action<OpenAIClientOptions>? configure = null,
        string? transcriptionApiVersion = null)
    {
        return CreateAudioClient(endpoint, deployment, apiKey, useAzureCredential, configure, transcriptionApiVersion, null);
    }

    internal static AudioClient CreateAudioClient(
        Uri endpoint,
        string deployment,
        string? apiKey,
        bool useAzureCredential,
        Action<OpenAIClientOptions>? configure,
        string? transcriptionApiVersion,
        AuthenticationTokenProvider? tokenProvider)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = CreateAudioEndpoint(endpoint, deployment)
        };
        options.AddPolicy(
            new AudioApiVersionPolicy(transcriptionApiVersion ?? AudioTranscriptionApiVersion),
            PipelinePosition.PerCall);
        configure?.Invoke(options);

        AuthenticationPolicy authenticationPolicy;
        if (useAzureCredential)
        {
            authenticationPolicy = new BearerTokenPolicy(
                tokenProvider ?? new DefaultAzureCredential(),
                AudioEntraScope);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException(
                    "An API key is required when Azure credential authentication is disabled.",
                    nameof(apiKey));

            authenticationPolicy = ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(
                new ApiKeyCredential(apiKey),
                "api-key");
        }

        return new AudioClient(deployment, authenticationPolicy, options);
    }

    /// <summary>
    /// Builds the deployment-specific Audio endpoint required by the Azure v1 Audio routes.
    /// Preserves any custom path prefix in front of the normalized <c>v1</c> segment
    /// (e.g. <c>https://host/api/v1</c> -&gt; <c>https://host/api/deployments/{deployment}</c>)
    /// instead of replacing the whole path, so custom (non-Azure-root) endpoints keep working
    /// the same way they do for Chat and Responses.
    /// </summary>
    private static Uri CreateAudioEndpoint(Uri endpoint, string deployment)
    {
        var normalized = AzureOpenAIEndpoint.Normalize(endpoint);
        var path = normalized.AbsolutePath.TrimEnd('/');
        const string v1Suffix = "/v1";
        if (path.EndsWith(v1Suffix, StringComparison.OrdinalIgnoreCase))
            path = path[..^v1Suffix.Length];

        var builder = new UriBuilder(normalized)
        {
            Path = $"{path}/deployments/{Uri.EscapeDataString(deployment)}"
        };
        return builder.Uri;
    }

    private sealed class AudioApiVersionPolicy(string transcriptionApiVersion) : PipelinePolicy
    {
        public override void Process(
            PipelineMessage message,
            IReadOnlyList<PipelinePolicy> pipeline,
            int currentIndex)
        {
            AddApiVersion(message);
            ProcessNext(message, pipeline, currentIndex);
        }

        public override ValueTask ProcessAsync(
            PipelineMessage message,
            IReadOnlyList<PipelinePolicy> pipeline,
            int currentIndex)
        {
            AddApiVersion(message);
            return ProcessNextAsync(message, pipeline, currentIndex);
        }

        private void AddApiVersion(PipelineMessage message)
        {
            var requestUri = message.Request.Uri
                ?? throw new InvalidOperationException("The audio request URI is not available.");
            var builder = new UriBuilder(requestUri);
            var query = builder.Query.TrimStart('?');
            var apiVersion = builder.Path.EndsWith("/audio/speech", StringComparison.OrdinalIgnoreCase)
                ? AudioSpeechApiVersion
                : transcriptionApiVersion;
            builder.Query = string.IsNullOrEmpty(query)
                ? $"api-version={apiVersion}"
                : $"{query}&api-version={apiVersion}";
            message.Request.Uri = builder.Uri;
        }
    }
}
