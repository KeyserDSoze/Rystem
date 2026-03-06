# rystem.repository.client

TypeScript client for Repository Framework APIs.

## Installation

```bash
npm install rystem.repository.client
```

## Service setup

```typescript
import { RepositoryServices } from "rystem.repository.client";

RepositoryServices
  .Create("https://localhost:7058/api/")
  .addRepository<User, string>(x => {
    x.name = "users";
    x.path = "User";
  })
  .addQuery<Team, string>(x => {
    x.name = "teams";
    x.path = "Team";
  })
  .addCommand<Order, string>(x => {
    x.name = "orders";
    x.path = "Order";
  });
```

## Add headers and error handlers

```typescript
RepositoryServices
  .Create("https://localhost:7058/api/")
  .addRepository<User, string>(x => {
    x.name = "users";
    x.path = "User";

    x.addHeadersEnricher(async () => ({
      Authorization: "Bearer <token>"
    }));

    x.addErrorHandler(async (_endpoint, _uri, _method, _headers, _body, err) => {
      return err?.status === 401;
    });
  });
```

## Usage

```typescript
const repository = RepositoryServices.Repository<User, string>("users");
const command = RepositoryServices.Command<Order, string>("orders");
const query = RepositoryServices.Query<Team, string>("teams");
```

## Main operations

```typescript
const insertState = await repository.insert("u-1", { id: "u-1", name: "Alice" });
const updateState = await repository.update("u-1", { id: "u-1", name: "Alice 2" });
const existState = await repository.exist("u-1");
const deleteState = await repository.delete("u-1");

const entities = await repository.query().execute();
const count = await repository.query().where().select(x => x.id).equal("u-1").count();
```

## Batch

```typescript
import type { BatchResult } from "rystem.repository.client";

const batchResults: BatchResult<User, string>[] = await repository
  .batch()
  .addInsert("u-2", { id: "u-2", name: "Bob" })
  .addUpdate("u-1", { id: "u-1", name: "Alice 3" })
  .addDelete("u-3")
  .execute();
```
