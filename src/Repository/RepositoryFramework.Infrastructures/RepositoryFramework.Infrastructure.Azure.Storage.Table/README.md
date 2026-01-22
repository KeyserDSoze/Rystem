### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Azure Table Storage Integration

This package provides Azure Table Storage integration for the Repository Framework, perfect for **structured NoSQL data at massive scale with minimal cost**.

### 🎯 When to Use Azure Table Storage

✅ **High Volume Data** - Store billions of entities  
✅ **Cheap Storage** - Lowest cost Azure storage option  
✅ **Structured Data** - Key-value pairs with properties  
✅ **Partition/Row Key Model** - Optimize access patterns  
✅ **Semi-Structured Data** - Flexible schema with optional properties  
✅ **Legacy System Migration** - Often replacing old NoSQL solutions  

### ⚠️ Limitations
Table Storage is not suitable for complex relationships or full-text search. For complex queries, use **Cosmos DB** or **Entity Framework**.

---

## Basic Configuration

### Understanding Table Storage Keys

Table Storage uses two keys:
- **Partition Key**: Determines which partition stores the entity (groups related data)
- **Row Key**: Unique identifier within the partition

```csharp
var builder = WebApplication.CreateBuilder(args);

await services.AddRepositoryAsync<User, Guid>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
        
        // Define partition and row keys
        tableStorageBuilder
            .WithTableStorageKeyReader<DefaultTableStorageKeyReader>()
            .WithPartitionKey(x => x.TenantId)      // Partition by tenant
            .WithRowKey(x => x.Id);                  // Row key is user ID
            
    }).ToResult();
}).ToResult();

var app = builder.Build();
```

### Configuration Breakdown

**ConnectionString**: Azure Storage connection string

**WithPartitionKey**: Property used to partition data (critical for performance)

**WithRowKey**: Unique identifier within partition

**WithTimestamp**: Optional - tracks last modified time

---

## Domain Model Setup

```csharp
public class User
{
    public Guid Id { get; set; }
    public string TenantId { get; set; }  // Partition key
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }  // Optional timestamp
}
```

---

## Complete Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Async setup with table storage configuration
await builder.Services.AddRepositoryAsync<User, Guid>(async repositoryBuilder =>
{
    // Step 1: Configure Table Storage
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
        
        tableStorageBuilder
            .WithTableStorageKeyReader<DefaultTableStorageKeyReader>()
            .WithPartitionKey(x => x.TenantId)           // Partition by tenant
            .WithRowKey(x => x.Id)                       // Row key is user ID
            .WithTimestamp(x => x.ModifiedAt);           // Track modifications
            
    }).ToResult();
    
    // Step 2: Add business logic interceptors
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
        .AddBusinessBeforeUpdate<UserBeforeUpdateBusiness>();
        
}).ToResult();

var app = builder.Build();
```

### Business Logic Interceptors

Example: Auto-update modification timestamp

```csharp
public class UserBeforeUpdateBusiness : IRepositoryBusiness<User, Guid>
{
    public async ValueTask<User?> BeforeUpdateAsync(User entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        return entity;
    }
}
```

See [IRepositoryBusiness](https://rystem.net/mcp/resources/background-jobs.md) documentation for all lifecycle hooks.

---

## Using the Repository

### Inject and Use

```csharp
public class UserService(IRepository<User, Guid> repository)
{
    public async Task CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        await repository.InsertAsync(user);
    }
    
    public async Task<User?> GetUserAsync(Guid userId)
    {
        return await repository.GetByKeyAsync(userId);
    }
    
    public async Task<IEnumerable<User>> GetTenantUsersAsync(string tenantId)
    {
        // Query by partition key for fast retrieval
        return await repository.QueryAsync(x => x.TenantId == tenantId);
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await repository.UpdateAsync(user);
    }
}
```

---

## Partition Key Strategy

The partition key is critical for Table Storage performance and cost:

### By Tenant (Multi-Tenant Apps)

```csharp
.WithPartitionKey(x => x.TenantId)
```

**Benefit**: All tenant data together, great for tenant queries  
**Example**: SaaS application with multiple customers

### By Date (Time-Series Data)

```csharp
.WithPartitionKey(x => x.CreatedAt.ToString("yyyy-MM-dd"))
```

**Benefit**: Organize logs/events by date, easy to archive  
**Example**: Application logs, telemetry, events

### By Region (Geo-Distributed)

```csharp
.WithPartitionKey(x => x.Region)
```

**Benefit**: Keep regional data together  
**Example**: Global app with regional databases

### Composite Partition Key

```csharp
.WithPartitionKey(x => $"{x.Region}#{x.Department}")
```

**Benefit**: Multiple levels of organization  
**Example**: Large enterprise with regional + department structure

---

## Advanced Patterns

### Custom Key Reader

```csharp
public class CustomTableStorageKeyReader : ITableStorageKeyReader<User>
{
    public string GetPartitionKey(User entity) => entity.TenantId;
    public string GetRowKey(User entity) => entity.Id.ToString();
}

