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
            data: "iVBORw0KGgoAAAANSUhEUgAA...",
            mediaType: "image/png"
        },
        {
            type: "data",
            data: "JVBERi0xLjQKJeLjz9M...",
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

## 💾 Conversation Management (Repository Pattern)

If the backend has **Repository persistence enabled**, you can manage conversations using REST endpoints.

### List Conversations

```typescript
import { ConversationSortOrder } from "@rystem/playframework-client";

// Get conversations with filters
const conversations = await client.listConversations({
    searchText: "weather",                          // Search in message text
    includePublic: true,                            // Include public conversations
    includePrivate: true,                           // Include private conversations
    orderBy: ConversationSortOrder.TimestampDescending, // Sort by newest first
    skip: 0,                                        // Pagination offset
    take: 50                                        // Page size
});

console.log(conversations);
// [
//   {
//     conversationKey: "abc-123",
//     userId: "user@example.com",
//     isPublic: false,
//     timestamp: "2025-01-15T10:30:00Z",
//     messages: [...],
//     executionState: {...}
//   }
// ]
```

### Get Single Conversation

```typescript
const conversation = await client.getConversation("abc-123");

if (conversation) {
    console.log(conversation.messages); // Full message history
    console.log(conversation.isPublic); // Public vs private
} else {
    console.log("Conversation not found or unauthorized");
}
```

**Authorization**: Private conversations require userId match (set via backend `IAuthorizationLayer`).

### Delete Conversation

```typescript
// Owner-only operation
await client.deleteConversation("abc-123");
console.log("Conversation deleted");
```

**Response:**
- ✅ Success - Conversation deleted
- ❌ `403 Forbidden` - Not the owner
- ❌ `404 Not Found` - Conversation not found

### Update Visibility (Public/Private)

```typescript
// Toggle conversation visibility (owner-only)
const updated = await client.updateConversationVisibility("abc-123", true);
console.log(`Conversation is now ${updated.isPublic ? "public" : "private"}`);
```

### React Example: Conversation List UI

```tsx
import { useState, useEffect } from "react";
import { usePlayFramework, StoredConversation, ConversationSortOrder } from "@rystem/playframework-client";

function ConversationList() {
    const client = usePlayFramework("default");
    const [conversations, setConversations] = useState<StoredConversation[]>([]);
    const [searchText, setSearchText] = useState("");
    const [showPublic, setShowPublic] = useState(true);
    const [showPrivate, setShowPrivate] = useState(true);
    const [loading, setLoading] = useState(false);

    const loadConversations = async () => {
        setLoading(true);
        try {
            const result = await client.listConversations({
                searchText: searchText || undefined,
                includePublic: showPublic,
                includePrivate: showPrivate,
                orderBy: ConversationSortOrder.TimestampDescending,
                take: 100
            });
            setConversations(result);
        } catch (error) {
            console.error("Failed to load conversations:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadConversations();
    }, [showPublic, showPrivate]);

    const handleDelete = async (key: string) => {
        if (!window.confirm("Delete this conversation?")) return;
        try {
            await client.deleteConversation(key);
            await loadConversations(); // Reload list
        } catch (error: any) {
            alert(`Failed to delete: ${error.message}`);
        }
    };

    const handleLoadConversation = async (key: string) => {
        const conv = await client.getConversation(key);
        if (conv) {
            // Load conversation into chat UI
            console.log("Loaded:", conv.messages);
        }
    };

    return (
        <div>
            <h2>Conversations</h2>

            {/* Search & Filters */}
            <input
                type="text"
                placeholder="Search messages..."
                value={searchText}
                onChange={e => setSearchText(e.target.value)}
            />
            <button onClick={loadConversations}>Search</button>

            <label>
                <input
                    type="checkbox"
                    checked={showPublic}
                    onChange={e => setShowPublic(e.target.checked)}
                />
                Public
            </label>
            <label>
                <input
                    type="checkbox"
                    checked={showPrivate}
                    onChange={e => setShowPrivate(e.target.checked)}
                />
                Private
            </label>

            {/* Conversation List */}
            {loading ? (
                <div>Loading...</div>
            ) : (
                <ul>
                    {conversations.map(conv => (
                        <li key={conv.conversationKey}>
                            <div onClick={() => handleLoadConversation(conv.conversationKey)}>
                                <strong>{new Date(conv.timestamp).toLocaleString()}</strong>
                                <span style={{ color: conv.isPublic ? "green" : "red" }}>
                                    {conv.isPublic ? "Public" : "Private"}
                                </span>
                                <p>{conv.messages[0]?.text || "Empty conversation"}</p>
                                <small>{conv.messages.length} messages</small>
                            </div>
                            <button onClick={() => handleDelete(conv.conversationKey)}>Delete</button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
```

### Backend Setup Required

For conversation management to work, the backend must:

1. **Enable Repository persistence:**
   ```csharp
   builder.Services.AddPlayFramework("default", pb => pb
       .UseRepository());
   ```

2. **Enable conversation endpoints:**
   ```csharp
   app.MapPlayFramework("default", settings =>
   {
       settings.EnableConversationEndpoints = true;
   });
   ```

See [Backend README](../../../README.md) for full setup guide.

---

## 🔄 Combining Stored Conversations with Live Streaming

This section shows how to **load historic messages** from `StoredConversation` and **continue the conversation** with live SSE streaming.

### Understanding the Two Models

PlayFramework uses **two different models** for different purposes:

| Model | Purpose | When | Format | Content |
|-------|---------|------|--------|---------|
| **`AiSceneResponse`** | Real-time execution tracking | During `executeStepByStep()` / `executeTokenStreaming()` | SSE (Server-Sent Events) | Status updates, streaming chunks, scene metadata |
| **`StoredConversation`** | Persistent conversation history | When loading from repository via REST API | JSON | Complete messages, user metadata, execution state |

**Why two models?**
- `AiSceneResponse` contains **temporary execution metadata** (status, sceneName, functionName) needed for real-time UI updates
- `StoredConversation` contains **only essential data** (messages, userId, timestamp) for efficient storage and querying

### Complete Example: Chat with History

```tsx
import React, { useState, useEffect } from 'react';
import { usePlayFramework, StoredConversation, AiSceneResponse } from '@rystem/playframework-client';

interface ChatMessage {
    role: 'user' | 'assistant';
    text: string;
    isStreaming?: boolean;  // Distinguishes streaming vs historic messages
    status?: string;        // For showing execution status
}

function ChatWithHistory() {
    const client = usePlayFramework('default');

    // State
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState('');
    const [conversationKey, setConversationKey] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    // 📥 LOAD: Load historic conversation from repository
    const loadConversation = async (key: string) => {
        try {
            // Fetch from REST API → StoredConversation
            const stored = await client.getConversation(key);

            if (!stored) {
                alert('Conversation not found or unauthorized');
                return;
            }

            // Convert StoredMessage[] → ChatMessage[]
            const historicMessages: ChatMessage[] = stored.messages.map(msg => ({
                role: msg.role as 'user' | 'assistant',
                text: msg.text || '',
                isStreaming: false  // Historic messages are complete
            }));

            setMessages(historicMessages);
            setConversationKey(stored.conversationKey);

            console.log(`✅ Loaded ${stored.messages.length} messages from conversation ${key}`);
        } catch (error) {
            console.error('Failed to load conversation:', error);
            alert(`Error: ${error instanceof Error ? error.message : 'Unknown error'}`);
        }
    };

    // 📤 SEND: Send message and stream response
    const sendMessage = async () => {
        if (!input.trim() || loading) return;

        const userMessage = input;
        setInput('');
        setLoading(true);

        // Add user message to UI
        const userMsg: ChatMessage = { role: 'user', text: userMessage };
        setMessages(prev => [...prev, userMsg]);

        // Add placeholder for assistant response
        const assistantMsg: ChatMessage = { 
            role: 'assistant', 
            text: '', 
            isStreaming: true,  // Flag: currently streaming
            status: 'initializing'
        };
        setMessages(prev => [...prev, assistantMsg]);

        try {
            // 🔄 Stream from PlayFramework → AiSceneResponse events
            for await (const step of client.executeStepByStep({
                message: userMessage,
                settings: {
                    conversationKey: conversationKey || undefined,  // Resume existing or start new
                    enableStreaming: true
                }
            })) {
                // Update conversationKey from first response
                if (step.conversationKey && !conversationKey) {
                    setConversationKey(step.conversationKey);
                }

                // Update message with streaming chunks
                if (step.streamingChunk) {
                    setMessages(prev => {
                        const updated = [...prev];
                        const last = updated[updated.length - 1];
                        last.text += step.streamingChunk;
                        last.status = step.status;
                        return updated;
                    });
                }

                // Update status (planning, executingScene, etc.)
                else if (step.status) {
                    setMessages(prev => {
                        const updated = [...prev];
                        const last = updated[updated.length - 1];
                        last.status = step.status;
                        return updated;
                    });
                }
            }

            // Mark streaming as complete
            setMessages(prev => {
                const updated = [...prev];
                const last = updated[updated.length - 1];
                last.isStreaming = false;
                delete last.status;
                return updated;
            });

        } catch (error) {
            console.error('Streaming error:', error);
            setMessages(prev => {
                const updated = [...prev];
                updated[updated.length - 1] = {
                    role: 'assistant',
                    text: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`,
                    isStreaming: false,
                    status: 'error'
                };
                return updated;
            });
        } finally {
            setLoading(false);
        }
    };

    // 🗑️ CLEAR: Start new conversation
    const clearConversation = () => {
        setMessages([]);
        setConversationKey(null);
    };

    return (
        <div style={{ maxWidth: '900px', margin: '0 auto', padding: '20px', fontFamily: 'sans-serif' }}>
            <h1>💬 PlayFramework Chat</h1>

            {/* Conversation Info */}
            <div style={{ 
                padding: '10px', 
                marginBottom: '10px', 
                backgroundColor: '#f0f0f0', 
                borderRadius: '8px',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center'
            }}>
                <div>
                    {conversationKey ? (
                        <>
                            <strong>Conversation:</strong> {conversationKey.substring(0, 8)}...
                            <span style={{ marginLeft: '10px', color: '#666' }}>
                                ({messages.length} messages)
                            </span>
                        </>
                    ) : (
                        <span style={{ color: '#999' }}>New conversation</span>
                    )}
                </div>
                <button 
                    onClick={clearConversation}
                    style={{
                        padding: '5px 15px',
                        borderRadius: '6px',
                        border: '1px solid #ccc',
                        backgroundColor: '#fff',
                        cursor: 'pointer'
                    }}
                >
                    Clear
                </button>
            </div>

            {/* Messages */}
            <div style={{ 
                height: '500px', 
                overflowY: 'auto', 
                border: '1px solid #ddd', 
                borderRadius: '8px',
                padding: '15px', 
                marginBottom: '15px',
                backgroundColor: '#fafafa'
            }}>
                {messages.length === 0 ? (
                    <div style={{ textAlign: 'center', color: '#999', marginTop: '50px' }}>
                        <p>No messages yet. Start a conversation or load an existing one.</p>
                        <button 
                            onClick={() => loadConversation('some-conversation-key')}
                            style={{
                                padding: '8px 16px',
                                borderRadius: '6px',
                                border: '1px solid #61dafb',
                                backgroundColor: '#61dafb',
                                color: '#fff',
                                cursor: 'pointer',
                                marginTop: '10px'
                            }}
                        >
                            Load Example Conversation
                        </button>
                    </div>
                ) : (
                    messages.map((msg, i) => (
                        <div
                            key={i}
                            style={{
                                padding: '12px',
                                margin: '8px 0',
                                borderRadius: '12px',
                                backgroundColor: msg.role === 'user' ? '#e3f2fd' : '#fff',
                                border: msg.role === 'assistant' ? '1px solid #e0e0e0' : 'none',
                                maxWidth: '85%',
                                marginLeft: msg.role === 'user' ? 'auto' : '0',
                                marginRight: msg.role === 'user' ? '0' : 'auto',
                            }}
                        >
                            <div style={{ 
                                display: 'flex', 
                                justifyContent: 'space-between', 
                                marginBottom: '6px',
                                fontSize: '12px',
                                color: '#666'
                            }}>
                                <strong style={{ color: msg.role === 'user' ? '#1976d2' : '#388e3c' }}>
                                    {msg.role === 'user' ? '👤 You' : '🤖 AI'}
                                </strong>

                                {/* Show streaming indicator or status */}
                                {msg.isStreaming && (
                                    <span style={{ 
                                        color: '#ff6b6b',
                                        fontStyle: 'italic',
                                        fontSize: '11px'
                                    }}>
                                        {msg.status || 'streaming'} ●
                                    </span>
                                )}
                            </div>

                            <div style={{ 
                                whiteSpace: 'pre-wrap', 
                                wordBreak: 'break-word',
                                lineHeight: '1.5'
                            }}>
                                {msg.text || '...'}
                            </div>
                        </div>
                    ))
                )}
            </div>

            {/* Input */}
            <div style={{ display: 'flex', gap: '10px' }}>
                <input
                    type="text"
                    value={input}
                    onChange={e => setInput(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && !loading && sendMessage()}
                    placeholder={loading ? 'AI is responding...' : 'Type your message...'}
                    disabled={loading}
                    style={{
                        flex: 1,
                        padding: '12px 16px',
                        fontSize: '15px',
                        borderRadius: '8px',
                        border: '1px solid #ddd',
                        outline: 'none'
                    }}
                />
                <button
                    onClick={sendMessage}
                    disabled={loading || !input.trim()}
                    style={{
                        padding: '12px 24px',
                        fontSize: '15px',
                        borderRadius: '8px',
                        border: 'none',
                        backgroundColor: loading ? '#ccc' : '#61dafb',
                        color: loading ? '#666' : '#fff',
                        cursor: loading ? 'not-allowed' : 'pointer',
                        fontWeight: 600
                    }}
                >
                    {loading ? 'Sending...' : 'Send'}
                </button>
            </div>

            {/* Helper Text */}
            <div style={{ 
                marginTop: '15px', 
                fontSize: '13px', 
                color: '#666',
                textAlign: 'center'
            }}>
                💡 Messages are automatically saved to the repository when backend persistence is enabled.
                <br />
                Use the conversation list to browse and load previous conversations.
            </div>
        </div>
    );
}

