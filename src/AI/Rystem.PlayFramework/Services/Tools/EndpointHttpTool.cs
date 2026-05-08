using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Rystem.PlayFramework.Helpers;

namespace Rystem.PlayFramework;

/// <summary>
/// An <see cref="ISceneTool"/> that executes an HTTP endpoint as an AI tool.
/// The tool schema is built at construction time from the route template,
/// optional query parameters, and an optional typed request body.
/// </summary>
internal sealed class EndpointHttpTool : ISceneTool, ISceneToolMetadata
{
    private readonly EndpointToolConfiguration _config;
    private readonly IJsonService _jsonService;
    private readonly List<string> _routeParameters;

    // Regex to extract {param} placeholders from route templates
    private static readonly Regex RouteParamRegex = new(@"\{(\w+)\}", RegexOptions.Compiled, matchTimeout: TimeSpan.FromSeconds(1));

    public EndpointHttpTool(EndpointToolConfiguration config, IJsonService? jsonService = null)
    {
        _config = config;
        _jsonService = jsonService ?? new DefaultJsonService();

        // Extract route parameter names once
        _routeParameters = RouteParamRegex
            .Matches(config.RouteTemplate)
            .Select(m => m.Groups[1].Value)
            .ToList();

        Name = config.ToolName;
        Description = config.Description;
        ToolDescription = BuildAiTool();
    }

    // ── ISceneTool ────────────────────────────────────────────────────────────

    public string Name { get; }
    public string Description { get; }
    public AITool ToolDescription { get; }

    // ── ISceneToolMetadata ────────────────────────────────────────────────────

    public PlayFrameworkToolSourceType SourceType => PlayFrameworkToolSourceType.Endpoint;
    public string? SourceName => _config.ClientType.Name;
    public string? MemberName => _config.RouteTemplate;
    public bool IsCommand => false;
    public string? JsonSchema => null;

    // ── Execution ─────────────────────────────────────────────────────────────

    public async Task<object?> ExecuteAsync(
        string arguments,
        SceneContext context,
        CancellationToken cancellationToken)
    {
        // 1. Resolve IHttpClientFactory from the DI container
        var factory = context.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(_config.ClientType.Name);

        // 2. Deserialise arguments JSON from the AI model
        Dictionary<string, JsonElement>? argsDict = null;
        if (!string.IsNullOrWhiteSpace(arguments) && arguments != "{}")
        {
            argsDict = _jsonService.Deserialize<Dictionary<string, JsonElement>>(arguments);
        }

        // 3. Build the request URL
        var url = BuildUrl(argsDict);

        // 4. Create the HttpRequestMessage
        var request = new HttpRequestMessage(_config.HttpMethod, url);

        // 5. Serialise and attach the request body when a body type is configured
        if (_config.RequestBodyType != null && argsDict != null)
        {
            var body = ExtractBodyFromArguments(argsDict);
            request.Content = new StringContent(
                _jsonService.Serialize(body, _config.RequestBodyType),
                Encoding.UTF8,
                "application/json");
        }

        // 6. Send the request
        var response = await client.SendAsync(request, cancellationToken);

        // 7. Read the response body
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // 8. Try to deserialise into TResponse; fall back to raw string on failure
        object? deserialisedBody;
        try
        {
            deserialisedBody = _jsonService.Deserialize(responseBody, _config.ResponseType);
        }
        catch
        {
            deserialisedBody = responseBody;
        }

        // 9. Return status + body so the AI can react to 4xx/5xx errors
        return new EndpointHttpResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = deserialisedBody
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string BuildUrl(Dictionary<string, JsonElement>? argsDict)
    {
        var url = _config.RouteTemplate;

        if (argsDict == null)
            return url;

        // Replace route placeholders: /orders/{orderId} → /orders/abc-123
        foreach (var routeParam in _routeParameters)
        {
            if (argsDict.TryGetValue(routeParam, out var value))
            {
                url = url.Replace(
                    $"{{{routeParam}}}",
                    value.GetString() ?? value.GetRawText());
            }
        }

        // Append query-string parameters
        var queryParts = _config.QueryParameters
            .Where(qp => argsDict.ContainsKey(qp.Name))
            .Select(qp =>
            {
                var raw = argsDict[qp.Name].GetString() ?? argsDict[qp.Name].GetRawText();
                return $"{qp.Name}={Uri.EscapeDataString(raw)}";
            });

        var queryString = string.Join("&", queryParts);
        if (!string.IsNullOrEmpty(queryString))
            url += $"?{queryString}";

        return url;
    }

    /// <summary>
    /// Reconstructs the request body object from the flat argument dictionary.
    /// Only the properties of <see cref="EndpointToolConfiguration.RequestBodyType"/> are included.
    /// </summary>
    private object? ExtractBodyFromArguments(Dictionary<string, JsonElement> argsDict)
    {
        if (_config.RequestBodyType == null)
            return null;

        // Build a JsonObject with only the body properties and then deserialise it
        var bodyNode = new JsonObject();

        // Collect names to skip (route + query params)
        var skipNames = new HashSet<string>(_routeParameters, StringComparer.OrdinalIgnoreCase);
        foreach (var qp in _config.QueryParameters)
            skipNames.Add(qp.Name);

        foreach (var kv in argsDict)
        {
            if (!skipNames.Contains(kv.Key))
                bodyNode[kv.Key] = JsonNode.Parse(kv.Value.GetRawText());
        }

        var bodyJson = bodyNode.ToJsonString();
        return _jsonService.Deserialize(bodyJson, _config.RequestBodyType);
    }

    // ── AI Tool construction ──────────────────────────────────────────────────

    private AITool BuildAiTool()
    {
        var schemaElement = BuildParametersSchema();
        return AIFunctionFactory.CreateDeclaration(Name, Description, schemaElement);
    }

    /// <summary>
    /// Builds a JSON Schema <see cref="JsonElement"/> that describes the tool parameters:
    /// route params + declared query params + (optional) request body properties.
    /// </summary>
    private JsonElement BuildParametersSchema()
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        // ── Route parameters ─────────────────────────────────────────────────
        foreach (var rp in _routeParameters)
        {
            properties[rp] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = $"Route parameter: {rp}"
            };
            required.Add(JsonValue.Create(rp)!);
        }

