using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Rystem.PlayFramework.Test.Tests;

// ── Marker types used as IHttpClientFactory keys ─────────────────────────────

internal interface IOrderServiceClient { }

// ── Shared HTTP helper types ──────────────────────────────────────────────────

/// <summary>
/// Captures the last outgoing <see cref="HttpRequestMessage"/> and returns a
/// pre-configured <see cref="HttpResponseMessage"/>.
/// </summary>
internal sealed class CapturingHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public HttpRequestMessage? CapturedRequest { get; private set; }
    public string? CapturedRequestBody { get; private set; }

    public CapturingHttpHandler(HttpResponseMessage response) => _response = response;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        if (request.Content is not null)
            CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        return _response;
    }
}

/// <summary>
/// Mock <see cref="IChatClient"/> that on the first call emits a single tool
/// call with the configured arguments, and on every subsequent call returns a
/// plain text "Done" message.
/// </summary>
internal sealed class EndpointToolCallingChatClient : IChatClient
{
    private readonly string _toolName;
    private readonly Dictionary<string, object?> _arguments;
    private int _callCount;

    public EndpointToolCallingChatClient(string toolName, Dictionary<string, object?> arguments)
    {
        _toolName = toolName;
        _arguments = arguments;
    }

    public ChatClientMetadata Metadata => new("endpoint-mock", null, "mock-model");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _callCount++;
        ChatMessage msg;

        if (_callCount == 1)
        {
            msg = new ChatMessage(ChatRole.Assistant, "Calling tool");
            msg.Contents.Add(new FunctionCallContent(
                Guid.NewGuid().ToString(),
                _toolName,
                _arguments));
        }
        else
        {
            msg = new ChatMessage(ChatRole.Assistant, "Done");
        }

        return Task.FromResult(new ChatResponse([msg]) { ModelId = "mock-model" });
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

// ── Request / Response body DTOs used in schema tests ────────────────────────

internal sealed class CreateOrderRequest
{
    public string CustomerName { get; set; } = "";
    public int Quantity { get; set; }           // non-nullable value type → required
    public decimal? TotalPrice { get; set; }    // nullable → not required
}

internal sealed class OrderResponse
{
    public string OrderId { get; set; } = "";
    public string Status { get; set; } = "";
}

/// <summary>
/// Request DTO with nullable <see cref="List{T}"/> of <see cref="Guid"/> — the scenario
/// that was producing "array schema missing items" when sent to AI model APIs.
/// </summary>
internal sealed class FindByGuidsRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("projectIds")]
    public List<Guid>? ProjectIds { get; set; } = [];

    [System.Text.Json.Serialization.JsonPropertyName("entityIds")]
    public List<Guid>? EntityIds { get; set; } = [];
}

/// <summary>
/// Request DTO that covers all collection variants to be supported.
/// </summary>
internal sealed class CollectionTypesRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("guidList")]
    public List<Guid>? GuidList { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("intList")]
    public List<int> IntList { get; set; } = [];

    [System.Text.Json.Serialization.JsonPropertyName("stringArray")]
    public string[] StringArray { get; set; } = [];

    [System.Text.Json.Serialization.JsonPropertyName("enumerable")]
    public IEnumerable<string>? Enumerable { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("readOnlyList")]
    public IReadOnlyList<decimal>? ReadOnlyList { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("collection")]
    public ICollection<bool>? Collection { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("scalar")]
    public string? Scalar { get; set; }
}

internal enum SortOrder { Ascending, Descending, Relevance }

/// <summary>
/// Request DTO with an enum property, to verify "enum":[names] in schema.
/// </summary>
internal sealed class SortedSearchRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    public string? Query { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("sortOrder")]
    public SortOrder SortOrder { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("nullableSortOrder")]
    public SortOrder? NullableSortOrder { get; set; }
}