export default ChatWithHistory;
```

### Key Features Demonstrated

✅ **Load Historic Messages**: Converts `StoredMessage[]` → `ChatMessage[]`  
✅ **Continue Conversation**: Uses same `conversationKey` to resume context  
✅ **Real-Time Streaming**: Updates UI with `AiSceneResponse` chunks  
✅ **Status Indicators**: Shows execution status during streaming  
✅ **Seamless UX**: Historic + streaming messages in same UI  

### Flow Diagram

```
1. User clicks "Load Conversation"
   ↓
2. GET /api/ai/default/conversations/{key}
   ↓
3. Receives StoredConversation (REST JSON)
   ├─ messages: StoredMessage[]
   ├─ conversationKey: "abc-123"
   └─ timestamp, userId, isPublic
   ↓
4. Convert to ChatMessage[] and display
   ↓
5. User types new message
   ↓
6. POST /api/ai/default (SSE streaming)
   settings: { conversationKey: "abc-123" }
   ↓
7. Receives AiSceneResponse events (SSE)
   ├─ status: "planning" → "executingScene" → "streaming"
   ├─ streamingChunk: "Hello", " world", "!"
   └─ conversationKey: "abc-123" (same as before)
   ↓
8. Backend loads context from cache/repository
   ↓
9. LLM generates response using historic context
   ↓
