# Migrazione dell'adapter Azure OpenAI a OpenAI 2.12.0

Piano eseguibile per migrare `Rystem.PlayFramework.Adapters` da `Azure.AI.OpenAI 2.9.0-beta.1` al client diretto `OpenAI 2.12.0`, mantenendo `Microsoft.Extensions.AI.OpenAI 10.9.0` e la compatibilita funzionale dell'adapter Azure.

## 1. Stato del documento

- Tipo: piano di implementazione e accettazione.
- Branch di pianificazione: `plan/openai-sdk-2.12-migration`.
- Baseline: `master` al commit `679a1e959583eeae1c2c1af4c25f52626a926116`.
- Stato: **implementato**. Il difetto e riprodotto, il design e verificato per compilazione, traffico HTTP e live gate su Azure; A3, A6 e A7 sono risolti (vedi §3.1) e i rischi residui della sezione 15 sono stati mitigati nel codice.
- Scope: `src/AI/**`. L'infrastruttura di release condivisa del repository e trattata nell'appendice A come dipendenza esterna, non come lavoro di questo intervento.

### 1.1 Baseline verificata sul repository

| Fatto | Valore osservato | Riferimento |
| --- | --- | --- |
| TargetFramework di tutti i progetti AI | `net10.0` | `Rystem.PlayFramework.Adapters.csproj:4` |
| Versione progetti AI | `10.1.0-beta.5` | `Rystem.PlayFramework.Adapters.csproj:21` |
| `NoWarn` gia presente | `$(NoWarn);OPENAI001;AOAI001` | `Rystem.PlayFramework.Adapters.csproj:25` |
| Riferimento al core | `ProjectReference` in Debug, `PackageReference 10.1.0-beta.5` altrimenti | `Rystem.PlayFramework.Adapters.csproj:41-52` |
| Framework di test | xunit `2.9.3` (v2) + `xunit.runner.visualstudio 4.0.0` | `Rystem.PlayFramework.Test.csproj:24-25` |
| Reference dell'adapter nei test | assente | `Rystem.PlayFramework.Test.csproj:31-34` |
| `Trait` nel progetto di test | nessuna occorrenza | progetto `Rystem.PlayFramework.Test` |
| Central Package Management | assente | repository |

Conseguenze operative:

- ogni `.csproj` va aggiornato singolarmente: non esiste `Directory.Packages.props`;
- il progetto di test replica `Azure.AI.OpenAI 2.9.0-beta.1` e `Microsoft.Extensions.AI.OpenAI 10.9.0` (`Rystem.PlayFramework.Test.csproj:12`, `:22`), quindi va migrato insieme all'adapter;
- il build in configurazione diversa da Debug richiede `Rystem.PlayFramework 10.1.0-beta.5` risolvibile da un feed.

## 2. Difetto riprodotto

La configurazione corrente dichiara:

```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.9.0-beta.1" />
<PackageReference Include="Azure.Identity" Version="1.21.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.9.0" />
<PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="10.0.11" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.11" />
```

`Microsoft.Extensions.AI.OpenAI 10.9.0` richiede `OpenAI >= 2.12.0 e < 2.13.0`, mentre `Azure.AI.OpenAI 2.9.0-beta.1` e compilato contro `OpenAI 2.9.1`. NuGet risolve `OpenAI 2.12.0` e il companion SDK cerca una firma che non esiste piu.

Riproduzione eseguita su progetto isolato con la stessa graph:

```text
Azure.AI.OpenAI                2.9.0-beta.1  (diretto)
Microsoft.Extensions.AI.OpenAI 10.9.0        (diretto)
OpenAI                         2.12.0        (transitivo)
System.ClientModel             1.14.0        (transitivo)
```

Esito:

```text
new AzureOpenAIClient(endpoint, apiKeyCredential).GetResponsesClient()
  -> System.MissingMethodException: Method not found:
     'Void OpenAI.Responses.ResponsesClient..ctor(
        System.ClientModel.Primitives.ClientPipeline,
        OpenAI.OpenAIClientOptions)'

new AzureOpenAIClient(...).GetChatClient("dep").AsIChatClient()
  -> OK: Microsoft.Extensions.AI.OpenAIChatClient
```

Due conseguenze che orientano tutto il piano:

1. la diagnosi e confermata nel dettaglio, incluso il membro mancante;
2. **il difetto e limitato alla Responses API**. Chat Completions funziona sulla graph attuale. Poiche `AdapterSettings.UseResponsesApi` vale `true` per default (`AdapterSettings.cs:31`), esiste una mitigazione immediata senza ripacchettizzare: impostare `UseResponsesApi = false`. Questo cambia la sezione 12 sul rollback.

Il punto di innesco nel codice e `ServiceCollectionExtensions.cs:58`.

## 3. Decisione architetturale

Rimuovere `Azure.AI.OpenAI` da `Rystem.PlayFramework.Adapters` e usare direttamente `OpenAI 2.12.0` contro l'endpoint Azure OpenAI v1:

```text
https://<resource>.openai.azure.com/openai/v1/
```

Il bridge `Microsoft.Extensions.AI.OpenAI 10.9.0` continua a convertire i client OpenAI in `IChatClient`.

Riferimenti:

- [Azure OpenAI SDK language support](https://learn.microsoft.com/azure/foundry/openai/supported-languages)
- [Azure OpenAI v1 API lifecycle and code changes](https://learn.microsoft.com/azure/foundry/openai/api-version-lifecycle#code-changes)
- [Azure SDK migration guidance](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/migration-guidance.md)
- [OpenAI .NET client](https://github.com/openai/openai-dotnet)

### 3.1 Esito delle verifiche di design

Spike eseguito su progetto isolato `net10.0` con `OpenAI 2.12.0`, `Microsoft.Extensions.AI.OpenAI 10.9.0`, `Azure.Identity 1.21.0`. Tutto cio che segue e misurato, non ipotizzato.

| # | Oggetto | Esito | Evidenza |
| --- | --- | --- | --- |
| A1 | `OpenAIClientOptions.Endpoint` | **verificato** | property `System.Uri`, `CanWrite = true` |
| A2 | `BearerTokenPolicy` con `DefaultAzureCredential` | **verificato, senza adattatore** | `Azure.Core.TokenCredential` deriva da `System.ClientModel.AuthenticationTokenProvider`; il costruttore `BearerTokenPolicy(AuthenticationTokenProvider, string scope)` lega direttamente |
| A4 | Header emesso nel percorso API key | **misurato** | `Authorization: Bearer <key>`; nessun header `api-key`, nessun query `api-version` |
| A5 | Graph transitiva | **verificato** | `System.ClientModel 1.14.0`, `Azure.Core 1.53.0`, `System.Memory.Data 10.0.3` |
| A3 | Scope Entra corretto per Azure v1 | **risolto**: `https://ai.azure.com/.default` per Chat/Responses; l'Audio v1 richiede uno scope distinto, `https://cognitiveservices.azure.com/.default` (`OpenAIClientFactory.AudioEntraScope`) | `OpenAIClientFactory.cs` |
| A6 | Route Audio su `/openai/v1/` senza `api-version` | **risolto**: le route Audio non funzionano su `/openai/v1/` senza versione; si usa l'endpoint deployment-specific `/openai/deployments/{deployment}` con `api-version` iniettato da una policy dedicata (`2025-04-01-preview` per trascrizioni e speech, l'unica versione che documenta sia `whisper-1` sia `gpt-4o-transcribe`/`gpt-4o-mini-transcribe`; overridabile per singolo adapter via `AdapterSettings.SpeechToTextApiVersion`/`VoiceAdapterSettings.SttApiVersion`) | `OpenAIClientFactory.CreateAudioClient`, `AudioApiVersionPolicy` |
| A7 | Purpose dei file consumati da Responses | **risolto**: `FileUploadPurpose.Assistants` accettato senza modifiche, confermato dal live gate Files | `MultiModalChatClient.cs:148-149`, `AzureOpenAIFilesLiveGateTests` |

Vincolo emerso da A2, da non violare: `Azure.Identity` deve restare **>= 1.21.0**, perche porta `Azure.Core 1.53.0`, la prima linea in cui `TokenCredential` deriva da `AuthenticationTokenProvider`. Un downgrade di `Azure.Identity` rompe la compilazione del percorso Entra.

### 3.2 Traffico HTTP misurato

Client costruito con `Endpoint = https://name.openai.azure.com/openai/v1/` e `ApiKeyCredential("SECRET-KEY")`, transport di cattura, chiamata Responses via bridge con deployment `my-deployment`:

```text
POST https://name.openai.azure.com/openai/v1/responses
Accept: application/json, text/event-stream
Content-Type: application/json
User-Agent: OpenAI/2.12.0 (.NET 10.0.10; macOS 26.6.0)
User-Agent: MEAI/10.9.0
Authorization: Bearer SECRET-KEY

{"model":"my-deployment","input":[{"type":"message","role":"user",
 "content":[{"type":"input_text","text":"hi"}]}]}
```

Conferme dirette:

- il deployment viene trasmesso nel campo `model`, come richiesto da Azure v1;
- `OpenAIClientOptions.Transport` e **pubblico**, quindi i contract test non richiedono seam `InternalsVisibleTo` per il transport;
- non viene emesso alcun `api-version`.

## 4. Obiettivi

1. Eliminare la `MissingMethodException` senza retrocedere `OpenAI` o `Microsoft.Extensions.AI.OpenAI`.
2. Usare questa matrice NuGet, con `OpenAI` dichiarato come range esatto:

   ```text
   OpenAI                                    [2.12.0]   diretto, range esatto
   Microsoft.Extensions.AI.OpenAI            10.9.0     diretto
   Azure.Identity                            1.21.0     diretto, floor vincolante
   Microsoft.Extensions.Caching.Abstractions 10.0.11    diretto, invariato
   Microsoft.Extensions.Logging.Abstractions 10.0.11    diretto, invariato
   System.ClientModel                        1.14.0     transitivo, verificato
   Azure.Core                                1.53.0     transitivo, verificato
   ```

3. Conservare invariate le API pubbliche Rystem: `AddAdapterForAzureOpenAI`, `AddVoiceAdapterForAzureOpenAI`, `AdapterSettings`, `VoiceAdapterSettings`, `UseResponsesApi`, `EnableFileUpload`, `UseAzureCredential`, nomi di deployment e factory.
4. Conservare i valori di configurazione endpoint gia distribuiti, inclusi gli endpoint Azure root senza `/openai/v1/`.
5. Conservare i percorsi funzionali: Responses, Chat Completions, streaming e non streaming, function/tool calling, file upload e hosted file, immagini e audio inline supportati dal modello, speech-to-text, text-to-speech, API key, Microsoft Entra ID e Managed Identity, cost tracking.
6. Verificare il pacchetto NuGet come consumer esterno, non soltanto tramite project reference.

## 5. Non obiettivi

- Reintrodurre un fallback runtime su `Azure.AI.OpenAI`.
- Mantenere contemporaneamente entrambi gli SDK nel pacchetto pubblicato.
- Aggiungere supporto a OpenAI pubblico o provider generici attraverso le API nominate Azure.
- Ridisegnare il modello di cost tracking.
- Introdurre nuove funzionalita Responses, Files o Audio non gia esposte da Rystem.
- Modificare le API pubbliche del core `Rystem.PlayFramework`.
- Modificare l'infrastruttura di release condivisa del repository: vedere appendice A.
- Correggere i difetti preesistenti dei wrapper, da tracciare come issue separate:
  - i due `Dictionary` non sincronizzati di `MultiModalChatClient` (`:30` `_fallbackCache`, `:33` `_remoteIndex`), mutati da percorsi async in un servizio Singleton (`ServiceCollectionExtensions.cs:46`);
  - il riuso di file remoti basato sul solo nome (`MultiModalChatClient.cs:195-217`);
  - il sync-over-async nei percorsi streaming (`MultiModalChatClient.cs:70-71`, `SpeechToTextChatClient.cs:40-41`).

Eccezione esplicita: la mancata propagazione del `CancellationToken` in `SpeechToTextChatClient.cs:94` **e** in scope, perche la Fase 4 tocca comunque quella chiamata e perche `AzureOpenAIVoiceAdapter.cs:46` il token lo passa, il che rende la differenza una svista.

## 6. Contratti di compatibilita

### 6.1 Compatibilita sorgente

Questo codice consumer deve continuare a compilare senza modifiche:

```csharp
services.AddAdapterForAzureOpenAI("default", settings =>
{
    settings.Endpoint = new Uri(configuration["AzureOpenAI:Endpoint"]!);
    settings.ApiKey = configuration["AzureOpenAI:Key"];
    settings.Deployment = configuration["AzureOpenAI:Deployment"]!;
});
```

Nessuna ridenominazione di metodi pubblici in questa release. Eventuali alias generici sono API additive successive.

### 6.2 Contratto di normalizzazione dell'endpoint

Comportamento misurato del client `OpenAI 2.12.0` al variare di `OpenAIClientOptions.Endpoint`:

| `Endpoint` impostato | URI Responses prodotto | URI Chat prodotto |
| --- | --- | --- |
| `https://name.openai.azure.com/openai/v1/` | `.../openai/v1/responses` | `.../openai/v1/chat/completions` |
| `https://name.openai.azure.com/openai/v1` | `.../openai/v1/responses` | `.../openai/v1/chat/completions` |
| `https://name.openai.azure.com/` | `.../responses` (**errato**) | `.../chat/completions` (**errato**) |
| `https://name.openai.azure.com` | `.../responses` (**errato**) | `.../chat/completions` (**errato**) |

Due conclusioni operative:

1. lo slash finale e **irrilevante**: il client normalizza da solo. Il requisito "terminare con slash" e cosmetico e non va trasformato in un test di comportamento;
2. il segmento `/openai/v1` e **obbligatorio**: senza di esso il client compone una route inesistente. Questa e l'unica trasformazione che il normalizzatore deve garantire.

Contratto richiesto al normalizzatore:

| Input | Output |
| --- | --- |
| `https://name.openai.azure.com` | `https://name.openai.azure.com/openai/v1` |
| `https://name.openai.azure.com/` | `https://name.openai.azure.com/openai/v1` |
| `https://name.openai.azure.com/openai` | `https://name.openai.azure.com/openai/v1` |
| `https://name.openai.azure.com/openai/` | `https://name.openai.azure.com/openai/v1` |
| `https://name.openai.azure.com/openai/v1` | invariato |
| `https://name.openai.azure.com/openai/v1/` | invariato |
| endpoint custom terminante in `/v1` o `/v1/` | invariato |
| `https://name.openai.azure.com/openai/deployments/<dep>` | rifiutato: formato legacy deployment-specific non supportato |
| URI relativo | rifiutato |
| schema diverso da HTTPS | rifiutato, salvo `http://localhost` per sviluppo locale |
| URI con query o fragment | rifiutato con messaggio esplicito |

Regola di implementazione: se il path contiene gia un segmento `v1`, non modificare; altrimenti appendere `openai/v1`. Usare `Uri`/`UriBuilder`, mai concatenazione di stringhe non validata. L'helper deve essere puro, interno e testabile senza rete.

Caso di regressione obbligatorio: il formato prodotto dai test attuali, `https://{AzureResourceName}.openai.azure.com/` (`Configuration/OpenAiSettings.cs:11`), ricade nella seconda riga della tabella.

### 6.3 Tipi pubblici

`MultiModalChatClient` e `SpeechToTextChatClient` espongono `OpenAI.Files.OpenAIFileClient` (`MultiModalChatClient.cs:37`) e `OpenAI.Audio.AudioClient` (`SpeechToTextChatClient.cs:18`) nei costruttori pubblici.

Questi tipi provengono gia oggi dal pacchetto `OpenAI`, non da `Azure.AI.OpenAI`: namespace e firme non cambiano, cambia solo la versione dell'assembly che il consumer risolve. La migrazione non rompe la firma di questi costruttori.

`AzureOpenAIVoiceAdapter` e `internal` (`AzureOpenAIVoiceAdapter.cs:10`): rinominabile o ristrutturabile senza impatto pubblico.

Il package dichiara `OpenAI` con `Version="[2.12.0]"` per impedire un restore ambiguo. Costo accettato: un consumer che referenzia direttamente `OpenAI 2.12.1` o successive ottiene un conflitto irrisolvibile. Il pin va rimosso appena `Microsoft.Extensions.AI.OpenAI` allarga il proprio range; tracciarlo come debito con scadenza.

### 6.4 Autenticazione

Entrambe le forme sono verificate per compilazione contro `OpenAI 2.12.0`.

API key:

```csharp
new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = normalizedEndpoint });
```

Il client emette `Authorization: Bearer <apiKey>`, non `api-key`. E il comportamento atteso dall'endpoint Azure v1 e va confermato dal gate live della Fase 5.

Microsoft Entra ID:

```csharp
new OpenAIClient(
    new BearerTokenPolicy(new DefaultAzureCredential(), EntraScope),
    new OpenAIClientOptions { Endpoint = normalizedEndpoint });
```

`EntraScope` e una costante interna. Valore di partenza `https://ai.azure.com/.default`; se il gate Entra della Fase 5 restituisce 401/403, provare `https://cognitiveservices.azure.com/.default`. Poiche il valore e centralizzato, il cambio e una modifica di una riga: non e un rischio di progetto.

`DefaultAzureCredential` resta il comportamento pubblico corrente (`ServiceCollectionExtensions.cs:189`). La documentazione deve raccomandare Managed Identity in Azure e indicare i ruoli data-plane richiesti.

## 7. Architettura target

```text
AdapterSettings / VoiceAdapterSettings
                |
                v
       AzureOpenAIEndpoint.Normalize   (internal, puro)
                |
                v
       OpenAIClientFactory.Create      (internal, condiviso)
       |                    |
       | API key            | Entra ID
       v                    v
 ApiKeyCredential     BearerTokenPolicy(DefaultAzureCredential, EntraScope)
       \                    /
        \                  /
          OpenAIClient 2.12.0
          /      |       \
         /       |        \
ResponsesClient ChatClient  OpenAIFileClient / AudioClient
       |             |
       +------ Microsoft.Extensions.AI bridge ------+
                                                     |
                                                     v
                                                IChatClient
```

Due soli helper interni:

1. `AzureOpenAIEndpoint.Normalize(Uri) -> Uri`, funzione pura che implementa la tabella 6.2;
2. `OpenAIClientFactory.Create(endpoint, apiKey, useAzureCredential, Action<OpenAIClientOptions>? configure)`, condivisa da chat adapter e voice adapter per impedire divergenze di endpoint e autenticazione.

Testabilita: `OpenAIClientOptions.Transport` e pubblico, quindi il parametro `configure` e sufficiente per iniettare un transport di cattura nei contract test. **Non servono seam `InternalsVisibleTo` per il transport.** Un seam interno resta utile solo per sostituire `DefaultAzureCredential` nei test offline del percorso Entra.

Le API pubbliche non espongono `configure`.

## 8. File coinvolti

### 8.1 Adapter

- `src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj`
  - rimuovere `Azure.AI.OpenAI` (`:29`);
  - aggiungere `<PackageReference Include="OpenAI" Version="[2.12.0]" />`;
  - mantenere `Microsoft.Extensions.AI.OpenAI 10.9.0`, `Azure.Identity 1.21.0` (floor vincolante, vedere 3.1), `Microsoft.Extensions.Caching.Abstractions 10.0.11`, `Microsoft.Extensions.Logging.Abstractions 10.0.11`;
  - in `NoWarn` (`:25`) rimuovere `AOAI001`, specifico di `Azure.AI.OpenAI`, e **mantenere `OPENAI001`**: la Responses API di `OpenAI` resta experimental;
  - non toccare il blocco `Choose` (`:41-52`);
  - aggiornare descrizione (`:15`) e release notes (`:22`), che citano ancora "beta.24".
- `src/AI/Rystem.PlayFramework.Adapters/AzureOpenAIEndpoint.cs` (nuovo)
  - `internal static class` con la funzione pura di normalizzazione e i messaggi di rifiuto.
- `src/AI/Rystem.PlayFramework.Adapters/OpenAIClientFactory.cs` (nuovo)
  - `internal static class` con `EntraScope` e la creazione del client per entrambe le modalita.
- `src/AI/Rystem.PlayFramework.Adapters/ServiceCollectionExtensions.cs`
  - sostituire `AzureOpenAIClient` con `OpenAIClient` in `CreateAzureOpenAIClient` (`:185-193`), rinominando il metodo;
  - instradare entrambi i percorsi sulla factory, inclusa `CreateVoiceAdapter` (`:158`);
  - mantenere invariata la selezione Responses/Chat (`:56-63`) e l'ordine dei wrapper (`:66-86`);
  - conservare le validazioni esistenti (`:100-115`, `:166-179`), aggiungendo la validazione dell'endpoint della tabella 6.2;
  - rimuovere gli `using Azure.AI.OpenAI` (`:1`).
- `src/AI/Rystem.PlayFramework.Adapters/AdapterSettings.cs`
  - documentare endpoint Azure root e v1 e il deployment come valore `model`;
  - correggere il commento `:57-63`, che dichiara la registrazione di un `ICostCalculator` mentre il codice avvolge direttamente in `CostTrackingChatClient` (`ServiceCollectionExtensions.cs:85-86`).
- `src/AI/Rystem.PlayFramework.Adapters/VoiceAdapterSettings.cs`
  - correggere i commenti `:10` e `:13` ("If null, reuses the endpoint/key from `AdapterSettings`"): il riuso non esiste, `ValidateVoiceSettings` lancia se `Endpoint` e null (`ServiceCollectionExtensions.cs:168-172`) e il voice adapter costruisce un client separato (`:158`). Correggere la documentazione; l'implementazione del riuso e lavoro separato.
- `src/AI/Rystem.PlayFramework.Adapters/MultiModalChatClient.cs`
  - ricompilare contro `OpenAI 2.12.0`; nessuna modifica comportamentale prevista.
- `src/AI/Rystem.PlayFramework.Adapters/SpeechToTextChatClient.cs`
  - ricompilare; propagare il `CancellationToken` a `:94`.
- `src/AI/Rystem.PlayFramework.Adapters/AzureOpenAIVoiceAdapter.cs`
  - ricompilare e validare STT/TTS.
- `src/AI/Rystem.PlayFramework.Adapters/README.md`
  - aggiornare dipendenze, endpoint, autenticazione e migrazione dal companion SDK.

### 8.2 Test

Decisione presa, non piu aperta: xunit 2.9.3 non offre skip dinamico, quindi le suite live usano un **`FactAttribute` custom** che imposta `Skip` in base a variabile d'ambiente. Nessuna nuova dipendenza, nessuna modifica al runner.

```csharp
// src/AI/Test/Rystem.PlayFramework.Test/Infrastructure/AzureLiveFactAttribute.cs
internal sealed class AzureLiveFactAttribute : FactAttribute
{
    public AzureLiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RYSTEM_RUN_AZURE_OPENAI_INTEGRATION") != "1")
            Skip = "Set RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1 to run Azure live tests.";
    }
}
```

Le suite si selezionano con `[Trait("Category", "AzureOpenAIApiKey")]` e `[Trait("Category", "AzureOpenAIEntra")]`, oggi assenti dal progetto.

- `src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj`
  - rimuovere `Azure.AI.OpenAI` (`:12`);
  - aggiungere `OpenAI 2.12.0`;
  - aggiungere `ProjectReference` a `Rystem.PlayFramework.Adapters`. I test girano in Debug, dove l'adapter risolve il core via `ProjectReference`: nessuna doppia risoluzione.
- `Infrastructure/AzureOpenAIChatClientAdapter.cs`
  - eliminarlo dal percorso dei test di integrazione: usa `AzureKeyCredential` e `GetChatClient` (`:25-26`), quindi non copre ne Responses ne Entra ID;
  - prerequisito alla rimozione: due dei quattro test di `OpenAIAdapterCompatibilityTests` dipendono dal suo `internal static CreateChatCompletionOptions` (`:177-192`, usato a `OpenAIAdapterCompatibilityTests.cs:27` e `:53`). Riscriverli sulle estensioni pubbliche `AsOpenAIChatTool()` / `AsOpenAIResponseTool()`, gia usate dagli altri due test dello stesso file.
- `Infrastructure/PlayFrameworkTestBase.cs`
  - registrare `AddAdapterForAzureOpenAI` nei test opt-in;
  - rimuovere il fallback silenzioso a `MockChatClient` (`:56-59`), che oggi rende verde un test di integrazione senza mai contattare Azure;
  - supportare API key e Entra ID.
- `Tests/OpenAIAdapterCompatibilityTests.cs`
  - mantenere i test di declaration per Chat e Responses;
  - aggiungere smoke test di costruzione del client reale, senza rete;
  - normalizzare `#pragma warning disable OPENAI001` (`:81`, `restore` a `:91` fuori dal metodo).
- `Tests/AzureOpenAIIntegrationTests.cs`
  - sostituire i quattro `Skip` statici (`:51`, `:76`, `:118`, `:142`) con `AzureLiveFact` e i trait;
  - testare il production adapter.
- `Tests/AzureOpenAIEndpointTests.cs` (nuovo)
  - coprire tutte le righe della tabella 6.2, inclusi i casi di rifiuto.
- `Tests/OpenAIClientContractTests.cs` (nuovo)
  - contract test HTTP con transport di cattura via `OpenAIClientOptions.Transport`.

### 8.3 Sample e documentazione

- `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`
  - mantenere API key; consentire Managed Identity quando endpoint e deployment sono presenti ma la key e assente; mantenere la configurazione voice.
- `src/AI/Test/Rystem.PlayFramework.Api/appsettings.json` e README
  - aggiornare endpoint e istruzioni; nessun segreto.
- `src/AI/Rystem.PlayFramework/Extensions/ServiceCollectionExtensions_ChatClient.cs`
  - aggiornare gli esempi XML che costruiscono direttamente `AzureOpenAIClient`.
- `src/AI/Rystem.PlayFramework/README.md`
  - aggiornare esempi e note di dipendenza.

## 9. Piano di implementazione

Cinque fasi. Le prime quattro sono offline e deterministiche; la quinta e l'unica che richiede Azure.

### Fase 1 - Dipendenze e baseline

1. Aggiungere il `ProjectReference` all'adapter nel progetto di test.
2. Registrare la graph corrente con `dotnet list package --include-transitive` e allegarla al PR insieme allo stack trace della sezione 2.
3. Rimuovere `Azure.AI.OpenAI` da adapter e test; aggiungere `OpenAI` con range `[2.12.0]` a entrambi.
4. Aggiornare `NoWarn` e i metadati del package.
5. Eseguire restore con `--force --no-cache`.

Gate:

```text
OpenAI                                    2.12.0
Microsoft.Extensions.AI.OpenAI            10.9.0
System.ClientModel                        1.14.0
Azure.Core                                1.53.0
Azure.Identity                            1.21.0
Microsoft.Extensions.Caching.Abstractions 10.0.11
Microsoft.Extensions.Logging.Abstractions 10.0.11
Azure.AI.OpenAI                           assente
```

### Fase 2 - Endpoint e factory

1. Implementare `AzureOpenAIEndpoint.Normalize` secondo la tabella 6.2.
2. Implementare `OpenAIClientFactory.Create` con `EntraScope` centralizzato e il parametro `configure` per i test.
3. Scrivere gli unit test dell'endpoint prima di collegare la factory: sono puri e non richiedono nulla.
4. Non loggare API key, bearer token o query di autenticazione.

Deliverable: normalizzatore coperto al 100% dei casi della tabella; factory che produce un `OpenAIClient` per adapter.

### Fase 3 - Chat e Responses

1. Sostituire il tipo parent con `OpenAIClient` in `ServiceCollectionExtensions`.
2. Mantenere le due chiamate invariate nella forma:

   ```csharp
   openAIClient.GetResponsesClient().AsIChatClient(settings.Deployment)
   openAIClient.GetChatClient(settings.Deployment).AsIChatClient()
   ```

3. Conservare l'ordine dei wrapper:

   ```text
   OpenAI bridge
       -> MultiModalChatClient   (Responses + file upload)
       -> SpeechToTextChatClient (quando configurato)
       -> CostTrackingChatClient (outermost)
   ```

4. Scrivere i contract test HTTP e verificare contro il traffico documentato in 3.2: path, campo `model`, header di autenticazione, assenza di `api-version`.

Deliverable: `MissingMethodException` eliminata, verificabile offline.

### Fase 4 - Files, audio e test del production adapter

1. Ricompilare `MultiModalChatClient`; confermare che `IsUploadableFile` (`:223-231`) continui a escludere `image/*` e `audio/*` e che la cache a tre livelli (`:26-27`, `:161-191`) sia invariata.
2. Ricompilare `SpeechToTextChatClient` e il voice adapter; propagare il `CancellationToken` a `SpeechToTextChatClient.cs:94`.
3. Verificare che `GetAudioClient` produca client distinti per STT e TTS come oggi (`ServiceCollectionExtensions.cs:159-160`).
4. Introdurre `AzureLiveFactAttribute` e i trait.
5. Riscrivere i due test dipendenti da `AzureOpenAIChatClientAdapter` e rimuovere l'adapter di test dal percorso di integrazione.
6. Rimuovere il fallback a `MockChatClient` per i test live.
7. Eseguire l'intera suite offline.

Deliverable: i test esercitano lo stesso codice distribuito nel NuGet.

### Fase 5 - Gate live Azure

Unica fase che richiede una risorsa Azure. Prerequisito da verificare **prima** di iniziarla: esistenza, nella stessa risorsa, di deployment per chat, immagini, audio inline, Whisper e TTS. Se qualcuno manca, la matrice di accettazione della sezione 11 va rinegoziata prima, non a meta strada.

Ordine obbligatorio:

1. API key su Responses non streaming. Chiude A4. Se fallisce con 401, il client sta emettendo `Authorization: Bearer` dove Azure attende `api-key`: aggiungere una policy di riscrittura header confinata nella factory.
2. Entra ID su Responses non streaming. Chiude A3. Se 401/403, cambiare `EntraScope` in `https://cognitiveservices.azure.com/.default` e ripetere; se persiste, verificare il ruolo data-plane dell'identita.
3. Responses streaming, Chat non streaming, Chat streaming.
4. Function calling.
5. Files: list, upload con `FileUploadPurpose.Assistants` (`MultiModalChatClient.cs:148-149`) e uso del file in Responses. Chiude A7. Se la purpose e rifiutata, adottare quella accettata e dichiararla come modifica comportamentale nelle release notes.
6. Image input sui deployment compatibili. Il gate audio inline forte resta sospeso e tracciato separatamente finche il deployment configurato non elabora effettivamente l'`input_audio`; una risposta di rifiuto non viene considerata copertura valida.
7. STT e TTS. Chiude A6. Se il routing v1 non funziona, verificare la route con `api-version=preview`; usare endpoint deployment-specific solo se la risorsa lo richiede; aggiungere la policy `api-version` alla sola factory audio, condividendo credenziale e regola di autenticazione. Non reintrodurre `Azure.AI.OpenAI`.
8. Usage e cost mapping.

Regole per ogni test live: timeout, cancellation propagata, token e file di dimensione limitata, nessun log di segreti o contenuti, registrazione di request ID e status, serializzazione quando si condividono file o deployment, pulizia degli artefatti remoti creati.

Gate bloccanti:

- fallimento di 1 o 2: la migrazione non procede finche l'autenticazione non funziona;
- fallimento di 3 o 4: fermare e correggere;
- fallimento di 5, 7: Files, STT e TTS sono release blocker. O si risolve, o si approva esplicitamente una release breaking che rimuove la funzionalita e aggiorna obiettivi, API e documentazione.

Deliverable: matrice di accettazione della sezione 11 compilata; tabella 3.1 chiusa su A3, A6, A7.

## 10. Piano di test

### 10.1 Unit test offline

**Endpoint** — tutte le righe della tabella 6.2, casi di rifiuto inclusi, piu casing di host e path.

**Validation** — endpoint mancante; deployment mancante; API key mancante con `UseAzureCredential = false`; API key assente ammessa con `UseAzureCredential = true`; configurazione ambigua API key + `UseAzureCredential` rifiutata; override `api-version` STT vuoti rifiutati; STT deployment mancante; voice STT/TTS mancanti.

**Client construction** — API key crea `OpenAIClient` senza rete; Entra costruisce la pipeline con `BearerTokenPolicy` e lo scope previsto; Responses e Chat producono `IChatClient`; risoluzione named e default factory; ordine dei wrapper; cost wrapper presente solo se configurato.

**Tool compatibility** — declaration-only per Chat e per Responses; `AIFunction` invocabile; schema JSON preservato; round trip tool call/result su risposte simulate.

**Files** — classificazione uploadable/image/audio; filename da `Name` e da `AdditionalProperties`; estensione da media type; cache hit distribuita, memory e fallback; remote reuse; upload su miss; sostituzione con `HostedFileContent`; file multipli e contenuti misti; errori e cancellazione; preprocessing in streaming.

**Audio** — mapping estensioni; sostituzione audio con testo trascritto; contenuti non audio invariati; voice mapping; output format; speed ratio; language e duration; errori e cancellazione.

### 10.2 Contract test HTTP

Transport di cattura iniettato tramite `OpenAIClientOptions.Transport`, senza rete e senza credenziali. Riferimento atteso: il traffico documentato in 3.2.

- path `/openai/v1/responses` e `/openai/v1/chat/completions`;
- deployment nel campo `model` del payload;
- percorso API key: header `Authorization: Bearer <key>`, nessun `api-key`;
- percorso Entra: presenza dell'header bearer senza asserire o loggare il token;
- assenza di `api-version` su Responses e Chat;
- payload dei tool;
- richieste streaming;
- route Files e Audio scelte dalla factory;
- gestione 401, 403, 404, 429, 5xx;
- cancellazione.

### 10.3 Integration test Azure opt-in

```text
AZURE_OPENAI_ENDPOINT
AZURE_OPENAI_DEPLOYMENT
AZURE_OPENAI_API_KEY                    # suite API key
AZURE_OPENAI_STT_DEPLOYMENT             # suite audio
AZURE_OPENAI_TTS_DEPLOYMENT             # suite audio
RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1
```

Per Entra ID nessun secret nel repository: `DefaultAzureCredential` e identita con ruolo data-plane appropriato.

Contenuto delle suite: sequenza della Fase 5. La suite Entra include una verifica 403 con identita priva di ruolo, eseguita in un ambiente amministrato separatamente e non nella CI standard.

Se il deployment principale non supporta immagini o audio inline, configurare deployment dedicati. Uno skip e ammesso nella CI ordinaria con motivazione. Il gate audio inline resta un prerequisito di release da risolvere o accettare esplicitamente come limite noto; una risposta non vuota che rifiuta l'audio non costituisce un'esecuzione riuscita.

### 10.4 Package-consumer test

Progetto pulito che referenzia il `.nupkg` prodotto:

- restore senza warning di downgrade o version range;
- assenza di `Azure.AI.OpenAI`;
- `OpenAI 2.12.0`, `Microsoft.Extensions.AI.OpenAI 10.9.0`, `System.ClientModel 1.14.0`;
- un consumer che aggiunge esplicitamente `OpenAI 2.12.1` produce un errore di risoluzione riconoscibile, non un downgrade silenzioso;
- compilazione dei metodi pubblici Rystem e dei costruttori che espongono `OpenAIFileClient` e `AudioClient`;
- API compatibility report rispetto al package precedente;
- risoluzione DI degli adapter Responses e Chat;
- avvio senza `MissingMethodException`.

### 10.5 Regressione PlayFramework

Intera suite `Rystem.PlayFramework.Test`; test factory pattern; runtime description e tool declaration; streaming; multimodal; cost tracking e budget; client interaction; build e test Foundry Local applicabili; build del sample API.

## 11. Matrice di accettazione

| Area | Offline | Azure API key | Azure Entra | Package consumer |
| --- | --- | --- | --- | --- |
| Endpoint normalization | obbligatorio | implicito | implicito | obbligatorio |
| Client creation | obbligatorio | obbligatorio | obbligatorio | obbligatorio |
| Responses non streaming | contract test | obbligatorio | obbligatorio | startup |
| Responses streaming | contract test | obbligatorio | raccomandato | startup |
| Chat non streaming | contract test | obbligatorio | obbligatorio | startup |
| Chat streaming | contract test | obbligatorio | raccomandato | startup |
| Tool calling | obbligatorio | obbligatorio | raccomandato | compilazione |
| Files | obbligatorio | obbligatorio | raccomandato | compilazione |
| Image input | simulato | obbligatorio su modello compatibile | raccomandato | compilazione |
| Audio inline MultiModal | simulato | obbligatorio su modello compatibile | raccomandato | compilazione |
| STT | obbligatorio | obbligatorio | raccomandato | compilazione |
| TTS | obbligatorio | obbligatorio | raccomandato | compilazione |
| Usage/cost | obbligatorio | obbligatorio | raccomandato | compilazione |
| Assenza `Azure.AI.OpenAI` | graph | graph | graph | obbligatorio |
| Assenza `MissingMethodException` | smoke | obbligatorio | obbligatorio | obbligatorio |

## 12. Mitigazione immediata e rollback

### 12.1 Mitigazione senza rilascio

La riproduzione della sezione 2 dimostra che il difetto colpisce **solo** la Responses API. Un consumer bloccato oggi puo sbloccarsi con una modifica di configurazione, senza attendere questa migrazione:

```csharp
settings.UseResponsesApi = false;
```

Costo: si perde il percorso Responses e, di conseguenza, il file upload automatico, che `ServiceCollectionExtensions.cs:66` attiva solo con `UseResponsesApi && EnableFileUpload`. Restano funzionanti chat, streaming, tool calling, STT via `AudioMode.SpeechToText` e cost tracking.

Questa mitigazione va comunicata nelle release notes e nella documentazione: e la rete di sicurezza che il piano non aveva.

### 12.2 Rollback dopo il rilascio

Non esiste una versione pubblicata dell'adapter con una graph priva del difetto, quindi "tornare alla versione precedente" non e una strategia. Le opzioni reali, in ordine di preferenza:

1. impostare `UseResponsesApi = false` nel consumer, come sopra: nessun redeploy del package;
2. ripristinare l'artefatto applicativo del consumer precedente, se la sua graph e nota e funzionante;
3. pubblicare un hotfix con `Microsoft.Extensions.AI.OpenAI` a una versione il cui range di `OpenAI` sia compatibile con `Azure.AI.OpenAI 2.9.0-beta.1`, cioe inferiore a `10.9.0`.

L'opzione 1 rende superflua la preparazione anticipata dell'opzione 3.

Non introdurre un doppio percorso runtime `OpenAI`/`Azure.AI.OpenAI`: aumenterebbe la superficie di test e potrebbe ricaricare assembly incompatibili nello stesso processo.

Attivare il rollback in presenza di: nuova `MissingMethodException` o `TypeLoadException`; regressione sistematica Responses/Chat; impossibilita di autenticazione con entrambe le modalita; perdita non accettata di Files/STT/TTS; regressione di costo o latenza oltre le soglie definite dall'applicazione consumer.

## 13. Comandi di verifica

Eseguire dalla root repository.

### Clean, restore e graph

```bash
dotnet clean "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" -c Debug
dotnet clean "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" -c Release
dotnet restore "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" --force --no-cache
dotnet list "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" package --include-transitive
```

### Build mirate

```bash
dotnet build "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" -c Debug --no-restore -p:GeneratePackageOnBuild=false
dotnet build "src/AI/Rystem.PlayFramework.Adapters.FoundryLocal/Rystem.PlayFramework.Adapters.FoundryLocal.csproj" -c Debug -p:GeneratePackageOnBuild=false
dotnet build "src/AI/Test/Rystem.PlayFramework.Api/Rystem.PlayFramework.Api.csproj" -c Debug -p:GeneratePackageOnBuild=false
```

Il build Release dell'adapter richiede `Rystem.PlayFramework 10.1.0-beta.5` risolvibile da un feed, per via del blocco `Choose` (`Rystem.PlayFramework.Adapters.csproj:47-51`).

### Test

```bash
dotnet restore "src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj" --force --no-cache
dotnet test "src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj" -c Debug --no-restore
dotnet test "src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj" -c Debug --no-restore \
  --filter "FullyQualifiedName~AzureOpenAIEndpointTests|FullyQualifiedName~OpenAIClientContractTests"
```

Suite live, dopo aver esportato le variabili della sezione 10.3:

```bash
RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1 dotnet test "src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj" \
  -c Debug --filter "Category=AzureOpenAIApiKey"
RYSTEM_RUN_AZURE_OPENAI_INTEGRATION=1 dotnet test "src/AI/Test/Rystem.PlayFramework.Test/Rystem.PlayFramework.Test.csproj" \
  -c Debug --filter "Category=AzureOpenAIEntra"
```

### Pack e package-consumer test

```bash
ARTIFACT_ROOT="$(mktemp -d)/rystem-adapter-validation"
PACKAGE_DIR="$ARTIFACT_ROOT/packages"
CONSUMER_DIR="$ARTIFACT_ROOT/consumer"
NUGET_PACKAGES="$ARTIFACT_ROOT/nuget"
NUGET_CONFIG="$ARTIFACT_ROOT/NuGet.Config"
mkdir -p "$PACKAGE_DIR" "$NUGET_PACKAGES"
cat > "$NUGET_CONFIG" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="migration-local" value="$PACKAGE_DIR" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
dotnet pack "src/AI/Rystem.PlayFramework/Rystem.PlayFramework.csproj" -c Release --output "$PACKAGE_DIR" --configfile "$NUGET_CONFIG"
dotnet pack "src/AI/Rystem.PlayFramework.Adapters/Rystem.PlayFramework.Adapters.csproj" -c Release --output "$PACKAGE_DIR" --configfile "$NUGET_CONFIG"
dotnet new console -n RystemAdapterConsumer -o "$CONSUMER_DIR" -f net10.0
NUGET_PACKAGES="$NUGET_PACKAGES" dotnet add "$CONSUMER_DIR/RystemAdapterConsumer.csproj" package Rystem.PlayFramework.Adapters --version "<versione-generata>" --no-restore
NUGET_PACKAGES="$NUGET_PACKAGES" dotnet restore "$CONSUMER_DIR/RystemAdapterConsumer.csproj" --configfile "$NUGET_CONFIG" --force --no-cache
NUGET_PACKAGES="$NUGET_PACKAGES" dotnet list "$CONSUMER_DIR/RystemAdapterConsumer.csproj" package --include-transitive
NUGET_PACKAGES="$NUGET_PACKAGES" dotnet build "$CONSUMER_DIR/RystemAdapterConsumer.csproj" --no-restore
```

Ispezionare il `.nuspec` incluso e verificare che il range `[2.12.0]` sia emesso letteralmente. Gli artefatti temporanei non vanno committati.

### Soluzione completa

```bash
dotnet restore "Rystem.sln" --force --no-cache
dotnet build "Rystem.sln" -c Debug --no-restore
```

## 14. Criteri di completamento

1. `Azure.AI.OpenAI` assente dal `.csproj` dell'adapter e da quello del progetto di test, dalla graph risolta e dal `.nuspec` pubblicato.
2. La graph risolta corrisponde al gate della Fase 1, senza warning di versione.
3. A3, A6 e A7 hanno esito documentato nel PR.
4. Gli endpoint Azure root preesistenti funzionano tramite normalizzazione, incluso il formato di `OpenAiSettings.cs:11`.
5. Responses e Chat funzionano in streaming e non streaming.
6. Function calling conserva nomi, descrizioni e schema.
7. Files, STT e TTS hanno test live riusciti su deployment compatibili, oppure la release e ripianificata come rimozione breaking approvata.
8. API key e Entra ID validate su Azure, con lo scope effettivo registrato in `OpenAIClientFactory` e nella documentazione.
9. Nessun test marcato live ricade su `MockChatClient`; i test di integrazione usano il production adapter.
10. Il package-consumer test passa.
11. La suite PlayFramework e le build dei consumer transitivi passano.
12. README, sample e XML docs aggiornati, incluse le correzioni ai commenti errati di `VoiceAdapterSettings.cs:10,13` e `AdapterSettings.cs:57-63`.
13. Release notes descrivono dipendenze, endpoint v1, la mitigazione `UseResponsesApi = false` e il vincolo introdotto dal range esatto `[2.12.0]`.

## 15. Rischi residui

### Scope Entra errato

**Risolto.** `https://ai.azure.com/.default` funziona per Chat/Responses; l'Audio richiede lo scope distinto `https://cognitiveservices.azure.com/.default`, centralizzato in `OpenAIClientFactory.AudioEntraScope` e coperto da `OpenAIClientContractTests.Audio_Entra_UsesConfiguredScopeAndBearerToken`.

### Header API key non accettato da Azure v1

**Non riscontrato.** Il client emette `Authorization: Bearer <key>` (misurato in 3.2) e Azure v1 lo accetta per Chat/Responses; nessuna policy di riscrittura header e stata necessaria su quel percorso. L'Audio invece richiede l'header `api-key` classico, gestito da `OpenAIClientFactory.CreateAudioClient` con `ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy`.

### Routing Audio su v1

**Risolto.** Le route Audio non sono raggiungibili su `/openai/v1/`: si usa l'endpoint deployment-specific `/openai/deployments/{deployment}` con una policy `api-version` dedicata (`AudioApiVersionPolicy`), verificato dai contract test e dai live gate `AzureOpenAIVoiceLiveGateTests`.

### Disponibilita Files/Responses per regione e modello

Impatto: il codice compila ma il servizio risponde 404/400. Mitigazione: verifica dei deployment prima della Fase 5; nessun fallback silenzioso. **Verificato in Fase 5** per Files (`AzureOpenAIFilesLiveGateTests`); dipende comunque dai deployment disponibili nella risorsa del consumer.

### Range esatto `[2.12.0]`

Impatto: rompe i consumer che referenziano `OpenAI 2.12.1` o successive. Mitigazione: release notes, beta release, consumer test; rimozione del pin tracciata come debito.

### Test non rappresentativi

Impatto: verde su mock, rosso in produzione. `PlayFrameworkTestBase.cs:56-59` oggi lo consente. Mitigazione: rimozione del fallback e package-consumer test. Risolto dalla Fase 4.

### Concorrenza preesistente in `MultiModalChatClient`

Fuori scope, ma la migrazione non la peggiora ne la nasconde. Tracciare come issue separata prima del rilascio, per non lasciarla implicita nelle release notes.

## 16. Sequenza dei commit

1. `test(playframework): reference adapter and capture Azure OpenAI SDK mismatch`
2. `build(adapters): move from Azure.AI.OpenAI to OpenAI 2.12`
3. `refactor(adapters): add Azure v1 endpoint normalizer and client factory`
4. `fix(adapters): route responses and chat through OpenAI client`
5. `test(adapters): add endpoint and HTTP contract coverage`
6. `test(adapters): run integration suite against the production adapter`
7. `test(adapters): verify packed NuGet consumer graph`
8. `docs(adapters): document direct OpenAI SDK migration and UseResponsesApi fallback`

Il commit 1 include il `ProjectReference` all'adapter, prerequisito della riproduzione. Ogni commit deve compilare. I test live girano da pipeline dedicata e non devono bloccare contributor privi di credenziali.

## Appendice A - Dipendenze fuori dal perimetro PlayFramework

Questi punti riguardano l'infrastruttura condivisa del repository. Vanno risolti prima della pubblicazione, ma non appartengono a questo intervento e non ne bloccano l'implementazione.

1. **Disallineamento versioni.** `.github/release-packages.json:2` e a `10.1.0-beta.3`, i `.csproj` interessati da questa migrazione a `10.1.0-beta.5`. Causa: `scripts/release/Prepare-Release.ps1:167-170` aggiorna il manifest solo con `Profile -eq 'all'`. Rilasciare con profilo `all` oppure aggiornare il manifest come step esplicito.
2. **Doppio publisher.** `PackageDeploy.Rystem.PlayFramework.Adapters.yml:6-8` si attiva su modifica del `.csproj`, che `Prepare-Release.ps1:106-120` modifica e `Release.AllPackages.yml:82-95` committa. Disabilitare il workflow dedicato e lasciare `Release.AllPackages.yml` come unico publisher.
3. **Ricostruzione degli artefatti.** `scripts/release/Publish-Package.ps1:66-75` esegue `dotnet build --force` seguito da `dotnet pack --no-build`, quindi il `.nupkg` pubblicato non e quello validato in QA. Decidere se modificare lo script o accettare e documentare la ricostruzione.
4. **Livello di release perso.** `Prepare-Release.ps1:179` emette `level_0..level_5`, `Release.AllPackages.yml:55-59` ne consuma cinque. Un grafo a sei livelli perderebbe l'ultimo. Il profilo `ai` ha tre package e non raggiunge la soglia.
5. **Documentazione MCP.** La copia generata `rystemapp/public/mcp/tools/get-rystem-docs/ai-playframework-adapters.md` e stata riallineata al README dell'adapter tramite `npm --prefix rystemapp run build-mcp`. Gli altri artefatti gia disallineati prodotti dalla rigenerazione completa sono stati esclusi dalla PR per non introdurre modifiche documentali estranee alla migrazione.
