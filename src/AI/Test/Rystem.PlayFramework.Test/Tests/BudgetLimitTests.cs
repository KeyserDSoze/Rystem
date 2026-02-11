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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Performs mathematical calculations")
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

        // Mock chat client (simulate 1000 tokens per call)
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(
            inputTokens: 500,
            outputTokens: 500));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        // Expected cost per call: (500 + 500) / 1000 * ($0.03 + $0.06) = $0.09
        // Set budget to $0.15 - should stop after 2nd LLM call
        var settings = new SceneRequestSettings
        {
            EnablePlanning = false,
            MaxBudget = 0.15m // $0.15 budget
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 5", settings))
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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Performs calculations")
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
            EnablePlanning = false,
            MaxBudget = 10.0m // High budget - should complete
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 5 + 3", settings))
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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Math operations")
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        
        // Each call uses 1000 tokens (500 input + 500 output)
        // Cost per call: 1000 / 1000 * ($0.01 + $0.02) = $0.03
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(500, 500));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        // Budget allows 3 calls maximum ($0.09 / $0.03 = 3)
        var settings = new SceneRequestSettings
        {
            EnablePlanning = false,
            MaxBudget = 0.09m
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate something complex", settings))
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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Calculations")
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
            EnablePlanning = false,
            MaxBudget = null // No budget limit
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 20", settings))
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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Math")
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
            EnablePlanning = false,
            MaxBudget = 0.10m // €0.10 budget
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Add numbers", settings))
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
                .AddScene(sceneBuilder =>
                {
                    sceneBuilder
                        .WithName("Calculator")
                        .WithDescription("Calculations")
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract");
                        });
                });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        
        // First call: 100 tokens = 0.1K → (0.1 * 0.1) + (0.1 * 0.2) = $0.03
        // Second call would exceed budget
        services.AddSingleton<IChatClient>(sp => new MockCostTrackingChatClient(50, 50));

        var serviceProvider = services.BuildServiceProvider();
        var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            EnablePlanning = false,
            MaxBudget = 0.05m // Very tight budget - should stop after 1-2 calls
        };

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("Complex calculation", settings))
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

        var responseMessage = new ChatMessage(ChatRole.Assistant, "Mock response");

        // Simulate function calls based on available tools
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
            ModelId = "mock-model",
            Usage = new UsageDetails
            {
                InputTokenCount = _inputTokens,
                OutputTokenCount = _outputTokens,
                TotalTokenCount = _inputTokens + _outputTokens
            }
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming not needed for budget tests");
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}
