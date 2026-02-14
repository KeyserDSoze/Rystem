namespace Rystem.PlayFramework;

/// <summary>
/// Extensions for configuring rate limiting in PlayFramework.
/// </summary>
public static class PlayFrameworkBuilderRateLimitExtensions
{
    /// <summary>
    /// Enable rate limiting for this PlayFramework instance.
    /// Rate limits can be grouped by metadata keys (e.g., userId, tenantId).
    /// </summary>
    /// <param name="builder">PlayFramework builder</param>
    /// <param name="configure">Rate limit configuration</param>
    /// <example>
    /// <code>
    /// services.AddPlayFramework(builder => builder
    ///     .WithRateLimit(limit => limit
    ///         .GroupBy("userId")
    ///         .TokenBucket(capacity: 100, refillRate: 10)
    ///         .WaitOnExceeded(TimeSpan.FromSeconds(30))));
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithRateLimit(
        this PlayFrameworkBuilder builder,
        Action<RateLimitBuilder> configure)
    {
        var rateLimitBuilder = new RateLimitBuilder(builder.Services, builder.Name);
        configure(rateLimitBuilder);
        
        var settings = rateLimitBuilder.Build();
        builder.Settings.RateLimiting = settings;

        return builder;
    }
}
