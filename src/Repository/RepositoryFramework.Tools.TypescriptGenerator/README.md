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

The `--models` option accepts repository definitions in the following format:

```
"{Model,Key,Type,Factory,BackendFactory},{Model2,Key2,Type2,Factory2,}"
```

**Fields:**
- **ModelName**: Name of the C# entity class. Can be simple (`Calendar`) or fully qualified (`Fantacalcio.Domain.Calendar`)
- **KeyName**: Name of the key type. Can be simple (`LeagueKey`) or fully qualified (`Fantacalcio.Domain.LeagueKey`)
- **RepositoryType**: One of `Repository`, `Query`, or `Command`
- **FactoryName**: Client-side name used in `RepositoryLocator.{factoryName}` (optional, defaults to ModelName)
- **BackendFactoryName**: Backend factory name used in API path (optional)

**API Path Generation:**
- If **BackendFactoryName is empty** (e.g., `{Rank,RankKey,Repository,rank,}`) → path = `Rank` (just ModelName)
- If **BackendFactoryName is set** (e.g., `{Rank,RankKey,Repository,rank,rank}`) → path = `Rank/rank` (ModelName/BackendFactory)

> **Note:** If multiple types with the same name exist in different namespaces, you must use the fully qualified name (with namespace) to avoid ambiguity.

### Examples

#### Single Repository (no backend factory - path will be just ModelName)

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{User,Guid,Repository,users,}"
```

This generates `x.path = 'User'` and `x.name = 'users'`.

#### With Backend Factory Name (path will be ModelName/BackendFactory)

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{User,Guid,Repository,users,users}"
```

This generates `x.path = 'User/users'` and `x.name = 'users'`.

#### Multiple Repositories with Mixed Backend Factories

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Calendar,LeagueKey,Repository,serieA,serieA},{Team,Guid,Query,teams,},{Match,int,Command,matches,matches}"
```

This generates:
- Calendar: `x.path = 'Calendar/serieA'`, `x.name = 'serieA'`
- Team: `x.path = 'Team'`, `x.name = 'teams'`
- Match: `x.path = 'Match/matches'`, `x.name = 'matches'`

#### With Fully Qualified Names (Namespaces)

Use fully qualified names when you have types with the same name in different namespaces:

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Fantacalcio.Domain.Calendar,Fantacalcio.Domain.LeagueKey,Repository,serieA}"
```

#### With Specific Project

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Product,int,Repository,products}" \
  --project ./src/MyApp.Core/MyApp.Core.csproj
```

#### Disable Overwrite

```bash
rystem-ts generate \
  --dest ./src/api \
  --models "{Order,Guid,Repository,orders}" \
  --overwrite false
```

## Generated Output

The tool generates the following structure:

```
📁 <destination>/
├── 📁 types/
│   ├── calendar.ts           # Raw + Clean interfaces + Mappers
│   ├── leaguekey.ts
│   ├── team.ts
│   └── dayofweek.ts          # Enums
├── 📁 transformers/
│   ├── CalendarTransformer.ts    # ITransformer<Calendar>
│   ├── LeagueKeyTransformer.ts   # ITransformer<LeagueKey>
│   └── index.ts
├── 📁 bootstrap/
│   └── repositorySetup.ts    # RepositoryServices configuration with transformers
└── 📁 services/
    └── repositoryLocator.ts  # RepositoryLocator.{factoryName} -> IRepository
