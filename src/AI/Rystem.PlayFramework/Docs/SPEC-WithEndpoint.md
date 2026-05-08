# Specifica: `WithEndpoint<TClient>()` — Chiamate HTTP esterne come tool AI

> **Versioni**
> - **v1.0** — Configurazione manuale degli endpoint tramite `WithAction` _(questa release)_
> - **v1.1** — Auto-generazione dei tool da una specifica OpenAPI _(release successiva)_

---

## v1.0 — Configurazione manuale con `WithAction`

### Obiettivo

Permettere a una scena del PlayFramework di invocare endpoint HTTP esterni come tool AI, con supporto completo per:

- URL assoluti (`http://localhost/Controller/Action`)
- Route relative (`/Controller/Action`) quando configurato un `IHttpClientFactory` con base address
- DelegatingHandler per autorizzazione, retry, circuit breaker
- Resiliency tramite le estensioni standard .NET (Polly, `AddStandardResilienceHandler()`)
- Serializzazione/deserializzazione custom tramite `IJsonService`

---

### Architettura generale

La feature si divide in **due livelli**:

| Livello | Dove | Responsabilità |
|---------|------|----------------|
| **Infrastruttura HTTP** | `PlayFrameworkBuilder` | Registra l'`HttpClient` tipizzato con base URL, DelegatingHandler, policy di resiliency |
| **Configurazione tool** | `SceneBuilder` | Definisce quali endpoint esporre come tool AI nella scena, con nome, route, HTTP method, parametri |

Questa separazione garantisce che:
- La configurazione dell'infrastruttura HTTP (auth, retry, timeout) vive in un unico punto
- Più scene possono riutilizzare lo stesso client HTTP configurato
- Ogni scena espone solo gli endpoint rilevanti per il proprio contesto

---

### API pubblica

#### 1. Registrazione HttpClient — `PlayFrameworkBuilder`

```csharp
// File: Builder/PlayFrameworkBuilder_HttpClient.cs (partial class)

public sealed partial class PlayFrameworkBuilder
{
    /// <summary>
    /// Registra un HttpClient tipizzato tramite IHttpClientFactory.
    /// TClient è un tipo marker (interface vuota) usato come chiave per il named client.
    /// Il configure espone IHttpClientBuilder standard .NET per configurare
    /// base address, DelegatingHandler, Polly, timeout, ecc.
    /// </summary>
    public PlayFrameworkBuilder WithHttpClient<TClient>(Action<IHttpClientBuilder> configure)
        where TClient : class;
}
```

**Esempio d'uso:**

```csharp
builder.WithHttpClient<IOrderServiceClient>(httpBuilder =>
{
    httpBuilder.ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5001/api");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // DelegatingHandler per autenticazione Bearer
    httpBuilder.AddHttpMessageHandler<BearerTokenHandler>();

    // Resiliency con Polly (Microsoft.Extensions.Http.Resilience)
    httpBuilder.AddStandardResilienceHandler();
});
```

