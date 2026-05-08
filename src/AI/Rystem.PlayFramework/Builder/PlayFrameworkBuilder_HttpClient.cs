using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework;

/// <summary>
/// Extension methods for registering typed HTTP clients on PlayFrameworkBuilder.
/// </summary>
public static class PlayFrameworkBuilder_HttpClient
{
    /// <summary>
    /// Registers a typed HTTP client via IHttpClientFactory.
    /// <typeparamref name="TClient"/> is a marker type (typically an empty interface)
    /// used as the named-client key: <c>typeof(TClient).Name</c>.
    /// The <paramref name="configure"/> delegate exposes the standard
    /// <see cref="IHttpClientBuilder"/> so you can set base address,
    /// add <see cref="System.Net.Http.DelegatingHandler"/>s, Polly resilience, etc.
    /// </summary>
    /// <typeparam name="TClient">
    /// Marker type used as the factory key.
    /// Must be the same type passed to <c>SceneBuilder.WithEndpoint&lt;TClient&gt;()</c>.
    /// </typeparam>
    /// <param name="builder">The PlayFramework builder.</param>
    /// <param name="configure">Action that configures the underlying <see cref="IHttpClientBuilder"/>.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.WithHttpClient&lt;IOrderServiceClient&gt;(http =&gt;
    /// {
    ///     http.ConfigureHttpClient(c =&gt;
    ///     {
    ///         c.BaseAddress = new Uri("http://order-service:5001/api");
    ///         c.Timeout = TimeSpan.FromSeconds(30);
    ///     });
    ///     http.AddHttpMessageHandler&lt;BearerTokenHandler&gt;();
    ///     http.AddStandardResilienceHandler();
    /// });
    /// </code>
    /// </example>
    public static PlayFrameworkBuilder WithHttpClient<TClient>(
        this PlayFrameworkBuilder builder,
        Action<IHttpClientBuilder> configure)
        where TClient : class
    {
        var httpClientBuilder = builder.Services.AddHttpClient(typeof(TClient).Name);
        configure(httpClientBuilder);
        return builder;
    }
}
