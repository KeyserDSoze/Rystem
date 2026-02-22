# 🎮 Rystem PlayFramework Client

[![npm version](https://img.shields.io/npm/v/@rystem/playframework-client.svg)](https://www.npmjs.com/package/@rystem/playframework-client)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.7%2B-blue?logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

> **Official TypeScript/JavaScript client for [Rystem PlayFramework](https://rystem.net) HTTP API**

Production-ready client with full support for:
- ✅ **Step-by-step streaming** (SSE) - Each scene/actor as separate event
- ✅ **Token-level streaming** (SSE) - Real-time text chunks
- ✅ **Client-side tools** - Camera, geolocation, file picker, microphone
- ✅ **Multi-modal content** - Images, audio, video, PDFs, URIs
- ✅ **React hooks** - `usePlayFramework()` for easy integration
- ✅ **Execution modes** - Direct, Planning, DynamicChaining, Scene
- ✅ **Configurable** - Headers, retry, error handling, timeouts
- ✅ **Type-safe** - Full TypeScript definitions

---

## 📦 Installation

```bash
npm install @rystem/playframework-client
```

Or with yarn:
```bash
yarn add @rystem/playframework-client
```

---

## 🚀 Quick Start

### 1. Configure Client

```typescript
import { PlayFrameworkServices } from "@rystem/playframework-client";

// Configure factory (once at app startup)
await PlayFrameworkServices.configure("default", "http://localhost:5158/api/ai", settings => {
    settings.timeout = 120_000; // 2 minutes
    settings.maxReconnectAttempts = 3;
    settings.reconnectBaseDelay = 1000;
});
```

### 2. Use Client (Vanilla JS/TS)

```typescript
const client = PlayFrameworkServices.resolve("default");

// Step-by-step streaming
for await (const step of client.executeStepByStep({
    message: "Tell me a joke"
})) {
    console.log(`[${step.status}] ${step.message}`);
}
// Output:
// [planning] Analyzing request...
// [executingScene] Generating joke...
// [completed] Why did the chicken cross the road? To get to the other side!

// Token-level streaming (real-time chunks)
for await (const chunk of client.executeTokenStreaming({
    message: "Write a short story"
})) {
    process.stdout.write(chunk.streamingChunk || "");
}
// Output: "Once" → " upon" → " a" → " time" → "..."
```

### 3. Use with React

```tsx
import { usePlayFramework } from "@rystem/playframework-client";
import { useState } from "react";

function ChatComponent() {
    const client = usePlayFramework("default");
    const [messages, setMessages] = useState<string[]>([]);
    const [loading, setLoading] = useState(false);

    const handleSend = async (input: string) => {
        setLoading(true);
        const newMessages: string[] = [];

        try {
            for await (const step of client.executeStepByStep({
                message: input
            })) {
                if (step.streamingChunk) {
                    // Real-time token streaming
                    newMessages.push(step.streamingChunk);
                } else if (step.message) {
                    // Step-by-step message
                    newMessages.push(`[${step.status}] ${step.message}`);
                }
                setMessages([...newMessages]);
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <input onKeyDown={e => e.key === 'Enter' && handleSend(e.currentTarget.value)} />
            <div>
                {messages.map((msg, i) => <div key={i}>{msg}</div>)}
            </div>
        </div>
    );
}
```

---

## 🎯 Core Features

### Step-by-Step vs Token Streaming

**Step-by-Step** (`executeStepByStep`):
- Returns each **scene/actor execution** as separate event
- Best for: Multi-step workflows, debugging, progress tracking
- Example: Planning → Actor1 → Actor2 → Completed

**Token Streaming** (`executeTokenStreaming`):
- Returns **individual text chunks** as they're generated
- Best for: Real-time chat UIs, typewriter effects
- Example: "Hello" → " world" → "!" → "..."

```typescript
// Step-by-step - structured workflow
for await (const step of client.executeStepByStep({ message: "..." })) {
    console.log(step.status);        // "planning", "executingScene", "completed"
    console.log(step.sceneName);     // "ChatScene"
    console.log(step.message);       // Full response
}

// Token streaming - real-time chunks
for await (const chunk of client.executeTokenStreaming({ message: "..." })) {
    console.log(chunk.streamingChunk); // "Hello", " world", "!"
}
```

---

## 🛠️ Client-Side Tools

Execute **browser-specific operations** (camera, geolocation, file picker) when LLM requests them.

### Server Setup (C#)

```csharp
services.AddPlayFramework(builder =>
{
    builder.AddScene("vision", "Analyze user photos", scene =>
    {
        scene.OnClient(client =>
        {
            client.AddTool("capturePhoto", "Take photo from camera");
            client.AddTool("getCurrentLocation", "Get GPS coordinates");
            client.AddTool("selectFiles", "Open file picker");
        });
    });
});
```

### Client Setup (TypeScript)

```typescript
import { PlayFrameworkServices, AIContentConverter } from "@rystem/playframework-client";

const client = PlayFrameworkServices.resolve("default");
const registry = client.getClientRegistry();

// Register camera tool
registry.register("capturePhoto", async () => {
    const content = await AIContentConverter.fromCamera(
        { video: { facingMode: "environment" } }, // Rear camera
        1920, 1080
    );
    return [content];
});

// Register geolocation tool
registry.register("getCurrentLocation", async () => {
    const content = await AIContentConverter.fromGeolocation({ timeout: 10_000 });
    return [content];
});

// Register file picker
registry.register("selectFiles", async (args?: { accept?: string }) => {
    return new Promise((resolve) => {
        const input = document.createElement("input");
        input.type = "file";
        input.accept = args?.accept || "*/*";
        input.onchange = async () => {
            const contents = await AIContentConverter.fromMultipleFiles(input.files!);
            resolve(contents);
        };
        input.click();
    });
});
```

**Client automatically resumes execution after tool completion!**

```typescript
for await (const step of client.executeStepByStep({ message: "Take a photo and describe it" })) {
    console.log(step.status);
    // 1. "executingScene" - LLM requests camera
    // 2. "awaitingClient" - Client executes tool
    // 3. "executingScene" - LLM analyzes photo
    // 4. "completed" - Final response
}
```

### AIContentConverter Helpers

```typescript
// Camera (returns Base64 JPEG)
const photo = await AIContentConverter.fromCamera();

// Geolocation (returns JSON with lat/lng)
const location = await AIContentConverter.fromGeolocation();

// File upload (returns Base64 data)
const file = await AIContentConverter.fromFile(fileInput.files[0]);

// Multiple files
const files = await AIContentConverter.fromMultipleFiles(fileInput.files);

// Microphone (returns Base64 audio)
const audio = await AIContentConverter.fromMicrophone(5000); // 5 seconds

// Plain text
const text = AIContentConverter.fromText("Hello world");
```

---

## 🎛️ Execution Modes

Control **how scenes are selected and executed**:

```typescript
import { PlayFrameworkRequest } from "@rystem/playframework-client";

const request: PlayFrameworkRequest = {
    message: "Book a flight to Paris",
    settings: {
        executionMode: "Planning", // Direct | Planning | DynamicChaining | Scene
        maxRecursionDepth: 5,
        enableSummarization: true,
        enableDirector: false
    }
};
```

### Available Modes

| Mode | Description | Use Case |
|------|-------------|----------|
| `Direct` | Single scene, no planning | Simple queries, fast responses |
| `Planning` | Upfront multi-step plan | Known workflows (booking, checkout) |
| `DynamicChaining` | LLM decides next step live | Exploratory tasks (research, debugging) |
| `Scene` | Execute specific scene by name | Resuming after client interaction |

---

## 📸 Multi-Modal Content

Send **images, audio, video, PDFs, URIs** with your request:

```typescript
import { PlayFrameworkRequest, ContentItem } from "@rystem/playframework-client";

const request: PlayFrameworkRequest = {
    message: "Describe this image and summarize the PDF",
    contents: [
        {
            type: "image",
            base64Data: "iVBORw0KGgoAAAANSUhEUgAA...",
            mediaType: "image/png"
        },
        {
            type: "data",
            base64Data: "JVBERi0xLjQKJeLjz9M...",
            mediaType: "application/pdf"
        },
        {
            type: "uri",
            uri: "https://example.com/document.pdf",
            mediaType: "application/pdf"
        },
        {
            type: "text",
            text: "Additional context"
        }
    ]
};

for await (const step of client.executeStepByStep(request)) {
    console.log(step.message);
}
```

---

## ⚙️ Configuration & Settings

### Global Configuration

```typescript
import { PlayFrameworkServices } from "@rystem/playframework-client";

await PlayFrameworkServices.configure("premium", "https://api.example.com/api/ai", settings => {
    // Timeout (ms)
    settings.timeout = 120_000; // 2 minutes

    // Reconnection on SSE failures
    settings.maxReconnectAttempts = 3;
    settings.reconnectBaseDelay = 1000; // 1 second

    // Default headers
    settings.defaultHeaders = {
        "Content-Type": "application/json",
        "X-App-Version": "1.0.0"
    };

    // Dynamic headers (auth token)
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
            await refreshAuthToken();
            return true; // Retry
        }
        return false; // Don't retry
    });
});
```

### Per-Request Settings

```typescript
const request: PlayFrameworkRequest = {
    message: "Your query",
    settings: {
        // Execution mode
        executionMode: "Planning",              // Direct | Planning | DynamicChaining | Scene

        // Planning settings
        maxRecursionDepth: 5,                   // Max planning depth
        maxDynamicScenes: 10,                   // Max scenes in DynamicChaining

        // Features
        enableSummarization: true,              // Auto-summarize long contexts
        enableDirector: false,                  // Multi-scene orchestration
        enableStreaming: true,                  // Token-level streaming

        // Model overrides
        modelId: "gpt-4o",                      // Override default model
        temperature: 0.7,                       // 0.0 - 2.0
        maxTokens: 4096,                        // Max response tokens

        // Caching
        cacheBehavior: "Default",               // Default | Avoidable | Forever
        conversationKey: "user-123-session-1",  // Unique conversation ID

        // Budget
        maxBudget: 0.50,                        // Max cost in USD (null = unlimited)

        // Scene selection (Scene mode only)
        sceneName: "SpecificScene"              // Execute specific scene
    },
    metadata: {
        userId: "user-123",
        sessionId: "session-abc",
        customKey: "customValue"
    }
};
```

---

## 🔄 Conversation State & Caching

Use `conversationKey` to maintain **multi-turn conversations**:

```typescript
const conversationKey = crypto.randomUUID();

// First request
for await (const step of client.executeStepByStep({
    message: "What's the weather in Paris?",
    settings: { conversationKey }
})) {
    console.log(step.message);
}

// Follow-up request (uses cached context)
for await (const step of client.executeStepByStep({
    message: "And in London?",
    settings: { conversationKey }
})) {
    console.log(step.message); // LLM remembers Paris context
}
```

---

## 🚫 Cancellation

Use `AbortController` to **cancel ongoing requests**:

```typescript
const controller = new AbortController();

// Start streaming
const promise = (async () => {
    try {
        for await (const step of client.executeStepByStep(
            { message: "Long task..." },
            controller.signal
        )) {
            console.log(step.message);
        }
    } catch (error) {
        if (error instanceof DOMException && error.name === "AbortError") {
            console.log("Request cancelled");
        }
    }
})();

// Cancel after 5 seconds
setTimeout(() => controller.abort(), 5000);
```

---

## 📊 Response Status Types

```typescript
type AiResponseStatus =
    | "initializing"        // Starting execution
    | "planning"            // Creating execution plan
    | "executingScene"      // Running scene/actor
    | "functionRequest"     // Calling server-side tool
    | "functionCompleted"   // Tool call completed
    | "streaming"           // Streaming text chunks
    | "awaitingClient"      // Waiting for client-side tool
    | "completed"           // Success
    | "error"               // Failure
    | "budgetExceeded";     // Max cost exceeded
```

---

## 🧪 Testing

```typescript
import { PlayFrameworkClient, PlayFrameworkSettings } from "@rystem/playframework-client";

describe("PlayFramework Client", () => {
    it("should stream responses", async () => {
        const settings = new PlayFrameworkSettings("default", "http://localhost:5158/api/ai");
        const client = new PlayFrameworkClient(settings);

        const responses: string[] = [];

        for await (const step of client.executeStepByStep({ message: "Test" })) {
            if (step.message) responses.push(step.message);
        }

        expect(responses.length).toBeGreaterThan(0);
    });
});
```

---

## 📚 API Reference

### `PlayFrameworkServices`

#### `configure(name: string, baseUrl: string, configure?: (settings: PlayFrameworkSettings) => void): Promise<PlayFrameworkSettings>`

Configure a PlayFramework client factory.

```typescript
await PlayFrameworkServices.configure("chat", "http://localhost:5158/api/ai");
```

#### `resolve(name: string): PlayFrameworkClient`

Get configured client instance.

```typescript
const client = PlayFrameworkServices.resolve("chat");
```

---

### `PlayFrameworkClient`

#### `executeStepByStep(request: PlayFrameworkRequest, signal?: AbortSignal): AsyncIterableIterator<AiSceneResponse>`

Execute with **step-by-step streaming** (each scene as separate event).

#### `executeTokenStreaming(request: PlayFrameworkRequest, signal?: AbortSignal): AsyncIterableIterator<AiSceneResponse>`

Execute with **token-level streaming** (each text chunk as separate event).

#### `getClientRegistry(): ClientInteractionRegistry`

Get registry for client-side tools.

---

### `ClientInteractionRegistry`

#### `register<TArgs = any>(toolName: string, handler: ClientTool<TArgs>): void`

Register client-side tool.

```typescript
registry.register("myTool", async (args?: { key: string }) => {
    return [AIContentConverter.fromText(args?.key || "default")];
});
```

---

### `AIContentConverter`

#### Static Methods

- `fromCamera(constraints?, width?, height?): Promise<AIContent>` - Capture photo
- `fromGeolocation(options?): Promise<AIContent>` - Get GPS coordinates
- `fromFile(file: File | Blob): Promise<AIContent>` - Convert file to Base64
- `fromMultipleFiles(files: FileList): Promise<AIContent[]>` - Convert multiple files
- `fromMicrophone(durationMs?: number, mimeType?: string): Promise<AIContent>` - Record audio
- `fromText(text: string): AIContent` - Create text content

---

## 🎨 React Hook (`usePlayFramework`)

```tsx
import { usePlayFramework } from "@rystem/playframework-client";

function MyComponent() {
    const client = usePlayFramework("default");

    const handleClick = async () => {
        for await (const step of client.executeStepByStep({ message: "Hello" })) {
            console.log(step);
        }
    };

    return <button onClick={handleClick}>Send</button>;
}
```

---

## 📁 Where to Place This README

This README should be placed in:
```
src/AI/Rystem.PlayFramework.Client/src/rystem/README.md
```

This is the **library source** directory that gets published to npm.

---

## 🔗 Links

- 📖 **Documentation**: https://rystem.net/playframework
- 💻 **GitHub**: https://github.com/KeyserDSoze/Rystem
- 📦 **npm**: https://www.npmjs.com/package/@rystem/playframework-client
- 🎯 **MCP Tools**: https://rystem.net/mcp

---

## 📄 License

MIT © [Alessandro Rapiti](https://github.com/KeyserDSoze)

---

## 🤝 Contributing

Contributions welcome! Please open an issue or PR.

1. Fork the repo
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 💡 Examples

### Complete Chat Application

```tsx
import React, { useState, useRef, useEffect } from 'react';
import { usePlayFramework, AIContentConverter } from '@rystem/playframework-client';

interface Message {
    role: 'user' | 'assistant';
    text: string;
    status?: string;
}

function ChatApp() {
    const client = usePlayFramework('default');
    const [messages, setMessages] = useState<Message[]>([]);
    const [input, setInput] = useState('');
    const [loading, setLoading] = useState(false);
    const [conversationKey] = useState(crypto.randomUUID());

    // Register client tools
    useEffect(() => {
        const registry = client.getClientRegistry();

        registry.register('getCurrentLocation', async () => {
            const content = await AIContentConverter.fromGeolocation();
            return [content];
        });

        registry.register('getUserConfirmation', async (args?: { question?: string }) => {
            const confirmed = window.confirm(args?.question ?? 'Do you confirm?');
            return [AIContentConverter.fromText(confirmed ? 'confirmed' : 'denied')];
        });
    }, [client]);

    const handleSend = async () => {
        if (!input.trim() || loading) return;

        const userMessage: Message = { role: 'user', text: input };
        setMessages(prev => [...prev, userMessage]);
        setInput('');
        setLoading(true);

        try {
            const assistantMessage: Message = { role: 'assistant', text: '', status: 'initializing' };
            setMessages(prev => [...prev, assistantMessage]);

            for await (const step of client.executeStepByStep({
                message: input,
                settings: {
                    conversationKey,
                    executionMode: 'Direct',
                    enableStreaming: true
                }
            })) {
                setMessages(prev => {
                    const updated = [...prev];
                    const last = updated[updated.length - 1];

                    if (step.streamingChunk) {
                        last.text += step.streamingChunk;
                    } else if (step.message) {
                        last.text = step.message;
                    }
                    last.status = step.status;

                    return updated;
                });
            }
        } catch (error) {
            console.error('Chat error:', error);
            setMessages(prev => [...prev, {
                role: 'assistant',
                text: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`,
                status: 'error'
            }]);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{ maxWidth: '800px', margin: '0 auto', padding: '20px' }}>
            <h1>PlayFramework Chat</h1>

            <div style={{ height: '500px', overflowY: 'auto', border: '1px solid #ccc', padding: '10px', marginBottom: '10px' }}>
                {messages.map((msg, i) => (
                    <div key={i} style={{
                        padding: '10px',
                        margin: '5px 0',
                        borderRadius: '8px',
                        backgroundColor: msg.role === 'user' ? '#e3f2fd' : '#f5f5f5',
                        textAlign: msg.role === 'user' ? 'right' : 'left'
                    }}>
                        <strong>{msg.role === 'user' ? 'You' : 'AI'}</strong>
                        {msg.status && <small style={{ color: '#666', marginLeft: '8px' }}>[{msg.status}]</small>}
                        <div>{msg.text}</div>
                    </div>
                ))}
            </div>

            <div style={{ display: 'flex', gap: '10px' }}>
                <input
                    type="text"
                    value={input}
                    onChange={e => setInput(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && handleSend()}
                    placeholder="Type a message..."
                    disabled={loading}
                    style={{ flex: 1, padding: '10px', fontSize: '16px' }}
                />
                <button onClick={handleSend} disabled={loading} style={{ padding: '10px 20px', fontSize: '16px' }}>
                    {loading ? 'Sending...' : 'Send'}
                </button>
            </div>
        </div>
    );
}

export default ChatApp;
```

---

**Made with ❤️ by the Rystem team**
