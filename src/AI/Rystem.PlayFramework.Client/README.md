# Rystem.PlayFramework.Client Workspace

`src/AI/Rystem.PlayFramework.Client` is the private React + Vite sample app for the PlayFramework TypeScript client.

It is not the published NPM package. The reusable client library lives under `src/AI/Rystem.PlayFramework.Client/src/rystem`.

## What this folder is

This workspace is a demo/test app that exercises:

- SSE chat execution
- multi-step scene updates
- token streaming
- conversation management UI
- client-side tools
- the local TypeScript client library from `src/rystem`

The app package itself is private and currently named `rystem.playframework.client.testapp` in `src/AI/Rystem.PlayFramework.Client/package.json`.

## Run the app

```bash
cd src/AI/Rystem.PlayFramework.Client
npm install
npm run dev
```

The current `vite.config.ts` pins the dev server to:

```text
http://localhost:3000
```

This workspace is not using the default Vite port.

## Available scripts

```bash
npm run dev
npm run build
npm run preview
npm run test
npm run test:ui
npm run lint
npm run lint:fix
npm run format
```

## Backend expectation

The app expects a PlayFramework backend that exposes SSE endpoints.

At minimum that usually means:

```csharp
builder.Services.AddPlayFramework("default", framework =>
{
    framework.WithChatClient("default");
});

app.MapPlayFramework("default", settings =>
{
    settings.BasePath = "/api/ai";
});
```

If you want conversation management in the UI, the backend also needs:

- `UseRepository()` in the PlayFramework builder
- a matching `IRepository<StoredConversation, string>` registration for the same factory name
- `EnableConversationEndpoints = true` on `MapPlayFramework(...)`

Important correction: the real builder method is parameterless `UseRepository()`. The sample backend is responsible for registering the repository separately.

## Backend URL configuration

The current sample app does not read `VITE_API_URL` yet.

Right now `src/App.tsx` hardcodes:

```text
http://localhost:5158/api/ai
```

There is also a Vite proxy entry in `vite.config.ts`, but the current app does not use that proxy path because it calls the backend through the hardcoded absolute URL.

## Relationship to the published TS client

This workspace consumes the library in:

- `src/AI/Rystem.PlayFramework.Client/src/rystem`

That library README is the real API documentation for:

- `PlayFrameworkServices`
- `PlayFrameworkClient`
- `ClientInteractionRegistry`
- `AIContentConverter`
- `usePlayFramework`

## Important caveats

### This folder is app docs, not package docs

If you are integrating PlayFramework into your own frontend, start from `src/AI/Rystem.PlayFramework.Client/src/rystem/README.md` instead.

### Vite commands are the source of truth

Use `npm run dev`, not `npm start`.

### Conversation UI depends on backend persistence

The conversation list, load, delete, and visibility features only work when the server exposes the optional conversation endpoints.

## Useful references

- `src/AI/Rystem.PlayFramework.Client/package.json`
- `src/AI/Test/Rystem.PlayFramework.Api/Program.cs`
- `src/AI/Rystem.PlayFramework.Client/src/rystem/README.md`

Use this folder when you want the sample frontend workspace; use the `src/rystem` package when you want the reusable client library.
