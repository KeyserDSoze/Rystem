using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit.Abstractions;

namespace Rystem.PlayFramework.Test;

/// <summary>
/// Integration tests with real Azure OpenAI, exercising PlayFramework's SceneManager
/// (scene selection, tool calling, cost tracking) end to end.
/// These tests require valid Azure OpenAI credentials in user secrets.
/// Basic client connectivity and auth (API key / Entra ID, Responses / Chat, streaming)
/// are covered at the adapter level by <c>AzureOpenAILiveGateTests</c> and are not repeated here.
/// </summary>
public sealed class AzureOpenAIIntegrationTests : PlayFrameworkTestBase
{
    private readonly ITestOutputHelper _output;

    public AzureOpenAIIntegrationTests(ITestOutputHelper output) : base(useRealAzureOpenAI: true)
    {
        _output = output;
    }

    protected override void ConfigurePlayFramework(IServiceCollection services)
    {
        // Register calculator service
        services.AddScoped<ICalculatorService, CalculatorService>();

        services.AddPlayFramework(builder =>
        {
            builder
                .AddMainActor("You are a helpful math assistant. When asked to perform calculations, use the available calculator tools.")
                .AddCache(cache => cache.WithMemory())
                .AddScene("Calculator", "Use this scene to perform mathematical calculations. Available operations: add, subtract, multiply, divide.", sceneBuilder =>
                {
                    sceneBuilder
                        .WithService<ICalculatorService>(serviceBuilder =>
                        {
                            serviceBuilder
                                .WithMethod(x => x.AddAsync(default, default), "add", "Add two numbers together. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.SubtractAsync(default, default), "subtract", "Subtract second number from first. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.MultiplyAsync(default, default), "multiply", "Multiply two numbers. Parameters: a (first number), b (second number)")
                                .WithMethod(x => x.DivideAsync(default, default), "divide", "Divide first number by second. Parameters: a (numerator), b (denominator)");
                        })
                        .WithActors(actorBuilder =>
                        {
                            actorBuilder
                                .AddActor("Always use the calculator tools to perform calculations. Do not calculate manually.")
                                .AddActor("Return the result in a clear format like: 'The result is: [number]'");
                        });
                });
        });
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task PlayFramework_WithAzureOpenAI_ShouldExecuteCalculation()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Calculate 15 + 27", cancellationToken: timeout.Token))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.SceneName}: {response.Message}");

            if (response.FunctionName != null)
            {
                _output.WriteLine($"  Function: {response.FunctionName}");
                _output.WriteLine($"  Arguments: {response.FunctionArguments}");
            }

