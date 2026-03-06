# Rystem.RepositoryFramework.TypescriptGenerator

CLI tool that generates TypeScript models and service wrappers from C# Repository/CQRS models.

> **Requires .NET 10 SDK or later.**

## Prerequisites

Install the `rystem.repository.client` npm package in your frontend project:

```bash
npm install rystem.repository.client
```

## Installation

```bash
# Global
dotnet tool install -g Rystem.RepositoryFramework.TypescriptGenerator

# Local
dotnet new tool-manifest
dotnet tool install Rystem.RepositoryFramework.TypescriptGenerator
```

## Update / uninstall

```bash
# Update
dotnet tool update -g Rystem.RepositoryFramework.TypescriptGenerator

# Uninstall
dotnet tool uninstall -g Rystem.RepositoryFramework.TypescriptGenerator
```

## Command

```bash
rystem-ts generate --dest <destination> --models <definitions> [options]
```

## Options

| Option | Alias | Required | Description |
| --- | --- | --- | --- |
| `--dest` | `-d` | Yes | Destination folder |
| `--models` | `-m` | Yes | Repository definitions |
| `--project` | `-p` | No | Path to `.csproj` or `.dll` |
| `--overwrite` |  | No | Overwrite existing files (default `true`) |
| `--include-deps` |  | No | Scan referenced dependencies (default `false`) |
| `--deps-prefix` |  | No | Prefix filter when `--include-deps` is enabled |

## Model definition format

```text
"{Model,Key,Type[,Factory[,BackendFactory]]},{Model2,Key2,Type2,...}"
```

- `Model` (**required**): C# entity type name (simple or fully qualified)
- `Key` (**required**): C# key type name or primitive (`string`, `Guid`, `int`, etc.)
- `Type` (**required**): `Repository`, `Query`, or `Command`
- `Factory` (optional): client-side name used by the generated locator — defaults to `Model`
- `BackendFactory` (optional): backend factory segment used in the endpoint path

Legacy bracket format `[{...},{...}]` is also accepted.

## Examples

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{User,Guid,Repository,users}"
```

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Calendar,LeagueKey,Repository,serieA,serieA},{Team,Guid,Query,teams}" \
  --project ./src/MyApp.Api/MyApp.Api.csproj \
  --include-deps \
  --deps-prefix "MyCompany."
```

## Generated structure

```text
<destination>/
  types/
    DateMappers.ts          # date parse/format utilities (always generated)
    {ModelName}.ts          # per-model file (see below)
    index.ts
  transformers/
    {ModelName}Transformer.ts
    index.ts
  services/
    repositoryLocator.ts    # typed RepositoryLocator object
  bootstrap/
    repositorySetup.ts      # setupRepositoryServices() configuration
```

### Per-model type file contents

Each `types/{ModelName}.ts` file can contain:

- **Enums** — TypeScript `enum` declarations
- **Raw interfaces** — property names and types that match the JSON wire format exactly (generated only when a model has `[JsonPropertyName]` attributes or `DateTime`/`DateOnly`/`DateTimeOffset` properties)
- **Clean interfaces** — readable TypeScript interfaces (camelCase names, `Date` instead of `string` for date types)
- **Mapping functions** — `mapRaw{Name}To{Name}` / `map{Name}ToRaw{Name}` converters between Raw and Clean
- **Helper classes** — factory/builder helpers for creating model instances

## Integration with Repository API

### Server (.NET)

```csharp
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```

### Client setup (generated `bootstrap/repositorySetup.ts`)

Minimal usage:

```typescript
import { setupRepositoryServices } from "./bootstrap/repositorySetup";

setupRepositoryServices({
  baseUrl: "https://localhost:7058/api/"
});
```

Full `RepositoryConfig` options:

```typescript
setupRepositoryServices({
  baseUrl: "https://localhost:7058/api/",

  // Optional: enrich request headers (e.g. add Authorization)
  headersEnricher: async (endpoint, uri, method, headers, body) => ({
    ...headers,
    Authorization: `Bearer ${getToken()}`
  }),

  // Optional: handle errors (return true to retry)
  errorHandler: async (endpoint, uri, method, headers, body, err) => {
    if (err?.status === 401) {
      await refreshToken();
      return true; // retry the request
    }
    return false;
  }
});
```

### Typed repository access (`services/repositoryLocator.ts`)

```typescript
import { RepositoryLocator } from "./services/repositoryLocator";

// IRepository — full CRUD
const user = await RepositoryLocator.users.get("some-guid");
const all   = await RepositoryLocator.users.query().toListAsync();
await RepositoryLocator.users.insert("id", newUser);
await RepositoryLocator.users.delete("id");

// IQuery — read-only
const teams = await RepositoryLocator.teams.query().toListAsync();

// ICommand — write-only
await RepositoryLocator.orders.insert("id", newOrder);
```

## Troubleshooting

- `Project file not found`: verify `--project` path.
- `Multiple .csproj files found`: pass explicit `--project`.
- `Model not found in assembly`: build the project first and verify type names.
- `Multiple types found`: use fully qualified names (e.g. `MyApp.Core.User`) in `--models`.

