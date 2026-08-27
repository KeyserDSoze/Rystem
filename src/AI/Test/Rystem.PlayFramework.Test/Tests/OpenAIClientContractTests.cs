using Azure.Core;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Audio;
using Rystem.PlayFramework.Adapters;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Rystem.PlayFramework.Test.Tests;

#pragma warning disable OPENAI001
public sealed class OpenAIClientContractTests
{
    [Fact]
    public async Task Responses_UsesAzureV1ModelAndBearerApiKey()
    {
        var handler = new CaptureHandler(CreateResponsesResponse);
        var client = CreateClient(handler, useAzureCredential: false);
        var chatClient = client.GetResponsesClient().AsIChatClient("my-deployment");

        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Equal("https://name.openai.azure.com/openai/v1/responses", handler.RequestUri?.AbsoluteUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("SECRET-KEY", handler.AuthorizationParameter);
        Assert.False(handler.HasApiKeyHeader);
        Assert.DoesNotContain("api-version", handler.RequestUri?.Query, StringComparison.OrdinalIgnoreCase);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("my-deployment", payload.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task ChatCompletions_UsesAzureV1ModelAndNoApiVersion()
    {
        var handler = new CaptureHandler(CreateChatResponse);
        var client = CreateClient(handler, useAzureCredential: false);
        var chatClient = client.GetChatClient("my-deployment").AsIChatClient();

        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Equal("https://name.openai.azure.com/openai/v1/chat/completions", handler.RequestUri?.AbsoluteUri);
        Assert.DoesNotContain("api-version", handler.RequestUri?.Query, StringComparison.OrdinalIgnoreCase);
        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.Equal("my-deployment", payload.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task Entra_UsesConfiguredScopeAndBearerToken()
    {
        var handler = new CaptureHandler(CreateResponsesResponse);
        var credential = new RecordingTokenCredential();
        var client = CreateClient(handler, useAzureCredential: true, credential);
        var chatClient = client.GetResponsesClient().AsIChatClient("my-deployment");

        await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Equal(OpenAIClientFactory.EntraScope, Assert.Single(credential.Scopes));
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("ENTRA-TOKEN", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task Responses_SendsToolDeclarationAndStreamingRequest()
    {
        var handler = new CaptureHandler(CreateStreamingResponsesResponse);
        var client = CreateClient(handler, useAzureCredential: false);
        var chatClient = client.GetResponsesClient().AsIChatClient("my-deployment");
        var function = AIFunctionFactory.Create(
            (string city) => city,
            new AIFunctionFactoryOptions { Name = "search_weather", Description = "Search weather" });
        var options = new ChatOptions { Tools = [function] };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "weather")], options))
        {
            updates.Add(update);
        }

        using var payload = JsonDocument.Parse(handler.Body!);
        Assert.True(payload.RootElement.GetProperty("stream").GetBoolean());
        var tool = Assert.Single(payload.RootElement.GetProperty("tools").EnumerateArray());
        Assert.Equal("search_weather", tool.GetProperty("name").GetString());

        // A regression that dropped every `response.output_text.delta` event (e.g. an SDK/bridge
        // update that stopped surfacing text deltas) would still satisfy the request-side assertions
        // above, since those only inspect what Rystem sent. Assert on the response side too.
        Assert.NotEmpty(updates);
        var text = string.Concat(updates.Select(u => u.Text));
        Assert.Equal("weather is sunny", text);
    }

    [Fact]
    public async Task Responses_PropagatesServiceErrors()
    {
        var handler = new CaptureHandler(() => JsonResponse(
            """{"error":{"message":"too many requests","type":"rate_limit_error"}}""",
            HttpStatusCode.TooManyRequests));
        var client = CreateClient(handler, useAzureCredential: false);
        var chatClient = client.GetResponsesClient().AsIChatClient("my-deployment");

        await Assert.ThrowsAsync<ClientResultException>(() =>
            chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
    }

    [Fact]
    public async Task Responses_PropagatesCancellation()
    {
        var handler = new CaptureHandler(CreateResponsesResponse, waitForCancellation: true);
        var client = CreateClient(handler, useAzureCredential: false);
        var chatClient = client.GetResponsesClient().AsIChatClient("my-deployment");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "hi")],
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Audio_UsesDeploymentSpecificRouteApiVersionAndApiKeyHeader()
    {
        var handler = new CaptureHandler(CreateTranscriptionResponse);
        var client = OpenAIClientFactory.CreateAudioClient(
            new Uri("https://name.openai.azure.com/"),
            "speech-deployment",
            "SECRET-KEY",
            useAzureCredential: false,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)));
        using var audio = new MemoryStream([0, 1, 2, 3]);

        await client.TranscribeAudioAsync(audio, "sample.wav");

        Assert.Equal(
            "https://name.openai.azure.com/openai/deployments/speech-deployment/audio/transcriptions?api-version=2025-04-01-preview",
            handler.RequestUri?.AbsoluteUri);
        Assert.True(handler.HasApiKeyHeader);
        Assert.Equal("SECRET-KEY", handler.ApiKey);
        Assert.Null(handler.AuthorizationScheme);
    }

    [Fact]
    public async Task Speech_UsesPreviewApiVersionRequiredByAzureTts()
    {
        var handler = new CaptureHandler(CreateSpeechResponse);
        var client = OpenAIClientFactory.CreateAudioClient(
            new Uri("https://name.openai.azure.com/"),
            "tts-deployment",
            "SECRET-KEY",
            useAzureCredential: false,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)));

