using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

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
        var registry = _registryFactory.Create(factoryName)!;

        // ── 1. BeforeExecution hooks ──────────────────────────────────────────
        var beforeHooks = registry
            .SortByPriority(_beforeFactory.CreateAll(factoryName))
            .ToList();

        foreach (var hook in beforeHooks)
        {
            var guard = await hook.BeforeExecutionAsync(context, cancellationToken);

            if (guard.Type == PlayFrameworkGuardResultType.Deny)
            {
                yield return (null, guard.DenyResult);
                yield break;
            }

            if (guard.Type == PlayFrameworkGuardResultType.ShortCircuit)
            {
                yield return (guard.ShortCircuitResponse, null);
                yield break;
            }
            // Allow → continue to next hook
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
                    var result = await hook.AfterSceneAsync(current, context, cancellationToken);

                    switch (result.Type)
                    {
                        case PlayFrameworkSceneResultType.Suppress:
                            suppressed = true;
                            goto afterHooksDone; // break inner loop

                        case PlayFrameworkSceneResultType.ForwardAndInject:
                            current = result.Scene!;
                            extras = result.ExtraItems;
                            goto afterHooksDone; // extras bypass further after-hooks

                        default: // Forward
                            current = result.Scene!;
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
                    var injected = await hook.OnTerminalAsync(response, context, cancellationToken);
                    if (injected is not null)
                    {
                        foreach (var item in injected)
                            yield return (item, null);
                    }
                }
            }
        }
    }
}
