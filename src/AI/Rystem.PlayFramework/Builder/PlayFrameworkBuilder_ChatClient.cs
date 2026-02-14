using Microsoft.Extensions.DependencyInjection;
using Rystem;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for configuring chat client load balancing, fallback, and retry features.
/// </summary>
public static class PlayFrameworkBuilder_ChatClient
{
    /// <summary>
    /// Adds a chat client to the PRIMARY load balancing pool.
    /// These clients share the request load based on LoadBalancingMode.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="name">Factory key for the chat client.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddPlayFramework(builder =>
    /// {
    ///     builder.WithChatClient("gpt-4o-instance-1")
    ///            .WithChatClient("gpt-4o-instance-2")
    ///            .WithLoadBalancingMode(LoadBalancingMode.RoundRobin);
    /// });
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithChatClient(
        this PlayFrameworkBuilder builder,
        string name)
    {
        builder.ConfigureSettings(settings =>
        {
            if (!settings.ChatClientNames.Contains(name))
            {
                settings.ChatClientNames.Add(name);
            }
        });
        return builder;
    }

    /// <summary>
    /// Adds a chat client to the FALLBACK chain.
    /// These clients are used ONLY if all primary clients fail.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="name">Factory key for the fallback chat client.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddPlayFramework(builder =>
    /// {
    ///     builder.WithChatClient("gpt-4o-1")
    ///            .WithChatClient("gpt-4o-2")
    ///            .WithChatClientAsFallback("claude-sonnet")
    ///            .WithChatClientAsFallback("llama-3.1");
    /// });
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithChatClientAsFallback(
        this PlayFrameworkBuilder builder,
        string name)
    {
        builder.ConfigureSettings(settings =>
        {
            if (!settings.FallbackChatClientNames.Contains(name))
            {
                settings.FallbackChatClientNames.Add(name);
            }
        });
        return builder;
    }

    /// <summary>
    /// Sets the load balancing mode for primary client pool.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="mode">
    /// Load balancing mode:
    /// - None: Use only first client (no load balancing)
    /// - Sequential: Distribute sequentially
    /// - RoundRobin: Balanced rotation
    /// - Random: Random selection
    /// </param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithLoadBalancingMode(LoadBalancingMode.RoundRobin);
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithLoadBalancingMode(
        this PlayFrameworkBuilder builder,
        LoadBalancingMode mode)
    {
        builder.ConfigureSettings(settings =>
        {
            settings.LoadBalancingMode = mode;
        });
        return builder;
    }

    /// <summary>
    /// Sets the fallback mode for fallback chain.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="mode">
    /// Fallback mode:
    /// - Sequential: Try in order (A → B → C)
    /// - RoundRobin: Distribute load evenly
    /// - Random: Random selection
    /// </param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithFallbackMode(FallbackMode.Sequential);
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithFallbackMode(
        this PlayFrameworkBuilder builder,
        FallbackMode mode)
    {
        builder.ConfigureSettings(settings =>
        {
            settings.FallbackMode = mode;
        });
        return builder;
    }

    /// <summary>
    /// Sets retry configuration for transient errors.
    /// </summary>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="maxAttempts">Maximum retry attempts per client (default: 3).</param>
    /// <param name="baseDelaySeconds">Base delay for exponential backoff in seconds (default: 1.0).</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithRetryPolicy(maxAttempts: 5, baseDelaySeconds: 2.0);
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithRetryPolicy(
        this PlayFrameworkBuilder builder,
        int maxAttempts = 3,
        double baseDelaySeconds = 1.0)
    {
        builder.ConfigureSettings(settings =>
        {
            settings.MaxRetryAttempts = maxAttempts;
            settings.RetryBaseDelaySeconds = baseDelaySeconds;
        });
        return builder;
    }

    /// <summary>
    /// Registers a custom transient error detector for retry logic.
    /// </summary>
    /// <typeparam name="TDetector">Custom implementation of ITransientErrorDetector.</typeparam>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// By default, PlayFramework uses DefaultTransientErrorDetector which handles:
    /// - Transient: timeouts, rate limits, 5xx errors, model overloaded
    /// - Non-transient: auth failures, bad requests, content violations
    /// 
    /// Use this method to customize error classification for specific scenarios.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomErrorDetector : ITransientErrorDetector
    /// {
    ///     public bool IsTransient(Exception ex) => 
    ///         ex is CustomRetryableException;
    ///     
    ///     public bool IsNonTransient(Exception ex) =>
    ///         ex is CustomFatalException;
    /// }
    ///
    /// services.AddFactory&lt;ITransientErrorDetector, CustomErrorDetector&gt;("myapp");
    /// 
    /// services.AddPlayFramework("myapp", builder =>
    /// {
    ///     builder.WithTransientErrorDetector&lt;CustomErrorDetector&gt;("myapp");
    /// });
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithTransientErrorDetector<TDetector>(
        this PlayFrameworkBuilder builder,
        AnyOf<string?, Enum>? name = null)
        where TDetector : class, ITransientErrorDetector
    {
        builder.Services.AddFactory<ITransientErrorDetector, TDetector>(
            name: name ?? builder.Name,
            lifetime: ServiceLifetime.Singleton);

        builder.HasCustomTransientErrorDetector = true;

        return builder;
    }
}