**Dettagli implementativi:**
- Internamente chiama `_services.AddHttpClient(typeof(TClient).Name)` e passa l'`IHttpClientBuilder` al delegate
- `TClient` è un **tipo marker** (tipicamente un'interface vuota): serve solo come chiave per risolvere il client corretto a runtime via `IHttpClientFactory.CreateClient(typeof(TClient).Name)`
- L'utente ha accesso a **tutte** le estensioni standard di `IHttpClientBuilder`: `AddHttpMessageHandler`, `ConfigurePrimaryHttpMessageHandler`, `SetHandlerLifetime`, `AddStandardResilienceHandler`, ecc.

---

#### 2. Configurazione endpoint nella scena — `SceneBuilder`

```csharp
// File: Builder/SceneBuilder.cs (metodo aggiuntivo)

public sealed class SceneBuilder
{
    /// <summary>
    /// Aggiunge tool che invocano endpoint HTTP esterni.
    /// TClient deve essere un tipo precedentemente registrato con
    /// PlayFrameworkBuilder.WithHttpClient<TClient>().
    /// </summary>
    public SceneBuilder WithEndpoint<TClient>(Action<EndpointToolBuilder<TClient>> configure)
        where TClient : class;
}
```

**Esempio d'uso:**

```csharp
scene.WithEndpoint<IOrderServiceClient>(endpoint =>
{
    // GET senza body — parametri estratti dalla route template
    endpoint.WithAction<OrderDto>(
        "GetOrder",
        HttpMethod.Get,
        "/orders/{orderId}",
        "Recupera un ordine per ID");

    // GET con query parameters aggiuntivi
    endpoint.WithAction<List<OrderDto>>(
        "SearchOrders",
        HttpMethod.Get,
        "/orders",
        "Cerca ordini con filtri")
        .WithParameter("status", "Stato dell'ordine (pending, completed, cancelled)", typeof(string))
        .WithParameter("fromDate", "Data inizio ricerca (ISO 8601)", typeof(DateTime));

    // POST con body tipizzato
    endpoint.WithAction<CreateOrderRequest, OrderDto>(
        "CreateOrder",
        HttpMethod.Post,
        "/orders",
        "Crea un nuovo ordine");

    // PUT con body tipizzato
    endpoint.WithAction<UpdateOrderRequest, OrderDto>(
        "UpdateOrder",
        HttpMethod.Put,
        "/orders/{orderId}",
        "Aggiorna un ordine esistente");

    // DELETE senza body
    endpoint.WithAction<bool>(
        "DeleteOrder",
        HttpMethod.Delete,
        "/orders/{orderId}",
        "Elimina un ordine");
});
```

---

#### 3. `EndpointToolBuilder<TClient>` — Builder per i singoli tool

```csharp
// File: Builder/EndpointToolBuilder.cs

public sealed class EndpointToolBuilder<TClient> where TClient : class
{
    /// <summary>
    /// Aggiunge un tool HTTP senza request body (GET, DELETE, HEAD, OPTIONS).
    /// I parametri della route template ({param}) vengono estratti automaticamente.
    /// TResponse è il tipo atteso nella risposta JSON.
    /// </summary>
    public EndpointActionBuilder WithAction<TResponse>(
        string toolName,
        HttpMethod method,
        string routeTemplate,
        string description);

    /// <summary>
    /// Aggiunge un tool HTTP con request body (POST, PUT, PATCH).
    /// TRequest definisce lo schema del body JSON inviato.
    /// TResponse è il tipo atteso nella risposta JSON.
    /// I parametri della route template ({param}) vengono estratti automaticamente.
    /// </summary>
    public EndpointActionBuilder WithAction<TRequest, TResponse>(
        string toolName,
        HttpMethod method,
        string routeTemplate,
        string description);
}

/// <summary>
/// Builder fluent per aggiungere parametri aggiuntivi (query params) a un'azione endpoint.
/// </summary>
public sealed class EndpointActionBuilder
{
    /// <summary>
    /// Aggiunge un parametro query string al tool.
    /// Questo parametro viene esposto nello schema AI e passato come ?key=value nella request.
    /// </summary>
    /// <param name="name">Nome del parametro (usato sia nello schema AI sia nella query string).</param>
    /// <param name="description">Descrizione per l'AI.</param>
    /// <param name="type">Tipo C# del parametro (default: string). Usato per la generazione dello schema JSON.</param>
    public EndpointActionBuilder WithParameter(string name, string description, Type? type = null);
}
```

---

### Modello di configurazione interno

#### `EndpointToolConfiguration`

```csharp
// File: Builder/EndpointToolBuilder.cs (o file separato)

internal sealed class EndpointToolConfiguration
{
    /// <summary>
    /// Tipo marker del client HTTP (chiave per IHttpClientFactory).
    /// </summary>
    public required Type ClientType { get; init; }

    /// <summary>
    /// Nome del tool esposto all'AI.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Descrizione del tool per l'AI.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Metodo HTTP (GET, POST, PUT, DELETE, PATCH).
    /// </summary>
    public required HttpMethod HttpMethod { get; init; }

    /// <summary>
    /// Route template con placeholder: es. "/orders/{orderId}".
    /// I {param} vengono estratti automaticamente come parametri del tool.
    /// </summary>
    public required string RouteTemplate { get; init; }

    /// <summary>
    /// Tipo del request body (null se nessun body, es. per GET/DELETE).
    /// Lo schema JSON dell'AI viene generato da questo tipo.
    /// </summary>
    public Type? RequestBodyType { get; init; }

    /// <summary>
    /// Tipo della response attesa (per deserializzazione).
    /// </summary>
    public required Type ResponseType { get; init; }

    /// <summary>
    /// Parametri aggiuntivi (query string) dichiarati via WithParameter().
    /// </summary>
    public List<EndpointParameterDefinition> QueryParameters { get; init; } = [];
}

internal sealed class EndpointParameterDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public Type Type { get; init; } = typeof(string);
}
```

#### Aggiunta a `SceneConfiguration`

```csharp
internal sealed class SceneConfiguration
{
    // ... campi esistenti ...

    /// <summary>
    /// Tool basati su endpoint HTTP esterni registrati via WithEndpoint<TClient>().
    /// </summary>
    public List<EndpointToolConfiguration> EndpointTools { get; set; } = [];
}
```

---

### Implementazione runtime — `EndpointHttpTool`

#### Classe

```csharp
// File: Services/Tools/EndpointHttpTool.cs

internal sealed class EndpointHttpTool : ISceneTool, ISceneToolMetadata
{
    // Implementa ISceneTool
    public string Name { get; }
    public string Description { get; }
    public AITool ToolDescription { get; }

    // Implementa ISceneToolMetadata
    public PlayFrameworkToolSourceType SourceType => PlayFrameworkToolSourceType.Endpoint;
    public string? SourceName => _config.ClientType.Name;
    public string? MemberName => _config.RouteTemplate;
    public bool IsCommand => false;
    public string? JsonSchema => null;
}
```

#### Costruzione dello schema AI

Lo schema JSON esposto al modello AI viene costruito nel costruttore di `EndpointHttpTool`:

1. **Parametri route**: estratti dalla route template via regex `\{(\w+)\}` → diventano proprietà `string` nello schema
2. **Query parameters**: dichiarati via `.WithParameter()` → proprietà nello schema con tipo specificato
3. **Body (TRequest)**: se presente, le proprietà di `TRequest` vengono aggiunte allo schema tramite `AIJsonUtilities.CreateParametersJsonSchema(typeof(TRequest))`

Lo schema risultante viene passato a `AIFunctionFactory.CreateDeclaration(name, description, schema)` per generare l'`AITool`.

**Esempio di schema generato per `GetOrder`:**

```json
{
  "type": "object",
  "properties": {
    "orderId": { "type": "string", "description": "Route parameter: orderId" }
  },
  "required": ["orderId"]
}
```

**Esempio di schema generato per `CreateOrder` (con body `CreateOrderRequest`):**

```json
{
  "type": "object",
  "properties": {
    "customerName": { "type": "string" },
    "items": {
      "type": "array",
      "items": { "$ref": "#/$defs/OrderItem" }
    },
    "shippingAddress": { "type": "string" }
  },
  "required": ["customerName", "items"]
}
```

#### Esecuzione (`ExecuteAsync`)

```csharp
public async Task<object?> ExecuteAsync(
    string arguments,
    SceneContext context,
    CancellationToken cancellationToken)
{
    // 1. Risolvere IHttpClientFactory dal service provider
    var factory = context.ServiceProvider.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient(_config.ClientType.Name);

    // 2. Deserializzare gli argomenti JSON dall'AI
    var argsDict = _jsonService.Deserialize<Dictionary<string, JsonElement>>(arguments);

    // 3. Costruire l'URL:
    //    a) Sostituire i {param} nella route template con i valori ricevuti
    //    b) Aggiungere i query parameters come ?key=value
    var url = BuildUrl(argsDict);

    // 4. Creare la HttpRequestMessage
    var request = new HttpRequestMessage(_config.HttpMethod, url);

    // 5. Se c'è un body (TRequest != null), serializzare e impostare come content
    if (_config.RequestBodyType != null)
    {
        var body = ExtractBodyFromArguments(argsDict);
        request.Content = new StringContent(
            _jsonService.Serialize(body, _config.RequestBodyType),
            Encoding.UTF8,
            "application/json");
    }

    // 6. Inviare la request
    var response = await client.SendAsync(request, cancellationToken);

    // 7. Leggere il body della response
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

    // 8. Tentare la deserializzazione di TResponse
    object? deserializedBody;
    try
    {
        deserializedBody = _jsonService.Deserialize(responseBody, _config.ResponseType);
    }
    catch
    {
        // Fallback: restituire il body come stringa
        deserializedBody = responseBody;
    }

    // 9. Restituire risultato con status code + body
    return new EndpointHttpResponse
    {
        StatusCode = (int)response.StatusCode,
        Body = deserializedBody
    };
}
```

#### Costruzione URL

```csharp
private string BuildUrl(Dictionary<string, JsonElement>? argsDict)
{
    var url = _config.RouteTemplate;

    if (argsDict == null)
        return url;

    // Sostituzione parametri route: /orders/{orderId} → /orders/abc-123
    foreach (var routeParam in _routeParameters)
    {
        if (argsDict.TryGetValue(routeParam, out var value))
        {
            url = url.Replace($"{{{routeParam}}}", value.GetString() ?? value.GetRawText());
        }
    }

    // Aggiunta query parameters
    var queryParams = _config.QueryParameters
        .Where(qp => argsDict.ContainsKey(qp.Name))
        .Select(qp => $"{qp.Name}={Uri.EscapeDataString(argsDict[qp.Name].GetString() ?? argsDict[qp.Name].GetRawText())}");

    var queryString = string.Join("&", queryParams);
    if (!string.IsNullOrEmpty(queryString))
        url += $"?{queryString}";

    return url;
}
```

#### Tipo di risposta restituito all'AI

```csharp
/// <summary>
/// Risposta di un endpoint HTTP restituita all'AI.
/// Include lo status code per permettere all'AI di reagire a errori (400, 422, 500).
/// </summary>
internal sealed class EndpointHttpResponse
{
    public int StatusCode { get; init; }
    public object? Body { get; init; }
}
```

---

### Integrazione in `Scene.cs`

```csharp
internal sealed class Scene : IScene
{
    public Scene(SceneConfiguration configuration, IJsonService? jsonService = null)
    {
        // ... codice esistente per serviceTools e clientTools ...

        // Creazione tool da endpoint HTTP
        var endpointTools = _config.EndpointTools
            .Select(et => new EndpointHttpTool(et, jsonService))
            .ToList();

        // Combinazione di tutti i tool
        Tools = [.. serviceTools];
        Tools.AddRange(clientTools);
        Tools.AddRange(endpointTools);  // ← AGGIUNTA
        AiTools = [.. Tools.Select(x => x.ToolDescription)];

        // ... resto del costruttore ...
    }
}
```

---

### Enum `PlayFrameworkToolSourceType`

```csharp
public enum PlayFrameworkToolSourceType
{
    Service,
    Client,
    Mcp,
    Rag,
    WebSearch,
    Endpoint  // ← NUOVO VALORE
}
```

---

### File coinvolti v1.0 — Riepilogo

| File | Azione | Contenuto |
|------|--------|-----------|
| `Builder/PlayFrameworkBuilder_HttpClient.cs` | **Nuovo** | Partial class con `WithHttpClient<TClient>()` |
| `Builder/EndpointToolBuilder.cs` | **Nuovo** | `EndpointToolBuilder<TClient>`, `EndpointActionBuilder`, `EndpointToolConfiguration`, `EndpointParameterDefinition` |
| `Services/Tools/EndpointHttpTool.cs` | **Nuovo** | Implementazione `ISceneTool` + `ISceneToolMetadata` per chiamate HTTP |
| `Builder/SceneBuilder.cs` | **Modifica** | Aggiunta metodo `WithEndpoint<TClient>()` + campo `EndpointTools` in `SceneConfiguration` |
| `Services/Scenes/Scene.cs` | **Modifica** | Istanziazione `EndpointHttpTool` dalla configurazione |
| Enum `PlayFrameworkToolSourceType` | **Modifica** | Aggiunta valore `Endpoint` |

---

### Esempio completo end-to-end v1.0

```csharp
// === Program.cs / Startup ===

// 1. Tipo marker per il client (interface vuota)
public interface IOrderServiceClient { }

// 2. DelegatingHandler per autenticazione
public class BearerTokenHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "...");
        return await base.SendAsync(request, ct);
    }
}

// 3. Configurazione PlayFramework
services.AddPlayFramework(builder =>
{
    // Registrazione client HTTP con auth + resiliency
    builder.WithHttpClient<IOrderServiceClient>(http =>
    {
        http.ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri("http://order-service:5001/api");
            c.DefaultRequestHeaders.Add("X-Api-Version", "2");
        });
        http.AddHttpMessageHandler<BearerTokenHandler>();
        http.AddStandardResilienceHandler();
    });

    // Configurazione scena
    builder.AddScene("OrderManagement", "Gestione ordini", scene =>
    {
        scene.WithEndpoint<IOrderServiceClient>(endpoint =>
        {
            endpoint.WithAction<OrderDto>(
                "GetOrder", HttpMethod.Get, "/orders/{orderId}",
                "Recupera i dettagli di un ordine dato il suo ID");

            endpoint.WithAction<List<OrderSummaryDto>>(
                "ListOrders", HttpMethod.Get, "/orders",
                "Elenca gli ordini con filtri opzionali")
                .WithParameter("status", "Filtra per stato: pending, shipped, delivered", typeof(string))
                .WithParameter("customerId", "Filtra per ID cliente", typeof(Guid));

            endpoint.WithAction<CreateOrderRequest, OrderDto>(
                "CreateOrder", HttpMethod.Post, "/orders",
                "Crea un nuovo ordine con gli articoli specificati");

            endpoint.WithAction<UpdateOrderRequest, OrderDto>(
                "UpdateOrder", HttpMethod.Put, "/orders/{orderId}",
                "Aggiorna un ordine esistente");

            endpoint.WithAction<bool>(
                "CancelOrder", HttpMethod.Delete, "/orders/{orderId}",
                "Annulla un ordine (se non ancora spedito)");
        });
    });
});
```

---

### Decisioni di design v1.0

| Decisione | Scelta | Motivazione |
|-----------|--------|-------------|
| Nome metodo SceneBuilder | `WithEndpoint<TClient>()` | Coerente con `WithService<TService>()`, generico per typed client |
| Ruolo di TClient | Solo marker per IHttpClientFactory | Nessuna reflection su metodi, massima semplicità |
| Registrazione HttpClient | Su `PlayFrameworkBuilder` separatamente | Separazione infrastruttura vs configurazione tool |
| Esposizione IHttpClientBuilder | Direttamente al configure | Accesso completo all'ecosistema .NET (Polly, handler, ecc.) |
| Base URL | Dentro `ConfigureHttpClient()` su IHttpClientBuilder | Standard .NET, nessuna API custom necessaria |
| Overload WithAction | `WithAction<TResponse>` (no body) + `WithAction<TRequest, TResponse>` (con body) | API pulita, TRequest nullable internamente |
| Parametri route | Parsing automatico da template `{param}` | Nessuna configurazione manuale per parametri ovvi |
| Query parameters | `.WithParameter()` esplicito | Necessario perché non deducibili dalla route |
| Schema AI | `AIFunctionFactory.CreateDeclaration()` con schema costruito | Nessun MethodInfo disponibile per endpoint HTTP |
| Risoluzione HttpClient a runtime | Da `context.ServiceProvider` in ExecuteAsync | Stesso pattern di ServiceMethodTool per servizi |
| Gestione risposta HTTP | Body + StatusCode senza EnsureSuccessStatusCode | Anche errori 4xx/5xx contengono info utili per l'AI |
| Deserializzazione | IJsonService con fallback a string raw | Supporta tipi custom (OneOf, ecc.) |
| SourceType metadata | Nuovo valore `Endpoint` nella enum | Distinguibile da Service/Mcp/etc. nella discovery |
| File organizzazione | Partial class + file separati per builder e tool | Coerente con pattern esistente nel codebase |

---

---

## v1.1 — Auto-generazione tool da specifica OpenAPI

### Obiettivo

Estendere `WithEndpoint<TClient>()` per accettare una sorgente OpenAPI (URL remoto, file locale o stream) e generare automaticamente i tool AI da tutte le operazioni della spec, eliminando la necessità di chiamare `WithAction` manualmente per ogni endpoint. I tool generati dalla spec rimangono sovrascrivibili con `WithAction` espliciti.

---

### Principi chiave

- **La spec è un shortcut, non un vincolo**: `WithAction` ha sempre precedenza sulla spec (match su `toolName == operationId`)
- **Caricamento lazy**: la spec viene caricata e cachata al primo utilizzo della scena, non a startup. Questo rende compatibile l'uso con URL self-hosted (l'app serve la propria spec su `/openapi/v1.json`)
- **servers[] della spec vengono ignorati**: la base URL è sempre gestita da `IHttpClientFactory` (configurata con `WithHttpClient<TClient>`). La spec fornisce solo i path relativi
- **Nessuna dipendenza circolare**: il parser OpenAPI è una classe interna stateless; non è un servizio DI