        await client.GenerateSpeechAsync("hello", GeneratedSpeechVoice.Alloy);

        Assert.Equal(
            "https://name.openai.azure.com/openai/deployments/tts-deployment/audio/speech?api-version=2025-04-01-preview",
            handler.RequestUri?.AbsoluteUri);
        Assert.True(handler.HasApiKeyHeader);
        Assert.Equal("SECRET-KEY", handler.ApiKey);
    }

    [Fact]
    public async Task Audio_Entra_UsesConfiguredScopeAndBearerToken()
    {
        var handler = new CaptureHandler(CreateTranscriptionResponse);
        var credential = new RecordingTokenCredential();
        var client = OpenAIClientFactory.CreateAudioClient(
            new Uri("https://name.openai.azure.com/"),
            "speech-deployment",
            apiKey: null,
            useAzureCredential: true,
            configure: options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
            transcriptionApiVersion: null,
            tokenProvider: credential);
        using var audio = new MemoryStream([0, 1, 2, 3]);

        await client.TranscribeAudioAsync(audio, "sample.wav");

        Assert.Equal(OpenAIClientFactory.AudioEntraScope, Assert.Single(credential.Scopes));
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("ENTRA-TOKEN", handler.AuthorizationParameter);
        Assert.False(handler.HasApiKeyHeader);
    }

    [Fact]
    public async Task Audio_PreservesCustomEndpointPathPrefix()
    {
        var handler = new CaptureHandler(CreateTranscriptionResponse);
        var client = OpenAIClientFactory.CreateAudioClient(
            new Uri("https://custom.example/api/v1"),
            "speech-deployment",
            "SECRET-KEY",
            useAzureCredential: false,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)));
        using var audio = new MemoryStream([0, 1, 2, 3]);

        await client.TranscribeAudioAsync(audio, "sample.wav");

        Assert.Equal(
            "https://custom.example/api/deployments/speech-deployment/audio/transcriptions?api-version=2025-04-01-preview",
            handler.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task Audio_AllowsOverridingTranscriptionApiVersion()
    {
        // AdapterSettings.SpeechToTextApiVersion / VoiceAdapterSettings.SttApiVersion thread through to
        // this override, so a resource/deployment that needs a different api-version than the library
        // default is not stuck waiting for a new NuGet release.
        var handler = new CaptureHandler(CreateTranscriptionResponse);
        var client = OpenAIClientFactory.CreateAudioClient(
            new Uri("https://name.openai.azure.com/"),
            "speech-deployment",
            "SECRET-KEY",
            useAzureCredential: false,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
            transcriptionApiVersion: "2024-10-21");
        using var audio = new MemoryStream([0, 1, 2, 3]);

        await client.TranscribeAudioAsync(audio, "sample.wav");

        Assert.Equal(
            "https://name.openai.azure.com/openai/deployments/speech-deployment/audio/transcriptions?api-version=2024-10-21",
            handler.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task VoiceTranscription_PreservesLanguageAndDurationWhenVerboseIsSupported()
    {
        var handler = new SequenceHandler(
            JsonResponse(
                """{"text":"hello","language":"en","duration":1.5,"words":[],"segments":[]}"""));
        var client = CreateAudioClient(handler, "stt-deployment");
        var adapter = new AzureOpenAIVoiceAdapter(client, client, new VoiceAdapterSettings());

        var result = await adapter.TranscribeAsync(new byte[] { 0, 1, 2, 3 }, "sample.wav");

        Assert.Equal("hello", result.Text);
        Assert.Equal("en", result.DetectedLanguage);
        Assert.Equal(1.5, result.DurationSeconds);
        Assert.Contains("verbose_json", Assert.Single(handler.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task VoiceTranscription_FallsBackToSimpleOnlyWhenVerboseIsRejected()
    {
        var handler = new SequenceHandler(
            JsonResponse(
                """{"error":{"message":"response_format 'verbose_json' is not compatible with this model","type":"invalid_request_error","param":"response_format","code":"unsupported_value"}}""",
                HttpStatusCode.BadRequest),
            JsonResponse("""{"text":"hello"}"""));
        var client = CreateAudioClient(handler, "stt-deployment");
        var adapter = new AzureOpenAIVoiceAdapter(client, client, new VoiceAdapterSettings());

        var result = await adapter.TranscribeAsync(new byte[] { 0, 1, 2, 3 }, "sample.wav");

        Assert.Equal("hello", result.Text);
        Assert.Null(result.DetectedLanguage);
        Assert.Null(result.DurationSeconds);
        Assert.Equal(2, handler.Bodies.Count);
        Assert.Contains("verbose_json", handler.Bodies[0], StringComparison.Ordinal);
        Assert.Contains("response_format", handler.Bodies[1], StringComparison.Ordinal);
        Assert.Contains("json", handler.Bodies[1], StringComparison.Ordinal);
        Assert.DoesNotContain("verbose_json", handler.Bodies[1], StringComparison.Ordinal);
    }

    private static OpenAIClient CreateClient(
        CaptureHandler handler,
        bool useAzureCredential,
        AuthenticationTokenProvider? tokenProvider = null)
    {
        return OpenAIClientFactory.Create(
            new Uri("https://name.openai.azure.com/"),
            "SECRET-KEY",
            useAzureCredential,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
            tokenProvider);
    }

    private static AudioClient CreateAudioClient(HttpMessageHandler handler, string deployment) =>
        OpenAIClientFactory.CreateAudioClient(
            new Uri("https://name.openai.azure.com/"),
            deployment,
            "SECRET-KEY",
            useAzureCredential: false,
            options => options.Transport = new HttpClientPipelineTransport(new HttpClient(handler)));

    private static HttpResponseMessage CreateResponsesResponse() => JsonResponse(
        """
        {"id":"resp_123","object":"response","created_at":1700000000,"status":"completed","model":"my-deployment","output":[],"parallel_tool_calls":true,"tool_choice":"auto","tools":[],"usage":{"input_tokens":1,"output_tokens":1,"total_tokens":2}}
        """);

    private static HttpResponseMessage CreateChatResponse() => JsonResponse(
        """
        {"id":"chatcmpl_123","object":"chat.completion","created":1700000000,"model":"my-deployment","choices":[{"index":0,"message":{"role":"assistant","content":"ok"},"finish_reason":"stop"}],"usage":{"prompt_tokens":1,"completion_tokens":1,"total_tokens":2}}
        """);

    private static HttpResponseMessage CreateStreamingResponsesResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            "data: {\"type\":\"response.output_text.delta\",\"sequence_number\":0,\"item_id\":\"msg_1\",\"output_index\":0,\"content_index\":0,\"delta\":\"weather is \"}\n\n" +
            "data: {\"type\":\"response.output_text.delta\",\"sequence_number\":1,\"item_id\":\"msg_1\",\"output_index\":0,\"content_index\":0,\"delta\":\"sunny\"}\n\n" +
            "data: {\"type\":\"response.completed\",\"sequence_number\":2,\"response\":{\"id\":\"resp_123\",\"object\":\"response\",\"created_at\":1700000000,\"status\":\"completed\",\"model\":\"my-deployment\",\"output\":[],\"parallel_tool_calls\":true,\"tool_choice\":\"auto\",\"tools\":[],\"usage\":{\"input_tokens\":1,\"output_tokens\":1,\"total_tokens\":2}}}\n\n" +
            "data: [DONE]\n\n",
            Encoding.UTF8,
            "text/event-stream")
    };

    private static HttpResponseMessage CreateTranscriptionResponse() => JsonResponse(
        """{"text":"transcribed","language":"en","duration":1.0}""");

    private static HttpResponseMessage CreateSpeechResponse()
    {
        var content = new ByteArrayContent([0, 1, 2, 3]);
        content.Headers.ContentType = new("application/octet-stream");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class CaptureHandler(
        Func<HttpResponseMessage> responseFactory,
        bool waitForCancellation = false) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public bool HasApiKeyHeader { get; private set; }
        public string? ApiKey { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            HasApiKeyHeader = request.Headers.Contains("api-key");
            ApiKey = request.Headers.TryGetValues("api-key", out var values)
                ? values.Single()
                : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (waitForCancellation)
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return responseFactory();
        }
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Bodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            return _responses.Dequeue();
        }
    }

    private sealed class RecordingTokenCredential : TokenCredential
    {
        public string[] Scopes { get; private set; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Scopes = requestContext.Scopes;
            return new AccessToken("ENTRA-TOKEN", DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(GetToken(requestContext, cancellationToken));
        }
    }
}
#pragma warning restore OPENAI001
