using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    protected PlayFrameworkTestBase(bool useRealAzureOpenAI = false)
    {
        UseRealAzureOpenAI = useRealAzureOpenAI;

        // Build configuration with user secrets
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<PlayFrameworkTestBase>()
            .Build();

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

        // Register IChatClient - real or mock based on parameter
        if (UseRealAzureOpenAI && !string.IsNullOrEmpty(OpenAiSettings.ApiKey))
        {
            services.AddSingleton<IChatClient>(sp => new AzureOpenAIChatClientAdapter(
                OpenAiSettings.Endpoint,
                OpenAiSettings.ApiKey,
                OpenAiSettings.ModelName));
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
            ModelId = "mock-model"
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