---

### Nuova dipendenza NuGet

```xml
<!-- File: Rystem.PlayFramework.csproj -->
<PackageReference Include="Microsoft.OpenApi.Readers" Version="2.x" />
```

`Microsoft.OpenApi.Readers` è il parser OpenAPI ufficiale Microsoft, già usato internamente da ASP.NET Core OpenAPI in .NET 10. Gestisce OpenAPI 3.x, risoluzione di `$ref`, `allOf`, `oneOf`, `anyOf`.

---

### Modifiche all'API pubblica

#### `SceneBuilder` — nuovo overload di `WithEndpoint`

```csharp
public sealed class SceneBuilder
{
    // Overload v1.0 — INVARIATO
    public SceneBuilder WithEndpoint<TClient>(Action<EndpointToolBuilder<TClient>> configure)
        where TClient : class;

    // Overload v1.1 — NUOVO
    /// <summary>
    /// Aggiunge tool generati automaticamente da una specifica OpenAPI.
    /// La spec viene caricata lazily al primo utilizzo della scena.
    /// I tool generati dalla spec possono essere sovrascritti con WithAction nel configure.
    /// </summary>
    /// <param name="specSource">
    ///   Sorgente della spec: stringa URL (http/https), file path locale, Uri o Stream.
    /// </param>
    /// <param name="configure">
    ///   Opzionale: filtri sulla spec e/o override di singole operazioni con WithAction.
    /// </param>
    public SceneBuilder WithEndpoint<TClient>(
        AnyOf<string, Uri, Stream> specSource,
        Action<EndpointToolBuilder<TClient>>? configure = null)
        where TClient : class;
}
```