10. Response streamed to UI in real-time
    ↓
11. Backend saves new messages to repository
    ↓
12. Conversation continues seamlessly
```

### Best Practices

1. **Separate concerns**: Use `StoredMessage` for storage, `AiSceneResponse` for streaming
2. **Flag streaming state**: Add `isStreaming` flag to distinguish live vs historic
3. **Show status**: Display execution status (`planning`, `executingScene`) during streaming
4. **Handle errors**: Wrap streaming in try-catch and show error messages
5. **Auto-save**: Backend automatically persists conversations when repository is enabled

---

## 📸 Multi-Modal Content (Images, Audio, Video, PDFs)

PlayFramework supports **base64-encoded media content** in messages. The client provides helper utilities to convert Base64 data to Blob URLs for browser display.

### ContentUrlConverter Helper

Converts `AIContent` (base64 data) to Blob URLs that can be used in HTML `<img>`, `<audio>`, `<video>`, and `<iframe>` elements.

```typescript
import { ContentUrlConverter, AIContent } from "@rystem/playframework-client";

const content: AIContent = {
    type: "data",
    data: "iVBORw0KGgoAAAANSUhEUgAA...",  // Base64 JPEG
    mediaType: "image/jpeg"
};

// Convert to Blob URL
const url = ContentUrlConverter.toBlobUrl(content);

