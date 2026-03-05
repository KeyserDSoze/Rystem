### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Integration with Entity Framework and Repository Framework

This package enables seamless integration between Entity Framework Core and the Repository Framework, allowing you to leverage the Repository pattern with automatic mapping between your **Domain Model** and **Database Model**.

### Core Concepts

#### 1️⃣ **Domain Model vs Database Model**

In Domain-Driven Design (DDD), you often have:
- **Domain Model** (`MappingUser`): Your business model with domain logic and clean interfaces
- **Database Model** (`User`): The actual Entity Framework entity that maps to your database schema

The Entity Framework integration allows you to work with your domain model while the framework automatically handles mapping to/from the database model.

#### 2️⃣ **Configuration Components**

##### **DbSet**: Database Table Reference
```csharp
t.DbSet = x => x.Users;
```
This specifies **which DbSet from your Entity Framework context** holds the data for this repository. It tells the framework: "Find the data in the `Users` table through this DbSet property."

##### **References**: Related Data Loading
```csharp
t.References = x => x.Include(x => x.IdGruppos);
```
These are **Entity Framework Include() statements** that specify which related entities should be eagerly loaded with your main entity. In this example, whenever you fetch a `User`, its related `IdGruppos` collection will automatically be loaded. You can chain multiple includes here.

**Why this matters**: Without proper references, your related data won't be loaded, causing N+1 query problems.

##### **Translate**: Domain-to-Database Model Mapping
```csharp
builder.Translate<User>()
    .With(x => x.Username, x => x.Nome)
    .With(x => x.Username, x => x.Cognome)
    .With(x => x.Email, x => x.IndirizzoElettronico)
    .With(x => x.Groups, x => x.IdGruppos)
    .With(x => x.Id, x => x.Identificativo)
    .WithKey(x => x, x => x.Identificativo);
```
This configures **how properties are mapped** between your domain model and database model:
- `.With(domainProperty, databaseProperty)`: Maps individual properties
- `.WithKey(domainKey, databaseKey)`: Specifies the primary key mapping
- Each domain property is mapped to its corresponding database column/property

**When to use Translation**:
- ✅ Different property names (e.g., `Username` → `Nome`)
- ✅ Different data structures (e.g., entity relationships)
- ✅ Property name conventions differ between domain and database

### Two Approaches: Choose Your Path

#### **Approach A: Separate Models (with Translation)** - Recommended for DDD
Use **different classes** for domain and database, with translation configured:

```csharp
// Domain Model - Clean, business-focused
public class MappingUser
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<Group> Groups { get; set; }
}

// Database Model - Maps to actual DB schema
public class User
{
    public int Identificativo { get; set; }      // Different naming
    public string Nome { get; set; }
    public string Cognome { get; set; }
    public string IndirizzoElettronico { get; set; }
    public List<Gruppo> IdGruppos { get; set; }  // Different structure
}

// Configuration
services.AddRepository<MappingUser, int>(builder =>
{
    builder.WithEntityFramework<MappingUser, int, User, SampleContext>(
        t =>
        {
            t.DbSet = x => x.Users;
            t.References = x => x.Include(x => x.IdGruppos);
        });
    
    // Map domain model properties to database model properties
    builder.Translate<User>()
        .With(x => x.Username, x => x.Nome)
        .With(x => x.Email, x => x.IndirizzoElettronico)
        .With(x => x.Groups, x => x.IdGruppos)
        .With(x => x.Id, x => x.Identificativo)
        .WithKey(x => x, x => x.Identificativo);
});
```

**Benefits**:
- Domain model stays clean and focused on business logic
- Database schema can evolve independently
- Better encapsulation of domain concepts

#### **Approach B: Same Model (No Translation)** - Simpler Setup
Use the **same class** for both domain and database:

```csharp
// Single Model - Used for both domain and database
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<Group> Groups { get; set; }
}

// Configuration - No translation needed!
services.AddRepository<User, int>(builder =>
{
    builder.WithEntityFramework<User, int, User, SampleContext>(
        t =>
        {
            t.DbSet = x => x.Users;
            t.References = x => x.Include(x => x.Groups);
        });
    // No Translate() call needed when domain model = database model
});
```

**Benefits**:
- Simpler configuration (no mapping overhead)
- Faster setup for small projects
- Less boilerplate code

**Trade-offs**:
- Domain model is tightly coupled to database schema
- Harder to refactor database without affecting domain logic

### Complete Example with Business Logic

```csharp
services.AddDbContext<SampleContext>(options =>
{
    options.UseSqlServer(configuration["ConnectionString:Database"]);
}, ServiceLifetime.Scoped);

services.AddRepository<MappingUser, int>(builder =>
{
    // Step 1: Configure Entity Framework
    builder.WithEntityFramework<MappingUser, int, User, SampleContext>(
        t =>
        {
            t.DbSet = x => x.Users;
            t.References = x => x.Include(x => x.IdGruppos);
        });
    
    // Step 2: Configure Translation (if using separate models)
    builder.Translate<User>()
        .With(x => x.Username, x => x.Nome)
        .With(x => x.Username, x => x.Cognome)
        .With(x => x.Email, x => x.IndirizzoElettronico)
        .With(x => x.Groups, x => x.IdGruppos)
        .With(x => x.Id, x => x.Identificativo)
        .WithKey(x => x, x => x.Identificativo);
    
    // Step 3: Add Business Logic Interceptors
    // See: RepositoryFramework.Abstractions > Business > IRepositoryBusiness
    builder.AddBusiness()
        .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness>()
        .AddBusinessBeforeInsert<MappingUserBeforeInsertBusiness2>();
});
```

**Now available in Dependency Injection**:
```csharp
public class UserService(IRepository<MappingUser, int> repository)
{
    public async Task CreateUserAsync(MappingUser user)
    {
        // Business interceptors run here automatically
        await repository.InsertAsync(user);
    }
}
```

### Business Logic Interceptors

Business interceptors run at specific points in the repository lifecycle:

- **BeforeInsert**: Runs before inserting a new entity
- **AfterInsert**: Runs after inserting a new entity
- **BeforeUpdate**: Runs before updating an entity
- **AfterUpdate**: Runs after updating an entity
- **BeforeDelete**: Runs before deleting an entity
- **AfterDelete**: Runs after deleting an entity
- **BeforeQuery**: Runs before querying entities

See the `IRepositoryBusiness` interface in `RepositoryFramework.Abstractions` for detailed documentation on implementing custom business logic.

---

**Key Takeaway**: 
- Choose **Approach A (Separate Models)** for enterprise applications following DDD principles
- Choose **Approach B (Same Model)** for smaller projects prioritizing simplicity

## Automated REST API with Rystem.RepositoryFramework.Api.Server

Once your repository is configured, you can automatically expose it as a fully-featured REST API without writing endpoint code:

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// ... your repository configuration ...

builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Repository API")
    .WithPath("/api")
    .WithSwagger()
    .WithVersion("v1")
    .WithDocumentation()
    .WithDefaultCors("http://example.com");

var app = builder.Build();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

This automatically generates REST endpoints for all your repositories:
- `GET /api/mappinguser` - List all users
- `GET /api/mappinguser/{id}` - Get user by ID
- `POST /api/mappinguser` - Create user
- `PUT /api/mappinguser/{id}` - Update user
- `DELETE /api/mappinguser/{id}` - Delete user

Your business interceptors run automatically for each operation!

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.