#### `EndpointToolBuilder<TClient>` — nuovo metodo `FilterSpec`

```csharp
public sealed class EndpointToolBuilder<TClient> where TClient : class
{
    // Metodi v1.0 — INVARIATI
    public EndpointActionBuilder WithAction<TResponse>(...);
    public EndpointActionBuilder WithAction<TRequest, TResponse>(...);

    // Metodo v1.1 — NUOVO
    /// <summary>
    /// Configura i filtri sulle operazioni della spec da includere come tool.
    /// Di default, tutte le operazioni vengono incluse.
    /// </summary>
    public EndpointToolBuilder<TClient> FilterSpec(Action<OpenApiFilterSettings> configure);
}
```

---

### Nuovo tipo: `OpenApiFilterSettings`

```csharp
// File: Builder/OpenApiFilterSettings.cs

/// <summary>
/// Filtri applicati alle operazioni di una specifica OpenAPI
/// per determinare quali diventano tool AI.
/// Tutti i filtri sono OR tra elementi dello stesso tipo, AND tra tipi diversi.
/// Di default (tutti vuoti) = includi tutto.
/// </summary>
public sealed class OpenApiFilterSettings
{
    /// <summary>
    /// Include solo operazioni che hanno almeno uno di questi tag.
    /// Esempio: ["Orders", "Customers"]
    /// </summary>
    public List<string> Tags { get; } = [];

    /// <summary>
    /// Include solo operazioni con uno di questi operationId.
    /// Esempio: ["GetOrderById", "CreateOrder"]
    /// </summary>
    public List<string> OperationIds { get; } = [];

    /// <summary>
    /// Include solo operazioni il cui path inizia con uno di questi prefissi.
    /// Esempio: ["/orders", "/customers"]
    /// </summary>
    public List<string> PathPrefixes { get; } = [];

    /// <summary>
    /// Include solo operazioni con uno di questi metodi HTTP.
    /// Esempio: [HttpMethod.Get, HttpMethod.Post]
    /// </summary>
    public List<HttpMethod> Methods { get; } = [];
}
```

