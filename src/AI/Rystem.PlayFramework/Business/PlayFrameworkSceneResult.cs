namespace Rystem.PlayFramework;

/// <summary>
/// Possible outcomes of a <see cref="IPlayFrameworkAfterEachScene"/> hook.
/// </summary>
public enum PlayFrameworkSceneResultType
{
    /// <summary>The item (optionally modified) is forwarded to the SSE stream.</summary>
    Forward,

    /// <summary>The item is discarded; it is not sent to the client.</summary>
    Suppress,

    /// <summary>The item is forwarded and extra synthetic items are appended immediately after.</summary>
    ForwardAndInject
}

/// <summary>
/// Result returned by a <see cref="IPlayFrameworkAfterEachScene"/> hook.
/// Use the static factory methods to create instances.
/// </summary>
public sealed class PlayFrameworkSceneResult
{
    /// <summary>The type of result.</summary>
    public PlayFrameworkSceneResultType Type { get; }

    /// <summary>
    /// Non-null when <see cref="Type"/> is <see cref="PlayFrameworkSceneResultType.Forward"/>
    /// or <see cref="PlayFrameworkSceneResultType.ForwardAndInject"/>.
    /// The (possibly modified) scene response to send to the client.
    /// </summary>
    public AiSceneResponse? Scene { get; }

    /// <summary>
    /// Non-null when <see cref="Type"/> is <see cref="PlayFrameworkSceneResultType.ForwardAndInject"/>.
    /// Extra items to append to the SSE stream after <see cref="Scene"/>.
    /// These extra items bypass <see cref="IPlayFrameworkAfterEachScene"/> hooks.
    /// </summary>
    public AiSceneResponse[]? ExtraItems { get; }

    private PlayFrameworkSceneResult(
        PlayFrameworkSceneResultType type,
        AiSceneResponse? scene = null,
        AiSceneResponse[]? extra = null)
    {
        Type = type;
        Scene = scene;
        ExtraItems = extra;
    }

    /// <summary>
    /// Forwards the (optionally modified) scene response to the client.
    /// </summary>
    /// <param name="scene">The scene to forward.</param>
    public static PlayFrameworkSceneResult Forward(AiSceneResponse scene)
        => new(PlayFrameworkSceneResultType.Forward, scene);

    /// <summary>
    /// Suppresses the scene response; it will not be written to the SSE stream.
    /// </summary>
    public static PlayFrameworkSceneResult Suppress()
        => new(PlayFrameworkSceneResultType.Suppress);

    /// <summary>
    /// Forwards the scene response and appends extra synthetic items to the SSE stream.
    /// The extra items bypass after-each-scene hooks.
    /// </summary>
    /// <param name="scene">The primary scene to forward.</param>
    /// <param name="extra">Additional items to inject after <paramref name="scene"/>.</param>
    public static PlayFrameworkSceneResult ForwardAndInject(AiSceneResponse scene, params AiSceneResponse[] extra)
        => new(PlayFrameworkSceneResultType.ForwardAndInject, scene, extra);
}
