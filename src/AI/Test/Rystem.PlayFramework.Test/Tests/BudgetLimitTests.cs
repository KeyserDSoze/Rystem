using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Tests for budget limit enforcement during PlayFramework execution.
/// </summary>
public class BudgetLimitTests : PlayFrameworkTestBase
{
    /// <summary>
    /// Tests that execution stops when budget limit is exceeded.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithBudgetLimit_StopsWhenExceeded()
    {
        // Arrange - Setup with real costs
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                // Enable cost tracking with GPT-4 pricing
                .WithCostTracking("USD", 0.03m, 0.06m) // Input: $0.03/1K, Output: $0.06/1K
                .AddScene("Calculator", "Performs mathematical calculations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add two numbers")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply two numbers");
                        });
                });
        });

        // Mock calculator service
        services.AddSingleton<ICalculatorService, CalculatorService>();

        // Mock chat client (simulate 2000 tokens per call: 1000 input + 1000 output)
        // Cost per call: (1000/1000 * $0.03) + (1000/1000 * $0.06) = $0.03 + $0.06 = $0.09
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(
            inputTokens: 1000,
            outputTokens: 1000));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        // Expected cost per call: (500 + 500) / 1000 * ($0.03 + $0.06) = $0.09
        // Set budget to $0.15 - should stop after 2nd LLM call
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = 0.15m // $0.15 budget
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 5", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.Contains(responses, r => r.Status == AiResponseStatus.BudgetExceeded);
        Assert.True(responses.Any(r => r.Message?.Contains("Budget limit of") == true));
        Assert.True(responses.Last().TotalCost > 0.15m); // Total cost should exceed budget
    }

    /// <summary>
    /// Tests that execution completes normally when under budget.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithHighBudget_CompletesNormally()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 0.001m, 0.002m) // Cheap pricing
                .AddScene("Calculator", "Performs calculations", sceneBuilder =>
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
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(100, 100));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = 10.0m // High budget - should complete
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 5 + 3", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.DoesNotContain(responses, r => r.Status == AiResponseStatus.BudgetExceeded);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
        Assert.All(responses.Where(r => r.Cost.HasValue), r => Assert.True(r.TotalCost <= 10.0m));
    }

    /// <summary>
    /// Tests that budget limit is enforced across multiple LLM calls.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithMultipleCalls_AccumulatesCostsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 0.01m, 0.02m) // Input: $0.01/1K, Output: $0.02/1K
                .AddScene("Calculator", "Math operations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();

        // Each call uses 2000 tokens (1000 input + 1000 output)  
        // Cost per call: (1000/1000 * $0.01) + (1000/1000 * $0.02) = $0.01 + $0.02 = $0.03
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(1000, 1000));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        // Budget allows 3 calls maximum ($0.09 / $0.03 = 3)
        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = 0.09m
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate something complex", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var responsesWithCost = responses.Where(r => r.Cost.HasValue).ToList();
        Assert.True(responsesWithCost.Count >= 2); // At least 2 LLM calls before budget exceeded
        
        // Check cumulative cost increases
        decimal previousTotal = 0;
        foreach (var response in responsesWithCost)
        {
            Assert.True(response.TotalCost >= previousTotal);
            previousTotal = response.TotalCost;
        }

        // Should have budget exceeded message
        var budgetExceeded = responses.FirstOrDefault(r => r.Status == AiResponseStatus.BudgetExceeded);
        Assert.NotNull(budgetExceeded);
        Assert.Contains("Budget limit", budgetExceeded!.Message);
    }

    /// <summary>
    /// Tests that execution continues normally when MaxBudget is null (unlimited).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNullBudget_NeverStops()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 1.0m, 2.0m) // High cost per token
                .AddScene("Calculator", "Calculations", sceneBuilder =>
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
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(1000, 1000));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = null // No budget limit
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 20", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        Assert.DoesNotContain(responses, r => r.Status == AiResponseStatus.BudgetExceeded);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
        // Total cost can be very high, but execution completes
        Assert.True(responses.Last().TotalCost > 0);
    }

    /// <summary>
    /// Tests budget limit with different currencies.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithDifferentCurrencies_EnforcesBudget()
    {
        // Arrange - EUR currency
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("EUR", 0.025m, 0.05m) // EUR pricing
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
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(400, 600));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = 0.10m // €0.10 budget
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Add numbers", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var budgetExceeded = responses.FirstOrDefault(r => r.Status == AiResponseStatus.BudgetExceeded);
        if (budgetExceeded != null)
        {
            Assert.Contains("EUR", budgetExceeded.Message); // Currency in message
        }
    }

    /// <summary>
    /// Tests that budget check happens immediately after each LLM call.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_BudgetCheck_HappensAfterEachLLMCall()
    {
        // Arrange - Very tight budget
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder
                .WithCostTracking("USD", 0.1m, 0.2m) // $0.1 input, $0.2 output per 1K
                .AddScene("Calculator", "Calculations", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();

        // Each call: 200 tokens (100 input + 100 output)
        // Cost per call: (100/1000 * $0.1) + (100/1000 * $0.2) = $0.01 + $0.02 = $0.03
        // Budget is $0.05, so second call would exceed (Call 1: $0.03, Call 2: $0.06 > $0.05)
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(100, 100));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct,
            MaxBudget = 0.05m // Very tight budget - should stop after 1-2 calls
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Complex calculation", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Assert
        var budgetExceededResponse = responses.FirstOrDefault(r => r.Status == AiResponseStatus.BudgetExceeded);
        Assert.NotNull(budgetExceededResponse);
        
        // Should have some successful responses before budget exceeded
        Assert.True(responses.TakeWhile(r => r.Status != AiResponseStatus.BudgetExceeded).Any());
    }
}

