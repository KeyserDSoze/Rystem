# Rystem TypeScript Generator

CLI tool to generate TypeScript types and services from C# Rystem Repository/CQRS models.

## Installation

### Global Installation (recommended)

```bash
dotnet tool install -g Rystem.RepositoryFramework.TypescriptGenerator
```

### Local Installation (per-project)

```bash
dotnet new tool-manifest # if you don't have one already
dotnet tool install Rystem.RepositoryFramework.TypescriptGenerator
```

### Update

```bash
# Global
dotnet tool update -g Rystem.RepositoryFramework.TypescriptGenerator

# Local
dotnet tool update Rystem.RepositoryFramework.TypescriptGenerator
```

### Uninstall

```bash
# Global
dotnet tool uninstall -g Rystem.RepositoryFramework.TypescriptGenerator

# Local
dotnet tool uninstall Rystem.RepositoryFramework.TypescriptGenerator
```

## Usage

### Basic Syntax

```bash
rystem-ts generate --dest <destination> --models <model-definitions> [options]
```

### Options

| Option | Alias | Required | Description |
|--------|-------|----------|-------------|
| `--dest` | `-d` | ✅ | Destination folder for generated TypeScript files |
| `--models` | `-m` | ✅ | Repository definitions (see format below) |
| `--project` | `-p` | ❌ | Path to C# project (.csproj) or assembly (.dll) |
| `--overwrite` | | ❌ | Overwrite existing files (default: `true`) |

### Model Definition Format

The `--models` option accepts a JSON-like array of repository definitions:

```
[{ModelName,KeyName,RepositoryType,FactoryName},{...}]
```

**Fields:**
- **ModelName**: Name of the C# entity class (e.g., `Calendar`, `User`)
- **KeyName**: Name of the key type (e.g., `Guid`, `int`, `LeagueKey`)
- **RepositoryType**: One of `Repository`, `Query`, or `Command`
- **FactoryName**: Name for the generated service factory

### Examples

#### Single Repository

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "[{User,Guid,Repository,users}]"
```

#### Multiple Repositories

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "[{Calendar,LeagueKey,Repository,serieA},{Team,Guid,Query,teams},{Match,int,Command,matches}]"
```

#### With Specific Project

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "[{Product,int,Repository,products}]" \
  --project ./src/MyApp.Core/MyApp.Core.csproj
```

#### Disable Overwrite

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "[{Order,Guid,Repository,orders}]" \
  --overwrite false
```

## Generated Output

The tool generates the following structure:

```
📁 <destination>/
├── 📁 models/
│   ├── Calendar.ts           # Raw + Clean interfaces + Mapper
│   ├── LeagueKey.ts
│   ├── Team.ts
│   └── DayOfWeek.ts          # Enums
├── 📁 services/
│   ├── common.ts             # Entity, State, BatchOperation, Page, QueryOptions
│   └── serieA.service.ts     # Service class with API methods
└── index.ts                  # Services registry with lazy singleton pattern
```

### Generated Types

#### Models (`models/*.ts`)

For each C# model, the tool generates:

```typescript
// Raw interface (matches JSON from API)
export interface CalendarRaw {
  league_id: string;           // Uses JsonPropertyName if present
  start_date: string;          // Dates as strings
  teams: TeamRaw[];
}

// Clean interface (TypeScript-friendly)
export interface Calendar {
  leagueId: string;            // camelCase names
  startDate: Date;             // Proper Date type
  teams: Team[];
}

// Mapper function
export function mapCalendar(raw: CalendarRaw): Calendar {
  return {
    leagueId: raw.league_id,
    startDate: new Date(raw.start_date),
    teams: raw.teams.map(mapTeam),
  };
}
```

#### Common Types (`services/common.ts`)

```typescript
export interface Entity<T, TKey> {
  key: TKey;
  value: T | null;
}

export interface State<T> {
  isOk: boolean;
  message?: string;
  value?: T;
}

export interface Page<T, TKey> {
  items: Entity<T, TKey>[];
  totalCount: number;
  pageSize: number;
  pageIndex: number;
  totalPages: number;
}

export interface BatchOperation<T, TKey> {
  command: 'insert' | 'update' | 'delete';
  key: TKey;
  value?: T;
}
```

