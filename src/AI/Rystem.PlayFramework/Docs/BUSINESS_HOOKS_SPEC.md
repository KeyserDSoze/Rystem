# PlayFramework Business Hooks — Specification

**Versione**: 1.1  
**Stato**: Aggiornato con vincolo architetturale Factory pattern  
**Riferimento**: [PLAYFRAMEWORK_INTEGRATION_REQUEST.md](./PLAYFRAMEWORK_INTEGRATION_REQUEST.md)

---

## Obiettivo

Aggiungere a `Rystem.PlayFramework` un sistema di hook/middleware per logica di business
pre/post esecuzione, analogo a `IRepositoryBusinessBefore*` / `IRepositoryBusinessAfter*` di
`Rystem.RepositoryFramework`. L'obiettivo finale è permettere la rimozione del controller custom
(`AiController`) nei progetti consumer (es. TimeVision) delegando tutta la logica a
`MapPlayFramework`.

---

## Vincolo architetturale — Factory pattern di `Rystem.DependencyInjection`

**Tutto in PlayFramework è registrato e risolto tramite il factory pattern di
`Rystem.DependencyInjection`.** Questo vincolo si applica anche agli hook di business.

### Come funziona il factory pattern

La registrazione avviene con:
```csharp
services.AddFactory<TInterface, TImplementation>(name, ServiceLifetime);
```

La chiave interna nel DI keyed-services è normalizzata in:
```
"Rystem.Factory.{typeof(TInterface).FullName}.{name}"
```

La risoluzione avviene con:
```csharp
IFactory<TInterface>.Create(factoryName)        // una istanza
IFactory<TInterface>.CreateAll(factoryName)     // tutte le istanze registrate con quel nome
```

Il `factoryName` scorre da `AddPlayFramework("default", ...)` → `PlayFrameworkBuilder.Name` →
`AddFactory(..., Name, ...)` → `IFactory<T>.Create(factoryName)` nell'endpoint handler.

### Come va applicato agli hook

Ogni tipo di hook è un servizio factory-keyed per `factoryName`:

```csharp
// Registrazione (dentro PlayFrameworkBusinessBuilder)
services.AddFactory<IPlayFrameworkBeforeExecution, FeatureFlagGuard>(factoryName, Scoped);
services.AddFactory<IPlayFrameworkBeforeExecution, RateLimitGuard>(factoryName, Scoped);
services.AddFactory<IPlayFrameworkAfterEachScene, CostTrackingInterceptor>(factoryName, Scoped);
services.AddFactory<IPlayFrameworkOnTerminalScene, UsageWarningHook>(factoryName, Scoped);

// Registrazione del motore factory (una volta sola, in AddPlayFramework)
services.AddEngineFactory<IPlayFrameworkBeforeExecution>();
services.AddEngineFactory<IPlayFrameworkAfterEachScene>();
services.AddEngineFactory<IPlayFrameworkOnTerminalScene>();
services.AddEngineFactory<IPlayFrameworkBusinessManager>();
```

```csharp
// Risoluzione a runtime (nell'endpoint handler o nel business manager)
var beforeHooks = beforeExecutionFactory.CreateAll(factoryName); // tutti i guard per questa factory
var afterHooks  = afterEachSceneFactory.CreateAll(factoryName);
var terminalHooks = onTerminalFactory.CreateAll(factoryName);
```

`CreateAll(factoryName)` restituisce tutte le implementazioni registrate per quel nome, nell'ordine
di registrazione — che poi viene riordinato per `priority`.

### `IPlayFrameworkBusinessManager` è anch'esso factory-keyed

Il manager stesso viene registrato e risolto per factory name:

```csharp
// Registrazione
services.AddFactory<IPlayFrameworkBusinessManager, PlayFrameworkBusinessManager>(factoryName, Scoped);
services.AddEngineFactory<IPlayFrameworkBusinessManager>();

// Risoluzione nell'endpoint handler
var manager = businessManagerFactory.Create(factoryName);
```

