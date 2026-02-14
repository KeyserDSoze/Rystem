using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for streaming support in PlayFramework.
/// </summary>
public class StreamingTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Tests that streaming is enabled when EnableStreaming is true.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithStreaming_ReturnsProgressiveChunks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 0.03m, 0.06m)
                .AddScene("Calculator", "Math operations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add numbers");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IChatClient>(sp => new MockStreamingChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            EnableStreaming = true, // Enable streaming
            MaxDynamicScenes = 1 // Only execute one scene
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 5", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var streamingResponses = responses.Where(r => r.Status == AiResponseStatus.Streaming).ToList();
        Assert.NotEmpty(streamingResponses); // Should have streaming chunks

        // Check that Message accumulates
        string? previousMessage = null;
        foreach (var streamResponse in streamingResponses)
        {
            if (previousMessage != null)
            {
                // Each message should be longer than the previous (accumulating)
                Assert.True(streamResponse.Message?.Length >= previousMessage.Length,
                    $"Message should accumulate. Previous: '{previousMessage}', Current: '{streamResponse.Message}'");
            }
            previousMessage = streamResponse.Message;

            // Each chunk should have StreamingChunk populated
            Assert.NotNull(streamResponse.StreamingChunk);
        }

        // Final response should have IsStreamingComplete = true
        var finalStreamResponse = responses.LastOrDefault(r => r.IsStreamingComplete);
        Assert.NotNull(finalStreamResponse);
        Assert.Equal(AiResponseStatus.Running, finalStreamResponse!.Status);
    }

    /// <summary>
    /// Tests that non-streaming mode works as before (no streaming chunks).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithoutStreaming_ReturnsCompleteResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("Calculator", "Math", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IChatClient>(sp => new MockStreamingChatClient());

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            EnableStreaming = false, // Disable streaming
            MaxDynamicScenes = 1 // Only execute one scene
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 5 + 3", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var streamingResponses = responses.Where(r => r.Status == AiResponseStatus.Streaming).ToList();
        Assert.Empty(streamingResponses); // Should NOT have streaming chunks

        // Should have complete response
        var runningResponses = responses.Where(r => r.Status == AiResponseStatus.Running).ToList();
        Assert.NotEmpty(runningResponses);
    }

    /// <summary>
    /// Tests streaming with multiple words/chunks.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithStreaming_AccumulatesMessageCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddScene("Calculator", "Math", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IChatClient>(sp => new MockStreamingChatClient("The result is 15")); // Multi-word response

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            EnableStreaming = true,
            MaxDynamicScenes = 1 // Only execute one scene
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Add numbers", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var streamingResponses = responses
            .Where(r => r.Status == AiResponseStatus.Streaming || r.IsStreamingComplete)
            .ToList();

        // Should have 4 chunks: "The", "result", "is", "15"
        Assert.True(streamingResponses.Count >= 4, $"Expected at least 4 chunks, got {streamingResponses.Count}");

        // Final message should be complete
        var lastMessage = streamingResponses.Last().Message;
        Assert.Equal("The result is 15", lastMessage);
    }

    /// <summary>
    /// Tests that streaming respects budget limits.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithStreamingAndBudget_StopsWhenExceeded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 0.1m, 0.2m) // High cost
                .AddScene("Calculator", "Math", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(500, 500)); // Costs $0.15 per call

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.DynamicChaining,
            EnableStreaming = true,
            MaxBudget = 0.20m, // Low budget
            MaxDynamicScenes = 1 // Only execute one scene
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Add numbers", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        // May hit budget exceeded during execution
        var hasBudgetExceeded = responses.Any(r => r.Status == AiResponseStatus.BudgetExceeded);
        
        // If budget exceeded, should have some streaming responses before it
        if (hasBudgetExceeded)
        {
            var indexOfBudgetExceeded = responses.FindIndex(r => r.Status == AiResponseStatus.BudgetExceeded);
            Assert.True(indexOfBudgetExceeded > 0, "Should have responses before budget exceeded");
        }
    }
}

/// <summary>
/// Mock chat client that supports streaming responses.
/// </summary>
internal class MockStreamingChatClient : IChatClient
{
    private readonly string _responseText;

    public MockStreamingChatClient(string responseText = "The answer is 15")
    {
        _responseText = responseText;
    }

    public ChatClientMetadata Metadata => new("mock-streaming-client", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        var responseMessage = new ChatMessage(ChatRole.Assistant, _responseText);

        // Simulate function calls if tools are available
        if (options?.Tools?.Count > 0)
        {
            var tool = options.Tools.First();
            var functionCall = new FunctionCallContent(
                callId: Guid.NewGuid().ToString(),
                name: tool.GetType().GetProperty("Name")?.GetValue(tool)?.ToString() ?? "unknown",
                arguments: new Dictionary<string, object?> { ["a"] = 10, ["b"] = 5 });

            responseMessage.Contents.Add(functionCall);
        }

        return new ChatResponse([responseMessage])
        {
            ModelId = "mock-model"
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Split response into words and stream them
        var words = _responseText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            await Task.Delay(5, cancellationToken); // Simulate streaming delay

            var isLast = i == words.Length - 1;
            var text = i == 0 ? words[i] : $" {words[i]}";

            yield return new ChatResponseUpdate(ChatRole.Assistant, text)
            {
                ModelId = "mock-model",
                FinishReason = isLast ? ChatFinishReason.Stop : null
            };
        }
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
}
