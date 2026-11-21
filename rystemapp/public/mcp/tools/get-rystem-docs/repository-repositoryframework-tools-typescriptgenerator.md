# Rystem.RepositoryFramework.TypescriptGenerator

`Rystem.RepositoryFramework.TypescriptGenerator` is a .NET CLI tool that inspects C# Repository Framework models and emits a TypeScript client layer for `rystem.repository.client`.

It generates:

- TypeScript model files
- raw/clean mappers when JSON names or date types require them
- transformers for models and complex keys
- `bootstrap/repositorySetup.ts`
- `services/repositoryLocator.ts`

## Installation

```bash
# global
dotnet tool install -g Rystem.RepositoryFramework.TypescriptGenerator

# local
dotnet new tool-manifest
dotnet tool install Rystem.RepositoryFramework.TypescriptGenerator
```

The tool command name is:

```bash
rystem-ts
```

## Prerequisites

- .NET 10 SDK
- the frontend package `rystem.repository.client`

```bash
npm install rystem.repository.client
```

## What the tool actually does

The generator builds your target project, loads the produced assembly, analyzes the requested model and key types with reflection, then writes TypeScript files to the destination folder.

Practical consequence:

- generation is not metadata-only
- your target project must build successfully

## Command

```bash
rystem-ts generate --dest <destination> --models <definitions> [options]
```

### Options

| Option | Alias | Required | Notes |
| --- | --- | --- | --- |
| `--dest` | `-d` | yes | Output folder |
| `--models` | `-m` | yes | Repository definitions |
| `--project` | `-p` | recommended | Path to the `.csproj` to build |
| `--overwrite` |  | no | Defaults to `true` |
| `--include-deps` |  | no | Also scan built dependencies in the output folder |
| `--deps-prefix` |  | no | Filter dependency loading by prefix when `--include-deps` is enabled |

## Model definition format

```text
"{Model,Key,Type[,Factory[,BackendFactory]]},{Model2,Key2,Type2,...}"
```

Fields:

- `Model`: C# model type name, simple or fully qualified
- `Key`: C# key type name or primitive like `string`, `Guid`, `int`
- `Type`: `Repository`, `Query`, or `Command`
- `Factory`: optional client-side registration name; defaults to `Model`
- `BackendFactory`: optional server factory segment appended to the generated API path

Legacy bracket form like `[{...},{...}]` is also accepted.

## Examples

```bash
rystem-ts generate \
  --project ./src/MyApi/MyApi.csproj \
  --dest ./src/generated/repository \
  --models "{User,Guid,Repository,users}"
```

```bash
rystem-ts generate \
  --project ./src/MyApi/MyApi.csproj \
  --dest ./src/generated/repository \
  --models "{Calendar,LeagueKey,Repository,serieA,serieA},{Team,Guid,Query,teams}" \
  --include-deps \
  --deps-prefix "MyCompany."
```

## Output structure

The generator writes these folders:

```text
<destination>/
  types/
    DateMappers.ts
    <lowercase-model-file>.ts
    index.ts
  transformers/
    <ModelName>Transformer.ts
    index.ts               # only when transformers are emitted
  bootstrap/
    repositorySetup.ts
  services/
    repositoryLocator.ts
```

Notes:

- model filenames are lowercase base names, not necessarily exact CLR names
- `transformers/index.ts` is written only when there are transformers to export

## Generated TypeScript model shape

Depending on the analyzed type, each `types/*.ts` file can contain:

- TypeScript `enum`s
- raw interfaces that match wire JSON names
- clean interfaces with camel-cased property names
- raw/clean mapper functions
- helper classes for nested arrays and dictionaries

The generator emits raw + clean models only when it needs them, mainly for:

- `System.Text.Json` `[JsonPropertyName]` differences
- `DateTime`, `DateTimeOffset`, or `DateOnly`

## Type mapping behavior

Important mappings from the current implementation:

- `string`, `char` -> `string`
- numeric primitives and `decimal` -> `number`
- `bool` -> `boolean`
- `Guid` -> `string`
- `DateTime`, `DateTimeOffset`, `DateOnly` -> raw `string`, clean `Date`
- `TimeSpan`, `TimeOnly` -> `string`
- non-primitive custom objects -> generated interfaces
- arrays -> `T[]`
- dictionaries -> `Record<K, V>`
- enums -> numeric TypeScript `enum`

Non-primitive nullable/reference handling is simplified on the TypeScript side, so review emitted optional properties when null-vs-missing distinctions matter.

## Generic types

The generator understands open and closed generics.

- it emits the open generic model file once
- closed generic repository registrations reuse that base model
- generated bootstrap/transformers pass mapper functions for generic arguments when needed

## Generated bootstrap

`bootstrap/repositorySetup.ts` wires `RepositoryServices` for you.

```typescript
import { setupRepositoryServices } from "./bootstrap/repositorySetup";

setupRepositoryServices({
  baseUrl: "https://localhost:7058/api/",
});
```

The generated config surface is intentionally small:

```typescript
setupRepositoryServices({
  baseUrl: "https://localhost:7058/api/",
  headersEnricher: async (endpoint, uri, method, headers, body) => ({
    ...headers,
    Authorization: `Bearer ${getToken()}`,
  }),
  errorHandler: async (endpoint, uri, method, headers, body, err) => {
    if (err?.status === 401) {
      await refreshToken();
      return true;
    }
    return false;
  },
});
```

## Generated repository locator

`services/repositoryLocator.ts` exposes typed accessors over `RepositoryServices`.

```typescript
import { RepositoryLocator } from "./services/repositoryLocator";

const user = await RepositoryLocator.users.get("some-id");
const items = await RepositoryLocator.users.query().execute();
await RepositoryLocator.users.insert("some-id", userToSave);
```

It generates the correct client registration method based on the descriptor type:

- `Repository` -> `addRepository`
- `Query` -> `addQuery`
- `Command` -> `addCommand`

## Server-side alignment

The generated paths are meant for Repository Framework API endpoints.

Typical server setup:

```csharp
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```

## Important caveats

### `--project` is effectively required in most real runs

The CLI can auto-discover a `.csproj` only when exactly one exists in the current directory. In practice, passing `--project` explicitly is the reliable path.

### `.dll` input is not a true standalone flow today

The option text says `--project` can point to a `.csproj` or `.dll`, but the pipeline still performs `dotnet build` logic around a project path. Treat `.csproj` as the supported input.

### The tool always builds in `Release`

It invokes `dotnet build -c Release --no-restore` before analyzing output.

### Generated config surface is smaller than the runtime supports

The emitted `RepositoryConfig` only covers:

- `baseUrl`
- `headersEnricher`
- `errorHandler`

If you need runtime settings like custom `uri`, casing, or key separator tweaks, you may need to extend the generated bootstrap manually.

## When to use this tool

Use it when you want:

- TypeScript models kept in sync with Repository Framework DTOs
- ready-made `rystem.repository.client` bootstrap code
- generated transformers for dates, JSON-name differences, and complex keys

If you need full control over the frontend surface, treat the generated files as a starting point and commit only the parts you want to keep stable.
