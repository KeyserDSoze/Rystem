namespace Rystem.PlayFramework;

/// <summary>
/// Exception thrown when a business hook throws an unhandled exception.
/// Wraps the original exception and carries context about which hook failed,
/// in which pipeline phase, and at what priority level.
/// </summary>
public sealed class PlayFrameworkHookException : Exception
{
    /// <summary>
    /// The <see cref="Type.Name"/> of the hook implementation that threw.
    /// </summary>
    public string HookTypeName { get; }

    /// <summary>
    /// The pipeline phase in which the hook was executing.
    /// One of: "BeforeExecution", "AfterEachScene", "OnTerminalScene".
    /// </summary>
    public string Phase { get; }

    /// <summary>
    /// The priority value the hook was registered with (lower = runs first).
    /// -1 when the priority could not be determined.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="PlayFrameworkHookException"/>.
    /// </summary>
    /// <param name="hookTypeName">Short type name of the failing hook.</param>
    /// <param name="phase">Pipeline phase ("BeforeExecution" / "AfterEachScene" / "OnTerminalScene").</param>
    /// <param name="priority">Registered priority of the hook, or -1 if unknown.</param>
    /// <param name="innerException">The original exception thrown by the hook.</param>
    public PlayFrameworkHookException(
        string hookTypeName,
        string phase,
        int priority,
        Exception innerException)
        : base(
            $"Hook '{hookTypeName}' threw an unhandled exception during phase '{phase}' (priority {priority}).",
            innerException)
    {
        HookTypeName = hookTypeName;
        Phase = phase;
        Priority = priority;
    }
}
