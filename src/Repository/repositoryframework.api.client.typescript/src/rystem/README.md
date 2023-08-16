### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Client for Repository Framework

## Services setup
You need to install through the RepositoryServices class
all the repository clients, with settings.

.Create() => set a default uri for all clients.
In settings you have:
- name: a value to retrieve the installed client.
- path: a path which concatened with default uri create the final url for your client.
- uri: override the default uri and the path

```
import { RepositoryServices } from "rystem.repository.client";

RepositoryServices
    .Create("https://localhost:7058/api/")
    .addRepository<IperUser, string>(x => {
        x.name = "test";
        x.path = "SuperUser";
    })
    .addRepository<SuperUser, string>(x => {
        x.name = "test2"
        x.uri = "https://localhost:9090/api/SuperUser/inmemory/"
    });
```

You can also add Command or Query for your CQRS pattern with

```
RepositoryServices
    .Create("https://localhost:7058/api/")
    .addCommand<IperUser, string>(x => {
        x.name = "test";
        x.path = "SuperUser";
    })
    .addQuery<SuperUser, string>(x => {
        x.name = "test2"
        x.uri = "https://localhost:9090/api/SuperUser/inmemory/"
    });
```

### Add Custom Headers to each request
For instance when you need to add a token from your authentication flow you can use this implementation.

```
RepositoryServices
        .Create("http://localhost:5000/api/")
        .addRepository<IperUser, string>(x => {
            x.name = "test";
            x.path = "SuperUser";
            x.addHeadersEnricher((...args) => {
                return {
                    "Authorization-UI": "Bearer dsjadjalsdjalsdjalsda"
                }
            });
            x.addHeadersEnricher((endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => {
                return {
                    "Authorization-UI2": "Bearer dsjadjalsdjalsdjalsda"
                }
            })
        })
```

### Add Custom Error Handlers
For instance when you need to capture a not authorized error to request a new authentication flow before a new request.
Returning true you can retry automatically the request, with false the request chain will be stopped.

```
RepositoryServices
        .Create("http://localhost:5000/api/")
        .addRepository<IperUser, string>(x => {
            x.name = "test";
            x.path = "SuperUser";
            x.addErrorHandler((endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => {
                return (err as string).startsWith("big error");
            });
        })
```

## Usage
Always with RepositoryServices class you can retrieve with
the correct name the integration you setup during your startup.

```
const repository = RepositoryServices
    .Repository<IperUser, string>("test");
```

You can also retrieve CQRS interfaces.

```
const command = RepositoryServices
    .Command<IperUser, string>("test");
const query = RepositoryServices
    .Query<IperUser, string>("test2");
```

## Methods

### Insert

```
let response: State<IperUser, string> =
    await repository.insert(id, iperUser);
```

### Update

```
let response: State<IperUser, string> =
    await repository.update(id, iperUser);
```

### Exist

```
let response: State<IperUser, string> =
    await repository.exist(id);
```

### Delete

```
let response: State<IperUser, string> =
    await repository.delete(id);
```

### Batch operations

```
const batcher = repository.batch();
batcher
    .addInsert(id1, iperUser1)
    .addUpdate(id2, iperUser2)
    .addDelete(id3);
    const batchResults: BatchResults<IperUser, string> =
        await batcher.execute();
```

### Query operations
- get all elements
```
let queryResults = await repository.query().execute();
```
- filter as string
```
let id = "someId";
let queryResults = await repository
    .query()
    .filter(`x => x.id == "${id}"`)
    .execute();
```
- build a filter (similar to the previous example with addiction of ordering by ascending all retrieved elements)
```
let queryResults = await repository
    .query()
    .where()
    .select(x => x.id)
    .equal(id)
    .build()
    .orderBy(x => x.name)
    .execute();
```
- build a filter and count all elements
```
const count = await repository
    .query()
    .where()
    .select(x => x.id)
    .equal(id)
    .count();
```
- build a filter and sum a column of all elements
```
const sum = await repository
    .query()
    .where()
    .select(x => x.id)
    .equal(id)
    .sum(x => x.port);
```
- build a filter with greaterThanOrEqual for instance
```
const portGreaterThanZero = await repository
    .query()
    .where()
    .openRoundBracket()
    .select(x => x.port)
    .greaterThanOrEqual(0)
    .count();
```
- same of previous but ordered by a column
```
const portGreaterThanZeroOrderedByName = await repository
    .query()
    .where()
    .select(x => x.port)
    .greaterThanOrEqual(0)
    .build()
    .orderBy(x => x.name)
    .execute();
```