// Use in <img> tag
<img src={url} alt="Image" />

// IMPORTANT: Cleanup when done (frees memory!)
ContentUrlConverter.revokeUrl(url);
```

### API Methods

#### `toBlob(content: AIContent): Blob | null`
Decodes Base64 string to Blob object.

```typescript
const blob = ContentUrlConverter.toBlob(content);
```

#### `toBlobUrl(content: AIContent, cacheKey?: string): string | null`
Creates `blob:` URL for browser display. Supports optional caching.

```typescript
const url = ContentUrlConverter.toBlobUrl(content, 'image-123');
```

#### `revokeUrl(url: string, cacheKey?: string): void`
Revokes Blob URL to free memory. **Always call this when done!**

```typescript
ContentUrlConverter.revokeUrl(url, 'image-123');
```

#### `clearCache(): void`
Revokes all cached URLs at once.

```typescript
ContentUrlConverter.clearCache();
```

#### `downloadAsFile(content: AIContent, filename?: string): void`
Triggers browser download.

```typescript
ContentUrlConverter.downloadAsFile(content, 'image.jpg');
```

#### `getFileExtension(mediaType?: string): string`
Maps MIME type to file extension.

```typescript
const ext = ContentUrlConverter.getFileExtension('image/jpeg'); // ".jpg"
```

---

### React Example: Image Viewer

```tsx
import { useState, useEffect } from 'react';
import { ContentUrlConverter, AIContent } from "@rystem/playframework-client";

