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
    /// Duplicate registrations detected at startup.
    /// Each entry describes an implementation type that was registered more than once,
    /// listing the priorities it was registered with.
    /// Populated by <see cref="Build"/> and consumed by the business manager to emit warnings.
    /// </summary>
    internal IReadOnlyList<(Type HookImpl, IReadOnlyList<int> Priorities)> DuplicateRegistrations { get; private set; }
        = [];

    /// <summary>
    /// Builds the sorted position map from the full registration list.
    /// Called once by <see cref="Builder.PlayFrameworkBusinessBuilder.BuildRegistry"/> after
    /// all hooks have been registered, before the DI container is built.
    /// Equal-priority hooks preserve registration order (stable sort).
    /// Detects duplicate implementation types (across all phases) and stores them in
    /// <see cref="DuplicateRegistrations"/> for later logging.
    /// </summary>
    internal void Build(IReadOnlyList<(Type HookInterface, Type HookImpl, int Priority)> registrations)
    {
        // Raw priorities: first registration wins when the same type appears more than once.
        // All occurrences are examined for duplicate detection regardless.
        _rawPriorities = registrations
            .GroupBy(r => r.HookImpl)
            .ToDictionary(g => g.Key, g => g.First().Priority);

        // Detect duplicates: same HookImpl registered more than once (any phase)
        var duplicates = registrations
            .GroupBy(r => r.HookImpl)
            .Where(g => g.Count() > 1)
            .Select(g => (HookImpl: g.Key, Priorities: (IReadOnlyList<int>)g.Select(r => r.Priority).ToList()))
            .ToList();
        DuplicateRegistrations = duplicates;

        // Sorted positions: compute once, use first (lowest) position when the same type
        // appears multiple times so all instances of that type sort before any later type.
        var sortedEntries = registrations
            .Select((r, registrationOrder) => (r.HookImpl, r.Priority, registrationOrder))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.registrationOrder)
            .Select((x, position) => (x.HookImpl, position))
            .ToList();

        var positions = new Dictionary<Type, int>();
        foreach (var (hookImpl, position) in sortedEntries)
        {
            // TryAdd: first (lowest sort index = highest priority) wins for duplicate types.
            positions.TryAdd(hookImpl, position);
        }
        _sortedPositions = positions;
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