            if (response.Cost.HasValue)
            {
                _output.WriteLine($"  Cost: ${response.Cost:F6} (Total: ${response.TotalCost:F6})");
            }
        }

        // Assert
        Assert.NotEmpty(responses);
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);

        // The model must actually call the "add" tool with 15 and 27, not compute manually.
        var addCall = Assert.Single(
            responses,
            r => r.Status == AiResponseStatus.FunctionCompleted
                && string.Equals(r.FunctionName, "add", StringComparison.OrdinalIgnoreCase));
        AssertArguments(addCall.FunctionArguments, 15, 27);

        // The final answer text must contain the correct result (42), not just "some" completion.
        // Use FinalResponse rather than Running: SceneManager reuses and mutates a single
        // AiSceneResponse instance across some of its yields (observed live: the "Running" entry
        // carrying the answer text is later mutated in place into the "FinalResponse" entry), so an
        // intermediate "Running" status is not reliably queryable from a list collected after the loop.
        var finalResponse = responses.LastOrDefault(
            r => r.Status == AiResponseStatus.FinalResponse && !string.IsNullOrEmpty(r.Message));
        Assert.NotNull(finalResponse);
        Assert.Contains("42", finalResponse.Message);
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task PlayFramework_WithAzureOpenAI_ShouldHandleMultipleOperations()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "Calculate (10 + 5) * 3", cancellationToken: timeout.Token))
        {
            responses.Add(response);
            _output.WriteLine($"[{response.Status}] {response.Message ?? response.FunctionName}");
        }

        // Assert
        Assert.NotEmpty(responses);

        var toolCalls = responses.Where(r => r.Status == AiResponseStatus.FunctionCompleted).ToList();
        _output.WriteLine($"\nTotal operations: {toolCalls.Count}");

        // "(10 + 5) * 3" requires exactly one addition (10+5=15) and one multiplication (15*3=45),
        // not just "at least 2 calls" that could be duplicates of the same operation.
        var addCall = Assert.Single(
            toolCalls, r => string.Equals(r.FunctionName, "add", StringComparison.OrdinalIgnoreCase));
        AssertArguments(addCall.FunctionArguments, 10, 5);

        var multiplyCall = Assert.Single(
            toolCalls, r => string.Equals(r.FunctionName, "multiply", StringComparison.OrdinalIgnoreCase));
        AssertArguments(multiplyCall.FunctionArguments, 15, 3);

        var finalResponse = responses.LastOrDefault(
            r => r.Status == AiResponseStatus.FinalResponse && !string.IsNullOrEmpty(r.Message));
        Assert.NotNull(finalResponse);
        Assert.Contains("45", finalResponse.Message);
    }

    [AzureLiveFact]
    [Trait("Category", "AzureOpenAIApiKey")]
    public async Task PlayFramework_ShouldTrackCostsAccurately()
    {
        // Arrange
        var sceneManager = ServiceProvider.GetRequiredService<ISceneManager>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Act
        var responses = new List<AiSceneResponse>();
        await foreach (var response in sceneManager.ExecuteAsync(
            "What is 100 divided by 5?", cancellationToken: timeout.Token))
        {
            responses.Add(response);
        }

        // Assert
        var finalResponse = responses.LastOrDefault(r => r.Status == AiResponseStatus.Completed);
        Assert.NotNull(finalResponse);
        Assert.True(finalResponse.TotalCost > 0, "Total cost should be greater than zero");

        // Recompute the expected per-call cost from each response's own reported token counts and the
        // rates configured in PlayFrameworkTestBase (0.001m input and 0.003m output per 1K tokens,
        // with no cached
        // token rate configured), and compare it against that same response's own Cost. This is done
        // per response — rather than by summing InputTokens/OutputTokens/Cost across the whole list —
        // because SceneManager can mutate and re-yield the same AiSceneResponse instance (see the
        // comment on PlayFramework_WithAzureOpenAI_ShouldExecuteCalculation) and some terminal
        // responses carry token counts copied forward from an already-billed call rather than a new
        // one, so a naive aggregate sum double-counts. A per-response check still catches a swapped
        // input/output rate or a hardcoded Cost value, which "TotalCost > 0" alone would not.
        //
        // AiResponseStatus.Completed is excluded: it was observed live to report the *cumulative*
        // Cost (equal to TotalCost) alongside the *last call's* token counts copied forward, not a
        // fresh per-call charge, so it would never satisfy this per-row check even when everything
        // is working correctly.
        var billedResponses = responses
            .Distinct()
            .Where(r => r.Status != AiResponseStatus.Completed
                && r.Cost.HasValue && (r.InputTokens.HasValue || r.OutputTokens.HasValue))
            .ToList();
        Assert.NotEmpty(billedResponses);
        foreach (var billed in billedResponses)
        {
            var expectedInputCost = Math.Round((billed.InputTokens ?? 0) / 1000m * 0.001m, 6);
            var expectedOutputCost = Math.Round((billed.OutputTokens ?? 0) / 1000m * 0.003m, 6);
            var expectedCost = expectedInputCost + expectedOutputCost;
            Assert.True(
                Math.Abs(billed.Cost!.Value - expectedCost) < 0.000_001m,
                $"Response '{billed.Status}' reported Cost {billed.Cost:F6} but " +
                $"{billed.InputTokens} input / {billed.OutputTokens} output tokens at the configured " +
                $"rates expect {expectedCost:F6}.");
        }

        // The final answer must contain the correct result (100 / 5 = 20).
        var runningResponse = responses.LastOrDefault(
            r => r.Status == AiResponseStatus.FinalResponse && !string.IsNullOrEmpty(r.Message));
        Assert.NotNull(runningResponse);
        Assert.Contains("20", runningResponse.Message);
    }

    /// <summary>
    /// Verifies the exact operands sent to a commutative calculator operation.
    /// </summary>
    private static void AssertArguments(string? functionArguments, double expectedA, double expectedB)
    {
        Assert.NotNull(functionArguments);
        using var document = JsonDocument.Parse(functionArguments);
        var arguments = document.RootElement;
        Assert.Equal(2, arguments.EnumerateObject().Count());
        var actual = new[]
        {
            arguments.GetProperty("a").GetDouble(),
            arguments.GetProperty("b").GetDouble()
        };
        Array.Sort(actual);
        var expected = new[] { expectedA, expectedB };
        Array.Sort(expected);
        Assert.Equal(expected, actual);
    }
}
