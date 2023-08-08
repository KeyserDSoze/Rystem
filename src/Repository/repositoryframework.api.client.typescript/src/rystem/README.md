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

## Usage
Always with RepositoryServices class you can retrieve with
the correct name the integration you setup during your startup.

```
const repository = RepositoryServices
    .Repository<IperUser, string>("test");
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
    .build()
    .count();
```
- build a filter and sum a column of all elements
```
const sum = await repository
    .query()
    .where()
    .select(x => x.id)
    .equal(id)
    .build()
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
    .build()
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