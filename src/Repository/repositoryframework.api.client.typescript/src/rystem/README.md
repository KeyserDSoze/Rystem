# rystem.repository.client

[![npm](https://img.shields.io/npm/v/rystem.repository.client)](https://www.npmjs.com/package/rystem.repository.client)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

TypeScript/JavaScript client for [Rystem Repository Framework](https://github.com/KeyserDSoze/Rystem) API servers. Provides a typed, fluent interface for CRUD, querying, and batch operations against any Rystem-powered REST backend.

---

## Installation

```bash
npm install rystem.repository.client
```

---

## Quick Start

```typescript
import { RepositoryServices, useRepository } from 'rystem.repository.client';

// 1. Initialize the service (once, at app startup)
RepositoryServices
    .Create('https://your-api.example.com')
    .addRepository<User, string>(x => {
        x.name = 'users';
        x.path = '/api/User';
    });

// 2. Retrieve a typed repository instance
const repo = useRepository<User, string>('users');

// 3. Use it
const user = await repo.get('user-id-123');
const state = await repo.insert('new-id', { name: 'Alice', age: 30 });
```

---

## Setup — `RepositoryServices`

`RepositoryServices` is a singleton that manages all registered repositories.

### `RepositoryServices.Create(baseUri)`

Initializes the singleton with the base URI of the API server.

```typescript
RepositoryServices.Create('https://your-api.example.com')
```

### Adding Repositories

Chain registrations on the returned instance:

```typescript
RepositoryServices
    .Create('https://your-api.example.com')
    .addRepository<User, string>(x => {
        x.name = 'users';
        x.path = '/api/User';
    })
    .addCommand<Order, string>(x => {
        x.name = 'orders';
        x.path = '/api/Order';
    })
    .addQuery<Product, number>(x => {
        x.name = 'products';
        x.path = '/api/Product';
    });
```

| Method | Registered as | Exposes |
|---|---|---|
| `addRepository<T, TKey>(builder)` | Full repository | `get`, `exist`, `query`, `insert`, `update`, `delete`, `batch` |
| `addCommand<T, TKey>(builder)` | Command-only | `insert`, `update`, `delete`, `batch` |
| `addQuery<T, TKey>(builder)` | Query-only | `get`, `exist`, `query` |

---

## `RepositorySettings`

The `builder` callback receives a `RepositorySettings` instance. Configure it before the registration is stored.

```typescript
RepositoryServices.Create('https://api.example.com')
    .addRepository<User, string>(x => {
        x.name = 'users';           // required — service locator key
        x.path = '/api/User';       // path appended to baseUri
        x.case = 'CamelCase';       // JSON property casing (default: 'PascalCase')
        x.keySeparator = '---';     // separator for composite keys (default: '|||')
        x.complexKey = true;        // enables composite key serialization
        x.transformer = myTransformer;      // custom T serializer
        x.keyTransformer = myKeyTransformer; // custom TKey serializer
    });
```

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `name` | `string` | — | **Required.** Service locator key used when retrieving the repository. |
| `path` | `string \| null` | `null` | Path segment appended to the global `baseUri`. |
| `uri` | `string \| null` | `null` | Override the full base URI for this repository (ignores `baseUri + path`). |
| `case` | `'PascalCase' \| 'CamelCase'` | `'PascalCase'` | JSON property naming convention expected by the server. |
| `keySeparator` | `string` | `'|||'` | Delimiter when serializing composite (multi-part) keys. |
| `complexKey` | `boolean` | `false` | Set `true` when `TKey` is an object with multiple properties. |
| `transformer` | `ITransformer<T> \| undefined` | `undefined` | Custom serializer/deserializer for entity values. |
| `keyTransformer` | `ITransformer<TKey> \| undefined` | `undefined` | Custom serializer/deserializer for keys. |

### Headers Enricher

Add one or more functions to inject custom headers before each request (e.g., authentication tokens):

```typescript
x.addHeadersEnricher(async (endpoint, uri, method, headers, body) => {
    const token = await getAccessToken();
    return {
        ...headers as Record<string, string>,
        Authorization: `Bearer ${token}`,
    };
});
```

Multiple enrichers are chained — each receives the output of the previous one.

### Error Handler

Add one or more functions to intercept and handle request errors. Return `true` to retry the request:

```typescript
x.addErrorHandler(async (endpoint, uri, method, headers, body, err) => {
    if (err.status === 401) {
        await refreshToken();
        return true; // retry
    }
    return false; // do not retry
});
```

---

## `ITransformer<T>`

Custom serializer for mapping between your TypeScript model and the plain JSON the server sends or receives.

```typescript
interface ITransformer<T> {
    toPlain: (input: T | any) => any;
    fromPlain: (input: any) => T;
}
```

Example — mapping a `Date` field:

```typescript
const dateTransformer: ITransformer<User> = {
    toPlain: (user: User) => ({ ...user, birthDate: user.birthDate.toISOString() }),
    fromPlain: (plain: any): User => ({ ...plain, birthDate: new Date(plain.birthDate) }),
};

x.transformer = dateTransformer;
```

---

## Retrieving Instances

### Hooks (React)

```typescript
import {
    useRepository, useCommand, useQuery,
    useRepositoryPattern, useCommandPattern, useQueryPattern,
} from 'rystem.repository.client';

const repo    = useRepository<User, string>('users');
const command = useCommand<Order, string>('orders');
const query   = useQuery<Product, number>('products');

// Untyped variants (any/any)
const anyRepo = useRepositoryPattern('users');
```

### Static Accessors

```typescript
import { RepositoryServices } from 'rystem.repository.client';

const repo    = RepositoryServices.Repository<User, string>('users');
const command = RepositoryServices.Command<Order, string>('orders');
const query   = RepositoryServices.Query<Product, number>('products');

// Untyped
const anyRepo = RepositoryServices.RepositoryPattern('users');
```

---

## CRUD Operations

All write operations return `Promise<State<T, TKey>>`. `get` returns `Promise<T>`. `exist` returns `Promise<State<T, TKey>>`.

### `get`

```typescript
const user: User = await repo.get('user-id-123');
```

### `exist`

```typescript
const state = await repo.exist('user-id-123');
if (state.isOk) {
    console.log('Found:', state.entity.value);
}
```

### `insert`

```typescript
const state = await repo.insert('new-id', { name: 'Alice', age: 30 });
console.log(state.isOk, state.code, state.message);
```

### `update`

```typescript
const state = await repo.update('user-id-123', { name: 'Alice', age: 31 });
```

### `delete`

```typescript
const state = await repo.delete('user-id-123');
```

---

## Querying — `QueryBuilder`

Start a query with `repo.query()`. The builder is fully fluent.

```typescript
const users = await repo
    .query()
    .where()
        .select(u => u.name).equal('Alice')
        .and()
        .select(u => u.age).greaterThan(18)
    .build()
    .orderBy(u => u.name)
    .top(10)
    .execute();
```

### `QueryBuilder` Methods

| Method | Description |
|---|---|
| `where()` | Opens a `WhereBuilder` for building predicate conditions. |
| `filter(predicate: string)` | Raw C# lambda string predicate (e.g. `"_rystem => _rystem.Age > 18"`). |
| `top(value: number)` | Limits results to `value` items. |
| `skip(value: number)` | Skips the first `value` items (pagination offset). |
| `orderBy(predicate)` | Sorts ascending by the selected property. |
| `orderByDescending(predicate)` | Sorts descending by the selected property. |
| `thenBy(predicate)` | Secondary ascending sort. |
| `thenByDescending(predicate)` | Secondary descending sort. |
| `execute()` | Executes and returns `Promise<Array<Entity<T, TKey>>>`. |
| `executeAsStream(reader, token?)` | Streams results, calling `reader` for each entity as it arrives. |
| `count()` | Returns `Promise<number>` — total matching records. |
| `max(predicate)` | Returns `Promise<number>` — maximum value of selected property. |
| `min(predicate)` | Returns `Promise<number>` — minimum value of selected property. |
| `average(predicate)` | Returns `Promise<number>` — average of selected property. |
| `sum(predicate)` | Returns `Promise<number>` — sum of selected property. |

---

## Where Conditions — `WhereBuilder`

`where()` returns a `WhereBuilder`. Call `build()` to return to `QueryBuilder`, or call terminal methods directly.

### Selecting a Property

```typescript
.where().select(u => u.age)
```

### Comparison Operators

| Method | Description |
|---|---|
| `equal(value)` | Property `==` value |
| `notEqual(value)` | Property `!=` value |
| `greaterThan(value)` | Property `>` value |
| `greaterThanOrEqual(value)` | Property `>=` value |
| `lesserThan(value)` | Property `<` value |
| `lesserThanOrEqual(value)` | Property `<=` value |
| `contains(value)` | String contains value |
| `startsWith(value)` | String starts with value |
| `endsWith(value)` | String ends with value |

### Logical Operators

| Method | Description |
|---|---|
| `and()` | Appends `&&` between conditions |
| `or()` | Appends `\|\|` between conditions |
| `openRoundBracket()` | Opens a grouping `(` |
| `closeRoundBracket()` | Closes a grouping `)` |

### Building and Executing

| Method | Description |
|---|---|
| `build()` | Returns the parent `QueryBuilder` with the filter applied. |
| `execute()` | Shorthand for `build().execute()`. |
| `executeAsStream(reader, token?)` | Shorthand for `build().executeAsStream(...)`. |
| `count()` | Shorthand for `build().count()`. |
| `max(predicate)` | Shorthand for `build().max(...)`. |
| `min(predicate)` | Shorthand for `build().min(...)`. |
| `average(predicate)` | Shorthand for `build().average(...)`. |
| `sum(predicate)` | Shorthand for `build().sum(...)`. |

### Complex Filter Example

```typescript
const results = await repo
    .query()
    .where()
        .openRoundBracket()
            .select(u => u.role).equal('admin')
            .or()
            .select(u => u.role).equal('moderator')
        .closeRoundBracket()
        .and()
        .select(u => u.age).greaterThanOrEqual(18)
    .execute();
```

---

## Batch Operations — `BatchBuilder`

```typescript
const results = await repo
    .batch()
    .addInsert('id-1', { name: 'Alice', age: 30 })
    .addUpdate('id-2', { name: 'Bob',   age: 25 })
    .addDelete('id-3')
    .execute();
```

### `BatchBuilder` Methods

| Method | Description |
|---|---|
| `addInsert(key, value)` | Queues an insert operation. |
| `addUpdate(key, value)` | Queues an update operation. |
| `addDelete(key)` | Queues a delete operation. |
| `execute()` | Sends the batch and returns `Promise<Array<BatchResult<T, TKey>>>`. |
| `executeAsStream(reader, token?)` | Streams results, calling `reader` for each `BatchResult` as it arrives. |

---

## Model Types

### `Entity<T, TKey>`

Wraps a value and its key as returned by `query().execute()`.

```typescript
type Entity<T, TKey> = {
    value: T;
    key: TKey;
};
```

### `State<T, TKey>`

Returned by `insert`, `update`, `delete`, and `exist`.

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

| Field | Description |
|---|---|
| `isOk` | `true` if the operation succeeded. |
| `entity` | The entity snapshot at the time of the operation. |
| `code` | HTTP-style status code, or `null`. |
| `message` | Optional server message or error description. |

### `BatchResult<T, TKey>`

One result per operation in a batch execution.

```typescript
type BatchResult<T, TKey> = {
    code: 1 | 2 | 4;   // 1 = Insert, 2 = Update, 4 = Delete
    key: TKey;
    state: State<T, TKey>;
};
```

---

## Full Example

```typescript
import { RepositoryServices, useRepository } from 'rystem.repository.client';

interface User {
    id: string;
    name: string;
    age: number;
    role: string;
}

// Bootstrap (once, e.g. in main.ts or App.tsx)
RepositoryServices
    .Create('https://api.example.com')
    .addRepository<User, string>(x => {
        x.name = 'users';
        x.path = '/api/User';
        x.case = 'CamelCase';
        x.addHeadersEnricher(async (_endpoint, _uri, _method, headers, _body) => ({
            ...headers as Record<string, string>,
            Authorization: `Bearer ${localStorage.getItem('token')}`,
        }));
    });

// Use anywhere in the app
const repo = useRepository<User, string>('users');

// Insert
await repo.insert('alice', { id: 'alice', name: 'Alice', age: 30, role: 'admin' });

// Get
const user = await repo.get('alice');
console.log(user.name); // 'Alice'

// Query with filter
const admins = await repo
    .query()
    .where()
        .select(u => u.role).equal('admin')
        .and()
        .select(u => u.age).greaterThanOrEqual(18)
    .build()
    .orderBy(u => u.name)
    .execute();

// Count
const total = await repo.query().count();

// Aggregates
const avgAge = await repo
    .query()
    .where().select(u => u.role).equal('admin').average(u => u.age);

// Batch
const batchResults = await repo
    .batch()
    .addInsert('bob', { id: 'bob', name: 'Bob', age: 25, role: 'user' })
    .addDelete('old-user')
    .execute();

for (const result of batchResults) {
    console.log(result.code, result.state.isOk);
}
```

---

## License

MIT © Alessandro Rapiti