interface ImageViewerProps {
    content: AIContent;
}

function ImageViewer({ content }: ImageViewerProps) {
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        // Create Blob URL when component mounts
        const blobUrl = ContentUrlConverter.toBlobUrl(content, `image-${Date.now()}`);
        setUrl(blobUrl);

        // Cleanup when component unmounts (important for memory!)
        return () => {
            if (blobUrl) {
                ContentUrlConverter.revokeUrl(blobUrl);
            }
        };
    }, [content]);

    if (!url) return <div>Loading image...</div>;

    return (
        <div>
            <img src={url} alt="Image" style={{ maxWidth: '100%' }} />
            <button onClick={() => ContentUrlConverter.downloadAsFile(content, 'image.jpg')}>
                Download
            </button>
        </div>
    );
}
```

---

### React Example: Auto-Detection Content Viewer

```tsx
import { ContentUrlConverter, AIContent } from "@rystem/playframework-client";

function ContentViewer({ content }: { content: AIContent }) {
    const mediaType = content.mediaType?.toLowerCase() || '';
    const [url, setUrl] = useState<string | null>(null);

    useEffect(() => {
        const blobUrl = ContentUrlConverter.toBlobUrl(content);
        setUrl(blobUrl);

        return () => {
            if (blobUrl) ContentUrlConverter.revokeUrl(blobUrl);
        };
    }, [content]);

    if (!url) return <div>Loading...</div>;

    // Image
    if (mediaType.startsWith('image/')) {
        return <img src={url} alt="Image" style={{ maxWidth: '100%' }} />;
    }

    // Audio
    if (mediaType.startsWith('audio/')) {
        return (
            <audio controls style={{ width: '100%' }}>
                <source src={url} type={mediaType} />
            </audio>
        );
    }

    // Video
    if (mediaType.startsWith('video/')) {
        return (
            <video controls style={{ maxWidth: '100%' }}>
                <source src={url} type={mediaType} />
            </video>
        );
    }

    // PDF
    if (mediaType === 'application/pdf') {
        return (
            <iframe
                src={url}
                title="PDF Document"
                style={{ width: '100%', height: '600px', border: '1px solid #ddd' }}
            />
        );
    }

    // Fallback for unknown types
    return (
        <div>
            <p>📎 Attachment: {content.mediaType || 'unknown type'}</p>
            <button onClick={() => ContentUrlConverter.downloadAsFile(content)}>
                Download File
            </button>
        </div>
    );
}
```

---

### includeContents Parameter

When loading conversations, control whether to fetch base64 content:

```typescript
// ❌ List conversations WITHOUT media (faster, smaller payload)
const list = await client.listConversations({
    includeContents: false,  // Default: false
    take: 50
});

// ✅ Load single conversation WITH media (for display)
const conv = await client.getConversation(key, true);  // includeContents=true
```

**Why this matters:**

| Operation | includeContents | Payload Size | Speed |
|-----------|----------------|--------------|-------|
| List 100 conversations | `false` | ~50 KB | ⚡ Fast (~50ms) |
| List 100 conversations | `true` | ~5-50 MB | 🐢 Slow (~500ms) |
| Get single conversation | `true` | ~50-500 KB | ✅ Acceptable |

**Best Practice**: Always use `includeContents=false` for list operations, `true` only when loading a conversation for display.

---

### Full Example: Chat with Multi-Modal Support

```tsx
import { useState, useEffect } from 'react';
import { usePlayFramework, StoredMessage, AIContent, ContentUrlConverter } from "@rystem/playframework-client";

