using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests.BusinessHooks;

/// <summary>
/// Tests for <see cref="IPlayFrameworkOnTerminalScene"/> hooks.
/// Covers: hook fires on terminal status, fires even when the terminal item was suppressed,
/// injected items bypass after-each-scene hooks, and priority ordering.
/// </summary>
public class TerminalSceneLogTests
{
    private static readonly HashSet<AiResponseStatus> TerminalStatuses =
    [
        AiResponseStatus.Completed,
        AiResponseStatus.Error,
        AiResponseStatus.BudgetExceeded,
        AiResponseStatus.Unauthorized,
        AiResponseStatus.Timeout,
        AiResponseStatus.RateLimited
    ];

    // ── Hooks ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// On terminal: records the triggering status in <c>context.Items["terminalStatus"]</c>
    /// and injects a synthetic log item.
    /// </summary>
    private sealed class LogTerminalHook : IPlayFrameworkOnTerminalScene
    {
        public Task<IEnumerable<AiSceneResponse>?> OnTerminalAsync(
            AiSceneResponse terminalScene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            context.Items["terminalStatus"] = terminalScene.Status;
            var logItem = new AiSceneResponse
            {
                Status = AiResponseStatus.FinalResponse,
                Message = $"Log: terminal={terminalScene.Status}"
            };
            return Task.FromResult<IEnumerable<AiSceneResponse>?>([logItem]);
        }
    }

    /// <summary>Suppresses every item unconditionally (including terminal ones).</summary>
    private sealed class SuppressAllHook : IPlayFrameworkAfterEachScene
    {
        public Task<PlayFrameworkSceneResult> AfterSceneAsync(
            AiSceneResponse scene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
            => Task.FromResult(PlayFrameworkSceneResult.Suppress());
    }

    /// <summary>
    /// Forwards all items and increments <c>context.Items["afterCount"]</c> on each call.
    /// Used to verify that terminal-injected items bypass after-hooks.
    /// </summary>
    private sealed class CountingAfterHook : IPlayFrameworkAfterEachScene
    {
        public Task<PlayFrameworkSceneResult> AfterSceneAsync(
            AiSceneResponse scene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            var count = (int)context.Items.GetOrAdd("afterCount", _ => 0);
            context.Items["afterCount"] = count + 1;
            return Task.FromResult(PlayFrameworkSceneResult.Forward(scene));
        }
    }

    /// <summary>Runs at priority=1; appends "low" to <c>context.Items["terminalOrder"]</c>.</summary>
    private sealed class LogTerminalHookLow : IPlayFrameworkOnTerminalScene
    {
        public Task<IEnumerable<AiSceneResponse>?> OnTerminalAsync(
            AiSceneResponse terminalScene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            if (context.Items.TryGetValue("terminalOrder", out var raw) && raw is List<string> list)
                list.Add("low");
            return Task.FromResult<IEnumerable<AiSceneResponse>?>(null);
        }
    }

    /// <summary>Runs at priority=10; appends "high" to <c>context.Items["terminalOrder"]</c>.</summary>
    private sealed class LogTerminalHookHigh : IPlayFrameworkOnTerminalScene
    {
        public Task<IEnumerable<AiSceneResponse>?> OnTerminalAsync(
            AiSceneResponse terminalScene, PlayFrameworkExecutionContext context, CancellationToken ct = default)
        {
            if (context.Items.TryGetValue("terminalOrder", out var raw) && raw is List<string> list)
                list.Add("high");
            return Task.FromResult<IEnumerable<AiSceneResponse>?>(null);
        }
    }

    /// <summary>
    /// Stub <see cref="ISceneManager"/> that yields a fixed sequence of responses without
    /// calling an LLM. Used to inject specific terminal statuses in tests.
    /// </summary>
    private sealed class StubSceneManager : ISceneManager
    {
        private readonly AiSceneResponse[] _responses;

        public StubSceneManager(params AiSceneResponse[] responses) => _responses = responses;

