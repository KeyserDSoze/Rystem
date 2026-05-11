using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests.BusinessHooks;

/// <summary>
/// Tests for <see cref="IPlayFrameworkAfterEachScene"/> hooks.
/// Covers: cost accumulation via Forward, Suppress of non-terminal items, and ForwardAndInject.
/// </summary>
public class CostTrackingAfterSceneTests
{
    // ── Hooks ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Counts how many times the hook is invoked and accumulates <see cref="AiSceneResponse.TotalCost"/>
    /// into <c>context.Items["sceneCount"]</c> / <c>context.Items["totalCost"]</c>.
    /// </summary>
    private sealed class CostAccumulatorHook : IPlayFrameworkAfterEachScene
    {
        public Task<PlayFrameworkSceneResult> AfterSceneAsync(
            AiSceneResponse scene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            var count = (int)context.Items.GetOrAdd("sceneCount", _ => 0);
            context.Items["sceneCount"] = count + 1;

            var cost = (decimal)context.Items.GetOrAdd("totalCost", _ => 0m);
            context.Items["totalCost"] = cost + scene.TotalCost;

            return Task.FromResult(PlayFrameworkSceneResult.Forward(scene));
        }
    }

    private static readonly HashSet<AiResponseStatus> TerminalStatuses =
    [
        AiResponseStatus.Completed,
        AiResponseStatus.Error,
        AiResponseStatus.BudgetExceeded,
        AiResponseStatus.Unauthorized
    ];

    /// <summary>
    /// Suppresses all non-terminal scene items; lets terminal items through unchanged.
    /// </summary>
    private sealed class SuppressNonTerminalHook : IPlayFrameworkAfterEachScene
    {
        public Task<PlayFrameworkSceneResult> AfterSceneAsync(
            AiSceneResponse scene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            return Task.FromResult(TerminalStatuses.Contains(scene.Status)
                ? PlayFrameworkSceneResult.Forward(scene)
                : PlayFrameworkSceneResult.Suppress());
        }
    }

    /// <summary>
    /// For <see cref="AiResponseStatus.Completed"/> scenes: forwards the original and injects a
    /// synthetic cost-summary item via <see cref="PlayFrameworkSceneResult.ForwardAndInject"/>.
    /// Tracks total hook invocations in <c>context.Items["hookInvocations"]</c>.
    /// </summary>
    private sealed class InjectCostSummaryHook : IPlayFrameworkAfterEachScene
    {
        public Task<PlayFrameworkSceneResult> AfterSceneAsync(
            AiSceneResponse scene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            var count = (int)context.Items.GetOrAdd("hookInvocations", _ => 0);
            context.Items["hookInvocations"] = count + 1;

            if (scene.Status == AiResponseStatus.Completed)
            {
                var summary = new AiSceneResponse
                {
                    Status = AiResponseStatus.FinalResponse,
                    Message = $"Cost summary: {scene.TotalCost:F4}"
                };
                return Task.FromResult(PlayFrameworkSceneResult.ForwardAndInject(scene, summary));
            }

            return Task.FromResult(PlayFrameworkSceneResult.Forward(scene));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IServiceProvider BuildServices(string name, Action<PlayFrameworkBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new MockChatClient());
        services.AddPlayFramework(name, builder =>
        {
            builder
                .WithExecutionMode(SceneExecutionMode.Direct)
                .AddScene("Stub", "Stub scene for hook tests", _ => { });
            configure(builder);
        });
        return services.BuildServiceProvider();
    }

    private static async Task<List<(AiSceneResponse? Scene, PlayFrameworkDenyResult? Deny)>> RunAsync(
        IServiceProvider sp, string name, PlayFrameworkExecutionContext context)
    {
        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IFactory<IPlayFrameworkBusinessManager>>();
        var manager = factory.Create(name)!;
        var results = new List<(AiSceneResponse? Scene, PlayFrameworkDenyResult? Deny)>();
        await foreach (var item in manager.ExecuteAsync(name, context))
            results.Add(item);
        return results;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// An after-each-scene hook using Forward must be invoked once per scene item,
    /// with the invocation count matching the final result count.
    /// </summary>
    [Fact]
    public async Task AfterEachScene_Forward_HookCalledForEveryItem()
    {
        var sp = BuildServices("ct-forward", b =>
            b.Business.AddAfterEachScene<CostAccumulatorHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ct-forward", context);

        Assert.NotEmpty(results);
        Assert.True(context.Items.TryGetValue("sceneCount", out var rawCount));
        // Hook is called exactly once per result (no extras in this test)
        Assert.Equal(results.Count, (int)rawCount!);
    }

    /// <summary>
    /// A hook that suppresses non-terminal items must leave only terminal items in the stream.
    /// </summary>
    [Fact]
    public async Task AfterEachScene_SuppressNonTerminal_OnlyTerminalItemsInResults()
    {
        var sp = BuildServices("ct-suppress", b =>
            b.Business.AddAfterEachScene<SuppressNonTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ct-suppress", context);

        // All surviving items must have a terminal status
        Assert.All(results, r =>
        {
            Assert.NotNull(r.Scene);
            Assert.Contains(r.Scene!.Status, TerminalStatuses);
        });
    }

    /// <summary>
    /// A hook that uses ForwardAndInject must append extra items to the stream,
    /// and those injected items must bypass the hook (i.e. hook invocation count
    /// is strictly less than total result count).
    /// </summary>
    [Fact]
    public async Task AfterEachScene_ForwardAndInject_ExtraItemsBypassHook()
    {
        var sp = BuildServices("ct-inject", b =>
            b.Business.AddAfterEachScene<InjectCostSummaryHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ct-inject", context);

        // At least one synthetic cost-summary item must have been injected
        var summaries = results
            .Where(r => r.Scene?.Status == AiResponseStatus.FinalResponse)
            .ToList();
        Assert.NotEmpty(summaries);

        // Injected items bypass the hook, so invocations < total result count
        var invocations = context.Items.TryGetValue("hookInvocations", out var rawInv)
            ? (int)rawInv!
            : 0;
        Assert.True(invocations < results.Count,
            $"AfterEachScene hook ({invocations} calls) should be invoked fewer times than " +
            $"total results ({results.Count}) because injected items bypass the hook.");
    }
}
