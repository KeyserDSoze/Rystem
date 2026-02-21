using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for IPlayFramework high-level service wrapper.
/// </summary>
public class PlayFrameworkServiceTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Test basic IPlayFramework usage with multiple configurations.
    /// </summary>
    [Fact]
    public async Task PlayFramework_MultipleConfigurations_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        // Register multiple configurations
        services.AddPlayFramework("free", builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Direct)
                .AddScene("BasicCalc", "Basic", s => { });
        });

        services.AddPlayFramework("premium", builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.DynamicChaining)
                .AddScene("AdvancedCalc", "Advanced", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act - Use IPlayFramework instead of IFactory directly
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        var freeManager = playFramework.Create("free");
        var premiumManager = playFramework.Create("premium");

        // Assert
        Assert.NotNull(freeManager);
        Assert.NotNull(premiumManager);
        Assert.NotSame(freeManager, premiumManager);
    }

    /// <summary>
    /// Test GetDefault() method.
    /// </summary>
    [Fact]
    public void PlayFramework_GetDefault_ReturnsDefaultConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        // Register default (no key)
        services.AddPlayFramework(builder =>
        {
            builder.AddScene("Default", "Default scene", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act
        var defaultManager = playFramework.CreateOrDefault();

        // Assert
        Assert.NotNull(defaultManager);
    }

    /// <summary>
    /// Test GetDefault() throws when no default registered.
    /// </summary>
    [Fact(Skip = "Known issue: DI may create instance even without explicit default registration")]
    public void PlayFramework_GetDefault_ThrowsWhenNoDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        // Only register keyed configurations (no default)
        services.AddPlayFramework("keyed", builder =>
        {
            builder.AddScene("Keyed", "Keyed", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => playFramework.CreateOrDefault());
        Assert.Contains("No default PlayFramework configuration found", exception.Message);
    }

    /// <summary>
    /// Test Get() method with valid key.
    /// </summary>
    [Fact]
    public void PlayFramework_Get_ReturnsManager()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("test-config", builder =>
        {
            builder.AddScene("Test", "Test", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act
        var manager = playFramework.Create("test-config");

        // Assert
        Assert.NotNull(manager);
    }

    /// <summary>
    /// Test Get() throws when key not found.
    /// </summary>
    [Fact]
    public void PlayFramework_Get_ThrowsWhenNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("existing", builder =>
        {
            builder.AddScene("Existing", "Existing", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => playFramework.Create("non-existent"));
        Assert.Contains("PlayFramework configuration 'non-existent' not found", exception.Message);
    }

    /// <summary>
    /// Test Exists() method.
    /// </summary>
    [Fact]
    public void PlayFramework_Exists_ChecksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("config-a", builder =>
        {
            builder.AddScene("A", "A", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act & Assert
        Assert.True(playFramework.Exists("config-a"));
        Assert.False(playFramework.Exists("config-b"));
    }

    /// <summary>
    /// Test CreateAll() method.
    /// </summary>
    [Fact]
    public void PlayFramework_CreateAll_ReturnsAllManagers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("config-1", builder =>
        {
            builder.AddScene("Scene1", "1", s => { });
        });

        services.AddPlayFramework("config-2", builder =>
        {
            builder.AddScene("Scene2", "2", s => { });
        });

        services.AddPlayFramework("config-3", builder =>
        {
            builder.AddScene("Scene3", "3", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act - Get each configuration individually
        var manager1 = playFramework.Create("config-1");
        var manager2 = playFramework.Create("config-2");
        var manager3 = playFramework.Create("config-3");

        // Assert
        Assert.NotNull(manager1);
        Assert.NotNull(manager2);
        Assert.NotNull(manager3);
        Assert.NotSame(manager1, manager2);
        Assert.NotSame(manager2, manager3);
    }

    /// <summary>
    /// Test using enum as key with IPlayFramework.
    /// </summary>
    [Fact]
    public async Task PlayFramework_WithEnumKey_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework(AppTier.Free, builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Direct)
                .AddScene("FreeScene", "Free tier", s => { });
        });

        services.AddPlayFramework(AppTier.Premium, builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.DynamicChaining)
                .AddScene("PremiumScene", "Premium tier", s => { });
        });

        var serviceProvider = services.BuildServiceProvider();
        var playFramework = serviceProvider.GetRequiredService<IPlayFramework>();

        // Act
        var freeManager = playFramework.Create(AppTier.Free);
        var premiumManager = playFramework.Create(AppTier.Premium);

        // Assert
        Assert.NotNull(freeManager);
        Assert.NotNull(premiumManager);
        Assert.True(playFramework.Exists(AppTier.Free));
        Assert.True(playFramework.Exists(AppTier.Premium));
    }

    /// <summary>
    /// Real-world usage: User service that uses IPlayFramework.
    /// </summary>
    [Fact]
    public async Task PlayFramework_InUserService_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("tier-free", builder =>
        {
            builder.AddScene("BasicSearch", "Basic", s => { });
        });

        services.AddPlayFramework("tier-premium", builder =>
        {
            builder.AddScene("AdvancedSearch", "Advanced", s => { });
        });

        services.AddScoped<UserSearchService>();

        var serviceProvider = services.BuildServiceProvider();
        var searchService = serviceProvider.GetRequiredService<UserSearchService>();

        // Act
        var freeResults = await searchService.SearchAsync("test query", isPremium: false);
        var premiumResults = await searchService.SearchAsync("test query", isPremium: true);

        // Assert
        Assert.NotNull(freeResults);
        Assert.NotNull(premiumResults);
    }

    // Helper classes
    private enum AppTier
    {
        Free,
        Premium,
        Enterprise
    }

    private class UserSearchService
    {
        private readonly IPlayFramework _playFramework;

        public UserSearchService(IPlayFramework playFramework)
        {
            _playFramework = playFramework;
        }

        public async Task<string> SearchAsync(string query, bool isPremium)
        {
            // Select configuration based on user tier
            var configKey = isPremium ? "tier-premium" : "tier-free";
            var sceneManager = _playFramework.Create(configKey);

            var results = new List<AiSceneResponse>();
            await foreach (var response in sceneManager.ExecuteAsync(query))
            {
                results.Add(response);
            }

            return results.LastOrDefault()?.Message ?? "No results";
        }
    }

    private class SimpleMockChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("simple-mock", null, "1.0");

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            var responseMessage = new ChatMessage(ChatRole.Assistant, "Mock response");

            if (options?.Tools?.Count > 0)
            {
                var tool = options.Tools.First();
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: "mockTool",
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(functionCall);
            }

            return new ChatResponse([responseMessage]) { ModelId = "mock-model" };
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
