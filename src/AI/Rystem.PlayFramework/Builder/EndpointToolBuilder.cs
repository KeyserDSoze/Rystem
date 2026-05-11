using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// Builder for configuring HTTP endpoint tools for a specific typed client <typeparamref name="TClient"/>.
/// Each call to <see cref="WithAction{TResponse}"/> or <see cref="WithAction{TRequest,TResponse}"/>
/// registers one AI tool backed by an HTTP endpoint.
/// </summary>
/// <typeparam name="TClient">
/// The marker type registered with <c>PlayFrameworkBuilder.WithHttpClient&lt;TClient&gt;()</c>.
/// Used to resolve the named <see cref="System.Net.Http.IHttpClientFactory"/> client at runtime.
/// </typeparam>
public sealed class EndpointToolBuilder<TClient> where TClient : class
{
    private readonly SceneConfiguration _config;

    internal EndpointToolBuilder(SceneConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Registers an HTTP endpoint as an AI tool without a request body (GET, DELETE, HEAD, OPTIONS).
    /// Route template placeholders (<c>{param}</c>) are automatically extracted as required parameters.
    /// </summary>
    /// <typeparam name="TResponse">Expected JSON response type.</typeparam>
    /// <param name="toolName">Name exposed to the AI model.</param>
    /// <param name="method">HTTP method (e.g. <see cref="HttpMethod.Get"/>).</param>
    /// <param name="routeTemplate">
    /// Route relative to the base address, e.g. <c>/orders/{orderId}</c>.
    /// Absolute URLs are also supported.
    /// </param>
    /// <param name="description">Description for the AI model.</param>
    /// <returns>An <see cref="EndpointActionBuilder"/> for adding optional query parameters.</returns>
    public EndpointActionBuilder WithAction<TResponse>(
        string toolName,
        HttpMethod method,
        string routeTemplate,
        string description)
    {
        var config = new EndpointToolConfiguration
        {
            ClientType = typeof(TClient),
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            HttpMethod = method,
            RouteTemplate = routeTemplate,
            RequestBodyType = null,
            ResponseType = typeof(TResponse)
        };

        _config.EndpointTools.Add(config);
        return new EndpointActionBuilder(config);
    }

    /// <summary>
    /// Registers an HTTP endpoint as an AI tool with a typed request body (POST, PUT, PATCH).
    /// The public properties of <typeparamref name="TRequest"/> are exposed as tool parameters.
    /// Route template placeholders (<c>{param}</c>) are automatically extracted as well.
    /// </summary>
    /// <typeparam name="TRequest">Type of the JSON request body.</typeparam>
    /// <typeparam name="TResponse">Expected JSON response type.</typeparam>
    /// <param name="toolName">Name exposed to the AI model.</param>
    /// <param name="method">HTTP method (e.g. <see cref="HttpMethod.Post"/>).</param>
    /// <param name="routeTemplate">
    /// Route relative to the base address, e.g. <c>/orders/{orderId}</c>.
    /// </param>
    /// <param name="description">Description for the AI model.</param>
    /// <returns>An <see cref="EndpointActionBuilder"/> for adding optional query parameters.</returns>
    public EndpointActionBuilder WithAction<TRequest, TResponse>(
        string toolName,
        HttpMethod method,
        string routeTemplate,
        string description)
    {
        var config = new EndpointToolConfiguration
        {
            ClientType = typeof(TClient),
            ToolName = ToolNameNormalizer.Normalize(toolName),
            Description = description,
            HttpMethod = method,
            RouteTemplate = routeTemplate,
            RequestBodyType = typeof(TRequest),
            ResponseType = typeof(TResponse)
        };

        _config.EndpointTools.Add(config);
        return new EndpointActionBuilder(config);
    }
}

/// <summary>
/// Fluent builder for adding optional query-string parameters to an endpoint tool action.
/// </summary>
public sealed class EndpointActionBuilder
{
    private readonly EndpointToolConfiguration _config;

    internal EndpointActionBuilder(EndpointToolConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Adds a query-string parameter to the tool.
    /// This parameter is exposed in the AI schema and appended as <c>?name=value</c> to the URL.
    /// </summary>
    /// <param name="name">Parameter name (used both in the AI schema and in the query string).</param>
    /// <param name="description">Description for the AI model.</param>
    /// <param name="type">
    /// C# type of the parameter (default: <see cref="string"/>).
    /// Used to generate the JSON Schema type.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    public EndpointActionBuilder WithParameter(string name, string description, Type? type = null)
    {
        _config.QueryParameters.Add(new EndpointParameterDefinition
        {
            Name = name,
            Description = description,
            Type = type ?? typeof(string)
        });

        return this;
    }
}

/// <summary>
/// Internal configuration for a single HTTP-endpoint tool.
/// </summary>
internal sealed class EndpointToolConfiguration
{
    /// <summary>Marker type for the typed <see cref="System.Net.Http.IHttpClientFactory"/> client.</summary>
    public required Type ClientType { get; init; }

    /// <summary>Normalized tool name exposed to the AI model.</summary>
    public required string ToolName { get; init; }

    /// <summary>Description sent to the AI model.</summary>
    public required string Description { get; init; }

    /// <summary>HTTP method (GET, POST, PUT, DELETE, PATCH, …).</summary>
    public required HttpMethod HttpMethod { get; init; }

    /// <summary>
    /// Route template, e.g. <c>/orders/{orderId}</c>.
    /// Placeholders in curly braces are extracted automatically as required AI parameters.
    /// </summary>
    public required string RouteTemplate { get; init; }

    /// <summary>
    /// Type of the JSON request body, or <c>null</c> for methods without a body.
    /// </summary>
    public Type? RequestBodyType { get; init; }

    /// <summary>Type expected in the JSON response (used for deserialisation).</summary>
    public required Type ResponseType { get; init; }

    /// <summary>Query-string parameters declared via <see cref="EndpointActionBuilder.WithParameter"/>.</summary>
    public List<EndpointParameterDefinition> QueryParameters { get; init; } = [];
}

/// <summary>
/// Describes a single query-string parameter for an endpoint tool.
/// </summary>
internal sealed class EndpointParameterDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Type Type { get; init; } = typeof(string);
}