function ChatWithMedia() {
    const client = usePlayFramework('default');
    const [messages, setMessages] = useState<StoredMessage[]>([]);
    const [conversationKey, setConversationKey] = useState<string | null>(null);

    const loadConversation = async (key: string) => {
        // Load conversation WITH contents
        const conv = await client.getConversation(key, true);

        if (conv) {
            setMessages(conv.messages);
            setConversationKey(conv.conversationKey);
        }
    };

    return (
        <div>
            <h1>Chat with Media Support</h1>

            {messages.map((msg, i) => (
                <div key={i} style={{ 
                    backgroundColor: msg.role === 'user' ? '#e3f2fd' : '#fff',
                    padding: '12px',
                    margin: '8px 0',
                    borderRadius: '8px'
                }}>
                    <strong>{msg.role === 'user' ? '👤 You' : '🤖 AI'}</strong>
                    <p>{msg.text}</p>

                    {/* Display attached content (images, PDFs, etc.) */}
                    {msg.contents && msg.contents.length > 0 && (
                        <div style={{ marginTop: '8px' }}>
                            {msg.contents.map((content, idx) => (
                                <ContentViewer key={idx} content={content} />
                            ))}
                        </div>
                    )}
                </div>
            ))}

            <button onClick={() => loadConversation('example-key')}>
                Load Example Conversation
            </button>
        </div>
    );
}
```

---

### Memory Management Best Practices

1. **Always cleanup Blob URLs**: Use `useEffect` cleanup function in React
2. **Use caching for repeated content**: Pass `cacheKey` parameter
3. **Clear cache on unmount**: Call `ContentUrlConverter.clearCache()` when appropriate
4. **Lazy load media**: Only convert to Blob URL when needed for display
5. **Use includeContents wisely**: Exclude contents from list operations

**Example cleanup:**
```tsx
useEffect(() => {
    const url = ContentUrlConverter.toBlobUrl(content);
    setImageUrl(url);

    // Cleanup when component unmounts
    return () => {
        if (url) ContentUrlConverter.revokeUrl(url);
    };
}, [content]);

// Clear all on app unmount
useEffect(() => {
    return () => ContentUrlConverter.clearCache();
}, []);
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
    | "initializing"              // Starting execution
    | "loadingCache"              // Loading cached conversation context
    | "executingMainActors"       // Running global system prompts
    | "planning"                  // Creating execution plan
    | "executingScene"            // Running scene/actor
    | "functionRequest"           // Calling server-side tool
    | "functionCompleted"         // Tool call completed
    | "toolSkipped"               // Tool was skipped
    | "streaming"                 // Streaming text chunks
    | "running"                   // General execution in progress
    | "summarizing"               // Summarizing conversation
    | "directorDecision"          // Director evaluating results
    | "generatingFinalResponse"   // Generating final response
    | "savingCache"               // Saving to cache
    | "savingMemory"              // Saving conversation memory
    | "awaitingClient"            // Waiting for client-side tool response
    | "commandClient"             // Fire-and-forget command sent to client
    | "completed"                 // Success
    | "budgetExceeded"            // Max cost exceeded
    | "error"                     // Failure
    | "unauthorized";             // Authorization failed
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

Static service locator for managing named `PlayFrameworkClient` instances.

| Method | Signature | Description |
|--------|-----------|-------------|
| `configure` | `(name: string, baseUrl: string, configure?: (settings: PlayFrameworkSettings) => void \| Promise<void>): Promise<PlayFrameworkSettings>` | Register and configure a named client factory |
| `resolve` | `(name?: string): PlayFrameworkClient` | Get client by name (defaults to first registered) |
| `getClient` | `(name: string): PlayFrameworkClient` | Get client by exact name (throws if not found) |
| `getDefaultClient` | `(): PlayFrameworkClient` | Get the first registered client |
| `getSettings` | `(name: string): PlayFrameworkSettings` | Get settings by name (throws if not found) |
| `isConfigured` | `(name: string): boolean` | Check if a named client exists |
| `remove` | `(name: string): void` | Remove a named client and its settings |
| `clear` | `(): void` | Remove all clients and settings |

