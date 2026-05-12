using System.Collections.Concurrent;
using System.Security.Claims;

namespace Rystem.PlayFramework;

/// <summary>
/// Shared context passed to every hook in the same request pipeline.
/// Constructed by <see cref="IPlayFrameworkBusinessManager"/> before executing hooks.
/// </summary>
public sealed class PlayFrameworkExecutionContext
{
    /// <summary>The user's text message.</summary>
    public required string Message { get; init; }

    /// <summary>
    /// Multi-modal input (text + images/audio/files).
    /// When set, the business manager calls <see cref="ISceneManager.ExecuteAsync(MultiModalInput,Dictionary{string,object}?,SceneRequestSettings?,CancellationToken)"/>
    /// instead of the string overload.
    /// </summary>
    public MultiModalInput? Input { get; init; }

    /// <summary>Conversation key from the request (can be null for new conversations).</summary>
    public string? ConversationKey { get; init; }

    /// <summary>
    /// Request settings. Mutable — a <see cref="IPlayFrameworkBeforeExecution"/> hook can modify
    /// these before they are passed to <see cref="ISceneManager.ExecuteAsync"/>.
    /// </summary>
    public SceneRequestSettings Settings { get; init; } = new();

    /// <summary>
    /// HTTP metadata built from the request (userId, ipAddress, requestId, timestamp, custom keys).
    /// Passed directly to <see cref="ISceneManager.ExecuteAsync"/>.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// The authenticated user from the HTTP context.
    /// <c>null</c> for unauthenticated requests.
    /// Hook implementations read claim values via <c>User.FindFirst(...)</c>.
    /// </summary>
    public ClaimsPrincipal? User { get; init; }

    /// <summary>
    /// Thread-safe bag shared across all hooks in the same request.
    /// Use this to pass data from one hook to another (e.g. cache key computed in
    /// <see cref="IPlayFrameworkBeforeExecution"/> and consumed in <see cref="IPlayFrameworkAfterEachScene"/>).
    /// </summary>
    public ConcurrentDictionary<string, object> Items { get; init; } = new();
}
