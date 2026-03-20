# rystem.playframework.client

`rystem.playframework.client` is the published TypeScript client for the PlayFramework HTTP API.

It wraps PlayFramework SSE endpoints, auto-runs client-side tools, supports conversation CRUD and the server voice endpoint, and also ships browser-native helpers such as `AIContentConverter`, `ContentUrlConverter`, `VoiceRecorder`, and `BrowserVoiceClient`.

## Installation

```bash
npm install rystem.playframework.client
```

The current package name in `package.json` is `rystem.playframework.client`.

## What this package expects from the server

This client assumes a backend that maps PlayFramework under a base path such as:

```csharp
app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
    settings.EnableConversationEndpoints = true;
    settings.EnableVoiceEndpoints = true;
});
```

When you configure the TypeScript client with:

```text
http://localhost:5158/api/ai
```

the library appends `/{factoryName}` itself.

So for the `default` factory it calls:

- `POST http://localhost:5158/api/ai/default`
- `POST http://localhost:5158/api/ai/default/streaming`
- `GET http://localhost:5158/api/ai/default/discovery`
- `GET http://localhost:5158/api/ai/default/conversations`
- `POST http://localhost:5158/api/ai/default/voice`

## Architecture

The package is built around:

- `PlayFrameworkServices`
- `PlayFrameworkClient`
- `ClientInteractionRegistry`
- `AIContentConverter`
- `ContentUrlConverter`
- `VoiceRecorder`
- `BrowserVoiceClient`
- `usePlayFramework`

The usual lifecycle is:

1. configure one or more client factories with `PlayFrameworkServices.configure(...)`
2. resolve a `PlayFrameworkClient`
3. call `executeStepByStep(...)` or `executeTokenStreaming(...)`
4. optionally register browser-side tools through `getClientRegistry()`
5. optionally use conversation or voice helpers

## Example: bootstrap one client

`PlayFrameworkServices.configure(...)` is async and should usually run during application startup.

```typescript
import { PlayFrameworkServices } from "rystem.playframework.client";

await PlayFrameworkServices.configure("default", "http://localhost:5158/api/ai", async settings => {
  settings.timeout = 120_000;
  settings.maxReconnectAttempts = 3;
  settings.reconnectBaseDelay = 1000;

  settings.addHeadersEnricher(async (_url, _method, headers) => {
    return {
      ...Object.fromEntries(new Headers(headers).entries()),
      Authorization: `Bearer ${localStorage.getItem("token") ?? ""}`
    };
  });

  settings.addErrorHandler(async (_url, _method, _headers, _body, error) => {
    if (error instanceof Error && error.message.includes("401")) {
      localStorage.removeItem("token");
    }
    return false;
  });
});

const client = PlayFrameworkServices.resolve("default");
```

If you only configure one factory, `resolve()` with no name returns the first configured client:

```typescript
const defaultClient = PlayFrameworkServices.resolve();
```

## Example: configure multiple factories

The sample workspace in `src/AI/Rystem.PlayFramework.Client/src/App.tsx` configures both `default` and `foundry` against the same base path.

```typescript
await Promise.all([
  PlayFrameworkServices.configure("default", "http://localhost:5158/api/ai", settings => {
    settings.timeout = 120_000;
  }),
  PlayFrameworkServices.configure("foundry", "http://localhost:5158/api/ai", settings => {
    settings.timeout = 120_000;
  })
]);

const cloudClient = PlayFrameworkServices.resolve("default");
const localClient = PlayFrameworkServices.resolve("foundry");
```

This is useful when the backend exposes multiple PlayFramework factories with different scenes or model providers.

## Example: load scenes and tool metadata from the server

Use `getDiscovery()` when the frontend needs to know which scenes, DI tools, client tools, or MCP tools are currently available for a factory.

```typescript
const metadata = await client.getDiscovery();

for (const scene of metadata.scenes ?? []) {
  console.log(scene.name, scene.description);

  for (const tool of scene.tools ?? []) {
    console.log(tool.toolName, tool.sourceType, tool.sourceName, tool.memberName);
  }
}
```

This calls:

```text
GET {baseUrl}/{factoryName}/discovery
```

Typical uses:

- populate a scene dropdown with the normalized names expected by `settings.sceneName`
- show which tools come from DI services, client interactions, or MCP servers
- build `forcedTools` values without hardcoding service or MCP metadata in the frontend

## Example: step-by-step streaming

`executeStepByStep(...)` yields full `AiSceneResponse` events as the backend progresses through planning, scene execution, tool calls, and final output.

