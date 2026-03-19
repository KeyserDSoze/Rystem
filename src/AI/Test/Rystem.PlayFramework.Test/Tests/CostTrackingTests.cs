using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

/// <summary>
/// Diagnostics: verify AdditionalProperties CostCalculation round-trip.
/// </summary>
public class CostTrackingTests
{
    [Fact]
    public void AdditionalProperties_RoundTrip_ReturnsCostCalculation()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "test")]);

        var expected = new CostCalculation
        {
            InputCost = 0.03m,
            OutputCost = 0.06m,
            Currency = "USD",
            Usage = new TokenUsage { InputTokens = 1000, OutputTokens = 1000 }
        };

        response.AdditionalProperties ??= [];
        response.AdditionalProperties[PlayFrameworkCostConstants.CostCalculationKey] = expected;

        var found = response.AdditionalProperties.TryGetValue(PlayFrameworkCostConstants.CostCalculationKey, out var rawObj) && rawObj is CostCalculation;
        var actual = rawObj as CostCalculation;

        Assert.True(found, "TryGetValue should return true");
        Assert.NotNull(actual);
        Assert.Equal(0.09m, actual!.TotalCost);
    }

    /// <summary>
    /// Tests the exact pattern ChatClientManager uses with nullable ?. operator.
    /// </summary>
    [Fact]
    public void AdditionalProperties_NullConditional_ReturnsCostCalculation()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "test")]);

        var expected = new CostCalculation
        {
            InputCost = 0.03m,
            OutputCost = 0.06m,
            Currency = "USD",
            Usage = new TokenUsage { InputTokens = 1000, OutputTokens = 1000 }
        };

        response.AdditionalProperties ??= [];
        response.AdditionalProperties[PlayFrameworkCostConstants.CostCalculationKey] = expected;

        // Exact pattern from ChatClientManager.GetResponseAsync
        var costCalc = response.AdditionalProperties?.TryGetValue(PlayFrameworkCostConstants.CostCalculationKey, out var costObj) == true
            ? costObj as CostCalculation
            : null;

        Assert.NotNull(costCalc);
        Assert.Equal(0.09m, costCalc!.TotalCost);
    }

    /// <summary>
    /// Tests that MockCostTrackingChatClient actually embeds cost in AdditionalProperties.
    /// </summary>
    [Fact]
    public async Task MockClient_GetResponseAsync_EmbedsCostInAdditionalProperties()
    {
        var mock = new MockCostTrackingChatClient(inputTokens: 1000, outputTokens: 1000, currency: "USD", inputCostPer1K: 0.03m, outputCostPer1K: 0.06m);

        var response = await mock.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")],
            options: null);

        Assert.NotNull(response.AdditionalProperties);
        Assert.True(response.AdditionalProperties.ContainsKey(PlayFrameworkCostConstants.CostCalculationKey),
            "AdditionalProperties must contain the cost key");

        var costCalc = response.AdditionalProperties[PlayFrameworkCostConstants.CostCalculationKey] as CostCalculation;
        Assert.NotNull(costCalc);
        Assert.Equal(0.09m, costCalc!.TotalCost);
    }

    /// <summary>
    /// Verifies the entire pipeline with SceneManager: scene selection works and cost accumulates.
    /// </summary>
    [Fact]
    public async Task FullSceneManagerPipeline_DirectMode_ReturnsNonZeroCostAndStatuses()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPlayFramework(builder =>
        {
            builder.AddScene("Calculator", "Math", sceneBuilder =>
            {
                sceneBuilder.WithService<ICalculatorService>(sb =>
                    sb.WithMethod(x => x.AddAsync(default, default), "add", "Add"));
            });
        });

        services.AddSingleton<ICalculatorService, CalculatorService>();
        services.AddSingleton<IChatClient>(_ =>
            new MockCostTrackingChatClient(1000, 1000, "USD", 0.03m, 0.06m));

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<ISceneManager>();

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Direct
            // no MaxBudget - unlimited
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync("test", metadata: null, settings))
        {
            responses.Add(response);
        }

        // Collect statuses for diagnostics
        var statuses = string.Join(", ", responses.Select(r => $"{r.Status}(Cost={r.Cost?.ToString("F4") ?? "null"}, Total={r.TotalCost:F4})"));
        Assert.True(
            responses.Any(r => r.Cost.HasValue && r.Cost.Value > 0),
            $"Expected at least one response with Cost > 0. All responses: [{statuses}]");
    }
}