---

### Modifiche al modello di configurazione interno

#### `EndpointToolBuilder<TClient>` — stato interno aggiuntivo

```csharp
internal sealed class EndpointToolBuilder<TClient> where TClient : class
{
    // Stato v1.0 — invariato
    internal List<EndpointToolConfiguration> ManualTools { get; } = [];

    // Stato v1.1 — aggiunto
    internal AnyOf<string, Uri, Stream>? SpecSource { get; private set; }
    internal OpenApiFilterSettings? SpecFilter { get; private set; }

    // I toolName dichiarati via WithAction: usati per sapere quali operazioni
    // della spec NON devono essere auto-generate (WithAction ha precedenza)
    internal HashSet<string> ManualToolNames { get; } = new(StringComparer.OrdinalIgnoreCase);
}
```

#### `SceneConfiguration` — campo aggiuntivo

```csharp
internal sealed class SceneConfiguration
{
    // ... campi v1.0 invariati ...

    /// <summary>
    /// Configurazioni di endpoint con sorgente OpenAPI da caricare lazily.
    /// Ogni elemento rappresenta un WithEndpoint<TClient>(specSource, ...).
    /// </summary>
    public List<OpenApiEndpointGroupConfiguration> OpenApiEndpointGroups { get; set; } = [];
}

/// <summary>
/// Configurazione di un gruppo di endpoint generati da una specifica OpenAPI.
/// </summary>
internal sealed class OpenApiEndpointGroupConfiguration
{
    public required Type ClientType { get; init; }
    public required AnyOf<string, Uri, Stream> SpecSource { get; init; }
    public OpenApiFilterSettings? Filter { get; init; }

    /// <summary>
    /// Tool dichiarati manualmente con WithAction nello stesso WithEndpoint.
    /// Questi hanno precedenza sulle operazioni della spec con lo stesso toolName.
    /// </summary>
    public List<EndpointToolConfiguration> ManualOverrides { get; init; } = [];
}
```

