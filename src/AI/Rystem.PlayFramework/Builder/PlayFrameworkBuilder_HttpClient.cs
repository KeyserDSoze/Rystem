using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for registering typed HTTP clients on PlayFrameworkBuilder.
/// </summary>
public static class PlayFrameworkBuilder_HttpClient
{
    /// <summary>
    /// Registers a typed HTTP client configuring the <see cref="System.Net.Http.HttpClient"/>
    /// directly (base address, timeout, headers, etc.).
    /// </summary>
    /// <typeparam name="TClient">
    /// Marker type used as the factory key.
    /// Must be the same type passed to <c>SceneBuilder.WithEndpoint&lt;TClient&gt;()</c>.
    /// </typeparam>
    /// <example>
    /// <code>
    /// builder.WithHttpClient&lt;IOrderServiceClient&gt;(c =&gt;
    /// {
    ///     c.BaseAddress = new Uri("http://order-service:5001/api");
    ///     c.Timeout = TimeSpan.FromSeconds(30);
    /// });
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithHttpClient<TClient>(
        this PlayFrameworkBuilder builder,
        Action<HttpClient> configureClient)
        where TClient : class
    {
        builder.Services.AddHttpClient(typeof(TClient).Name, configureClient);
        return builder;
    }

    /// <summary>
    /// Registers a typed HTTP client configuring both the <see cref="System.Net.Http.HttpClient"/>
    /// (base address, timeout, headers) and the <see cref="IHttpClientBuilder"/>
    /// (message handlers, Polly resilience, etc.).
    /// </summary>
    /// <typeparam name="TClient">
    /// Marker type used as the factory key.
    /// Must be the same type passed to <c>SceneBuilder.WithEndpoint&lt;TClient&gt;()</c>.
    /// </typeparam>
    /// <param name="configureClient">Configures the <see cref="System.Net.Http.HttpClient"/> instance.</param>
    /// <param name="configureBuilder">Configures the <see cref="IHttpClientBuilder"/> (handlers, resilience, etc.).</param>
    /// <example>
    /// <code>
    /// builder.WithHttpClient&lt;IOrderServiceClient&gt;(
    ///     c =&gt;
    ///     {
    ///         c.BaseAddress = new Uri("http://order-service:5001/api");
    ///         c.Timeout = TimeSpan.FromSeconds(30);
    ///     },
    ///     b =&gt; b.AddHttpMessageHandler&lt;BearerTokenHandler&gt;()
    ///           .AddStandardResilienceHandler());
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithHttpClient<TClient>(
        this PlayFrameworkBuilder builder,
        Action<HttpClient> configureClient,
        Action<IHttpClientBuilder> configureBuilder)
        where TClient : class
    {
        var httpClientBuilder = builder.Services.AddHttpClient(typeof(TClient).Name, configureClient);
        configureBuilder(httpClientBuilder);
        return builder;
    }
}
