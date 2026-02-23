# Rystem PlayFramework Test API

API .NET per testare il **Rystem PlayFramework** con il client TypeScript.

## 🚀 Quick Start

### 1. Configura Azure OpenAI (Opzionale)

Per usare risposte AI reali, configura le credenziali Azure OpenAI:

**Opzione A: User Secrets (Raccomandato per development)**

```bash
cd src/AI/Test/Rystem.PlayFramework.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "<your-api-key>"
dotnet user-secrets set "AzureOpenAI:Deployment" "gpt-4o"
```

**Opzione B: appsettings.Development.json**

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "Key": "<your-api-key>",
    "Deployment": "gpt-4o"
  }
}
```

> ⚠️ **IMPORTANTE**: Non committare mai le API keys! Usa User Secrets o variabili d'ambiente.

### 2. Avvia l'API

```bash
cd src/AI/Test/Rystem.PlayFramework.Api
dotnet run
```

L'API sarà disponibile su:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

### 3. Test con il Client TypeScript

Apri un altro terminale e avvia il client React:

```bash
cd src/AI/Rystem.PlayFramework.Client
npm start
```

L'app sarà disponibile su http://localhost:3000 (o altra porta se occupata).

---

## 📡 Endpoints

### Step-by-Step Streaming
```
POST /api/ai/default
Content-Type: application/json

{
  "prompt": "Hello, how are you?",
  "sceneName": "Chat"
}
```

**Risponde con SSE**: Ogni step del PlayFramework come evento separato (Planning, Running, Completed).

### Token-Level Streaming
```
POST /api/ai/default/streaming
Content-Type: application/json

{
  "prompt": "Tell me a story",
  "sceneName": "Chat"
}
```

**Risponde con SSE**: Ogni chunk di testo come evento separato (più granulare).

### Conversation Management (se Repository abilitato)

#### List Conversations
```
GET /api/ai/default/conversations?searchText=weather&orderBy=TimestampDescending&take=20
```

Ottieni lista conversazioni con filtri:
- `searchText` - Cerca nei messaggi
- `includePublic` - Includi conversazioni pubbliche (default: `true`)
- `includePrivate` - Includi conversazioni private (default: `true`)
- `orderBy` - Ordinamento: `TimestampDescending` | `TimestampAscending`
- `skip` - Offset paginazione
- `take` - Dimensione pagina

#### Get Conversation
```
GET /api/ai/default/conversations/{conversationKey}
```

Ottieni singola conversazione (richiede autorizzazione per private).

#### Delete Conversation
```
DELETE /api/ai/default/conversations/{conversationKey}
```

Elimina conversazione (solo owner).

#### Update Visibility
```
PATCH /api/ai/default/conversations/{conversationKey}/visibility
Content-Type: application/json

{
  "isPublic": true
}
```

Cambia visibilità pubblico/privato (solo owner).

> **Nota**: Per abilitare questi endpoints, aggiungi `.UseRepository()` nella configurazione PlayFramework in `Program.cs`.

### Health Check
```
GET /health
```

Verifica che l'API sia online.

---

## 🎭 Scene Configurate

### **Chat** (default)
Conversazione generale e risposta a domande con AI assistant amichevole.

**Actors configurati:**
- ✅ Risposte chiare, concise e accurate
- ✅ Conversazione amichevole e coinvolgente  
- ✅ Ammette onestamente quando non sa qualcosa

**Esempio:**
```json
{
  "prompt": "What is the capital of France?",
  "sceneName": "Chat"
}
```

---

## 🔧 Configurazione

### CORS

CORS è configurato per permettere richieste da:
- `http://localhost:3000` (Create React App default)
- `http://localhost:5173` (Vite default)
- `http://localhost:5174` (Vite alternativo)

Per aggiungere altre origini, modifica `Program.cs`:

```csharp
policy.WithOrigins("http://localhost:3000", "http://localhost:YOUR_PORT")
```

### PlayFramework Settings

Modifica le impostazioni in `Program.cs`:

```csharp
.Configure(settings =>
{
    settings.Planning.Enabled = true;           // Planning automatico
    settings.Summarization.Enabled = false;     // Disabilita summarization
    settings.MaxTokenBudget = 4000;            // Budget massimo token
})
```

---

## 🧪 Testing

### Con cURL

**Step-by-Step:**
```bash
curl -X POST https://localhost:5001/api/ai/default \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Hello!","sceneName":"Chat"}' \
  --insecure
```

**Token Streaming:**
```bash
curl -X POST https://localhost:5001/api/ai/default/streaming \
  -H "Content-Type: application/json" \
  -d '{"prompt":"Tell me a joke","sceneName":"Chat"}' \
  --insecure
```

### Con il Client TypeScript

Vedi [Rystem.PlayFramework.Client README](../../Rystem.PlayFramework.Client/src/rystem/README.md)

---

## 🐛 Troubleshooting

### "Azure OpenAI credentials not configured"

Se non hai configurato Azure OpenAI, l'API userà un **MockChatClient** che risponde con testo mock. Le funzionalità SSE e streaming funzionano comunque per testare l'integrazione.

### CORS errors nel browser

Verifica che l'origine del client sia nelle allowed origins in `Program.cs`.

### Certificate SSL errors

Per testing locale, usa `--insecure` con curl o accetta il certificato self-signed nel browser.

---

## 📚 Risorse

- **PlayFramework Documentation**: [src/AI/Rystem.PlayFramework/README.md](../../Rystem.PlayFramework/README.md)
- **TypeScript Client**: [src/AI/Rystem.PlayFramework.Client/src/rystem/README.md](../../Rystem.PlayFramework.Client/src/rystem/README.md)
- **API Examples**: [AUTHORIZATION_EXAMPLE.md](../../Rystem.PlayFramework/Api/AUTHORIZATION_EXAMPLE.md)

---

## 🎯 Next Steps

1. ✅ Aggiungi più Scene per casi d'uso specifici
2. ✅ Configura autenticazione/autorizzazione per production
3. ✅ Aggiungi Tools/MCP per funzionalità avanzate
4. ✅ Implementa caching per ottimizzare performance
5. ✅ Aggiungi logging e monitoring

---

**Happy coding!** 🚀