#### Services (`services/*.service.ts`)

```typescript
export class SerieAService {
  private baseUrl: string;
  
  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  // Query methods
  async get(key: LeagueKey): Promise<Calendar | null> { ... }
  async query(options?: QueryOptions): Promise<Entity<Calendar, LeagueKey>[]> { ... }
  async page(pageIndex: number, pageSize: number): Promise<Page<Calendar, LeagueKey>> { ... }
  async exist(key: LeagueKey): Promise<State<boolean>> { ... }
  async count(): Promise<number> { ... }

  // Command methods (Repository/Command only)
  async insert(key: LeagueKey, value: Calendar): Promise<State<Calendar>> { ... }
  async update(key: LeagueKey, value: Calendar): Promise<State<Calendar>> { ... }
  async delete(key: LeagueKey): Promise<State<boolean>> { ... }
  async batch(operations: BatchOperation<Calendar, LeagueKey>[]): Promise<BatchResult[]> { ... }
}
```

#### Services Registry (`index.ts`)

```typescript
export interface ServiceConfig {
  baseUrl: string;
}

export class Services {
  private static config: ServiceConfig | null = null;
  private static _serieA: SerieAService | null = null;

  static configure(config: ServiceConfig): void {
    this.config = config;
  }

  static get serieA(): SerieAService {
    this.ensureConfigured();
    if (!this._serieA) {
      this._serieA = new SerieAService(this.config!.baseUrl);
    }
    return this._serieA;
  }
}

// Usage:
Services.configure({ baseUrl: 'https://api.example.com' });
const calendar = await Services.serieA.get({ leagueId: 'serie-a' });
```

## Repository Types

| Type | Query Methods | Command Methods |
|------|---------------|-----------------|
| `Repository` | ✅ get, query, page, exist, count | ✅ insert, update, delete, batch |
| `Query` | ✅ get, query, page, exist, count | ❌ |
| `Command` | ❌ | ✅ insert, update, delete, batch |

## C# Model Requirements

### Basic Model

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### With JsonPropertyName (for Raw types)

```csharp
public class User
{
    [JsonPropertyName("user_id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("full_name")]
    public string Name { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

### Custom Key Types

```csharp
public record LeagueKey(string LeagueId, int Season);
```

### Enums

```csharp
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3
}
```

## Integration with Rystem Repository API

This tool is designed to work with [Rystem.RepositoryFramework.Api.Server](../RepositoryFramework.Api.Server/README.md).

### Server Setup

```csharp
// Program.cs
builder.Services.AddRepository<Calendar, LeagueKey>(settings =>
{
    settings.WithEntityFramework<AppDbContext>();
});

// Expose as REST API
app.UseEndpointsForRepositoryPattern();
```

### Client Usage

```typescript
// Setup (once at app startup)
Services.configure({ 
  baseUrl: 'https://localhost:5001/api' 
});

// Use anywhere in your app
const calendars = await Services.serieA.query();
const calendar = await Services.serieA.get({ leagueId: 'serie-a', season: 2024 });

// Create/Update
await Services.serieA.insert(
  { leagueId: 'serie-a', season: 2025 },
  { startDate: new Date(), teams: [] }
);
```

## Troubleshooting

### "Project file not found"

Make sure the `--project` path is correct, or run the command from a directory containing a `.csproj` file.

### "Multiple .csproj files found"

Specify which project to use with `--project`:

```bash
rystem-ts generate --dest ./src/api --models "[{User,Guid,Repository,users}]" --project ./src/MyApp.Core.csproj
```

### "Model not found in assembly"

Ensure:
1. The model class is `public`
2. The project has been built (`dotnet build`)
3. The model name matches exactly (case-sensitive)

## Contributing

This tool is part of the [Rystem](https://github.com/KeyserDSoze/Rystem) framework.

## License

MIT License - see the main repository for details.
