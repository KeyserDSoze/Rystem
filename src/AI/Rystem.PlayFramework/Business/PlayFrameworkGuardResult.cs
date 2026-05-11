namespace Rystem.PlayFramework;

/// <summary>
/// Possible outcomes of a <see cref="IPlayFrameworkBeforeExecution"/> hook.
/// </summary>
public enum PlayFrameworkGuardResultType
{
    /// <summary>Execution proceeds to the next guard (or to the stream if last).</summary>
    Allow,

    /// <summary>Execution is blocked; the client receives an HTTP error response.</summary>
    Deny,

    /// <summary>Execution is short-circuited with a single synthetic SSE item.</summary>
    ShortCircuit
}

/// <summary>
/// Result returned by a <see cref="IPlayFrameworkBeforeExecution"/> hook.
/// Use the static factory methods to create instances.
/// </summary>
public sealed class PlayFrameworkGuardResult
{
    /// <summary>The type of result.</summary>
    public PlayFrameworkGuardResultType Type { get; }

    /// <summary>
    /// Non-null when <see cref="Type"/> is <see cref="PlayFrameworkGuardResultType.Deny"/>.
    /// Contains the HTTP status code and optional error detail.
    /// </summary>
    public PlayFrameworkDenyResult? DenyResult { get; }

    /// <summary>
    /// Non-null when <see cref="Type"/> is <see cref="PlayFrameworkGuardResultType.ShortCircuit"/>.
    /// The synthetic <see cref="AiSceneResponse"/> sent as the single SSE event.
    /// </summary>
    public AiSceneResponse? ShortCircuitResponse { get; }

    private PlayFrameworkGuardResult(
        PlayFrameworkGuardResultType type,
        PlayFrameworkDenyResult? deny = null,
        AiSceneResponse? shortCircuit = null)
    {
        Type = type;
        DenyResult = deny;
        ShortCircuitResponse = shortCircuit;
    }

    /// <summary>Allows execution to proceed to the next guard or to the stream.</summary>
    public static PlayFrameworkGuardResult Allow()
        => new(PlayFrameworkGuardResultType.Allow);

    /// <summary>
    /// Blocks execution; the endpoint returns the given HTTP status code with an optional error message.
    /// </summary>
    /// <param name="statusCode">HTTP status code (e.g. 403, 429).</param>
    /// <param name="errorDetail">Human-readable error detail.</param>
    public static PlayFrameworkGuardResult Deny(int statusCode, string? errorDetail = null)
        => new(PlayFrameworkGuardResultType.Deny,
               deny: new PlayFrameworkDenyResult { StatusCode = statusCode, ErrorDetail = errorDetail });

    /// <summary>
    /// Short-circuits execution; the SSE stream is opened with a single synthetic item then closed.
    /// Subsequent guards are skipped and <see cref="ISceneManager"/> is never called.
    /// </summary>
    /// <param name="response">The synthetic scene response to send.</param>
    public static PlayFrameworkGuardResult ShortCircuit(AiSceneResponse response)
        => new(PlayFrameworkGuardResultType.ShortCircuit, shortCircuit: response);
}
