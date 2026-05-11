using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests.BusinessHooks;

/// <summary>
/// Tests for <see cref="IPlayFrameworkBeforeExecution"/> hooks.
/// Covers: Deny (rate-limit), Allow (pass-through), ShortCircuit, and priority ordering.
/// </summary>
public class RateLimitBeforeExecutionTests
{
    // ── Hooks ─────────────────────────────────────────────────────────────────

    private sealed class DenyHook : IPlayFrameworkBeforeExecution
    {
        public Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
            PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            context.Items["hookCalled"] = true;
            return Task.FromResult(PlayFrameworkGuardResult.Deny(429, "Too many requests"));
        }
    }

    private sealed class AllowHook : IPlayFrameworkBeforeExecution
    {
        public Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
            PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            context.Items["hookCalled"] = true;
            return Task.FromResult(PlayFrameworkGuardResult.Allow());
        }
    }

    private sealed class ShortCircuitHook : IPlayFrameworkBeforeExecution
    {
        public Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
            PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            var synthetic = new AiSceneResponse
            {
                Status = AiResponseStatus.Completed,
                Message = "Short-circuited"
            };
            return Task.FromResult(PlayFrameworkGuardResult.ShortCircuit(synthetic));
        }
    }

    // Two hooks that append a tag to context.Items["order"] so we can observe execution order.
    private sealed class OrderHookLow : IPlayFrameworkBeforeExecution
    {
        public Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
            PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            if (context.Items.TryGetValue("order", out var raw) && raw is List<string> list)
                list.Add("low");
            return Task.FromResult(PlayFrameworkGuardResult.Allow());
        }
    }

    private sealed class OrderHookHigh : IPlayFrameworkBeforeExecution
    {
        public Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
            PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            if (context.Items.TryGetValue("order", out var raw) && raw is List<string> list)
                list.Add("high");
            return Task.FromResult(PlayFrameworkGuardResult.Allow());
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
    /// A deny hook must produce exactly one (null, DenyResult) tuple and stop the stream —
    /// the scene manager must never be reached.
    /// </summary>
    [Fact]
    public async Task DenyHook_Returns429_YieldsSingleDenyAndStops()
    {
        var sp = BuildServices("rl-deny", b => b.Business.AddBeforeExecution<DenyHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "rl-deny", context);

        Assert.Single(results);
        Assert.Null(results[0].Scene);
        Assert.NotNull(results[0].Deny);
        Assert.Equal(429, results[0].Deny!.StatusCode);
        Assert.Equal("Too many requests", results[0].Deny!.ErrorDetail);
        Assert.True((bool)context.Items["hookCalled"]);
    }

    /// <summary>
    /// An allow hook must let execution proceed to the scene manager, yielding at least one scene result
    /// with no deny payloads.
    /// </summary>
    [Fact]
    public async Task AllowHook_ProceedsToSceneManager_YieldsSceneResults()
    {
        var sp = BuildServices("rl-allow", b => b.Business.AddBeforeExecution<AllowHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "rl-allow", context);

        Assert.True((bool)context.Items["hookCalled"]);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Null(r.Deny));
    }

    /// <summary>
    /// A short-circuit hook must yield exactly the synthetic response it returns and stop
    /// the stream — the scene manager must never be reached.
    /// </summary>
    [Fact]
    public async Task ShortCircuitHook_YieldsSyntheticResponseAndStops()
    {
        var sp = BuildServices("rl-sc", b => b.Business.AddBeforeExecution<ShortCircuitHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "rl-sc", context);

        Assert.Single(results);
        Assert.Null(results[0].Deny);
        Assert.NotNull(results[0].Scene);
        Assert.Equal("Short-circuited", results[0].Scene!.Message);
        Assert.Equal(AiResponseStatus.Completed, results[0].Scene!.Status);
    }

    /// <summary>
    /// When two before-execution hooks are registered, they must run in ascending priority order
    /// regardless of their registration order.
    /// </summary>
    [Fact]
    public async Task Priority_LowerNumberRunsBeforeHigherNumber()
    {
        // OrderHookHigh registered first at priority=10, OrderHookLow registered second at priority=1.
        // Expected execution: "low" (priority=1) before "high" (priority=10).
        var sp = BuildServices("rl-pri", b =>
            b.Business
                .AddBeforeExecution<OrderHookHigh>(priority: 10)
                .AddBeforeExecution<OrderHookLow>(priority: 1));

        var order = new List<string>();
        var context = new PlayFrameworkExecutionContext
        {
            Message = "hello",
            Items = new ConcurrentDictionary<string, object> { ["order"] = order }
        };

        await RunAsync(sp, "rl-pri", context);

        Assert.Equal(2, order.Count);
        Assert.Equal("low", order[0]);   // priority=1 ran first
        Assert.Equal("high", order[1]);  // priority=10 ran second
    }
}