---

### Nuovo componente: `OpenApiSpecLoader`

```csharp
// File: Services/Tools/OpenApiSpecLoader.cs

/// <summary>
/// Carica e cachea una specifica OpenAPI da URL, file path o stream.
/// Thread-safe. Il parsing avviene una sola volta per sorgente.
/// </summary>
internal static class OpenApiSpecLoader
{
    // Cache keyed sul toString() della sorgente (URL o file path assoluto)
    private static readonly ConcurrentDictionary<string, Task<OpenApiDocument>> _cache = new();

    /// <summary>
    /// Carica la specifica OpenAPI dalla sorgente indicata.
    /// - Stringa che inizia con "http://" o "https://": fetch HTTP
    /// - Stringa altrimenti: file path locale
    /// - Uri: fetch HTTP
    /// - Stream: lettura diretta (non cachata, stream consumato una sola volta)
    /// </summary>
    public static Task<OpenApiDocument> LoadAsync(
        AnyOf<string, Uri, Stream> source,
        CancellationToken cancellationToken = default);
}
```

**Logica di caricamento:**

1. `Stream` → lettura diretta con `OpenApiStreamReader`, nessuna cache (lo stream non è riproducibile)
2. `Uri` o stringa `http/https` → download via `HttpClient` (client senza base address, solo per il fetch della spec), poi parse con `OpenApiStreamReader`; risultato cachato per URL
3. Stringa altrimenti → `File.OpenRead(path)`, parse con `OpenApiStreamReader`; risultato cachato per path assoluto

---

### Modifiche a `Scene.cs` — inizializzazione lazy async

In v1.0 i tool vengono costruiti nel costruttore di `Scene` (sincrono). In v1.1, la presenza di `OpenApiEndpointGroups` introduce tool che richiedono I/O asincrono.

**Approccio:** `Tools` e `AiTools` diventano lazy, popolati da un metodo `EnsureInitializedAsync()` protetto da un `SemaphoreSlim(1,1)`. Tutte le chiamate a `scene.Tools` e `scene.AiTools` che avvengono dopo la costruzione (es. in `SceneExecutor`) passano per questo gate.

