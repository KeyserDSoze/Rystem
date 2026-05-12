using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Telemetry;

namespace Rystem.PlayFramework;

/// <summary>
/// Default implementation of <see cref="IPlayFrameworkBusinessManager"/>.
/// Resolves hooks via the Rystem factory pattern, sorts them by priority, and orchestrates
/// the full before-execution → scene-stream → after-each-scene → on-terminal-scene pipeline.
/// </summary>
internal sealed class PlayFrameworkBusinessManager : IPlayFrameworkBusinessManager
{
    private readonly IFactory<IPlayFrameworkBeforeExecution> _beforeFactory;
    private readonly IFactory<IPlayFrameworkAfterEachScene> _afterFactory;
    private readonly IFactory<IPlayFrameworkOnTerminalScene> _terminalFactory;
    private readonly IFactory<ISceneManager> _sceneManagerFactory;
    private readonly IFactory<PlayFrameworkHookRegistry> _registryFactory;

    private static readonly HashSet<AiResponseStatus> TerminalStatuses =
    [
        AiResponseStatus.Completed,
        AiResponseStatus.Error,
        AiResponseStatus.BudgetExceeded,
        AiResponseStatus.Unauthorized,
        AiResponseStatus.Timeout,
        AiResponseStatus.RateLimited
    ];

    public PlayFrameworkBusinessManager(
        IFactory<IPlayFrameworkBeforeExecution> beforeFactory,
        IFactory<IPlayFrameworkAfterEachScene> afterFactory,
        IFactory<IPlayFrameworkOnTerminalScene> terminalFactory,
        IFactory<ISceneManager> sceneManagerFactory,
        IFactory<PlayFrameworkHookRegistry> registryFactory)
    {
        _beforeFactory = beforeFactory;
        _afterFactory = afterFactory;
        _terminalFactory = terminalFactory;
        _sceneManagerFactory = sceneManagerFactory;
        _registryFactory = registryFactory;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<(AiSceneResponse? Scene, PlayFrameworkDenyResult? Deny)> ExecuteAsync(
        string factoryName,
        PlayFrameworkExecutionContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var businessActivity = PlayFrameworkActivitySource.Instance
            .StartActivity(PlayFrameworkActivitySource.Activities.BusinessExecute, ActivityKind.Internal);
        businessActivity?.SetTag(PlayFrameworkActivitySource.Tags.FactoryName, factoryName);

        var registry = _registryFactory.Create(factoryName)!;

        // ── 1. BeforeExecution hooks ──────────────────────────────────────────
        var beforeHooks = registry
            .SortByPriority(_beforeFactory.CreateAll(factoryName))
            .ToList();

        foreach (var hook in beforeHooks)
        {
            var hookType = hook.GetType();
            var hookTypeName = hookType.Name;
            using var hookActivity = PlayFrameworkActivitySource.Instance
                .StartActivity(PlayFrameworkActivitySource.Activities.BusinessBeforeExecutionHook, ActivityKind.Internal);
            hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookType, hookTypeName);

            PlayFrameworkGuardResult guard;
            try
            {
                guard = await hook.BeforeExecutionAsync(context, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                hookActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw new PlayFrameworkHookException(
                    hookTypeName, "BeforeExecution", registry.GetPriority(hookType), ex);
            }

            if (guard.Type == PlayFrameworkGuardResultType.Deny)
            {
                hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "Deny");
                hookActivity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.HookDenied));
                hookActivity?.SetStatus(ActivityStatusCode.Ok);
                businessActivity?.SetStatus(ActivityStatusCode.Ok);
                yield return (null, guard.DenyResult);
                yield break;
            }