Se nessun hook è registrato per una factory, `CreateAll` ritorna una lista vuota e il manager
comporta come se non ci fossero hook (esecuzione diretta, nessun pre/post processing).

---

## Decisioni di design

### 1. Registrazione — dentro `AddPlayFramework`

Gli hook si registrano tramite una lambda `business` dentro il builder di `AddPlayFramework`,
nello stesso punto dove vengono configurate le altre impostazioni della factory.

```csharp
services.AddPlayFramework("default", config =>
{
    // configurazione esistente ...
    config.Business
        .AddBeforeExecution<FeatureFlagGuard>()
        .AddBeforeExecution<RateLimitGuard>(priority: 1)
        .AddAfterEachScene<CostTrackingInterceptor>()
        .AddOnTerminalScene<UsageWarningHook>();
});
```

Non esiste un metodo separato `AddPlayFrameworkBusiness`. La registrazione è inline nel builder.

`PlayFrameworkBusinessBuilder` riceve `IServiceCollection` e `factoryName` da `PlayFrameworkBuilder`
e chiama `AddFactory<TInterface, TImpl>(factoryName, Scoped)` per ogni hook registrato.

### 2. Scoping per factory name

Ogni istanza di `MapPlayFramework` (identificata da `factoryName`) ha il proprio set di hook.
Hook registrati per `"default"` non si applicano a `"premium"` e viceversa. Questo permette
logiche di rate limiting o feature flag differenziate per factory.

Lo scoping è garantito automaticamente dal factory pattern: `AddFactory<T, TImpl>(name)` registra
il servizio sotto la chiave `"Rystem.Factory.{T}.{name}"`, quindi hook di factory diverse non
collidono mai.

### 3. DI lifetime — Scoped

Gli hook sono registrati come `Scoped` nel DI container tramite
`AddFactory<TInterface, TImpl>(factoryName, ServiceLifetime.Scoped)`.
Una nuova istanza viene creata per ogni richiesta HTTP. Questo consente agli hook di iniettare
servizi scoped (es. `DbContext`, `ICacheService`) via costruttore.

```csharp
public sealed class RateLimitGuard(ICacheService cacheService, ILogger<RateLimitGuard> logger)
    : IPlayFrameworkBeforeExecution
{
    public async Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```

### 4. Molteplicità — pipeline ordinata per Priority

Più implementazioni dello stesso tipo di hook possono coesistere. Vengono eseguite in sequenza,
ordinate per `priority` (valore intero, ascending). A parità di priority, l'ordine è quello di
registrazione.

**Priority**: parametro opzionale, default `0`. Viene memorizzato in una struttura wrapper
insieme al tipo dell'hook durante la registrazione, poi usato per ordinare prima di eseguire.

La chiamata `IFactory<IPlayFrameworkBeforeExecution>.CreateAll(factoryName)` restituisce già
tutte le implementazioni nell'ordine di registrazione; il business manager le riordina per
priority prima di eseguirle.

In `BeforeExecution`: se un guard restituisce `Deny` o `ShortCircuit`, i guard successivi non
vengono eseguiti (short-circuit della pipeline).

### 5. Orchestratore — `IPlayFrameworkBusinessManager`

Un servizio `IPlayFrameworkBusinessManager` (Scoped, factory-keyed) coordina l'intera pipeline.
L'endpoint handler SSE in `WebApplicationExtensions.cs` risolve il manager tramite
`IFactory<IPlayFrameworkBusinessManager>.Create(factoryName)` e gli delega l'esecuzione.

Il manager inietta nel costruttore:
- `IFactory<IPlayFrameworkBeforeExecution>` → chiama `CreateAll(factoryName)`
- `IFactory<IPlayFrameworkAfterEachScene>` → chiama `CreateAll(factoryName)`
- `IFactory<IPlayFrameworkOnTerminalScene>` → chiama `CreateAll(factoryName)`
- `IFactory<ISceneManager>` → chiama `Create(factoryName)`

Il `factoryName` viene passato al manager come parametro (non iniettato): l'endpoint handler lo
conosce già (dal route o dalla configurazione) e lo passa al momento della chiamata.