        public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
            MultiModalInput input,
            Dictionary<string, object>? metadata = null,
            SceneRequestSettings? settings = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var r in _responses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return r;
            }
        }

        public async IAsyncEnumerable<AiSceneResponse> ExecuteAsync(
            string message,
            Dictionary<string, object>? metadata = null,
            SceneRequestSettings? settings = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var r in _responses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return r;
            }
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

    /// <summary>
    /// Builds a service provider where the scene manager is replaced by a
    /// <see cref="StubSceneManager"/> that emits a single response with
    /// <paramref name="terminalStatus"/> as its status.
    /// </summary>
    private static IServiceProvider BuildServicesWithStub(
        string name, AiResponseStatus terminalStatus, Action<PlayFrameworkBuilder> configure)
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
        // Replace the real SceneManager with a stub that emits exactly one terminal response.
        var stub = new StubSceneManager(new AiSceneResponse { Status = terminalStatus });
        services.AddOrOverrideFactory<ISceneManager, StubSceneManager>(stub, name, ServiceLifetime.Singleton);
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
    /// The OnTerminalScene hook must fire when the stream reaches a terminal status,
    /// record the triggering status, and append its injected log item to the stream.
    /// </summary>
    [Fact]
    public async Task OnTerminal_CompletedStatus_HookFiresAndInjectsLogItem()
    {
        var sp = BuildServices("ts-fire", b =>
            b.Business.AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ts-fire", context);

        // Hook must have fired and recorded the terminal status
        Assert.True(context.Items.TryGetValue("terminalStatus", out var rawStatus),
            "OnTerminalScene hook should have written terminalStatus to context.Items");
        var status = (AiResponseStatus)rawStatus!;
        Assert.Contains(status, TerminalStatuses);

        // The injected log item must appear in results
        var logItems = results
            .Where(r => r.Scene?.Status == AiResponseStatus.FinalResponse)
            .ToList();
        Assert.NotEmpty(logItems);
        Assert.Contains(logItems, r => r.Scene!.Message!.StartsWith("Log: terminal="));
    }

    /// <summary>
    /// When an after-each-scene hook suppresses the terminal item, the OnTerminalScene hook
    /// must still fire (trigger is the raw status, not whether the item was sent to the client).
    /// Items injected by the terminal hook must still appear in the stream.
    /// </summary>
    [Fact]
    public async Task OnTerminal_FiresEvenWhenTerminalItemIsSuppressed()
    {
        var sp = BuildServices("ts-suppress", b =>
            b.Business
                .AddAfterEachScene<SuppressAllHook>()
                .AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ts-suppress", context);

        // Terminal hook must still have fired despite suppression
        Assert.True(context.Items.ContainsKey("terminalStatus"),
            "OnTerminalScene hook must fire even when the terminal item is suppressed by AfterEachScene.");

        // Injected items from the terminal hook must appear in results
        var logItems = results
            .Where(r => r.Scene?.Status == AiResponseStatus.FinalResponse)
            .ToList();
        Assert.NotEmpty(logItems);
    }

    /// <summary>
    /// Items injected by OnTerminalScene must bypass after-each-scene hooks.
    /// The CountingAfterHook invocation count must be less than the total result count
    /// because the injected log item is never passed through it.
    /// </summary>
    [Fact]
    public async Task OnTerminal_InjectedItems_BypassAfterEachSceneHooks()
    {
        var sp = BuildServices("ts-bypass", b =>
            b.Business
                .AddAfterEachScene<CountingAfterHook>()
                .AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        var results = await RunAsync(sp, "ts-bypass", context);

        Assert.NotEmpty(results);

        var afterCount = context.Items.TryGetValue("afterCount", out var rawCount)
            ? (int)rawCount!
            : 0;

        // Total results include at least one log item from the terminal hook.
        // That log item bypasses CountingAfterHook, so afterCount < results.Count.
        Assert.True(afterCount < results.Count,
            $"AfterEachScene hook ({afterCount} invocations) should not be called for " +
            $"terminal-injected items (total results={results.Count}).");
    }

    /// <summary>
    /// The OnTerminalScene hook must fire when the stream reaches an
    /// <see cref="AiResponseStatus.Error"/> terminal status.
    /// </summary>
    [Fact]
    public async Task OnTerminal_ErrorStatus_HookFires()
    {
        var sp = BuildServicesWithStub("ts-error", AiResponseStatus.Error,
            b => b.Business.AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        await RunAsync(sp, "ts-error", context);

        Assert.True(context.Items.TryGetValue("terminalStatus", out var rawStatus));
        Assert.Equal(AiResponseStatus.Error, (AiResponseStatus)rawStatus!);
    }

    /// <summary>
    /// The OnTerminalScene hook must fire when the stream reaches a
    /// <see cref="AiResponseStatus.BudgetExceeded"/> terminal status.
    /// </summary>
    [Fact]
    public async Task OnTerminal_BudgetExceededStatus_HookFires()
    {
        var sp = BuildServicesWithStub("ts-budget", AiResponseStatus.BudgetExceeded,
            b => b.Business.AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        await RunAsync(sp, "ts-budget", context);

        Assert.True(context.Items.TryGetValue("terminalStatus", out var rawStatus));
        Assert.Equal(AiResponseStatus.BudgetExceeded, (AiResponseStatus)rawStatus!);
    }

    /// <summary>
    /// The OnTerminalScene hook must fire when the stream reaches an
    /// <see cref="AiResponseStatus.Unauthorized"/> terminal status.
    /// </summary>
    [Fact]
    public async Task OnTerminal_UnauthorizedStatus_HookFires()
    {
        var sp = BuildServicesWithStub("ts-unauth", AiResponseStatus.Unauthorized,
            b => b.Business.AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        await RunAsync(sp, "ts-unauth", context);

        Assert.True(context.Items.TryGetValue("terminalStatus", out var rawStatus));
        Assert.Equal(AiResponseStatus.Unauthorized, (AiResponseStatus)rawStatus!);
    }

    /// <summary>
    /// When multiple OnTerminalScene hooks are registered, they must run in ascending priority
    /// order regardless of their registration order.
    /// </summary>
    [Fact]
    public async Task OnTerminal_MultipleHooks_PriorityOrderRespected()
    {
        // LogTerminalHookHigh registered first at priority=10, LogTerminalHookLow registered second at priority=1.
        // Expected execution order: "low" (priority=1) then "high" (priority=10).
        var sp = BuildServicesWithStub("ts-pri", AiResponseStatus.Completed, b =>
            b.Business
                .AddOnTerminalScene<LogTerminalHookHigh>(priority: 10)
                .AddOnTerminalScene<LogTerminalHookLow>(priority: 1));

        var order = new List<string>();
        var context = new PlayFrameworkExecutionContext
        {
            Message = "hello",
            Items = new System.Collections.Concurrent.ConcurrentDictionary<string, object> { ["terminalOrder"] = order }
        };

        await RunAsync(sp, "ts-pri", context);

        Assert.Equal(2, order.Count);
        Assert.Equal("low", order[0]);
        Assert.Equal("high", order[1]);
    }

    /// <summary>
    /// The OnTerminalScene hook must fire when the stream reaches a
    /// <see cref="AiResponseStatus.Timeout"/> terminal status.
    /// </summary>
    [Fact]
    public async Task OnTerminal_TimeoutStatus_HookFires()
    {
        var sp = BuildServicesWithStub("ts-timeout", AiResponseStatus.Timeout,
            b => b.Business.AddOnTerminalScene<LogTerminalHook>());
        var context = new PlayFrameworkExecutionContext { Message = "hello" };

        await RunAsync(sp, "ts-timeout", context);

        Assert.True(context.Items.TryGetValue("terminalStatus", out var rawStatus));
        Assert.Equal(AiResponseStatus.Timeout, (AiResponseStatus)rawStatus!);
    }
}