            if (guard.Type == PlayFrameworkGuardResultType.ShortCircuit)
            {
                hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "ShortCircuit");
                hookActivity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.HookShortCircuited));
                hookActivity?.SetStatus(ActivityStatusCode.Ok);
                businessActivity?.SetStatus(ActivityStatusCode.Ok);
                yield return (guard.ShortCircuitResponse, null);
                yield break;
            }

            // Allow → continue to next hook
            hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "Allow");
            hookActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        // ── 2. Execute scene manager ──────────────────────────────────────────
        var sceneManager = _sceneManagerFactory.Create(factoryName)!;

        var afterHooks = registry
            .SortByPriority(_afterFactory.CreateAll(factoryName))
            .ToList();

        var terminalHooks = registry
            .SortByPriority(_terminalFactory.CreateAll(factoryName))
            .ToList();

        var stream = context.Input is not null
            ? sceneManager.ExecuteAsync(context.Input, context.Metadata, context.Settings, cancellationToken)
            : sceneManager.ExecuteAsync(context.Message, context.Metadata, context.Settings, cancellationToken);

        await foreach (var response in stream)
        {
            var isTerminal = TerminalStatuses.Contains(response.Status);

            // ── 3. AfterEachScene hooks (skipped when none registered) ─────────
            if (afterHooks.Count == 0)
            {
                yield return (response, null);
            }
            else
            {
                var current = response;
                AiSceneResponse[]? extras = null;
                var suppressed = false;

                foreach (var hook in afterHooks)
                {
                    var hookType = hook.GetType();
                    var hookTypeName = hookType.Name;
                    using var hookActivity = PlayFrameworkActivitySource.Instance
                        .StartActivity(PlayFrameworkActivitySource.Activities.BusinessAfterEachSceneHook, ActivityKind.Internal);
                    hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookType, hookTypeName);

                    PlayFrameworkSceneResult result;
                    try
                    {
                        result = await hook.AfterSceneAsync(current, context, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        hookActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        throw new PlayFrameworkHookException(
                            hookTypeName, "AfterEachScene", registry.GetPriority(hookType), ex);
                    }

                    switch (result.Type)
                    {
                        case PlayFrameworkSceneResultType.Suppress:
                            suppressed = true;
                            hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "Suppress");
                            hookActivity?.AddEvent(new ActivityEvent(PlayFrameworkActivitySource.Events.HookSuppressed));
                            hookActivity?.SetStatus(ActivityStatusCode.Ok);
                            goto afterHooksDone; // break inner loop

                        case PlayFrameworkSceneResultType.ForwardAndInject:
                            current = result.Scene!;
                            extras = result.ExtraItems;
                            hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "ForwardAndInject");
                            hookActivity?.SetStatus(ActivityStatusCode.Ok);
                            goto afterHooksDone; // extras bypass further after-hooks

                        default: // Forward
                            current = result.Scene!;
                            hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "Forward");
                            hookActivity?.SetStatus(ActivityStatusCode.Ok);
                            break;
                    }
                }

                afterHooksDone:
                if (!suppressed)
                {
                    yield return (current, null);

                    // Extra items from ForwardAndInject bypass after-hooks
                    if (extras is not null)
                    {
                        foreach (var extra in extras)
                            yield return (extra, null);
                    }
                }
            }

            // ── 4. OnTerminalScene hooks ──────────────────────────────────────
            // Fires even when the triggering item was suppressed (trigger is status, not send)
            if (isTerminal && terminalHooks.Count > 0)
            {
                foreach (var hook in terminalHooks)
                {
                    var hookType = hook.GetType();
                    var hookTypeName = hookType.Name;
                    using var hookActivity = PlayFrameworkActivitySource.Instance
                        .StartActivity(PlayFrameworkActivitySource.Activities.BusinessOnTerminalSceneHook, ActivityKind.Internal);
                    hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookType, hookTypeName);

                    IEnumerable<AiSceneResponse>? injected;
                    try
                    {
                        injected = await hook.OnTerminalAsync(response, context, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        hookActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        throw new PlayFrameworkHookException(
                            hookTypeName, "OnTerminalScene", registry.GetPriority(hookType), ex);
                    }
                    if (injected is not null)
                    {
                        foreach (var item in injected)
                            yield return (item, null);
                    }

                    hookActivity?.SetTag(PlayFrameworkActivitySource.Tags.HookOutcome, "Completed");
                    hookActivity?.SetStatus(ActivityStatusCode.Ok);
                }
            }
        }

        businessActivity?.SetStatus(ActivityStatusCode.Ok);
    }
}
