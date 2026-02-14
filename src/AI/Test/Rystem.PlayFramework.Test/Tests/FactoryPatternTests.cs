using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Demonstrates using IFactory<ISceneManager> for multiple configurations.
/// </summary>
public class FactoryPatternTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Test creating multiple PlayFramework configurations with different keys.
    /// </summary>
    [Fact]
    public async Task Factory_MultipleConfigurations_ResolvesCorrectly()
    {
        // Arrange - Setup two different configurations
        var services = new ServiceCollection();
        services.AddLogging();

        // Mock chat client
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        // Configuration 1: "basic" - Direct mode, no cost tracking
        services.AddPlayFramework("basic", builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Direct)
                .AddScene("Calculator", "Basic calculator", s => s
                    .WithService<CalculatorService>(b => b
                        .WithMethod(x => x.Add(default, default), "add", "Add numbers")));
        });

        // Configuration 2: "premium" - Dynamic chaining with cost tracking
        services.AddPlayFramework("premium", builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.DynamicChaining)
                .WithCostTracking("USD", 0.03m, 0.06m)
                .AddScene("AdvancedCalculator", "Advanced calculator with history", s => s
                    .WithService<CalculatorService>(b => b
                        .WithMethod(x => x.Add(default, default), "add", "Add")
                        .WithMethod(x => x.Multiply(default, default), "multiply", "Multiply")));
        });

        services.AddSingleton<CalculatorService>();

        var serviceProvider = services.BuildServiceProvider();

        // Act - Resolve different configurations via factory
        var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();

        var basicManager = factory.Create("basic");
        var premiumManager = factory.Create("premium");

        Assert.NotNull(basicManager);
        Assert.NotNull(premiumManager);

        // Verify they are different instances with different configs
        Assert.NotSame(basicManager, premiumManager);

        // Execute with basic configuration
        var basicResults = new List<AiSceneResponse>();
        await foreach (var response in basicManager!.ExecuteAsync("Calculate 5 + 3"))
        {
            basicResults.Add(response);
        }

        // Execute with premium configuration  
        var premiumResults = new List<AiSceneResponse>();
        await foreach (var response in premiumManager!.ExecuteAsync("Calculate 5 + 3"))
        {
            premiumResults.Add(response);
        }

        // Assert
        Assert.NotEmpty(basicResults);
        Assert.NotEmpty(premiumResults);
    }

    /// <summary>
    /// Test using enum keys for factory resolution.
    /// </summary>
    [Fact]
    public async Task Factory_WithEnumKey_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        // Define environment enum
        var devEnv = ConfigEnvironment.Development;
        var prodEnv = ConfigEnvironment.Production;

        // Development configuration
        services.AddPlayFramework(devEnv, builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Direct)
                .AddScene("DevCalculator", "DevCalculator", s => s = s);
        });

        // Production configuration
        services.AddPlayFramework(prodEnv, builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Planning)
                .WithPlanning()
                .AddScene("ProdCalculator", "ProdCalculator", s => s = s);
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();

        var devManager = factory.Create(devEnv);
        var prodManager = factory.Create(prodEnv);

        // Assert
        Assert.NotNull(devManager);
        Assert.NotNull(prodManager);
        Assert.NotSame(devManager, prodManager);
    }

    /// <summary>
    /// Test checking if a configuration exists.
    /// </summary>
    [Fact]
    public void Factory_Exists_ReturnsTrueForRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("existing-config", builder =>
        {
            builder.AddScene("Test", "Test", s => s = s);
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();

        // Act & Assert
        Assert.True(factory.Exists("existing-config"));
        Assert.False(factory.Exists("non-existent-config"));
    }

    /// <summary>
    /// Test getting all registered configurations.
    /// </summary>
    [Fact]
    public void Factory_CreateAll_ReturnsAllConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(sp => new SimpleMockChatClient());

        services.AddPlayFramework("config1", builder =>
        {
            builder.AddScene("Scene1", "1", s => s = s);
        });

        services.AddPlayFramework("config2", builder =>
        {
            builder.AddScene("Scene2", "2", s => s = s);
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IFactory<ISceneManager>>();

        // Act - Create each configuration individually
        var manager1 = factory.Create("config1");
        var manager2 = factory.Create("config2");

        // Assert
        Assert.NotNull(manager1);
        Assert.NotNull(manager2);
        Assert.NotSame(manager1, manager2);
    }

    // Helper classes
    private enum ConfigEnvironment
    {
        Development,
        Production
    }

    private class CalculatorService
    {
        public int Add(int a, int b) => a + b;
        public int Multiply(int a, int b) => a * b;
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
                    name: "add", // Always call "add" for simplicity
                    arguments: new Dictionary<string, object?> { ["a"] = 5, ["b"] = 3 });

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