        // ── Query-string parameters ───────────────────────────────────────────
        foreach (var qp in _config.QueryParameters)
        {
            properties[qp.Name] = new JsonObject
            {
                ["type"] = MapTypeToJsonSchemaType(qp.Type),
                ["description"] = qp.Description
            };
            // Query params are optional by default (not added to required)
        }

        // ── Request body properties ───────────────────────────────────────────
        if (_config.RequestBodyType != null)
        {
            foreach (var prop in _config.RequestBodyType
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead)
                    continue;

                var jsonName = GetJsonPropertyName(prop);
                properties[jsonName] = new JsonObject
                {
                    ["type"] = MapTypeToJsonSchemaType(prop.PropertyType)
                };

                // Non-nullable value types are required
                if (prop.PropertyType.IsValueType
                    && Nullable.GetUnderlyingType(prop.PropertyType) == null)
                {
                    required.Add(JsonValue.Create(jsonName)!);
                }
            }
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
            schema["required"] = required;

        return JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString());
    }

    /// <summary>Maps a CLR type to its JSON Schema "type" string.</summary>
    private static string MapTypeToJsonSchemaType(Type type)
    {
        // Unwrap nullable
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(bool)) return "boolean";

        if (underlying == typeof(byte)
            || underlying == typeof(sbyte)
            || underlying == typeof(short)
            || underlying == typeof(ushort)
            || underlying == typeof(int)
            || underlying == typeof(uint)
            || underlying == typeof(long)
            || underlying == typeof(ulong))
            return "integer";

        if (underlying == typeof(float)
            || underlying == typeof(double)
            || underlying == typeof(decimal))
            return "number";

        if (underlying.IsArray
            || (underlying.IsGenericType
                && typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying)))
            return "array";

        if (underlying.IsClass && underlying != typeof(string))
            return "object";

        // string, Guid, DateTime, DateTimeOffset, Uri, enums → "string"
        return "string";
    }

    /// <summary>
    /// Returns the JSON property name for a property, honouring <see cref="JsonPropertyNameAttribute"/>
    /// and falling back to camelCase.
    /// </summary>
    private static string GetJsonPropertyName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attr?.Name
               ?? JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
    }
}

/// <summary>
/// Response wrapper returned to the AI model after an HTTP endpoint call.
/// Includes the HTTP status code so the model can react to errors (400, 422, 500, …).
/// </summary>
internal sealed class EndpointHttpResponse
{
    public int StatusCode { get; init; }
    public object? Body { get; init; }
}
