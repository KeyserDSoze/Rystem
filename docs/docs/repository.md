# Repository Framework

### Help the project

Reach out us on [Discord](https://discord.gg/tkWvy4WPjt)

### Contribute: https://www.buymeacoffee.com/keyserdsoze

## [Showcase (youtube)](https://www.youtube.com/watch?v=quqHoSXNFek&ab_channel=alessandrorapiti)

## [Showcase (code)](https://github.com/KeyserDSoze/RepositoryFramework.Showcase)

**Rystem.RepositoryFramework allows you to use correctly concepts like repository pattern, CQRS, DDD and automated REPR (Request-Endpoint-Response) Pattern. You have interfaces for your domains, auto-generated api, auto-generated HttpClient to simplify connection "api to front-end", a functionality for auto-population in memory of your models, caching, a functionality to simulate exceptions and waiting time from external sources to improve your implementation/business test and load test.**

**Document to read before using this library:**
- Repository pattern, useful links: 
  - [Microsoft docs](https://docs.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application)
  - [Repository pattern explained](https://codewithshadman.com/repository-pattern-csharp/)
- CQRS, useful links:
  - [Microsoft docs](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
  - [Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- DDD, useful links:
  - [Wikipedia](https://en.wikipedia.org/wiki/Domain-driven_design)
  - [Microsoft docs](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
- REPR (Request-Endpoint-Response) Pattern
  - [MVC as Dinasours](https://ardalis.com/mvc-controllers-are-dinosaurs-embrace-api-endpoints/)

## Basic knowledge

### CQRS and Repository are two sides of the same coin.

![Framework abstractions](https://raw.githubusercontent.com/KeyserDSoze/Rystem/master/src/Repository/RepositoryFramework.Abstractions.png)

### Design and nuget map

![Framework design](https://raw.githubusercontent.com/KeyserDSoze/Rystem/master/src/Repository/RepositoryFramework.png)

### Logic design and flow
The same flow is valid for ICommand/ICommandPattern and IQuery/IQueryPattern

![Framework logic](https://raw.githubusercontent.com/KeyserDSoze/Rystem/master/src/Repository/RepositoryFramework.CacheFlow.png)

## Important!!!
Extends and use ``IRepository<T, TKey>`` and not ``IRepositoryPattern<T>``

Extends and use ``IQuery<T, TKey>`` and not ``IQueryPattern<T>``

Extends and use ``ICommand<T, TKey>`` and not ``ICommandPattern<T>``

### Abstractions (Domain)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Abstractions)

### In memory integration (Infrastructure for test purpose, load tests or functionality tests)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructure.InMemory)

### Migration tools (Tool to help during a data migration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.MigrationTools)

### Api.Server (Application for automatic integration of api endpoint for your repository or CQRS)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Api.Server)

### Api.Client (Application for http client integration of api endpoint for your repository or CQRS)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Api.Client)

### Azure TableStorage integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Table)

### Azure CosmosDB SQL integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Cosmos.Sql)

### Azure BlobStorage integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Azure.Storage.Blob)

### Dynamics Dataverse integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.Dynamics.Dataverse)

### Entity Framework integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.EntityFramework)

### MsSql integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Infrastructures/RepositoryFramework.Infrastructure.MsSql)

### Cache integration (with in memory default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Cache/RepositoryFramework.Cache)

### Cache with Azure BlobStorage integration (default integration)
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Cache/RepositoryFramework.Cache.Azure.Storage.Blob)

### Web UI for dashboard
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/RepositoryFramework.Web/RepositoryFramework.Web.Components)

### Client Api for Typescript/Javascript solutions
You may find the documentation at [this link](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/repositoryframework.api.client.typescript/src/rystem)