namespace Rystem.PlayFramework;

/// <summary>
/// Stores hook priority information for a single factory.
/// Registered as a Singleton per factory name via
/// <c>services.AddFactory(registry, factoryName, Singleton)</c>.
/// </summary>
public sealed class PlayFrameworkHookRegistry
{
    private readonly Dictionary<Type, int> _priorities = new();

    /// <summary>
    /// Records the priority for a hook implementation type.
    /// Called by <see cref="Builder.PlayFrameworkBusinessBuilder"/> during registration.
    /// </summary>
    internal void Register(Type hookImplementationType, int priority)
        => _priorities[hookImplementationType] = priority;

    /// <summary>
    /// Returns the given hooks sorted ascending by their registered priority.
    /// Hooks with no registered priority are treated as priority 0.
    /// </summary>
    public IEnumerable<T> SortByPriority<T>(IEnumerable<T> hooks) where T : notnull
        => hooks.OrderBy(h => _priorities.TryGetValue(h.GetType(), out var p) ? p : 0);
}