```typescript
import type { PlayFrameworkRequest } from "rystem.playframework.client";

const request: PlayFrameworkRequest = {
  message: "Calculate 12 * 7",
  metadata: {
    userId: "user-42",
    tenantId: "tenant-a"
  },
  settings: {
    executionMode: "Scene",
    sceneName: "Calculator"
  }
};

for await (const step of client.executeStepByStep(request)) {
  console.log(step.status, step.sceneName, step.message);

  if (step.totalTokens != null || step.totalCost != null) {
    console.log("usage", step.totalTokens, step.totalCost);
  }
}
```

This calls:

```text
POST {baseUrl}/{factoryName}
```

Common yielded statuses include:

- `initializing`
- `planning`
- `executingScene`
- `functionRequest`
- `functionCompleted`
- `running`
- `awaitingClient`
- `commandClient`

Completion and error markers are handled internally by the client runtime rather than surfaced as normal yielded events.

## Example: token streaming

`executeTokenStreaming(...)` still yields `AiSceneResponse` objects, but the text arrives through `streamingChunk`.

```typescript
let finalText = "";

for await (const chunk of client.executeTokenStreaming({
  message: "Write a short summary of PlayFramework",
  settings: {
    executionMode: "Direct"
  }
})) {
  if (chunk.streamingChunk) {
    finalText += chunk.streamingChunk;
    console.log(chunk.streamingChunk);
  }
}

console.log("final", finalText);
```

This calls:

```text
POST {baseUrl}/{factoryName}/streaming
```

## Request model

The main request type is `PlayFrameworkRequest`.

```typescript
const request = {
  message: "Book a flight to Paris",
  metadata: {
    userId: "123",
    tenantId: "tenant-a"
  },
  settings: {
    executionMode: "Planning",
    maxRecursionDepth: 5,
    sceneName: "Travel"
  },
  conversationKey: "conversation-123"
};
```

The `settings` object mirrors the server-side `SceneRequestSettings`, including:

- `executionMode`
- `sceneName`
- `forcedTools`
- `conversationKey`
- `clientInteractionResults`
- `enableStreaming`
- `enableSummarization`
- `enableDirector`
- `maxRecursionDepth`

The request can also include `contents` for multi-modal inputs.

## Example: force specific tools for a selected scene

The discovery payload returns the exact values you can send back inside `settings.forcedTools`.

```typescript
const discovery = await client.getDiscovery();
const calculatorScene = discovery.scenes?.find(scene => scene.name === "Calculator");
const addTool = calculatorScene?.tools?.find(tool => tool.toolName === "Add");
const subtractTool = calculatorScene?.tools?.find(tool => tool.toolName === "Subtract");

for await (const step of client.executeStepByStep({
  message: "Use only add and subtract",
  settings: {
    executionMode: "Scene",
    sceneName: "Calculator",
    forcedTools: [addTool, subtractTool]
      .filter((tool): tool is NonNullable<typeof tool> => Boolean(tool))
      .map(tool => ({
        sceneName: tool.sceneName,
        toolName: tool.toolName,
        sourceType: tool.sourceType,
        sourceName: tool.sourceName,
        memberName: tool.memberName
      }))
  }
})) {
  console.log(step.status, step.message);
}
```

Notes:

- `sceneName` must use the normalized server name from discovery
- `toolName` must use the normalized tool name from discovery
- `sourceName` is usually the DI service type name for service tools or the MCP server/factory name for MCP tools
- `memberName` is usually the service method name or the original MCP tool name

## Example: send files and multi-modal contents

The request model accepts `ContentItem[]`, while `AIContentConverter` helps you build the base64 payloads in the browser.

```typescript
import {
  AIContentConverter,
  type ContentItem,
  type PlayFrameworkRequest
} from "rystem.playframework.client";

async function buildRequest(file: File): Promise<PlayFrameworkRequest> {
  const converted = await AIContentConverter.fromFile(file);

  const contents: ContentItem[] = [
    {
      type: "file",
      data: converted.data,
      mediaType: converted.mediaType,
      name: file.name
    }
  ];

  return {
    message: "Summarize the attached document and extract the risks.",
    contents,
    settings: {
      executionMode: "Planning"
    }
  };
}

const request = await buildRequest(selectedFile);

for await (const step of client.executeStepByStep(request)) {
  console.log(step.status, step.message);
}
```

Browser helpers available in this package include:

- `AIContentConverter.fromFile(...)`
- `AIContentConverter.fromMultipleFiles(...)`
- `AIContentConverter.fromCamera(...)`
- `AIContentConverter.fromGeolocation(...)`
- `AIContentConverter.fromMicrophone(...)`
- `AIContentConverter.fromText(...)`