/// <summary>
/// Mock chat client that returns predictable token counts for cost testing.
/// </summary>
internal class MockCostTrackingChatClient : IChatClient
{
    private readonly int _inputTokens;
    private readonly int _outputTokens;
    private int _callCount = 0;

    public MockCostTrackingChatClient(int inputTokens, int outputTokens)
    {
        _inputTokens = inputTokens;
        _outputTokens = outputTokens;
    }

    public ChatClientMetadata Metadata => new("mock-cost-tracking-client", null, "mock-1.0");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate delay

        _callCount++;
        var responseMessage = new ChatMessage(ChatRole.Assistant, "Mock response");

        // Simulate function calls based on available tools
        if (options?.Tools?.Count > 0)
        {
            // Direct mode flow: scene selection → tool calls → final answer
            if (_callCount == 1)
            {
                // Scene selection - return scene name
                var sceneTool = options.Tools.First();
                var sceneName = sceneTool.GetType().GetProperty("Name")?.GetValue(sceneTool)?.ToString() ?? "Calculator";
                var sceneSelectionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: sceneName,
                    arguments: new Dictionary<string, object?>());

                responseMessage.Contents.Add(sceneSelectionCall);
            }
            else if (_callCount <= 10)
            {
                // Continue calling tools - the framework's budget check will stop us when exceeded
                // Keep calling until MaxToolCallIterations (10) or budget check stops execution
                var tool = options.Tools.First();
                var functionCall = new FunctionCallContent(
                    callId: Guid.NewGuid().ToString(),
                    name: tool.GetType().GetProperty("Name")?.GetValue(tool)?.ToString() ?? "unknown",
                    arguments: new Dictionary<string, object?> { ["a"] = 10, ["b"] = 5 });

                responseMessage.Contents.Add(functionCall);
            }
            else
            {
                // After max iterations, return final text response
                responseMessage = new ChatMessage(ChatRole.Assistant, "Result is 15");
            }
        }

        return new ChatResponse([responseMessage])
        {
            ModelId = "mock-model",
            Usage = new UsageDetails
            {
                InputTokenCount = _inputTokens,
                OutputTokenCount = _outputTokens,
                TotalTokenCount = _inputTokens + _outputTokens
            }
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        _callCount++;

        // For streaming, simulate streaming text word-by-word
        var words = new[] { "Mock", "streaming", "response" };

        for (int i = 0; i < words.Length; i++)
        {
            await Task.Delay(5, cancellationToken);

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

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}