/// <summary>
/// Request DTO with a Dictionary property, to verify it maps to "object" not "array".
/// </summary>
internal sealed class MetadataRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("counts")]
    public IDictionary<string, int>? Counts { get; set; }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public sealed class EndpointHttpToolTests
{
    /// <summary>
    /// Gets the JSON schema from an AITool via reflection, because in
    /// Microsoft.Extensions.AI.Abstractions ≥ 10.4 the JsonSchema property is
    /// declared on the concrete DefaultAIFunctionDeclaration class, not on AITool.
    /// </summary>
    private static JsonElement GetAiToolJsonSchema(AITool tool)
    {
        var prop = tool.GetType().GetProperty("JsonSchema",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        Assert.NotNull(prop);
        var value = prop.GetValue(tool);
        Assert.NotNull(value);
        return (JsonElement)value;
    }

    // ── Helper: build a minimal ServiceProvider with one HTTP-endpoint scene ──

    // ── Group 1: Registration ─────────────────────────────────────────────────

    [Fact]
    public void Registration_SingleGetAction_ToolAppearsInSceneTools()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("GetOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>(
                        "GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ISceneFactory>();
        var ordersScene = factory.TryGetScene("Orders");

        Assert.NotNull(ordersScene);
        Assert.Single(ordersScene.Tools);
        Assert.IsType<EndpointHttpTool>(ordersScene.Tools[0]);
        Assert.Equal("GetOrder", ordersScene.Tools[0].Name);
    }

    [Fact]
    public async Task Registration_WithClientAndBuilderConfig_HandlerIsInvoked()
    {
        // Verifies the two-parameter overload: Action<HttpClient> + Action<IHttpClientBuilder>
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"orderId":"x","status":"Ok"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient(
            "GetOrder",
            new Dictionary<string, object?> { ["orderId"] = "x" }));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c =>
                {
                    c.BaseAddress = new Uri("http://order-api/");
                    c.Timeout = TimeSpan.FromSeconds(10);
                },
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        await foreach (var _ in sceneManager.ExecuteAsync("Get order x", settings: settings)) { }

        // The custom handler (via IHttpClientBuilder) was invoked
        Assert.NotNull(handler.CapturedRequest);
        Assert.Contains("x", handler.CapturedRequest!.RequestUri!.ToString());
    }

    [Fact]
    public void Registration_MultipleActions_AllToolsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("GetOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                    ep.WithAction<CreateOrderRequest, OrderResponse>("CreateOrder", HttpMethod.Post, "/orders", "Create order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var scene = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!;

        Assert.Equal(2, scene.Tools.Count);
        Assert.Contains(scene.Tools, t => t.Name == "GetOrder");
        Assert.Contains(scene.Tools, t => t.Name == "CreateOrder");
    }

    [Fact]
    public void Registration_ToolNameWithSpaces_IsNormalized()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("Get_Order", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("Get Order", HttpMethod.Get, "/orders/{orderId}", "desc");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var scene = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!;

        // Spaces → underscores via ToolNameNormalizer
        Assert.Equal("Get_Order", scene.Tools[0].Name);
    }

    // ── Group 2: Schema ───────────────────────────────────────────────────────

    [Fact]
    public void Schema_GetWithRouteParam_SchemaContainsRequiredRouteParam()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("GetOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>(
                        "GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!.Tools[0];
        var schema = GetAiToolJsonSchema(tool.ToolDescription);

        using var doc = JsonDocument.Parse(schema.GetRawText());
        var root = doc.RootElement;

        // Has "orderId" in properties
        Assert.True(root.GetProperty("properties").TryGetProperty("orderId", out var orderIdProp));
        Assert.Equal("string", orderIdProp.GetProperty("type").GetString());

        // "orderId" is required
        var required = root.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("orderId", required);
    }

    [Fact]
    public void Schema_GetWithQueryParam_SchemaContainsOptionalQueryParam()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("SearchOrders", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("SearchOrders", HttpMethod.Get, "/orders", "Search orders")
                      .WithParameter("status", "Filter by status")
                      .WithParameter("limit", "Max results", typeof(int));
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!.Tools[0];
        var schema = GetAiToolJsonSchema(tool.ToolDescription);
        using var doc = JsonDocument.Parse(schema.GetRawText());
        var root = doc.RootElement;
        var props = root.GetProperty("properties");

        // "status" → string (default type)
        Assert.True(props.TryGetProperty("status", out var statusProp));
        Assert.Equal("string", statusProp.GetProperty("type").GetString());

        // "limit" → integer
        Assert.True(props.TryGetProperty("limit", out var limitProp));
        Assert.Equal("integer", limitProp.GetProperty("type").GetString());

        // Query params are NOT in the required array
        var required = root.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(e => e.GetString()).ToList()
            : [];
        Assert.DoesNotContain("status", required);
        Assert.DoesNotContain("limit", required);
    }

    [Fact]
    public void Schema_PostWithRequestBody_SchemaContainsBodyProperties()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("CreateOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<CreateOrderRequest, OrderResponse>(
                        "CreateOrder", HttpMethod.Post, "/orders", "Create a new order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!.Tools[0];
        var schema = GetAiToolJsonSchema(tool.ToolDescription);
        using var doc = JsonDocument.Parse(schema.GetRawText());
        var props = doc.RootElement.GetProperty("properties");

        // customerName → string (camelCase)
        Assert.True(props.TryGetProperty("customerName", out var nameProp));
        Assert.Equal("string", nameProp.GetProperty("type").GetString());

        // quantity → integer (non-nullable value type → required)
        Assert.True(props.TryGetProperty("quantity", out var qtyProp));
        Assert.Equal("integer", qtyProp.GetProperty("type").GetString());

        // totalPrice → number (nullable decimal → optional)
        Assert.True(props.TryGetProperty("totalPrice", out var priceProp));
        Assert.Equal("number", priceProp.GetProperty("type").GetString());

        // "quantity" is required, "totalPrice" is not
        var required = doc.RootElement.GetProperty("required").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        Assert.Contains("quantity", required);
        Assert.DoesNotContain("totalPrice", required);
        Assert.DoesNotContain("customerName", required);
    }

    // ── Group 2b: Schema – array / collection "items" ─────────────────────────

    /// <summary>
    /// Regression test for "array schema missing items".
    /// List&lt;Guid&gt;? must produce { "type": "array", "items": { "type": "string" } }.
    /// </summary>
    [Fact]
    public void Schema_Post_NullableListGuid_ArrayHasItemsTypeString()
    {
        var (props, _) = BuildSchemaProps<FindByGuidsRequest>();

        Assert.True(props.TryGetProperty("projectIds", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        var items = prop.GetProperty("items");
        Assert.Equal("string", items.GetProperty("type").GetString());
    }

    /// <summary>
    /// A second nullable List&lt;Guid&gt; in the same DTO must also have "items".
    /// </summary>
    [Fact]
    public void Schema_Post_SecondNullableListGuid_ArrayHasItemsTypeString()
    {
        var (props, _) = BuildSchemaProps<FindByGuidsRequest>();

        Assert.True(props.TryGetProperty("entityIds", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("string", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_ListInt_ArrayHasItemsTypeInteger()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        Assert.True(props.TryGetProperty("intList", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("integer", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_StringArray_ArrayHasItemsTypeString()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        Assert.True(props.TryGetProperty("stringArray", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("string", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_NullableIEnumerableString_ArrayHasItemsTypeString()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        Assert.True(props.TryGetProperty("enumerable", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("string", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_NullableIReadOnlyListDecimal_ArrayHasItemsTypeNumber()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        Assert.True(props.TryGetProperty("readOnlyList", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("number", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_NullableICollectionBool_ArrayHasItemsTypeBoolean()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        Assert.True(props.TryGetProperty("collection", out var prop));
        Assert.Equal("array", prop.GetProperty("type").GetString());
        Assert.Equal("boolean", prop.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void Schema_Post_NullableListGuid_ScalarSiblingIsUnaffected()
    {
        var (props, _) = BuildSchemaProps<CollectionTypesRequest>();

        // A plain scalar property alongside lists must still be a simple "string"
        Assert.True(props.TryGetProperty("scalar", out var prop));
        Assert.Equal("string", prop.GetProperty("type").GetString());
        Assert.False(prop.TryGetProperty("items", out _));
    }

    /// <summary>
    /// A query-string parameter declared with a List type must also produce
    /// { "type": "array", "items": { "type": "string" } }.
    /// </summary>
    [Fact]
    public void Schema_QueryParam_ListType_ArrayHasItems()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("SearchOrders", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("SearchOrders", HttpMethod.Get, "/orders", "Search orders")
                      .WithParameter("ids", "Filter by ids", typeof(List<Guid>));
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!.Tools[0];
        var schema = GetAiToolJsonSchema(tool.ToolDescription);
        using var doc = JsonDocument.Parse(schema.GetRawText());
        var props = doc.RootElement.GetProperty("properties");

        Assert.True(props.TryGetProperty("ids", out var idsProp));
        Assert.Equal("array", idsProp.GetProperty("type").GetString());
        Assert.Equal("string", idsProp.GetProperty("items").GetProperty("type").GetString());
    }

    // ── Helper: build schema properties for a typed request body ─────────────

    private (JsonElement properties, List<string> required) BuildSchemaProps<TRequest>()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("Action", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Test", "Test scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<TRequest, OrderResponse>("Action", HttpMethod.Post, "/action", "Test action");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Test")!.Tools[0];
        var schema = GetAiToolJsonSchema(tool.ToolDescription);
        using var doc = JsonDocument.Parse(schema.GetRawText());

        var props = doc.RootElement.GetProperty("properties").Clone();
        var required = doc.RootElement.TryGetProperty("required", out var req)
            ? req.EnumerateArray().Select(e => e.GetString()!).ToList()
            : new List<string>();
        return (props, required);
    }

    // ── Group 2c: Schema – enum "enum" values ─────────────────────────────────

    [Fact]
    public void Schema_Post_EnumProperty_HasTypeStringAndEnumValues()
    {
        var (props, _) = BuildSchemaProps<SortedSearchRequest>();

        Assert.True(props.TryGetProperty("sortOrder", out var prop));
        Assert.Equal("string", prop.GetProperty("type").GetString());

        var values = prop.GetProperty("enum").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        Assert.Contains("Ascending", values);
        Assert.Contains("Descending", values);
        Assert.Contains("Relevance", values);
    }

    [Fact]
    public void Schema_Post_NullableEnumProperty_HasEnumValues()
    {
        var (props, _) = BuildSchemaProps<SortedSearchRequest>();

        Assert.True(props.TryGetProperty("nullableSortOrder", out var prop));
        Assert.Equal("string", prop.GetProperty("type").GetString());
        var values = prop.GetProperty("enum").EnumerateArray()
            .Select(e => e.GetString()).ToList();
        Assert.Equal(3, values.Count);
    }

    [Fact]
    public void Schema_Post_EnumProperty_IsRequiredWhenNonNullable()
    {
        var (_, required) = BuildSchemaProps<SortedSearchRequest>();
        Assert.Contains("sortOrder", required);
        Assert.DoesNotContain("nullableSortOrder", required);
    }

    // ── Group 2d: Schema – Dictionary → "object" ──────────────────────────────

    [Fact]
    public void Schema_Post_DictionaryStringString_HasTypeObject()
    {
        var (props, _) = BuildSchemaProps<MetadataRequest>();

        Assert.True(props.TryGetProperty("metadata", out var prop));
        Assert.Equal("object", prop.GetProperty("type").GetString());
        Assert.False(prop.TryGetProperty("items", out _));
    }

    [Fact]
    public void Schema_Post_IDictionaryStringInt_HasTypeObject()
    {
        var (props, _) = BuildSchemaProps<MetadataRequest>();

        Assert.True(props.TryGetProperty("counts", out var prop));
        Assert.Equal("object", prop.GetProperty("type").GetString());
        Assert.False(prop.TryGetProperty("items", out _));
    }

    // ── Group 3b: Execution – array query param expands to repeated key=val ───

    [Fact]
    public async Task Execution_ArrayQueryParam_ExpandsToRepeatedQueryStringPairs()
    {
        var responsePayload = """{"orderId":"x","status":"Ok"}""";
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responsePayload, Encoding.UTF8, "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient(
            "SearchOrders",
            new Dictionary<string, object?>
            {
                // The AI sends the array as a JSON array value
                ["ids"] = JsonSerializer.Deserialize<JsonElement>("""["id-1","id-2","id-3"]""")
            }));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c => c.BaseAddress = new Uri("http://api/"),
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>(
                            "SearchOrders", HttpMethod.Get, "/orders", "Search orders")
                        .WithParameter("ids", "Filter by order ids", typeof(List<string>));
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        await foreach (var _ in sceneManager.ExecuteAsync("find orders with ids id-1 id-2 id-3", settings: settings)) { }

        var capturedUrl = handler.CapturedRequest?.RequestUri?.ToString();
        Assert.NotNull(capturedUrl);

        // Must NOT contain raw JSON brackets
        Assert.DoesNotContain("%5B", capturedUrl);   // [
        Assert.DoesNotContain("%5D", capturedUrl);   // ]

        // Each id must appear as a separate repeated param
        Assert.Contains("ids=id-1", capturedUrl);
        Assert.Contains("ids=id-2", capturedUrl);
        Assert.Contains("ids=id-3", capturedUrl);
    }

    // ── Group 3: Metadata ─────────────────────────────────────────────────────

    [Fact]
    public void Metadata_EndpointTool_HasCorrectSourceType()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("GetOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var tool = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!.Tools[0];
        var meta = tool as ISceneToolMetadata;

        Assert.NotNull(meta);
        Assert.Equal(PlayFrameworkToolSourceType.Endpoint, meta.SourceType);
        Assert.Equal(nameof(IOrderServiceClient), meta.SourceName);
        Assert.Equal("/orders/{orderId}", meta.MemberName);
        Assert.False(meta.IsCommand);
    }

    // ── Group 4: HTTP Execution via end-to-end pipeline ───────────────────────

    [Fact]
    public async Task Execution_GetWithRouteParam_SendsCorrectHttpRequest()
    {
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"orderId":"order-123","status":"Shipped"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();

        var chatClient = new EndpointToolCallingChatClient(
            "GetOrder",
            new Dictionary<string, object?> { ["orderId"] = "order-123" });

        services.AddSingleton<IChatClient>(chatClient);

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c => c.BaseAddress = new Uri("http://order-api/"),
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>(
                        "GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get an order by ID");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var r in sceneManager.ExecuteAsync("Get order 123", settings: settings))
            responses.Add(r);

        // HTTP request was sent
        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal(HttpMethod.Get, handler.CapturedRequest!.Method);
        Assert.Contains("order-123", handler.CapturedRequest.RequestUri!.ToString());

        // Scene execution completed
        Assert.Contains(responses, r => r.Status == AiResponseStatus.FunctionCompleted
                                        && r.FunctionName == "GetOrder");
        Assert.Contains(responses, r => r.Status == AiResponseStatus.Completed);
    }

    [Fact]
    public async Task Execution_GetWithQueryParam_AppendsQueryStringToUrl()
    {
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient(
            "SearchOrders",
            new Dictionary<string, object?> { ["status"] = "Shipped" }));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c => c.BaseAddress = new Uri("http://order-api/"),
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("SearchOrders", HttpMethod.Get, "/orders", "Search")
                      .WithParameter("status", "Filter by order status");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        await foreach (var _ in sceneManager.ExecuteAsync("Find shipped orders", settings: settings)) { }

        Assert.NotNull(handler.CapturedRequest);
        var url = handler.CapturedRequest!.RequestUri!.ToString();
        Assert.Contains("status=Shipped", url);
    }

    [Fact]
    public async Task Execution_PostWithBody_SerializesBodyCorrectly()
    {
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"orderId":"new-order","status":"Pending"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient(
            "CreateOrder",
            new Dictionary<string, object?>
            {
                ["customerName"] = "Alice",
                ["quantity"] = 3
            }));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c => c.BaseAddress = new Uri("http://order-api/"),
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<CreateOrderRequest, OrderResponse>(
                        "CreateOrder", HttpMethod.Post, "/orders", "Create order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        await foreach (var _ in sceneManager.ExecuteAsync("Create order for Alice", settings: settings)) { }

        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal(HttpMethod.Post, handler.CapturedRequest!.Method);
        Assert.NotNull(handler.CapturedRequestBody);

        // Request body should contain the body fields
        Assert.Contains("customerName", handler.CapturedRequestBody!,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Alice", handler.CapturedRequestBody!);
    }

    [Fact]
    public async Task Execution_NonSuccessHttpResponse_ToolStillCompletes()
    {
        // EndpointHttpTool does NOT throw on non-2xx; it returns the status code to the AI.
        var handler = new CapturingHttpHandler(
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(
                    """{"error":"order not found"}""",
                    Encoding.UTF8,
                    "application/json")
            });

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient(
            "GetOrder",
            new Dictionary<string, object?> { ["orderId"] = "missing-id" }));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(
                c => c.BaseAddress = new Uri("http://order-api/"),
                b => b.ConfigurePrimaryHttpMessageHandler(() => handler));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>(
                        "GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var sceneManager = sp.GetRequiredService<IFactory<ISceneManager>>().Create(null)!;

        var settings = new SceneRequestSettings
        {
            ExecutionMode = SceneExecutionMode.Scene,
            SceneName = "Orders"
        };

        var responses = new List<AiSceneResponse>();
        await foreach (var r in sceneManager.ExecuteAsync("Get missing order", settings: settings))
            responses.Add(r);

        // Tool execution should complete (not error) — the 404 is passed to the AI as tool result
        Assert.Contains(responses, r => r.Status == AiResponseStatus.FunctionCompleted
                                        && r.FunctionName == "GetOrder");
        Assert.DoesNotContain(responses, r => r.Status == AiResponseStatus.Error);
    }

    // ── Group 5: AiTools list reflects registered endpoints ───────────────────

    [Fact]
    public void AiTools_RegisteredEndpoints_AppearInSceneAiTools()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IChatClient>(new EndpointToolCallingChatClient("GetOrder", []));

        services.AddPlayFramework(builder =>
        {
            builder.WithHttpClient<IOrderServiceClient>(c => c.BaseAddress = new Uri("http://api/"));

            builder.AddScene("Orders", "Orders scene", scene =>
            {
                scene.WithEndpoint<IOrderServiceClient>(ep =>
                {
                    ep.WithAction<OrderResponse>("GetOrder", HttpMethod.Get, "/orders/{orderId}", "Get order");
                    ep.WithAction<CreateOrderRequest, OrderResponse>("CreateOrder", HttpMethod.Post, "/orders", "Create order");
                });
            });
        });

        var sp = services.BuildServiceProvider();
        var scene = sp.GetRequiredService<ISceneFactory>().TryGetScene("Orders")!;

        // AiTools mirrors Tools
        Assert.Equal(scene.Tools.Count, scene.AiTools.Count);
        var toolNames = scene.AiTools.Select(t => t.Name).ToList();
        Assert.Contains("GetOrder", toolNames);
        Assert.Contains("CreateOrder", toolNames);
    }
}
