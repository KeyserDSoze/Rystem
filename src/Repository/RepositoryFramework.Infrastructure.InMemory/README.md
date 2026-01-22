### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## In-Memory Repository Integration

This package provides a complete in-memory storage implementation for the Repository Framework, perfect for **testing, development, and prototyping** without requiring external databases.

### 🎯 When to Use In-Memory Storage

✅ **Unit Testing** - Fast, isolated tests without database setup  
✅ **Integration Testing** - Mock data for testing business logic  
✅ **Development & Prototyping** - Rapid development without infrastructure  
✅ **Load Testing** - Simulate delays and failures  
✅ **Reliability Testing** - Verify error handling with custom exceptions  

### ⚠️ NOT for Production
In-memory storage is volatile - all data is lost when the application restarts!

---

## Basic Configuration

### Same Model (Simplest Setup)

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
});

var app = builder.Build();
await app.Services.WarmUpAsync();  // Initialize repositories
```

### With Domain Model Translation

```csharp
services.AddRepository<DomainUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
    
    // Optional: Map domain model properties if needed
    repositoryBuilder.Translate<DomainUser>();
});
```

### With Business Logic Interceptors

```csharp
services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithInMemory();
    
    builder.AddBusiness()
        .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
        .AddBusinessAfterInsert<UserAfterInsertBusiness>();
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

See [IRepositoryBusiness](https://rystem.net/mcp/tools/rystem-reflection.md) in `RepositoryFramework.Abstractions` for detailed information.

---

## 🎲 Populating with Test Data

One of the most powerful features of the In-Memory repository is **automatic random data generation**.

### Simple Random Generation

Generate 100 random users without any configuration:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.PopulateWithRandomData(100);
    });
});

var app = builder.Build();
await app.Services.WarmUpAsync();
```

### Random Data with Regex Pattern

Control how properties are generated using regular expressions:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(100)
            .WithPattern(x => x.Email, @"[a-z]{4,10}@gmail\.com")
            .WithPattern(x => x.Username, @"[a-zA-Z]{5,15}");
    });
});
```

**Supported Types for Regex Patterns:**
```
Primitives: int, uint, byte, sbyte, short, ushort, long, ulong, nint, nuint
Floats: float, double, decimal
Others: bool, char, Guid, DateTime, TimeSpan, Range, string
Nullable versions: int?, string?, Guid?, DateTime?, etc.
Collections: IEnumerable, Array, IDictionary, etc.
```

### Collections with Related Data

Generate related entities (IEnumerable, Array):

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        // Generate 100 users, each with 5 groups
        inMemoryBuilder
            .PopulateWithRandomData(100, 5)
            .WithPattern(x => x.Groups!.First().Name, @"[A-Z][a-z]{3,8}");
    });
});
```

### Dictionaries with Random Values

Populate dictionary properties with patterned data:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        // Generate 100 users, each with 6 claims
        inMemoryBuilder
            .PopulateWithRandomData(100, 6)
            .WithPattern(x => x.Claims!.First().Value, @"[a-z]{4,5}");
    });
});
```

### Delegated Population

Use custom logic to populate properties:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(100, 6)
            .WithPattern(x => x.Claims!.First().Value, () => "FixedValue");
    });
});
```

### Interface Implementation Population

Specify concrete types for interface properties:

```csharp
services.AddRepository<Entity, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder
            .PopulateWithRandomData(100)
            .WithImplementation<IMyInterface, ConcreteImplementation>(x => x.MyInterface!);
    });
});
```

### Custom Regex Service

Override the default random generator:

```csharp
public class CustomRegexService : IRegexService
{
    public T? GenerateValue<T>(string pattern) 
    {
        // Your custom generation logic
    }
}

services.AddRegexService<CustomRegexService>();
```

---

## 🧪 Simulating Real-World Behavior

### Simulate Random Exceptions

Test your error handling with controlled exception injection:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        var exceptions = new List<ExceptionOdds>
        {
            new ExceptionOdds
            {
                Exception = new InvalidOperationException("Simulated error"),
                Percentage = 10.5m  // 10.5% chance
            },
            new ExceptionOdds
            {
                Exception = new TimeoutException("Simulated timeout"),
                Percentage = 5.0m   // 5% chance
            }
        };
        
        inMemoryBuilder.Settings.AddForRepositoryPattern(
            new MethodBehaviorSetting
            {
                ExceptionOdds = exceptions
            });
    });
});
```

**Affects Operations**: Delete, Get, Insert, Update, Query

### Simulate Network Delays

Test performance with artificial latency:

```csharp
services.AddRepository<User, string>(builder =>
{
    builder.WithInMemory(inMemoryBuilder =>
    {
        inMemoryBuilder.Settings.AddForRepositoryPattern(
            new MethodBehaviorSetting
            {
                MillisecondsOfWait = new Range(100, 500)  // Random 100-500ms delay
            });
    });
});
```

This is useful for:
- Load testing
- UI responsiveness testing
- Timeout handling verification

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<User, string>(repositoryBuilder =>
{
    // Step 1: Configure In-Memory storage
    repositoryBuilder.WithInMemory(inMemoryBuilder =>
    {
        // Populate with 50 random users
        inMemoryBuilder
            .PopulateWithRandomData(50)
            .WithPattern(x => x.Email, @"[a-z]{4,8}@gmail\.com")
            .WithPattern(x => x.Username, @"[A-Z][a-z]{5,10}");
        
        // Simulate occasional failures
        inMemoryBuilder.Settings.AddForRepositoryPattern(
            new MethodBehaviorSetting
            {
                ExceptionOdds = new List<ExceptionOdds>
                {
                    new() 
                    { 
                        Exception = new TimeoutException(), 
                        Percentage = 2.5m 
                    }
                },
                MillisecondsOfWait = new Range(50, 200)
            });
    });
    
    // Step 2: Add business logic
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<UserBeforeInsertBusiness>();
});

var app = builder.Build();

// Step 3: Warm up - initializes all repositories
await app.Services.WarmUpAsync();

app.Run();
```

Now available in Dependency Injection:
```csharp
public class UserService(IRepository<User, string> repository)
{
    public async Task GetAllUsersAsync()
    {
        var users = await repository.QueryAsync();
        return users;
    }
}
```

---

## Automated REST API

Expose your in-memory repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("In-Memory API")
    .WithPath("/api")
    .WithSwagger()
    .WithVersion("v1")
    .WithDocumentation()
    .WithDefaultCors("http://localhost:3000");

var app = builder.Build();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

Automatically generates endpoints:
- `GET /api/user` - List all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## 📚 Key Concepts

**WarmUpAsync()**: Must be called after `Build()` to initialize all repositories with random data and prepare them for use.

**Percentages**: When specifying exception odds, use decimal values (10.5 = 10.5%). Total doesn't need to sum to 100%.

**Thread-Safe**: The in-memory implementation is thread-safe by default.

**Business Interceptors**: Run at the same lifecycle points as any other repository implementation.

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [Business Logic Interceptors](https://rystem.net/mcp/resources/background-jobs.md)
- [Dependency Injection Guide](https://rystem.net/mcp/tools/rystem-dependencyinjection-factory.md)