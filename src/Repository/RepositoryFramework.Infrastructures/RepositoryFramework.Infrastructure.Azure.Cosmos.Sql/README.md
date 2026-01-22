### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Azure Cosmos DB (SQL API) Integration

This package provides Azure Cosmos DB SQL API integration for the Repository Framework, perfect for **globally distributed databases, high-scale applications, and real-time analytics**.

### 🎯 When to Use Azure Cosmos DB

✅ **Global Distribution** - Replicate data across regions worldwide  
✅ **High Availability** - Built-in failover and redundancy  
✅ **Massive Scale** - Handle millions of requests per second  
✅ **Low Latency** - Guaranteed single-digit millisecond response times  
✅ **Multi-Region Apps** - Serve users from nearest data center  
✅ **Real-Time Applications** - Live data with minimal lag  

### ⚠️ Cost Considerations
Cosmos DB is more expensive than SQL Server or Entity Framework. Use for applications requiring global scale or extreme high availability.

---

## Basic Configuration

### Simple Setup with Async Builder

```csharp
var builder = WebApplication.CreateBuilder(args);

await services.AddRepositoryAsync<User, string>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "myapp-db";
        
        // Key configuration
        cosmosBuilder.WithId(x => x.Id);
    }).NoContext();
}).NoContext();

var app = builder.Build();
```

### Understanding Configuration

**ConnectionString**: Cosmos DB connection string (Account endpoint + Primary key)

**DatabaseName**: The database within your Cosmos Account to use

**WithId**: Maps which property is the partition key (critical for performance!)

---

## Domain Model Setup

```csharp
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Role> Roles { get; set; } = new();
}

public class Role
{
    public string Name { get; set; }
    public List<Permission> Permissions { get; set; } = new();
}
```

---

## Complete Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Async setup pattern (required for Cosmos)
await builder.Services.AddRepositoryAsync<User, string>(async repositoryBuilder =>
{
    // Step 1: Configure Cosmos SQL
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "production-db";
        
        // Specify partition key - critical for performance!
        cosmosBuilder.WithId(x => x.Id);
        
    }).NoContext();
    
    // Step 2: Add business logic interceptors
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
        .AddBusinessAfterInsert<UserAfterInsertBusiness>();
        
}).NoContext();

var app = builder.Build();
```

### Business Logic Interceptors

Example: Auto-set creation timestamp

```csharp
public class UserBeforeInsertBusiness : IRepositoryBusiness<User, string>
{
    public async ValueTask<User?> BeforeInsertAsync(User entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        return entity;
    }
}
```

See [IRepositoryBusiness](https://rystem.net/mcp/resources/background-jobs.md) documentation for all lifecycle hooks.

---

## Using the Repository

### Inject and Use

```csharp
public class UserService(IRepository<User, string> repository)
{
    public async Task CreateUserAsync(User user)
    {
        // Insert - triggers business logic
        await repository.InsertAsync(user);
    }
    
    public async Task<User?> GetUserAsync(string userId)
    {
        return await repository.GetByKeyAsync(userId);
    }
    
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await repository.QueryAsync();
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await repository.UpdateAsync(user);
    }
    
    public async Task DeleteUserAsync(string userId)
    {
        await repository.DeleteAsync(userId);
    }
}
```

---

## Partition Key Strategy

The partition key (specified in `WithId()`) is **critical** for Cosmos performance:

### Single Partition Key

```csharp
// All users by tenant
cosmosBuilder.WithId(x => x.TenantId);
```

**Best for**: Multi-tenant apps where you query by tenant

### Composite Key

```csharp
// Region + UserId for geographic distribution
cosmosBuilder.WithId(x => $"{x.Region}#{x.UserId}");
```

**Best for**: Geo-distributed systems

---

## Advanced Patterns

### Multi-Region Setup

```csharp
await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
{
    cosmosBuilder.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
    cosmosBuilder.Settings.DatabaseName = "global-db";
    cosmosBuilder.WithId(x => x.Id);
    
    // Cosmos handles multi-region replication automatically
    // Configure via Azure Portal for read regions
    
}).NoContext();
```

### Partition Key Selection Examples

```csharp
// Single tenant app - partition by user
cosmosBuilder.WithId(x => x.UserId);

// Multi-tenant app - partition by tenant
cosmosBuilder.WithId(x => x.TenantId);

// E-commerce - partition by store
cosmosBuilder.WithId(x => x.StoreId);

// Analytics - partition by date
cosmosBuilder.WithId(x => x.CreatedAt.Date.ToString("yyyy-MM-dd"));
```

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Setup Cosmos DB repository
await services.AddRepositoryAsync<Product, string>(async repositoryBuilder =>
{
    await repositoryBuilder.WithCosmosSqlAsync(cosmosBuilder =>
    {
        cosmosBuilder.Settings.ConnectionString = configuration["ConnectionString:CosmosSql"];
        cosmosBuilder.Settings.DatabaseName = "ecommerce-db";
        
        // Partition by store for better scaling
        cosmosBuilder.WithId(x => x.StoreId);
        
    }).NoContext();
    
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<ProductValidationBusiness>();
        
}).NoContext();

var app = builder.Build();

// Map service
app.MapPost("/products", async (IRepository<Product, string> repo, Product product) =>
{
    await repo.InsertAsync(product);
    return Results.Created($"/products/{product.Id}", product);
});

app.Run();
```

---

## Automated REST API

Expose your Cosmos repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Cosmos DB API")
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
- `GET /api/user` - Query all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## 💡 Performance Tips

✅ Choose partition key carefully - affects throughput and cost  
✅ Use **query projection** to fetch only needed fields  
✅ Implement **TTL (Time-To-Live)** for auto-expiring data  
✅ Monitor **RU (Request Units)** consumption  
✅ Use **bulk operations** for mass inserts  
✅ Leverage **automatic indexing** - Cosmos indexes everything by default

---

## Async/Await Pattern

Note: Cosmos SQL setup is **async** by default due to database initialization:

```csharp
// Must use async pattern
await services.AddRepositoryAsync<Entity, Key>(async builder =>
{
    await builder.WithCosmosSqlAsync(config).NoContext();
}).NoContext();
```

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [Azure Cosmos DB Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/)
- [Partition Key Best Practices](https://learn.microsoft.com/en-us/azure/cosmos-db/partitioning-overview)
- [Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)
