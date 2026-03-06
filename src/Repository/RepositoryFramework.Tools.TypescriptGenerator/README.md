# Rystem.RepositoryFramework.TypescriptGenerator

CLI tool that generates TypeScript models and service wrappers from C# Repository/CQRS models.

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
| `--include-deps` |  | No | Scan referenced dependencies |
| `--deps-prefix` |  | No | Prefix filter when `--include-deps` is enabled |

## Model definition format

```text
"{Model,Key,Type,Factory,BackendFactory},{Model2,Key2,Type2,Factory2,}"
```

- `Model`: C# entity type name (simple or fully qualified)
- `Key`: C# key type name
- `Type`: `Repository`, `Query`, or `Command`
- `Factory`: client-side name used by generated locator
- `BackendFactory`: optional backend factory segment used in endpoint path

## Examples

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{User,Guid,Repository,users,}"
```

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Calendar,LeagueKey,Repository,serieA,serieA},{Team,Guid,Query,teams,}" \
  --project ./src/MyApp.Api/MyApp.Api.csproj \
  --include-deps \
  --deps-prefix "MyCompany."
```

## Generated structure

```text
<destination>/
  types/
  transformers/
  services/
  bootstrap/
  repositoryLocator.ts
```

## Integration with Repository API

Server example:

```csharp
builder.Services.AddRepository<User, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```

Client setup (generated `bootstrap/repositorySetup.ts` pattern):

```typescript
import { setupRepositoryServices } from "./bootstrap/repositorySetup";

setupRepositoryServices({
  baseUrl: "https://localhost:7058/api/"
});
```

## Troubleshooting

- `Project file not found`: verify `--project` path.
- `Multiple .csproj files found`: pass explicit `--project`.
- `Model not found in assembly`: build project and verify type names.
- `Multiple types found`: use fully qualified names in `--models`.

