# Rystem PlayFramework Client

TypeScript/JavaScript client for **Rystem PlayFramework HTTP API** with full support for:
- ✅ **Step-by-step streaming** (SSE) - Each PlayFramework step as separate event
- ✅ **Token-level streaming** (SSE) - Each text chunk as separate event
- ✅ **Multi-modal content** - Images, audio, video, files, URIs
- ✅ **React hooks** - Easy integration with React apps
- ✅ **Configurable** - Custom headers, retry logic, error handling
- ✅ **TypeScript** - Full type safety

---

## Installation

```bash
npm install rystem.playframework.client
```

---

## Quick Start

### 1. Configure Client

```typescript
import { PlayFrameworkServices } from "rystem.playframework.client";

// Configure factory
PlayFrameworkServices.configure("chat", "https://api.example.com/api/ai", settings => {
    // Add Authorization header
    settings.addHeadersEnricher(async (url, method, headers, body) => {
        return {
            ...headers,
            "Authorization": `Bearer ${getToken()}`
        };
    });

    // Add retry logic
    settings.addErrorHandler(async (url, method, headers, body, error) => {
        console.error("Request failed, retrying...", error);
        return true; // Retry
    });
});
```

### 2. Use Client (Vanilla JS/TS)

```typescript
import { PlayFrameworkServices } from "rystem.playframework.client";

const client = PlayFrameworkServices.getClient("chat");

// Step-by-step streaming
for await (const step of client.executeStepByStep({
    prompt: "Tell me a joke",
    sceneName: "ChatScene"
})) {
    console.log(`[${step.status}] ${step.message}`);
    // Output:
    // [Planning] Analyzing request...
    // [Running] Generating joke...
    // [Running] Why did the chicken cross the road? To get to the other side!
}

// Token-level streaming (more granular)
for await (const chunk of client.executeTokenStreaming({
    prompt: "Tell me a story"
})) {
    process.stdout.write(chunk.message || "");
    // Output: "Once" → " upon" → " a" → " time" → "..."
}
```

### 3. Use with React

```tsx
import { usePlayFramework } from "rystem.playframework.client";
import { useState } from "react";

function ChatComponent() {
    const client = usePlayFramework("chat");
    const [messages, setMessages] = useState<string[]>([]);

    const handleSend = async () => {
        const newMessages: string[] = [];

        for await (const step of client.executeStepByStep({
            prompt: "Hello, how are you?",
            sceneName: "ChatScene"
        })) {
            if (step.message) {
                newMessages.push(`[${step.status}] ${step.message}`);
                setMessages([...newMessages]);
            }
        }
    };

    return (
        <div>
            <button onClick={handleSend}>Send</button>
            {messages.map((msg, i) => <div key={i}>{msg}</div>)}
        </div>
    );
}
```

---

## Multi-Modal Content

```typescript
import { PlayFrameworkRequest, ContentItem } from "rystem.playframework.client";

const request: PlayFrameworkRequest = {
    prompt: "Describe this image",
    sceneName: "VisionScene",
    contents: [
        {
            type: "image",
            base64Data: "iVBORw0KGgoAAAANSUhEUgAA...",
            mediaType: "image/png"
        },
        {
            type: "text",
            text: "Additional context"
        },
        {
            type: "uri",
            uri: "https://example.com/document.pdf",
            mediaType: "application/pdf"
        }
    ]
};

for await (const step of client.executeStepByStep(request)) {
    console.log(step.message);
}
```

---

## Configuration Options

```typescript
PlayFrameworkServices.configure("premium", "https://api.example.com/api/ai/premium", settings => {
    // Factory name (used in URL)
    settings.factoryName; // "premium"

    // Base URL
    settings.baseUrl; // "https://api.example.com/api/ai/premium"

    // Default headers
    settings.defaultHeaders = {
        "Content-Type": "application/json",
        "X-Custom-Header": "value"
    };

    // Timeout (ms)
    settings.timeout = 120000; // 2 minutes

    // Add dynamic headers (e.g., auth token)
    settings.addHeadersEnricher(async (url, method, headers, body) => {
        const token = await getAuthToken();
        return {
            ...headers,
            "Authorization": `Bearer ${token}`
        };
    });

    // Error handling with retry
    settings.addErrorHandler(async (url, method, headers, body, error) => {
        if (error.message.includes("401")) {
            await refreshToken();
            return true; // Retry
        }
        return false; // Don't retry
    });
});
```

---

## Cancellation

```typescript
const controller = new AbortController();

// Start streaming
const promise = (async () => {
    for await (const step of client.executeStepByStep(
        { prompt: "Long task" },
        controller.signal // Pass AbortSignal
    )) {
        console.log(step.message);
    }
})();

// Cancel after 5 seconds
setTimeout(() => controller.abort(), 5000);
```

---

## API Reference

### PlayFrameworkClient

#### `executeStepByStep(request, signal?): AsyncIterableIterator<AiSceneResponse>`

Execute with **step-by-step streaming** (each PlayFramework step as SSE event).

**Returns:** Each step (Planning, Actor1, Actor2, etc.) as separate event.

#### `executeTokenStreaming(request, signal?): AsyncIterableIterator<AiSceneResponse>`

Execute with **token-level streaming** (each text chunk as SSE event).

**Returns:** Each text chunk as separate event (more granular than step-by-step).

---

### PlayFrameworkServices

#### `configure(name, baseUrl, configure?): PlayFrameworkSettings`

Configure a PlayFramework client factory.

```typescript
PlayFrameworkServices.configure("chat", "https://api.example.com/api/ai");
```

#### `getClient(name): PlayFrameworkClient`

Get configured client.

```typescript
const client = PlayFrameworkServices.getClient("chat");
```

#### `getSettings(name): PlayFrameworkSettings`

Get settings for modification.

```typescript
const settings = PlayFrameworkServices.getSettings("chat");
settings.timeout = 30000;
```

---

### PlayFrameworkSettings

#### `addHeadersEnricher(enricher): this`

Add dynamic header enricher (e.g., Authorization).

#### `addErrorHandler(handler): this`

Add error handler for retry logic.

---

## License

MIT © Alessandro Rapiti