```csharp
internal sealed class Scene : IScene
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized = false;

    // In v1.0 questi venivano popolati nel costruttore;
    // in v1.1 vengono popolati da EnsureInitializedAsync().
    public List<ISceneTool> Tools { get; private set; } = [];
    public List<AITool> AiTools { get; private set; } = [];

    /// <summary>
    /// Garantisce che i tool derivati da spec OpenAPI siano stati caricati.
    /// Chiamato da IScene prima di ogni utilizzo di Tools/AiTools.
    /// Idempotente e thread-safe.
    /// </summary>
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            // Carica i tool da ogni gruppo OpenAPI
            foreach (var group in _config.OpenApiEndpointGroups)
            {
                var specTools = await BuildToolsFromSpecAsync(group, cancellationToken);
                Tools.AddRange(specTools);
            }

            AiTools = [.. Tools.Select(x => x.ToolDescription)];
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<List<EndpointHttpTool>> BuildToolsFromSpecAsync(
        OpenApiEndpointGroupConfiguration group,
        CancellationToken cancellationToken)
    {
        var document = await OpenApiSpecLoader.LoadAsync(group.SpecSource, cancellationToken);
        var tools = new List<EndpointHttpTool>();

        var manualNames = group.ManualOverrides
            .Select(t => t.ToolName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, pathItem) in document.Paths)
        {
            foreach (var (operationType, operation) in pathItem.Operations)
            {
                // Applicare filtri
                if (!PassesFilter(operation, path, operationType, group.Filter))
                    continue;

                // Determinare il nome del tool
                var toolName = ToolNameNormalizer.Normalize(
                    !string.IsNullOrEmpty(operation.OperationId)
                        ? operation.OperationId
                        : $"{operationType}_{path.Replace("/", "_").Trim('_')}");

                // WithAction manuale ha precedenza
                if (manualNames.Contains(toolName))
                    continue;

                // Costruire EndpointToolConfiguration da spec
                var config = BuildConfigFromOperation(
                    group.ClientType, toolName, path, operationType, operation);

                tools.Add(new EndpointHttpTool(config, _jsonService));
            }
        }

        return tools;
    }
}
```

**Impatto su `IScene`:** aggiunta del metodo `Task EnsureInitializedAsync(CancellationToken)` all'interfaccia. Il chiamante (es. `SceneExecutor`) chiama `await scene.EnsureInitializedAsync()` prima di accedere a `scene.Tools`.

---

### Costruzione dello schema AI da operazione OpenAPI

Per ogni operazione la spec fornisce:
- **`parameters`**: array di `ParameterObject` con `name`, `in` (path/query/header/cookie), `description`, `required`, `schema`
- **`requestBody`**: `RequestBodyObject` con `content["application/json"].schema`

Lo schema AI unificato viene costruito così:

```
properties = {}
required   = []

per ogni parameter con in = "path" o "query":
    properties[param.name] = {
        "type":        OpenApiTypeToJsonSchemaType(param.schema),
        "description": param.description ?? "Parameter: {param.name}"
    }
    se param.required: required.append(param.name)

se requestBody presente e content["application/json"] esiste:
    FlattenSchemaIntoProperties(requestBody.schema, properties, required)

schema_finale = {
    "type":       "object",
    "properties": properties,
    "required":   required   // omesso se vuoto
}
```

`FlattenSchemaIntoProperties` risolve `$ref` (già risolti da `Microsoft.OpenApi`), gestisce `allOf` (merge delle proprietà), e copia le proprietà del body direttamente al top-level dello schema (lo stesso schema che `EndpointHttpTool` usa per separare route/query/body a runtime è gestito da `EndpointToolConfiguration`).

---

### Separazione route/query/body a runtime con spec

Quando i tool vengono generati dalla spec, `EndpointToolConfiguration` include informazioni aggiuntive per sapere, per ogni parametro dell'AI, dove va nella request HTTP:

```csharp
internal sealed class EndpointToolConfiguration
{
    // ... campi v1.0 invariati ...

    /// <summary>
    /// v1.1: nomi dei parametri che provengono dal path (per sostituire nella route template).
    /// Quando null, viene usato il parsing automatico da {} nella route template (comportamento v1.0).
    /// </summary>
    public HashSet<string>? RouteParameterNames { get; init; }

    /// <summary>
    /// v1.1: nomi dei parametri che vanno nella query string.
    /// Quando null, vengono usati i QueryParameters dichiarati con WithParameter() (comportamento v1.0).
    /// </summary>
    public HashSet<string>? QueryParameterNames { get; init; }

    /// <summary>
    /// v1.1: nomi delle proprietà che fanno parte del request body.
    /// Quando non null, ExtractBodyFromArguments usa questi nomi per costruire il body JSON
    /// invece di serializzare RequestBodyType via reflection.
    /// </summary>
    public HashSet<string>? BodyPropertyNames { get; init; }
}
```

---

### File coinvolti v1.1 — Riepilogo incrementale

I file v1.0 rimangono invariati salvo le modifiche indicate.