## Example: render or download returned contents

`AiSceneResponse.contents` can include generated images, audio, or files. `ContentUrlConverter` is the browser-side helper for previewing or downloading them.

```typescript
import { ContentUrlConverter } from "rystem.playframework.client";

for await (const step of client.executeStepByStep({
  message: "Generate an image of a blue robot"
})) {
  const media = step.contents?.[0];
  if (!media) continue;

  const url = ContentUrlConverter.toBlobUrl(media, `response-${Date.now()}`);
  if (url) {
    console.log("preview", url);
  }

  ContentUrlConverter.downloadAsFile(media, "playframework-output");
}
```

Remember to revoke blob URLs you keep around:

```typescript
ContentUrlConverter.revokeUrl(url, cacheKey);
```

## Example: client-side tools and commands

When the server emits `awaitingClient` or `commandClient`, the library can execute browser-side tools locally and resume automatically.

This is the same pattern used by the sample app in `src/AI/Rystem.PlayFramework.Client/src/App.tsx`.

```typescript
import {
  AIContentConverter,
  CommandResultHelper,
  PlayFrameworkServices
} from "rystem.playframework.client";

const client = PlayFrameworkServices.resolve("default");
const registry = client.getClientRegistry();

registry.register("getCurrentLocation", async () => {
  const content = await AIContentConverter.fromGeolocation({ timeout: 10_000 });
  return [content];
});

registry.register<{ question?: string }>("getUserConfirmation", async (args) => {
  const confirmed = window.confirm(args?.question ?? "Do you confirm?");
  return [AIContentConverter.fromText(confirmed ? "confirmed" : "denied")];
});

registry.registerCommand("logUserAction", async (args?: { action?: string }) => {
  console.log("action", args?.action);
  return CommandResultHelper.ok();
}, { feedbackMode: "never" });

registry.registerCommand("saveToLocalStorage", async (args?: { key: string; value: string }) => {
  if (!args) {
    return CommandResultHelper.fail("Missing args");
  }
  localStorage.setItem(args.key, args.value);
  return CommandResultHelper.ok(`Saved ${args.key}`);
}, { feedbackMode: "always" });
```

Supported command feedback modes are:

- `never`
- `onError`
- `always`

Important behavior:

- `PlayFrameworkClient` executes registered tools automatically when the server asks for them
- tool names must match what the server emits after its own normalization
- auto-resume uses the returned `conversationKey` and `clientInteractionResults` behind the scenes
- if a tool is missing, the library sends back an error result instead of silently succeeding

## Example: conversation management

`PlayFrameworkClient` exposes:

- `listConversations(...)`
- `getConversation(...)`
- `deleteConversation(...)`
- `updateConversationVisibility(...)`

Example:

```typescript
import {
  ConversationSortOrder,
  PlayFrameworkServices
} from "rystem.playframework.client";

const client = PlayFrameworkServices.resolve("default");

const conversations = await client.listConversations({
  searchText: "weather",
  orderBy: ConversationSortOrder.TimestampDescending,
  includePublic: true,
  includePrivate: true,
  skip: 0,
  take: 20
});

const selected = conversations[0];

if (selected) {
  const fullConversation = await client.getConversation(selected.conversationKey, true);
  console.log(fullConversation?.messages.length);

  await client.updateConversationVisibility(selected.conversationKey, true);
  await client.deleteConversation(selected.conversationKey);
}
```

These methods call:

```text
{baseUrl}/{factoryName}/conversations
```

They only work when the server has both:

- `UseRepository()` and a matching `IRepository<StoredConversation, string>` registration
- `EnableConversationEndpoints = true` in `MapPlayFramework(...)`

## Example: server voice endpoint

`executeVoice(...)` targets the PlayFramework HTTP voice endpoint and streams `VoiceEvent` objects back.

```typescript
import {
  PlayFrameworkServices,
  VoiceRecorder
} from "rystem.playframework.client";

const client = PlayFrameworkServices.resolve("default");
const recorder = new VoiceRecorder({ mode: "pushToTalk" });

await recorder.start({
  onRecorded: async (blob) => {
    for await (const event of client.executeVoice({
      audio: blob,
      conversationKey: "voice-conversation-1",
      metadata: { userId: "user-42" }
    })) {
      if (event.type === "transcription") {
        console.log("user said", event.text);
      }
      if (event.type === "scene") {
        console.log(event.status, event.message);
      }
      if (event.type === "audio") {
        console.log("received audio chunk", event.audio?.length ?? 0);
      }
    }
  }
});

// In push-to-talk mode you stop manually.
recorder.stop();
```

