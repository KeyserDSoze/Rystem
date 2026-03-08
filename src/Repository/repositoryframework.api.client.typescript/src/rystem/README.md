# rystem.repository.client

`rystem.repository.client` is the TypeScript client runtime for Repository Framework API servers.

It gives you:

- a singleton repository registry (`RepositoryServices`)
- typed repository/query/command clients
- a fluent query builder
- a fluent batch builder
- optional transformers for values and keys

## Installation

```bash
npm install rystem.repository.client
```

## What the package exports

Runtime exports:

- `RepositoryServices`
- `RepositorySettings`
- `RepositoryEndpoint`
- `useRepository`, `useCommand`, `useQuery`
- `useRepositoryPattern`, `useCommandPattern`, `useQueryPattern`

Type exports:

- `IRepository`, `ICommand`, `IQuery`
- `IRepositoryPattern`, `ICommandPattern`, `IQueryPattern`
- `Entity`
- `State`
- `BatchResult`
- `ITransformer`

## Bootstrap pattern

The package uses one singleton service registry.

```typescript
import { RepositoryServices } from 'rystem.repository.client';

RepositoryServices
  .Create('https://localhost:7058/api/')
  .addRepository<User, string>(settings => {
    settings.name = 'users';
    settings.path = '/User';
  });
```

Then retrieve the typed client by name:

```typescript
const repo = RepositoryServices.Repository<User, string>('users');
```

## Repository registration

Available registration methods:

| Method | Result |
| --- | --- |
| `addRepository<T, TKey>(...)` | full `IRepository<T, TKey>` |
| `addCommand<T, TKey>(...)` | `ICommand<T, TKey>` |
| `addQuery<T, TKey>(...)` | `IQuery<T, TKey>` |

Each registration builds one client instance and stores it under `settings.name`.

## `RepositorySettings`

`RepositorySettings` includes:

| Property | Default | Notes |
| --- | --- | --- |
| `name` | required | Lookup key inside `RepositoryServices` |
| `uri` | `null` | Full URI override for this repository |
| `path` | `null` | Appended to the global `baseUri` when `uri` is not set |
| `case` | `PascalCase` | Stored setting; the runtime does not actively transform payload casing by itself |
| `transformer` | `undefined` | Value transformer for `T` |
| `keyTransformer` | `undefined` | Transformer for `TKey` |
| `keySeparator` | `|||` | Used when flattening non-primitive querystring keys |
| `complexKey` | `false` | Switches key-based operations from querystring to JSON body |

## Basic example

```typescript
import { RepositoryServices } from 'rystem.repository.client';

interface User {
  id: string;
  name: string;
  age: number;
}

RepositoryServices
  .Create('https://api.example.com/api/')
  .addRepository<User, string>(settings => {
    settings.name = 'users';
    settings.path = '/User';
  });

const repo = RepositoryServices.Repository<User, string>('users');

const user = await repo.get('alice');
const created = await repo.insert('alice', { id: 'alice', name: 'Alice', age: 30 });
```

## Hooks

The package exports `useRepository`, `useCommand`, and `useQuery`, plus untyped `use*Pattern` helpers.

```typescript
import { useRepository, useQuery, useCommand } from 'rystem.repository.client';

const repo = useRepository<User, string>('users');
const query = useQuery<User, string>('usersQuery');
const command = useCommand<User, string>('usersCommand');
```

Important note: these are simple accessor functions over `RepositoryServices`. They are named like React hooks, but they do not use React state, effects, or context.

## CRUD API

`IQuery<T, TKey>`:

- `get(key)` -> `Promise<T>`
- `exist(key)` -> `Promise<State<T, TKey>>`
- `query()` -> `QueryBuilder<T, TKey>`

`ICommand<T, TKey>`:

- `insert(key, value)` -> `Promise<State<T, TKey>>`
- `update(key, value)` -> `Promise<State<T, TKey>>`
- `delete(key)` -> `Promise<State<T, TKey>>`
- `batch()` -> `BatchBuilder<T, TKey>`

## Query builder

Start with `repo.query()`.

```typescript
const items = await repo
  .query()
  .where()
    .select(x => x.name).equal('Alice')
    .and()
    .select(x => x.age).greaterThanOrEqual(18)
  .build()
  .orderBy(x => x.name)
  .top(10)
  .execute();
```

Supported fluent methods:

- `where()`
- `filter(predicate: string)`
- `top(value)`
- `skip(value)`
- `orderBy(predicate)`
- `orderByDescending(predicate)`
- `thenBy(predicate)`
- `thenByDescending(predicate)`
- `execute()`
- `executeAsStream(reader, token?)`
- `count()`
- `max(predicate)`
- `min(predicate)`
- `average(predicate)`
- `sum(predicate)`

