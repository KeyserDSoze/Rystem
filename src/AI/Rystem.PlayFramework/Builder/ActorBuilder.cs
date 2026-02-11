namespace Rystem.PlayFramework;

/// <summary>
/// Builder for adding actors to a scene.
/// </summary>
public sealed class ActorBuilder
{
    private readonly SceneConfiguration _config;

    internal ActorBuilder(SceneConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Adds a static system message actor.
    /// </summary>
    public ActorBuilder AddActor(string message, bool cacheForSubsequentCalls = false)
    {
        _config.Actors.Add(new ActorConfiguration
        {
            StaticMessage = message,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds a dynamic actor with context access.
    /// </summary>
    public ActorBuilder AddActor(Func<SceneContext, string> messageFactory, bool cacheForSubsequentCalls = false)
    {
        _config.Actors.Add(new ActorConfiguration
        {
            MessageFactory = messageFactory,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds an async dynamic actor.
    /// </summary>
    public ActorBuilder AddActor(Func<SceneContext, CancellationToken, Task<string>> asyncMessageFactory, bool cacheForSubsequentCalls = false)
    {
        _config.Actors.Add(new ActorConfiguration
        {
            AsyncMessageFactory = asyncMessageFactory,
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }

    /// <summary>
    /// Adds a custom actor type.
    /// </summary>
    public ActorBuilder AddActor<TActor>(bool cacheForSubsequentCalls = false) where TActor : class, IActor
    {
        _config.Actors.Add(new ActorConfiguration
        {
            ActorType = typeof(TActor),
            CacheForSubsequentCalls = cacheForSubsequentCalls
        });
        return this;
    }
}
