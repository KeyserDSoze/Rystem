using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rystem.PlayFramework.Adapters;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Base class for PlayFramework tests with dependency injection setup.
/// </summary>
public abstract class PlayFrameworkTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected IConfiguration Configuration { get; }
    protected OpenAiSettings OpenAiSettings { get; }
    protected bool UseRealAzureOpenAI { get; init; }
    protected Uri? AzureEndpoint { get; private set; }
    protected string? AzureApiKey { get; private set; }
    protected string? AzureDeployment { get; private set; }

    protected PlayFrameworkTestBase(
        bool useRealAzureOpenAI = false,
        bool useAzureCredential = false,
        string? deploymentEnvironmentVariable = null,
        Action<AdapterSettings>? configureAdapter = null)
    {
        UseRealAzureOpenAI = useRealAzureOpenAI;

        // Build configuration with user secrets
        Configuration = AzureLiveTestConfiguration.BuildConfiguration();

        // Load OpenAI settings
        OpenAiSettings = new OpenAiSettings();
        Configuration.GetSection("OpenAi").Bind(OpenAiSettings);

        // Build service collection
        var services = new ServiceCollection();

        // Register logging (required for SceneManager)
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        // Register configuration
        services.AddSingleton(Configuration);
        services.AddSingleton(OpenAiSettings);

        // Live tests always use the production adapter and fail fast on missing configuration.
        if (UseRealAzureOpenAI)
        {
            var endpoint = AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_ENDPOINT", Configuration);
            var deployment = deploymentEnvironmentVariable is null
                ? AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_DEPLOYMENT", Configuration)
                : AzureLiveTestConfiguration.Resolve(deploymentEnvironmentVariable, Configuration);
            var apiKey = useAzureCredential
                ? null
                : AzureLiveTestConfiguration.Resolve("AZURE_OPENAI_API_KEY", Configuration);

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is required for Azure OpenAI live tests.");
            if (string.IsNullOrWhiteSpace(deployment))
                throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT is required for Azure OpenAI live tests.");
            if (!useAzureCredential && string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("AZURE_OPENAI_API_KEY is required for the API key live test suite.");

            AzureEndpoint = new Uri(endpoint);
            AzureApiKey = apiKey;
            AzureDeployment = deployment;

            services.AddAdapterForAzureOpenAI(settings =>
            {
                settings.Endpoint = AzureEndpoint;
                settings.ApiKey = apiKey;
                settings.UseAzureCredential = useAzureCredential;
                settings.Deployment = deployment;
                settings.CostTracking = new TokenCostSettings
                {
                    InputTokenCostPer1K = 0.001m,
                    OutputTokenCostPer1K = 0.003m
                };
                configureAdapter?.Invoke(settings);
            });
        }
        else
        {
            services.AddSingleton<IChatClient>(sp => new MockChatClient());
        }

        // Configure PlayFramework
        ConfigurePlayFramework(services);

        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method to configure PlayFramework for specific tests.
    /// </summary>
    protected virtual void ConfigurePlayFramework(IServiceCollection services)
    {
        // Default: no configuration
        // Override in derived classes to add scenes, actors, etc.
    }

    /// <summary>
    /// Creates a mock chat client that returns a fixed response.
    /// </summary>
    public static IChatClient CreateMockChatClient(string response = "Mock response")
    {
        return new MockChatClient(response);
    }

    /// <summary>
    /// Creates a mock chat client that captures all messages sent to it.
    /// </summary>
    public static IChatClient CreateMockChatClient(string response, List<string> capturedMessages)
    {
        return new MockChatClient(response, capturedMessages);
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Mock IChatClient for testing without actual LLM calls.
/// </summary>
internal sealed class MockChatClient : IChatClient
{
    private readonly string _defaultResponse;
    private readonly List<string>? _capturedMessages;

    public MockChatClient(string defaultResponse = "Mock response", List<string>? capturedMessages = null)
    {
        _defaultResponse = defaultResponse;
        _capturedMessages = capturedMessages;
    }

    public ChatClientMetadata Metadata => new("mock-provider", new Uri("http://localhost"), "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Capture messages if requested
        if (_capturedMessages != null)
        {
            foreach (var message in messages)
            {
                if (message.Text != null)
                {
                    _capturedMessages.Add(message.Text);
                }
            }
        }

        var lastMessage = messages.LastOrDefault();
        var responseText = string.IsNullOrEmpty(_defaultResponse) 
            ? $"Mock response to: {lastMessage?.Text ?? "empty"}"
            : _defaultResponse;

        var response = new ChatResponse(
            [new ChatMessage(ChatRole.Assistant, responseText)]
        )
        {
            ModelId = "mock-model",
            Usage = new UsageDetails
            {
                InputTokenCount = 100,
                OutputTokenCount = 200
            }
        };

        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GetStreamingResponseAsyncCore(messages, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsyncCore(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        var responseText = string.IsNullOrEmpty(_defaultResponse) ? "Mock streaming response" : _defaultResponse;
        yield return new ChatResponseUpdate(ChatRole.Assistant, responseText);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