## How query serialization works

The builder serializes filters as string expressions intended for the Repository Framework server.

Examples:

- `orderBy(x => x.name)` becomes a string like `_rystem => _rystem.name`
- `where().select(x => x.age).greaterThan(18)` becomes a raw lambda string

So this client works best with simple property-access lambdas.

## Where builder

`where()` returns a `WhereBuilder<T, TKey>`.

```typescript
const adults = await repo
  .query()
  .where()
    .openRoundBracket()
      .select(x => x.role).equal('admin')
      .or()
      .select(x => x.role).equal('moderator')
    .closeRoundBracket()
    .and()
    .select(x => x.age).greaterThanOrEqual(18)
  .execute();
```

Comparison methods:

- `equal`
- `notEqual`
- `greaterThan`
- `greaterThanOrEqual`
- `lesserThan`
- `lesserThanOrEqual`
- `contains`
- `startsWith`
- `endsWith`

## Batch builder

```typescript
const results = await repo
  .batch()
  .addInsert('a', { id: 'a', name: 'Alice', age: 30 })
  .addUpdate('b', { id: 'b', name: 'Bob', age: 31 })
  .addDelete('c')
  .execute();
```

Supported methods:

- `addInsert(key, value)`
- `addUpdate(key, value)`
- `addDelete(key)`
- `execute()`
- `executeAsStream(reader, token?)`

## Transformers

Use `ITransformer<T>` when the wire JSON should differ from the TypeScript model.

```typescript
import type { ITransformer } from 'rystem.repository.client';

const userTransformer: ITransformer<User> = {
  toPlain: (user) => ({
    ...user,
    createdAt: user.createdAt.toISOString(),
  }),
  fromPlain: (plain) => ({
    ...plain,
    createdAt: new Date(plain.createdAt),
  }),
};
```

Register it:

```typescript
RepositoryServices
  .Create('https://api.example.com/api/')
  .addRepository<User, string>(settings => {
    settings.name = 'users';
    settings.path = '/User';
    settings.transformer = userTransformer;
  });
```

## Complex keys

For primitive keys, `get`, `exist`, and `delete` default to querystring calls like:

- `Get?key=...`
- `Exist?key=...`
- `Delete?key=...`

For complex keys, set:

```typescript
settings.complexKey = true;
settings.keyTransformer = complexKeyTransformer;
```

Then key-based operations switch to JSON-body requests instead of querystring serialization.

This is the safer mode for object keys.

## Header enrichers

You can register async header enrichers.

```typescript
settings.addHeadersEnricher(async (endpoint, uri, method, headers, body) => {
  const token = await getToken();
  return {
    ...headers as Record<string, string>,
    Authorization: `Bearer ${token}`,
  };
});
```

Important implementation note: enrichers are chained, but later enrichers do not overwrite headers that are already present.

## Error handlers

You can register async error handlers.

```typescript
settings.addErrorHandler(async (endpoint, uri, method, headers, body, err) => {
  if (err?.status === 401) {
    await refreshToken();
    return true;
  }
  return false;
});
```

Important note: the runtime is clearly designed for retry-on-`true`, but the current `makeRequest(...)` implementation does not reliably perform the retry loop. Treat error handlers as interception hooks first, and verify retry behavior in your app before depending on it.

## Response models

`Entity<T, TKey>`:

```typescript
type Entity<T, TKey> = {
  value: T;
  key: TKey;
};
```

`State<T, TKey>`:

```typescript
type State<T, TKey> = {
  isOk: boolean;
  entity: {
    key: TKey;
    value: T;
  };
  code: number | null;
  message: string | null;
};
```

`BatchResult<T, TKey>`:

```typescript
type BatchResult<T, TKey> = {
  code: 1 | 2 | 4;
  key: TKey;
  state: State<T, TKey>;
};
```

## Streaming note

`executeAsStream(...)` exists for query and batch operations and reads streamed JSON objects from the server.

The current stream decoder is simple and works best with normal Repository Framework streamed payloads. If your payload strings themselves contain many `{` or `}` characters, test carefully.

## When to use this package

Use it when you want:

- a lightweight TypeScript client for Repository Framework APIs
- a central registry for typed repositories
- fluent query and batch helpers without generating a custom SDK first

If you want generated typed bootstrap and transformers from your C# models, pair it with `Rystem.RepositoryFramework.TypescriptGenerator`.
