namespace Rystem.PlayFramework;

/// <summary>
/// Simple actor that returns a static message.
/// </summary>
internal sealed class StaticActor : IActor
{
    private readonly string _message;
    private readonly bool _cacheForSubsequentCalls;

    public StaticActor(string message, bool cacheForSubsequentCalls)
    {
        _message = message;
        _cacheForSubsequentCalls = cacheForSubsequentCalls;
    }

    public Task<ActorResponse> PlayAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ActorResponse
        {
            Message = _message,
            CacheForSubsequentCalls = _cacheForSubsequentCalls
        });
    }
}

/// <summary>
/// Actor that uses a factory function.
/// </summary>
internal sealed class DynamicActor : IActor
{
    private readonly Func<SceneContext, string> _messageFactory;
    private readonly bool _cacheForSubsequentCalls;

    public DynamicActor(Func<SceneContext, string> messageFactory, bool cacheForSubsequentCalls)
    {
        _messageFactory = messageFactory;
        _cacheForSubsequentCalls = cacheForSubsequentCalls;
    }

    public Task<ActorResponse> PlayAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        var message = _messageFactory(context);
        return Task.FromResult(new ActorResponse
        {
            Message = message,
            CacheForSubsequentCalls = _cacheForSubsequentCalls
        });
    }
}

/// <summary>
/// Actor that uses an async factory function.
/// </summary>
internal sealed class AsyncDynamicActor : IActor
{
    private readonly Func<SceneContext, CancellationToken, Task<string>> _asyncMessageFactory;
    private readonly bool _cacheForSubsequentCalls;

    public AsyncDynamicActor(Func<SceneContext, CancellationToken, Task<string>> asyncMessageFactory, bool cacheForSubsequentCalls)
    {
        _asyncMessageFactory = asyncMessageFactory;
        _cacheForSubsequentCalls = cacheForSubsequentCalls;
    }

    public async Task<ActorResponse> PlayAsync(SceneContext context, CancellationToken cancellationToken = default)
    {
        var message = await _asyncMessageFactory(context, cancellationToken);
        return new ActorResponse
        {
            Message = message,
            CacheForSubsequentCalls = _cacheForSubsequentCalls
        };
    }
}
