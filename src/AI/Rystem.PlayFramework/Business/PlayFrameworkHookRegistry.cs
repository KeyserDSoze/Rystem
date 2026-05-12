namespace Rystem.PlayFramework;

/// <summary>
/// Stores hook priority information for a single factory.
/// Registered as a Singleton per factory name via
/// <c>services.AddFactory(registry, factoryName, Singleton)</c>.
/// </summary>
public sealed class PlayFrameworkHookRegistry
{
    // Pre-computed at startup: type → positional sort index (ascending priority,
    // tiebroken by registration order so equal-priority hooks are stable).
    private Dictionary<Type, int> _sortedPositions = [];

    // Pre-computed at startup: type → raw registered priority value.
    private Dictionary<Type, int> _rawPriorities = [];

    /// <summary>
    /// Builds the sorted position map from the full registration list.
    /// Called once by <see cref="Builder.PlayFrameworkBusinessBuilder.BuildRegistry"/> after
    /// all hooks have been registered, before the DI container is built.
    /// Equal-priority hooks preserve registration order (stable sort).
    /// </summary>
    internal void Build(IReadOnlyList<(Type HookInterface, Type HookImpl, int Priority)> registrations)
    {
        _rawPriorities = registrations
            .ToDictionary(r => r.HookImpl, r => r.Priority);

        _sortedPositions = registrations
            .Select((r, registrationOrder) => (r.HookImpl, r.Priority, registrationOrder))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.registrationOrder)
            .Select((x, position) => (x.HookImpl, position))
            .ToDictionary(x => x.HookImpl, x => x.position);
    }

    /// <summary>
    /// Returns the raw registered priority for the given hook implementation type,
    /// or -1 if the type was not registered (e.g. a DI proxy/decorator).
    /// </summary>
    public int GetPriority(Type hookImplType)
        => _rawPriorities.TryGetValue(hookImplType, out var p) ? p : -1;

    /// <summary>
    /// Returns the given hooks sorted ascending by their registered priority.
    /// Equal-priority hooks run in their original registration order.
    /// Hooks whose type is not in the registry (e.g. proxy-wrapped implementations)
    /// are placed at the end of the sequence instead of silently receiving priority 0.
    /// </summary>
    public IEnumerable<T> SortByPriority<T>(IEnumerable<T> hooks) where T : notnull
        => hooks.OrderBy(h => _sortedPositions.TryGetValue(h.GetType(), out var pos) ? pos : int.MaxValue);
}