```

### Generated Types

#### Types (`types/*.ts`)

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

#### Transformers (`transformers/*.ts`)

Transformers implement `ITransformer<T>` from `rystem.repository.client` to convert between Raw and Clean types:

```typescript
import type { ITransformer } from 'rystem.repository.client';
import type { Calendar, CalendarRaw } from '../types/calendar';
import { mapRawCalendarToCalendar, mapCalendarToRawCalendar } from '../types/calendar';

export const CalendarTransformer: ITransformer<Calendar> = {
  fromPlain: (plain: CalendarRaw): Calendar => mapRawCalendarToCalendar(plain),
  toPlain: (instance: Calendar): CalendarRaw => mapCalendarToRawCalendar(instance),
};
```

#### Bootstrap Setup (`bootstrap/repositorySetup.ts`)

The bootstrap file configures `RepositoryServices` with transformers for automatic JSON conversion:

```typescript
import { RepositoryServices } from 'rystem.repository.client';
import type { RepositoryEndpoint } from 'rystem.repository.client';
import { CalendarTransformer } from '../transformers/CalendarTransformer';
import { LeagueKeyTransformer } from '../transformers/LeagueKeyTransformer';

export interface RepositoryConfig {
  baseUrl: string;
  headersEnricher?: (...) => Promise<HeadersInit>;
  errorHandler?: (...) => Promise<boolean>;
}

export const setupRepositoryServices = (config: RepositoryConfig): void => {
  const services = RepositoryServices.Create(config.baseUrl);

  services.addRepository<Calendar, LeagueKey>(x => {
    x.name = 'calendar';
    x.path = 'calendar';
    x.transformer = CalendarTransformer;       // Auto-converts model
    x.keyTransformer = LeagueKeyTransformer;   // Auto-converts key
    x.complexKey = true;
    if (config.headersEnricher) x.addHeadersEnricher(config.headersEnricher);
    if (config.errorHandler) x.addErrorHandler(config.errorHandler);
  });
};
```

#### Repository Locator (`repositoryLocator.ts`)

Provides strongly-typed access to all configured repositories:

```typescript
import { RepositoryServices } from 'rystem.repository.client';
import type { IRepository, IQuery, ICommand } from 'rystem.repository.client';
import type { Calendar } from './types/calendar';
import type { LeagueKey } from './types/leaguekey';

export const RepositoryLocator = {
  get calendar(): IRepository<Calendar, LeagueKey> {
    return RepositoryServices.Repository<Calendar, LeagueKey>('calendar');
  },
  get teams(): IQuery<Team, string> {
    return RepositoryServices.Query<Team, string>('teams');
  },
  get orders(): ICommand<Order, number> {
    return RepositoryServices.Command<Order, number>('orders');
  },
} as const;
```

**Usage:**

```typescript
import { setupRepositoryServices } from './bootstrap/repositorySetup';
import { RepositoryLocator } from './repositoryLocator';

// 1. Setup once at app startup
setupRepositoryServices({
  baseUrl: 'https://api.example.com/api/',
  headersEnricher: async () => ({
    Authorization: `Bearer ${getToken()}`
  }),
  errorHandler: async (endpoint, uri, method, headers, body, err) => {
    if (err?.status === 401) {
      await refreshToken();
      return true; // Retry
    }
    return false;
  }
});

// 2. Use anywhere in your app with full type safety
const calendar = await RepositoryLocator.calendar.get({ leagueId: 'serie-a', season: 2024 });
const allTeams = await RepositoryLocator.teams.query().toListAsync();
await RepositoryLocator.orders.insert({ orderId: 123 }, { /* order data */ });
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
rystem-ts generate --dest ./src/api --models "{User,Guid,Repository,users}" --project ./src/MyApp.Core.csproj
```

### "Model not found in assembly"

Ensure:
1. The model class is `public`
2. The project has been built (`dotnet build`)
3. The model name matches exactly (case-sensitive)

### "Multiple types found with name 'X'"

This error occurs when you have multiple classes with the same name in different namespaces:

```
Multiple types found with name 'Calendar'. Please use the fully qualified name:
  - Fantacalcio.Domain.Calendar
  - OtherProject.Models.Calendar
```

**Solution:** Use the fully qualified name (with namespace) in your `--models` argument:

```bash
# Instead of:
--models "{Calendar,LeagueKey,Repository,serieA}"

# Use:
--models "{Fantacalcio.Domain.Calendar,Fantacalcio.Domain.LeagueKey,Repository,serieA}"
```

## Contributing

This tool is part of the [Rystem](https://github.com/KeyserDSoze/Rystem) framework.

## License

MIT License - see the main repository for details.