Responsabilità del manager:
1. Costruire `PlayFrameworkExecutionContext`
2. Applicare il timeout (LinkedCancellationTokenSource)
3. Eseguire gli hook `BeforeExecution` in sequenza (ordinati per priority)
4. Gestire il risultato del guard (Allow / Deny / ShortCircuit)
5. Chiamare `ISceneManager.ExecuteAsync`
6. Per ogni item dello stream: eseguire gli hook `AfterEachScene` (ordinati per priority)
7. Al termine: eseguire gli hook `OnTerminalScene` (ordinati per priority)

Il manager espone:
```csharp
public interface IPlayFrameworkBusinessManager
{
    IAsyncEnumerable<(AiSceneResponse? Scene, PlayFrameworkDenyResult? Deny)> ExecuteAsync(
        string factoryName,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

L'endpoint handler SSE itera il risultato e scrive gli item SSE direttamente su `HttpResponse`.

### 6. Gestione errori — propagazione

Le eccezioni non gestite lanciate dagli hook si propagano verso l'endpoint handler, che risponde
con `500 Internal Server Error`. Non esiste un hook `OnError` dedicato.

---

## Hook interfaces

### `IPlayFrameworkBeforeExecution`

Gira **una volta** prima che lo stream parta. Può bloccare l'esecuzione o lasciarla procedere.

```csharp
public interface IPlayFrameworkBeforeExecution
{
    Task<PlayFrameworkGuardResult> BeforeExecutionAsync(
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

**Risultati possibili:**

| Risultato | Comportamento |
|-----------|---------------|
| `PlayFrameworkGuardResult.Allow()` | L'esecuzione procede al guard successivo (o allo stream se è l'ultimo) |
| `PlayFrameworkGuardResult.Deny(statusCode, errorDetail)` | Lo stream non parte. Il client riceve un HTTP error (es. 403). I guard successivi non girano. |
| `PlayFrameworkGuardResult.ShortCircuit(AiSceneResponse)` | Lo stream SSE viene aperto, viene inviato il singolo item sintetico, poi si chiude. I guard successivi non girano. |

**Note su `ShortCircuit`**: il server apre il flusso SSE (`text/event-stream`) e invia un singolo
evento `data: {...}\n\n`, poi chiude la connessione. Il client SSE riceve sempre SSE,
indipendentemente dal percorso, garantendo coerenza di protocollo.

**Caso d'uso TimeVision**:
- `FeatureFlagGuard`: controlla `IFeatureManager.IsEnabledAsync` + `IFeatureFlagService` →
  `Deny(403)` se AI non abilitata
- `RateLimitGuard`: legge il costo accumulato da cache → `ShortCircuit(BuildUsageLimitExceededResponse())`
  se soglia superata

---

### `IPlayFrameworkAfterEachScene`

Gira **per ogni `AiSceneResponse`** prodotta dallo stream, prima che venga scritta sul canale SSE.

```csharp
public interface IPlayFrameworkAfterEachScene
{
    Task<PlayFrameworkSceneResult> AfterSceneAsync(
        AiSceneResponse scene,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

**Risultati possibili:**

| Risultato | Comportamento |
|-----------|---------------|
| `PlayFrameworkSceneResult.Forward(scene)` | L'item (eventualmente modificato) viene inviato al client |
| `PlayFrameworkSceneResult.Suppress()` | L'item non viene inviato al client |
| `PlayFrameworkSceneResult.ForwardAndInject(scene, AiSceneResponse[] extra)` | L'item viene inviato, poi vengono inviati gli item `extra` in sequenza |

**Note**: la pipeline di hook `AfterEachScene` si applica anche agli item `extra` iniettati
tramite `ForwardAndInject`? **No** — gli item extra vengono inviati direttamente senza passare
per gli hook, per evitare loop infiniti.

**Caso d'uso TimeVision**:
- Skip `AiResponseStatus.ToolSkipped` → `Suppress()`
- Rename Summarizing → `Forward(scene con SceneName modificato)`
- Soppressione messaggi con prefix specifico → `Suppress()`
- Cost tracking: `cacheService.IncrementAsync(...)` come side-effect, poi `Forward(scene)`
- Logging per ogni risposta come side-effect

---

### `IPlayFrameworkOnTerminalScene`

Gira **una volta** quando viene rilevato uno status terminale
(`Completed | Error | BudgetExceeded | Unauthorized`).

La risposta terminale viene **prima inviata al client**, poi gli hook `OnTerminalScene` girano e
possono iniettare item aggiuntivi **in coda** al flusso SSE.

```csharp
public interface IPlayFrameworkOnTerminalScene
{
    Task<IEnumerable<AiSceneResponse>?> OnTerminalAsync(
        AiSceneResponse terminalScene,
        PlayFrameworkExecutionContext context,
        CancellationToken cancellationToken = default);
}
```

**Ritorno**: `null` o lista vuota = nessuna iniezione. Lista non-null = gli item vengono inviati
al client in sequenza dopo la risposta terminale.

Più hook `OnTerminalScene` girano in sequenza (pipeline); gli item iniettati da ciascun hook
vengono accodati nell'ordine degli hook.

**Caso d'uso TimeVision**: calcolo costo accumulato da cache; se supera la soglia configurata,
inietta un item `BuildUsageWarningResponse(...)` dopo il terminale.

---

## `PlayFrameworkExecutionContext`

Contesto condiviso tra tutti gli hook della stessa richiesta. Viene costruito dall'orchestratore
prima dell'esecuzione e passato a ogni hook.

```csharp
public sealed class PlayFrameworkExecutionContext
{
    /// Messaggio dell'utente
    public string Message { get; init; }