```typescript
// Configure
await PlayFrameworkServices.configure("chat", "http://localhost:5158/api/ai");

// Resolve
const client = PlayFrameworkServices.resolve("chat");

// Check & remove
if (PlayFrameworkServices.isConfigured("chat")) {
    PlayFrameworkServices.remove("chat");
}
```

---

### `PlayFrameworkClient`

Main client for executing AI requests via SSE streaming.

| Method | Signature | Description |
|--------|-----------|-------------|
| `executeStepByStep` | `(request: PlayFrameworkRequest, signal?: AbortSignal): AsyncIterableIterator<AiSceneResponse>` | Step-by-step streaming (one event per scene completion) |
| `executeTokenStreaming` | `(request: PlayFrameworkRequest, signal?: AbortSignal): AsyncIterableIterator<AiSceneResponse>` | Token-level streaming (one event per text chunk) |
| `getClientRegistry` | `(): ClientInteractionRegistry` | Get the client-side tool/command registry |
| `getConversation` | `(conversationKey: string): Promise<StoredConversation \| null>` | Load a stored conversation by key |
| `getConversations` | `(sortOrder?: ConversationSortOrder): Promise<StoredConversation[]>` | List conversations for the authenticated user |
| `deleteConversation` | `(conversationKey: string): Promise<boolean>` | Delete a stored conversation |

---

### `ClientInteractionRegistry`

Registry for client-side tools and commands invoked by the AI during execution.

| Method | Signature | Description |
|--------|-----------|-------------|
| `register` | `<TArgs>(toolName: string, handler: ClientTool<TArgs>): void` | Register a tool (server waits for response) |
| `registerCommand` | `<TArgs>(toolName: string, handler: ClientCommand<TArgs>, options?: CommandOptions): void` | Register a fire-and-forget command |
| `execute` | `(request: ClientInteractionRequest): Promise<ClientInteractionResult>` | Execute a registered tool/command |
| `has` | `(toolName: string): boolean` | Check if a tool/command is registered |
| `isCommand` | `(toolName: string): boolean` | Check if the registration is a command |
| `getCommandOptions` | `(toolName: string): CommandOptions \| undefined` | Get the command's feedback options |
| `getToolNames` | `(): string[]` | List all registered tool/command names |
| `unregister` | `(toolName: string): boolean` | Remove a registration |
| `clear` | `(): void` | Remove all registrations |

**Types:**

```typescript
type ClientTool<TArgs = any> = (args?: TArgs) => Promise<AIContent[]>;
type ClientCommand<TArgs = any> = (args?: TArgs) => Promise<CommandResult>;
type CommandFeedbackMode = 'never' | 'onError' | 'always';

interface CommandOptions {
    feedbackMode?: CommandFeedbackMode;
}
```

```typescript
// Tool — server waits for the result
registry.register("myTool", async (args?: { key: string }) => {
    return [AIContentConverter.fromText(args?.key || "default")];
});

// Command — fire-and-forget, feedback optional
registry.registerCommand("showNotification", async (args?: { text: string }) => {
    showToast(args?.text ?? "");
    return CommandResult.ok();
}, { feedbackMode: 'onError' });
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

## � Client Commands (Fire-and-Forget)

Commands are client-side tools that the server invokes but **does not wait for a response**. Use them for UI side effects (toasts, navigation, animations, etc.).

### Registering Commands

```typescript
const registry = client.getClientRegistry();

// Simple command — no feedback to server
registry.registerCommand("showToast", async (args?: { text: string }) => {
    showNotification(args?.text ?? "");
    return CommandResult.ok();
});

// Command with error feedback — server receives failure info
registry.registerCommand("navigateTo", async (args?: { url: string }) => {
    try {
        window.location.href = args?.url ?? "/";
        return CommandResult.ok();
    } catch (e) {
        return CommandResult.fail(`Navigation failed: ${e}`);
    }
}, { feedbackMode: 'onError' });

// Command with always-feedback — server always gets the result
registry.registerCommand("playSound", async (args?: { soundId: string }) => {
    await audioPlayer.play(args?.soundId ?? "notification");
    return CommandResult.ok("Sound played");
}, { feedbackMode: 'always' });
```

### `CommandResult` Helper

```typescript
interface CommandResult {
    success: boolean;
    message?: string;
}