This calls:

```text
POST {baseUrl}/{factoryName}/voice
```

It only works when the server enables the PlayFramework voice endpoint and has a matching `IVoiceAdapter`.

## Example: browser-native voice flow

`BrowserVoiceClient` is different from `executeVoice(...)`.

It does not use the server voice endpoint. Instead it uses browser speech recognition plus browser speech synthesis and sends the recognized text through the normal PlayFramework SSE endpoints.

```typescript
import {
  BrowserVoiceClient,
  PlayFrameworkServices
} from "rystem.playframework.client";

const client = PlayFrameworkServices.resolve("default");

if (BrowserVoiceClient.isSupported()) {
  const voice = new BrowserVoiceClient(
    client,
    { lang: "en-US" },
    { lang: "en-US" }
  );

  for await (const event of voice.executeWithBrowserVoice({
    streamingMode: "tokenStreaming",
    request: {
      metadata: { userId: "user-42" },
      settings: {
        executionMode: "Scene",
        sceneName: "General_Requests"
      }
    }
  })) {
    if (event.voiceStatus === "recognized") {
      console.log("transcript", event.transcript);
    }
    if (event.response?.streamingChunk) {
      console.log(event.response.streamingChunk);
    }
  }
}
```

Useful notes:

- `BrowserVoiceClient.isSupported()` checks browser STT + TTS support
- `speakResponse(...)` is a shortcut when you already have the input text
- `cancelSpeech()`, `cancelStream()`, and `cancelAll()` are available for UI controls
- scene names used in direct scene execution must match the server's normalized scene name, so a server scene like `AddScene("General Requests", ...)` is addressed as `General_Requests`

## `usePlayFramework`

The library exports `usePlayFramework(name?)`, but it is only a convenience accessor over `PlayFrameworkServices.resolve(name)`.

It does not manage React state or subscribe to updates by itself.

So even though the name looks React-specific, it is effectively a thin helper rather than a stateful hook system.

```typescript
import { usePlayFramework } from "rystem.playframework.client";

const client = usePlayFramework("default");

for await (const step of client.executeStepByStep({ message: "Hello" })) {
  console.log(step.message);
}
```

## Useful exports

Besides the main client classes, the package also exports:

- `AIContentConverter`
- `ContentUrlConverter`
- `VoiceRecorder`
- `BrowserSpeechRecognizer`
- `BrowserSpeechSynthesizer`
- `BrowserVoiceClient`
- `ConversationSortOrder`
- `CommandResultHelper`

## Important caveats

### The final `completed` marker is consumed internally

`PlayFrameworkClient` treats `status === "completed"` as an internal stop marker and does not yield that marker to callers as a final event.

### `status === "error"` throws

Server error markers are converted into thrown exceptions rather than yielded error events.

### Library code logs to the console

The current runtime includes several `console.log`, `console.warn`, and `console.error` calls.

### Auto-resume depends on registered tools

If the server asks for a client-side tool and the registry does not contain it, the library sends back an error result rather than magically handling the operation.

### `baseUrl` must point to the PlayFramework base path only

Use a base such as `http://localhost:5158/api/ai`, not `http://localhost:5158/api/ai/default`. The library appends `/{factoryName}` itself.

### Many helpers are browser-only

`AIContentConverter.fromCamera(...)`, `AIContentConverter.fromGeolocation(...)`, `AIContentConverter.fromMicrophone(...)`, `VoiceRecorder`, and `BrowserSpeech*` utilities depend on browser APIs and are not SSR-safe by default.

### Browser voice and server voice are different paths

`BrowserVoiceClient` uses browser STT/TTS plus normal SSE endpoints. `executeVoice(...)` uses the server-side `/voice` endpoint with a server `IVoiceAdapter`.

### `usePlayFramework(...)` is not a real reactive hook

It is only a resolver helper over `PlayFrameworkServices.resolve(...)`.

## Grounded by source files

- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/servicecollection/PlayFrameworkServices.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/servicecollection/PlayFrameworkSettings.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/engine/PlayFrameworkClient.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/engine/ClientInteractionRegistry.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/AIContentConverter.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/ContentUrlConverter.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/VoiceRecorder.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/utils/BrowserVoiceClient.ts`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/src/hooks/hooks.ts`
- `src/AI/Rystem.PlayFramework.Client/src/App.tsx`

Use this package when you want a typed client for a PlayFramework backend, including SSE execution, browser tool continuation, conversations, and both server-side and browser-side voice flows.