    /// Chiave conversazione (da request)
    public string? ConversationKey { get; init; }

    /// Settings della richiesta — mutable: BeforeExecution può modificarle
    /// prima che vengano passate a ISceneManager.ExecuteAsync
    public SceneRequestSettings Settings { get; init; }

    /// ClaimsPrincipal dell'utente autenticato (da HttpContext.User).
    /// Null se la richiesta non è autenticata.
    /// I claim type sono project-specific; gli hook li leggono con User.FindFirst(...)
    public ClaimsPrincipal? User { get; init; }

    /// Bag condiviso tra hook della stessa richiesta.
    /// Usato per passare dati calcolati da un hook (es. cache key) agli hook successivi.
    /// ConcurrentDictionary per sicurezza in scenari async.
    public ConcurrentDictionary<string, object> Items { get; init; }
}
```

**Pattern Items bag** (esempio TimeVision):
```csharp
// In RateLimitGuard.BeforeExecutionAsync:
var cacheKey = BuildCacheKey(tenantId, userId, month);
context.Items["usageCacheKey"] = cacheKey;

// In CostTrackingInterceptor.AfterSceneAsync:
var cacheKey = (string)context.Items["usageCacheKey"];
await cacheService.IncrementAsync(cacheKey, cost, cancellationToken);
```

**Nota**: `IServiceProvider` non è esposto nel context. Gli hook ricevono le dipendenze via
costruttore (DI scoped standard).

---

## Timeout per-richiesta

**Configurazione**: `PlayFrameworkApiSettings.TimeoutInSeconds` (server-side, non modificabile
dal client).

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/completions";
    settings.TimeoutInSeconds = 60;
});
```

**Scope del timeout**: copre l'intera pipeline — da `BeforeExecution` fino al completamento degli
hook `OnTerminalScene`.

**Implementazione**: il business manager crea un `CancellationTokenSource` con il timeout
configurato e lo linka al token della richiesta HTTP (`CancellationTokenSource.CreateLinkedTokenSource`).
Tutti gli hook e `ISceneManager.ExecuteAsync` ricevono il token linkato.

**Comportamento in caso di timeout**: `408 Request Timeout`. La distinzione tra timeout scaduto
e cancellazione da parte del client avviene tramite `when (timeoutCts.IsCancellationRequested)`
come nel controller originale.

---

## Flusso di esecuzione completo

```
POST /api/completions
       │
       ▼
[IPlayFrameworkBusinessManager.ExecuteAsync]
       │
       ├─ Costruisce PlayFrameworkExecutionContext
       ├─ Crea timeoutCts (se TimeoutInSeconds > 0) + linkedCts
       │
       ├─ [BeforeExecution hooks] (in ordine di priority)
       │      ├─ Allow → continua al prossimo hook
       │      ├─ Deny → scrive HTTP error, return
       │      └─ ShortCircuit → apre SSE, scrive 1 item, chiude, return
       │
       ├─ ISceneManager.ExecuteAsync(message, settings, linkedCts)
       │      │
       │      └─ await foreach AiSceneResponse
       │             │
       │             ├─ [AfterEachScene hooks] (in ordine di priority)
       │             │      ├─ Forward(scene) → scrivi su SSE
       │             │      ├─ Suppress() → skip
       │             │      └─ ForwardAndInject(scene, extra[]) → scrivi scene + extra
       │             │
       │             └─ se status terminale:
       │                    ├─ scrivi item terminale su SSE (già fatto sopra se Forward)
       │                    └─ [OnTerminalScene hooks] (in ordine di priority)
       │                           └─ inietta item extra su SSE
       │
       └─ Chiude lo stream SSE
```

---

## Builder API — shape definitiva

```csharp
// Registrazione (dentro AddPlayFramework)
services.AddPlayFramework("default", config =>
{
    config.DefaultExecutionMode = SceneExecutionMode.DynamicChaining;

    config.Business
        .AddBeforeExecution<FeatureFlagGuard>()
        .AddBeforeExecution<RateLimitGuard>(priority: 1)
        .AddAfterEachScene<CostTrackingInterceptor>()
        .AddOnTerminalScene<UsageWarningHook>();
});

// Mapping endpoint (invariato)
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/completions";
    settings.TimeoutInSeconds = 60;
    settings.EnableConversationEndpoints = true;
});
```

---

## Priorità implementazione

| Componente | Priorità | Blocca rimozione AiController |
|---|---|---|
| `IPlayFrameworkBeforeExecution` + `PlayFrameworkGuardResult` | Alta | Sì |
| `IPlayFrameworkAfterEachScene` + `PlayFrameworkSceneResult` | Alta | Sì |
| `PlayFrameworkExecutionContext` | Alta | Sì |
| `IPlayFrameworkBusinessManager` (orchestratore) | Alta | Sì |
| `IPlayFrameworkOnTerminalScene` | Media | Sì (per usage warning) |
| `PlayFrameworkApiSettings.TimeoutInSeconds` | Bassa | No (gestibile fuori) |

---

## Note implementative

- **Status terminali**: `AiResponseStatus.Completed | Error | BudgetExceeded | Unauthorized`
- **Item extra di `ForwardAndInject` e `OnTerminalScene`**: non passano per gli hook `AfterEachScene`
- **Hook `AfterEachScene` su item soppressi**: se un hook sopprime un item ma quello stesso item
  ha status terminale, `OnTerminalScene` gira comunque (il trigger è lo status, non l'invio al client)
- **Nessuna scansione automatica assembly**: gli hook si registrano esplicitamente nel builder,
  non tramite `Scan*`. Questo è intenzionale per mantenere la registrazione visibile e controllata.
- **Factory senza hook**: se nessun hook è registrato per una factory, `CreateAll` ritorna lista
  vuota e il manager esegue direttamente `ISceneManager.ExecuteAsync` senza overhead. Il
  `IPlayFrameworkBusinessManager` va comunque registrato come factory anche in assenza di hook,
  perché è lui che gestisce il timeout.
- **Priority storage**: la priority non è sull'interfaccia hook (a differenza di
  `IRepositoryBusiness.Priority`). Viene memorizzata in una struttura interna di
  `PlayFrameworkBusinessBuilder` durante la registrazione e trasferita al manager tramite
  `IPlayFrameworkHookRegistry` (o equivalente) registrato come singleton per factory name.