// Factory methods
CommandResult.ok();                   // { success: true }
CommandResult.ok("Done");            // { success: true, message: "Done" }
CommandResult.fail("Reason");        // { success: false, message: "Reason" }
```

### `CommandFeedbackMode`

| Mode | Behavior |
|------|----------|
| `'never'` | Server ignores the command's result (default) |
| `'onError'` | Server receives feedback only when `success: false` |
| `'always'` | Server always receives the command's result |

---

## 📋 `ClientInteractionRequest` Model

When the server invokes a client-side tool or command, it sends this object:

```typescript
interface ClientInteractionRequest {
    interactionId: string;                          // Unique ID for this invocation
    toolName: string;                               // Registered tool/command name
    description?: string;                           // Human-readable description
    arguments?: Record<string, any>;                // Arguments from AI
    argumentsSchema?: string;                       // JSON Schema string
    timeoutSeconds: number;                         // Max time to wait (tools only)
    isCommand?: boolean;                            // true = fire-and-forget command
    feedbackMode?: 'never' | 'onError' | 'always'; // Command feedback mode
}
```

---

## 📋 `ClientInteractionResult` Model

The response your tool handler returns to the server:

```typescript
interface AIContent {
    type: "text" | "data";
    text?: string;           // For type "text"
    data?: string;           // Base64-encoded (for type "data")
    mediaType?: string;      // e.g., "image/jpeg", "audio/webm"
}

interface ClientInteractionResult {
    interactionId: string;   // Must match the request
    contents: AIContent[];   // Tool output
    error?: string;          // Error message (if failed)
    executedAt: string;      // ISO 8601 timestamp
}
```

---

## 📋 `ExecutionState` and `ExecutionPhase`

The `ExecutionState` object tracks the AI orchestration progress during execution:

```typescript
type ExecutionPhase =
    | "notStarted"
    | "initialized"
    | "sceneSelected"
    | "executingScene"
    | "awaitingClient"
    | "sceneCompleted"
    | "chaining"
    | "generatingFinalResponse"
    | "completed"
    | "completedNoResponse"
    | "budgetExceeded"
    | "sceneNotFound"
    | "tooManyToolRequests"
    | "break"
    | "unauthorized";

interface ExecutionState {
    phase: ExecutionPhase;                          // Current execution phase
    executedSceneOrder: string[];                   // Scene names in execution order
    executedScenes: Record<string, any[]>;          // Scene name → results
    executedTools: string[];                        // Tool names invoked
    accumulatedCost: number;                        // Total $ cost so far
    currentSceneName?: string | null;               // Currently running scene
}
```

---

## 📋 `AiSceneResponse` Full Model

Each SSE event yields an `AiSceneResponse`:

```typescript
interface AiSceneResponse {
    status: AiResponseStatus;
    sceneName?: string;
    functionName?: string;
    functionArguments?: string;
    message?: string;                // Final response text (step-by-step)
    streamingChunk?: string;         // Partial text (token streaming)
    isStreamingComplete?: boolean;
    errorMessage?: string;
    inputTokens?: number;
    cachedInputTokens?: number;
    outputTokens?: number;
    totalTokens?: number;
    cost?: number;                   // Cost for this step
    totalCost?: number;              // Accumulated total cost
    conversationKey?: string;
    continuationToken?: string;      // Token for resuming after client tool
    clientInteractionRequest?: ClientInteractionRequest;
    timestamp?: string;
    metadata?: Record<string, any>;
    contents?: Array<{
        type: string;
        text?: string;
        data?: string;               // Base64 encoded
        mediaType?: string;
    }>;
}
```

---

## 📋 `StoredMessage` Full Model

Each message in a `StoredConversation.messages` array:

```typescript
interface StoredMessage {
    businessType: number;                           // Internal type discriminator
    label?: string | null;                          // Optional label (e.g., scene name)
    role: string;                                   // "user" | "assistant" | "system" | "tool"
    text?: string | null;                           // Plain text content
    contents?: any[] | null;                        // Multi-modal content items
    additionalProperties?: Record<string, any> | null; // Extra metadata
}
```

---

## 📋 `ContentItem` Full Model

Used in `PlayFrameworkRequest.contents` for multi-modal input:

```typescript
interface ContentItem {
    type: "text" | "image" | "audio" | "video" | "file" | "uri";
    text?: string;           // For type "text"
    data?: string;           // Base64-encoded binary data
    uri?: string;            // For type "uri"
    mediaType?: string;      // MIME type (e.g., "image/png", "audio/webm")
    name?: string;           // Optional filename
}
```

---

## �📁 Where to Place This README

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
