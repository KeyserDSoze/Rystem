using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

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
        using var request = new HttpRequestMessage(_config.HttpMethod, url);

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
        using var response = await client.SendAsync(request, cancellationToken);

        // 7. Read the response body
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // 8. Parse the response body as a JsonElement so that it can always be
        //    re-serialised safely by ToolExecutionManager, regardless of the
        //    configured ResponseType (e.g. OneOf<T0,T1>, AnyOf, custom union
        //    types). Deserialising into the C# type and then storing it in an
        //    object? field would force the downstream serialiser to handle the
        //    custom type — which fails without the right converters in scope.
        //    JsonElement is natively serialisable and preserves the JSON shape
        //    exactly as the AI needs to see it.
        object? parsedBody = null;
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                parsedBody = JsonDocument.Parse(responseBody).RootElement.Clone();
            }
            catch
            {
                parsedBody = responseBody;
            }
        }

        // 9. Return status + body so the AI can react to 4xx/5xx errors
        return new EndpointHttpResponse
        {
            StatusCode = (int)response.StatusCode,
            Body = parsedBody
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
            .SelectMany(qp =>
            {
                var element = argsDict[qp.Name];

                // Array values (e.g. List<Guid>) are expanded to repeated params:
                // ["a","b"] → key=a&key=b  (ASP.NET Core model-binding convention)
                if (element.ValueKind == JsonValueKind.Array)
                {
                    return element.EnumerateArray()
                        .Select(v => $"{qp.Name}={Uri.EscapeDataString(v.GetString() ?? v.GetRawText())}");
                }

                var raw = element.GetString() ?? element.GetRawText();
                return [$"{qp.Name}={Uri.EscapeDataString(raw)}"];
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

        foreach (var kv in argsDict.Where(kv => !skipNames.Contains(kv.Key)))
        {
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
            var qpNode = BuildJsonSchemaNode(qp.Type);
            qpNode["description"] = qp.Description;
            properties[qp.Name] = qpNode;
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
                properties[jsonName] = BuildJsonSchemaNode(prop.PropertyType);

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

        schema["required"] = required;

        return JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString());
    }

    /// <summary>
    /// Builds a JSON Schema <see cref="JsonObject"/> node for the given CLR type.
    /// For array / collection types the required <c>"items"</c> sub-schema is included
    /// recursively so that the generated schema is valid for AI model APIs.
    /// For enum types the valid names are listed in an <c>"enum"</c> array.
    /// <see cref="Dictionary{TKey,TValue}"/> and other <c>IDictionary</c> types are
    /// mapped to <c>"object"</c> rather than <c>"array"</c>.
    /// </summary>
    private static JsonObject BuildJsonSchemaNode(Type type)
    {
        // Unwrap Nullable<T>: List<Guid>? → List<Guid>, int? → int
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        // ── Enum ─────────────────────────────────────────────────────────────
        // Must be checked before IsArray / IEnumerable because Enum extends ValueType,
        // not IEnumerable, but being explicit prevents any future ambiguity.
        if (underlying.IsEnum)
        {
            var enumArray = new JsonArray();
            foreach (var name in Enum.GetNames(underlying))
                enumArray.Add(JsonValue.Create(name));
            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = enumArray
            };
        }

        // ── T[] ──────────────────────────────────────────────────────────────
        if (underlying.IsArray)
        {
            var elementType = underlying.GetElementType()!;
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildJsonSchemaNode(elementType)
            };
        }

        // ── IDictionary / Dictionary<K,V> → object ───────────────────────────
        // Must be checked BEFORE the generic IEnumerable branch, because
        // Dictionary<K,V> also implements IEnumerable<KeyValuePair<K,V>> and
        // would otherwise be misidentified as an array.
        // Covers: System.Collections.IDictionary (non-generic), IDictionary<K,V>
        // (generic interface itself), and any concrete type that implements it
        // (Dictionary<K,V>, SortedDictionary<K,V>, ConcurrentDictionary<K,V>, …).
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(underlying)
            || (underlying.IsGenericType
                && underlying.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>))
            || underlying.GetInterfaces().Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>)))
        {
            return new JsonObject { ["type"] = "object" };
        }

        // ── IEnumerable<T>: List<T>, ICollection<T>, IReadOnlyList<T>, etc. ──
        if (underlying.IsGenericType
            && typeof(System.Collections.IEnumerable).IsAssignableFrom(underlying))
        {
            var elementType = underlying.GetGenericArguments()[0];
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildJsonSchemaNode(elementType)
            };
        }

        // ── Scalar types ─────────────────────────────────────────────────────
        return new JsonObject { ["type"] = MapScalarTypeToJsonSchemaType(underlying) };
    }

    /// <summary>
    /// Maps a non-collection, already-unwrapped CLR type to its JSON Schema scalar
    /// type string: <c>"boolean"</c>, <c>"integer"</c>, <c>"number"</c>,
    /// <c>"object"</c>, or <c>"string"</c>.
    /// </summary>
    private static string MapScalarTypeToJsonSchemaType(Type type)
    {
        if (type == typeof(bool)) return "boolean";

        if (type == typeof(byte)
            || type == typeof(sbyte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong))
            return "integer";

        if (type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal))
            return "number";

        if (type.IsClass && type != typeof(string))
            return "object";

        // string, Guid, DateTime, DateTimeOffset, DateOnly, Uri, enums → "string"
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