// Use it
tableStorageBuilder.WithTableStorageKeyReader<CustomTableStorageKeyReader>();
```

### Query by Partition Key

```csharp
// Fast query - only one partition scanned
var tenantUsers = await repository.QueryAsync(
    x => x.TenantId == "tenant-123"
);

// Slow query - all partitions scanned (table scan)
var users = await repository.QueryAsync(
    x => x.Username == "john"
);
```

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Table Storage
await builder.Services.AddRepositoryAsync<Event, Guid>(async repositoryBuilder =>
{
    await repositoryBuilder.WithTableStorageAsync(tableStorageBuilder =>
    {
        tableStorageBuilder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
        
        tableStorageBuilder
            .WithTableStorageKeyReader<DefaultTableStorageKeyReader>()
            .WithPartitionKey(x => x.CreatedAt.ToString("yyyy-MM-dd"))  // Partition by date
            .WithRowKey(x => x.Id)
            .WithTimestamp(x => x.ReceivedAt);
            
    }).ToResult();
    
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<EventValidationBusiness>();
        
}).ToResult();

var app = builder.Build();

// Map endpoint
app.MapPost("/events", async (IRepository<Event, Guid> repo, Event evt) =>
{
    evt.ReceivedAt = DateTime.UtcNow;
    await repo.InsertAsync(evt);
    return Results.Accepted();
});

// Query events by date
app.MapGet("/events/{date}", async (IRepository<Event, Guid> repo, string date) =>
{
    var events = await repo.QueryAsync(x => x.CreatedAt.ToString("yyyy-MM-dd") == date);
    return Results.Ok(events);
});

app.Run();
```

---

## Automated REST API

Expose your Table Storage repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Table Storage API")
    .WithPath("/api")
    .WithSwagger()
    .WithVersion("v1")
    .WithDocumentation()
    .WithDefaultCors("*");

var app = builder.Build();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

Automatically generates endpoints:
- `GET /api/user` - Query users (table scan)
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## 💡 Performance Tips

✅ **Choose partition key carefully** - Affects access patterns and costs  
✅ **Keep partition sizes balanced** - Avoid "hot" partitions  
✅ **Query by partition key** - Dramatically faster than table scans  
✅ **Use row key efficiently** - Often used in range queries  
✅ **Monitor throughput** - Table Storage has limits per partition  
✅ **Archive old partitions** - Delete by date-based partition key  

---

## Toml() Extension

Note the `.ToResult()` extension - this is specific to Table Storage async setup:

```csharp
// Table Storage requires async configuration
await repositoryBuilder.WithTableStorageAsync(...).ToResult();
```

This converts async operations to synchronous for DI registration.

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [Azure Table Storage Documentation](https://learn.microsoft.com/en-us/azure/storage/tables/)
- [Partition Key Design](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-design)
- [Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)
