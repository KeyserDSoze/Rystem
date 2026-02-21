namespace Rystem.PlayFramework;

/// <summary>
/// Factory for creating actors from configuration.
/// </summary>
internal static class ActorFactory
{
    public static IActor Create(ActorConfiguration config)
    {
        // Custom actor type
        if (config.ActorType != null)
        {
            return new ServiceActor(config.ActorType, config.CacheForSubsequentCalls)!;
        }

        // Static message
        if (config.StaticMessage != null)
        {
            return new StaticActor(config.StaticMessage, config.CacheForSubsequentCalls);
        }

        // Dynamic factory
        if (config.MessageFactory != null)
        {
            return new DynamicActor(config.MessageFactory, config.CacheForSubsequentCalls);
        }

        // Async factory
        if (config.AsyncMessageFactory != null)
        {
            return new AsyncDynamicActor(config.AsyncMessageFactory, config.CacheForSubsequentCalls);
        }

        throw new InvalidOperationException("Invalid actor configuration");
    }
}