| File | Azione | Contenuto |
|------|--------|-----------|
| `Builder/OpenApiFilterSettings.cs` | **Nuovo** | `OpenApiFilterSettings` con Tags, OperationIds, PathPrefixes, Methods |
| `Services/Tools/OpenApiSpecLoader.cs` | **Nuovo** | Loader statico con cache `ConcurrentDictionary`, supporto URL/file/stream |
| `Rystem.PlayFramework.csproj` | **Modifica** | Aggiunta `<PackageReference Include="Microsoft.OpenApi.Readers" Version="2.x" />` |
| `Builder/EndpointToolBuilder.cs` | **Modifica** | Aggiunta `FilterSpec()`, stato `SpecSource`/`SpecFilter`/`ManualToolNames` |
| `Builder/SceneBuilder.cs` | **Modifica** | Nuovo overload `WithEndpoint<TClient>(specSource, configure?)` + `OpenApiEndpointGroups` in `SceneConfiguration` |
| `Services/Scenes/Scene.cs` | **Modifica** | Lazy init con `EnsureInitializedAsync()`, `BuildToolsFromSpecAsync()`, `BuildConfigFromOperation()` |
| `Abstractions/IScene.cs` | **Modifica** | Aggiunta `Task EnsureInitializedAsync(CancellationToken)` |

---

### Esempi d'uso v1.1

```csharp
// Caso 1: spec completa come unica riga — tutti gli endpoint diventano tool
scene.WithEndpoint<IOrderServiceClient>("http://localhost:5001/openapi/v1.json");

// Caso 2: spec con filtro per tag
scene.WithEndpoint<IOrderServiceClient>(
    "http://localhost:5001/openapi/v1.json",
    e => e.FilterSpec(f => f.Tags.Add("Orders")));

// Caso 3: spec con filtro multiplo (solo GET e POST su /orders/*)
scene.WithEndpoint<IOrderServiceClient>(
    "./specs/orders-v2.json",
    e => e.FilterSpec(f =>
    {
        f.PathPrefixes.Add("/orders");
        f.Methods.Add(HttpMethod.Get);
        f.Methods.Add(HttpMethod.Post);
    }));

// Caso 4: spec + override di una singola operazione
// "CreateOrder" (operationId dalla spec) viene sostituito con la versione manuale
scene.WithEndpoint<IOrderServiceClient>(
    "http://localhost:5001/openapi/v1.json",
    e => e
        .FilterSpec(f => f.Tags.Add("Orders"))
        .WithAction<CreateOrderRequest, OrderDto>(
            "CreateOrder",           // toolName == operationId → sovrascrive la spec
            HttpMethod.Post,
            "/orders",
            "Descrizione arricchita per l'AI, più dettagliata di quella nella spec"));

// Caso 5: spec da stream (es. embedded resource)
var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("MyApp.specs.orders.json")!;
scene.WithEndpoint<IOrderServiceClient>(stream);

// Caso 6: mix v1.0 e v1.1 nella stessa scena (due client diversi)
scene.WithEndpoint<IOrderServiceClient>(
    "http://localhost:5001/openapi/v1.json");  // v1.1: auto da spec

scene.WithEndpoint<IInventoryServiceClient>(endpoint =>  // v1.0: manuale
{
    endpoint.WithAction<StockDto>("GetStock", HttpMethod.Get, "/stock/{sku}", "...");
});
```

---

### Decisioni di design v1.1

| Decisione | Scelta | Motivazione |
|-----------|--------|-------------|
| Obiettivo | Spec come shortcut + override manuale possibile | Massima flessibilità senza duplicazione |
| Dove si configura la spec | Overload su `WithEndpoint<TClient>` | Ogni scena può puntare a API diverse |
| Formato sorgente | `AnyOf<string, Uri, Stream>` | Copre tutti i casi: URL remoto, file locale, stream embedded |
| Quando viene caricata | Lazy al primo utilizzo della scena | Compatibile con URL self-hosted (app serve la propria spec) |
| Parser OpenAPI | `Microsoft.OpenApi.Readers` | Parser ufficiale Microsoft, gestisce `$ref`, `allOf`, `oneOf` |
| Filtro operazioni | `FilterSpec(Action<OpenApiFilterSettings>)` opzionale | Di default tutto incluso; filtri per Tags/OperationIds/PathPrefixes/Methods |
| Naming tool | `operationId` se presente, altrimenti `{Method}_{path}` normalizzato | Standard OpenAPI, leggibile per l'AI |
| Schema AI | Path params + query params + requestBody unificati | Schema completo per l'AI; separazione route/query/body gestita da `EndpointToolConfiguration` |
| Precedenza WithAction vs spec | `WithAction` ha precedenza (match su `toolName == operationId`) | L'utente sovrascrive solo ciò che vuole |
| servers[] della spec | Ignorati | Base URL sempre da `IHttpClientFactory` |
| Lazy init in Scene.cs | `EnsureInitializedAsync()` con `SemaphoreSlim` | Thread-safe, compatibile con uso concorrente della stessa scena |
| Cache spec | `ConcurrentDictionary` in `OpenApiSpecLoader` | Una sola fetch/parse per sorgente per lifetime dell'applicazione |
