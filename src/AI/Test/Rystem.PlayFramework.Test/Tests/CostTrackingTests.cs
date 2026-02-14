using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Tests for cost tracking functionality.
/// </summary>
public sealed class CostTrackingTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register calculator service
        services.AddScoped<ICalculatorService, CalculatorService>();

        // Configure PlayFramework with cost tracking
        services.AddPlayFramework(builder =>
        {
            builder
                // Enable cost tracking with GPT-4 pricing (example)
                .WithCostTracking(
                    currency: "USD",
                    inputCostPer1K: 0.03m,      // $0.03 per 1K input tokens
                    outputCostPer1K: 0.06m,     // $0.06 per 1K output tokens
                    cachedInputCostPer1K: 0.003m // $0.003 per 1K cached tokens (10%)
                )
                // Add model-specific costs
                .WithModelCosts(
                    modelId: "gpt-4",
                    inputCostPer1K: 0.03m,
                    outputCostPer1K: 0.06m
                )
                .WithModelCosts(
                    modelId: "gpt-3.5-turbo",
                    inputCostPer1K: 0.0015m,
                    outputCostPer1K: 0.002m
                )
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor("You are a helpful math assistant.")
                .AddScene("Calculator", "Perform mathematical calculations", sceneBuilder =>
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
    }

    [Fact]
    public void CostCalculator_ShouldBeRegistered()
    {
        // Arrange & Act
        var costCalculator = ServiceProvider.GetService<ICostCalculator>();

        // Assert
        Assert.NotNull(costCalculator);
        Assert.True(costCalculator.IsEnabled);
        Assert.Equal("USD", costCalculator.Currency);
    }

    [Fact]
    public void CostCalculator_ShouldCalculateCorrectly()
    {
        // Arrange
        var costCalculator = ServiceProvider.GetRequiredService<ICostCalculator>();
        var usage = new TokenUsage
        {
            InputTokens = 1000,
            OutputTokens = 500,
            CachedInputTokens = 0,
            ModelId = "gpt-4"
        };

        // Act
        var calculation = costCalculator.Calculate(usage);

        // Assert
        // Input: 1000 tokens * $0.03/1K = $0.03
        // Output: 500 tokens * $0.06/1K = $0.03
        // Total: $0.06
        Assert.Equal(0.03m, calculation.InputCost);
        Assert.Equal(0.03m, calculation.OutputCost);
        Assert.Equal(0m, calculation.CachedInputCost);
        Assert.Equal(0.06m, calculation.TotalCost);
        Assert.Equal("USD", calculation.Currency);
    }

    [Fact]
    public void CostCalculator_ShouldUseModelSpecificCosts()
    {
        // Arrange
        var costCalculator = ServiceProvider.GetRequiredService<ICostCalculator>();
        
        var gpt4Usage = new TokenUsage
        {
            InputTokens = 1000,
            OutputTokens = 1000,
            ModelId = "gpt-4"
        };

        var gpt35Usage = new TokenUsage
        {
            InputTokens = 1000,
            OutputTokens = 1000,
            ModelId = "gpt-3.5-turbo"
        };

        // Act
        var gpt4Calc = costCalculator.Calculate(gpt4Usage);
        var gpt35Calc = costCalculator.Calculate(gpt35Usage);

        // Assert - GPT-4 is more expensive
        // GPT-4: (1000 * 0.03/1K) + (1000 * 0.06/1K) = $0.09
        // GPT-3.5: (1000 * 0.0015/1K) + (1000 * 0.002/1K) = $0.0035
        Assert.Equal(0.09m, gpt4Calc.TotalCost);
        Assert.Equal(0.0035m, gpt35Calc.TotalCost);
    }

    [Fact]
    public void CostCalculator_ShouldHandleCachedTokens()
    {
        // Arrange
        var costCalculator = ServiceProvider.GetRequiredService<ICostCalculator>();
        var usage = new TokenUsage
        {
            InputTokens = 500,
            OutputTokens = 500,
            CachedInputTokens = 500, // 500 tokens from cache
            ModelId = "gpt-4"
        };

        // Act
        var calculation = costCalculator.Calculate(usage);

        // Assert
        // Input: 500 * $0.03/1K = $0.015
        // Output: 500 * $0.06/1K = $0.03
        // Cached: 500 * $0.003/1K = $0.0015 (10% of input cost)
        // Total: $0.0465
        Assert.Equal(0.015m, calculation.InputCost);
        Assert.Equal(0.03m, calculation.OutputCost);
        Assert.Equal(0.0015m, calculation.CachedInputCost);
        Assert.Equal(0.0465m, calculation.TotalCost);
    }

    [Fact(Skip = "Requires LLM - Enable for integration testing")]
    public async Task PlayFramework_ShouldTrackCosts()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        var responses = new List<AiSceneResponse>();

        // Act
        await foreach (var response in sceneManager.ExecuteAsync("Calculate 10 + 5"))
        {
            responses.Add(response);
        }

        // Assert
        var responsesWithCost = responses.Where(r => r.Cost.HasValue && r.Cost.Value > 0).ToList();
        
        // Should have at least one response with cost > 0
        Assert.NotEmpty(responsesWithCost);

        // Total cost should be accumulated
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(finalResponse);
        Assert.True(finalResponse.TotalCost > 0, "Total cost should be > 0");

        // Cost should increase progressively
        var costsOverTime = responses.Where(r => r.Cost.HasValue).Select(r => r.TotalCost).ToList();
        for (int i = 1; i < costsOverTime.Count; i++)
        {
            Assert.True(costsOverTime[i] >= costsOverTime[i - 1], 
                $"Cost should increase or stay same: {costsOverTime[i - 1]} -> {costsOverTime[i]}");
        }

        // Token counts should be present
        var responsesWithTokens = responses.Where(r => 
            r.InputTokens.HasValue || r.OutputTokens.HasValue).ToList();
        Assert.NotEmpty(responsesWithTokens);
    }
}

/// <summary>
/// Tests with cost tracking disabled.
/// </summary>
public sealed class DisabledCostTrackingTests : PlayFrameworkTestBase
{
    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        services.AddScoped<ICalculatorService, CalculatorService>();

        services.AddPlayFramework(builder =>
        {
            builder
                // Cost tracking disabled
                .WithCostTracking(settings =>
                {
                    settings.Enabled = false;
                })
                .Configure(settings =>
                {
                    settings.Planning.Enabled = false;
                    settings.Summarization.Enabled = false;
                })
                .AddMainActor("You are a helpful assistant.")
                .AddScene("Calculator", "Calculator", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder.WithMethod(x => x.AddAsync(default, default), "add", "Add");
                        });
                });
        });
    }

    [Fact]
    public void CostCalculator_ShouldBeDisabled()
    {
        // Arrange & Act
        var costCalculator = ServiceProvider.GetRequiredService<ICostCalculator>();

        // Assert
        Assert.False(costCalculator.IsEnabled);
    }

    [Fact]
    public void CostCalculator_WhenDisabled_ShouldReturnZeroCosts()
    {
        // Arrange
        var costCalculator = ServiceProvider.GetRequiredService<ICostCalculator>();
        var usage = new TokenUsage
        {
            InputTokens = 1000,
            OutputTokens = 1000,
            ModelId = "gpt-4"
        };

        // Act
        var calculation = costCalculator.Calculate(usage);

        // Assert - all costs should be 0
        Assert.Equal(0m, calculation.InputCost);
        Assert.Equal(0m, calculation.OutputCost);
        Assert.Equal(0m, calculation.CachedInputCost);
        Assert.Equal(0m, calculation.TotalCost);
    }
}
