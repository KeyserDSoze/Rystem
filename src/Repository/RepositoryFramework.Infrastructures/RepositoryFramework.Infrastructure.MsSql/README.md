### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## MS SQL Server Integration

This package provides direct SQL Server integration for the Repository Framework (raw SQL approach without Entity Framework), perfect for **fine-grained control over SQL, legacy database migration, and custom query optimization**.

### 🎯 When to Use MS SQL (Direct)

✅ **Legacy Database Migration** - Work with existing SQL Server schemas  
✅ **Custom SQL Queries** - Direct control over SQL generation  
✅ **Performance Tuning** - Bypass ORM overhead for specific queries  
✅ **On-Premises SQL Server** - Direct connection to corporate databases  
✅ **Minimal ORM Overhead** - Lightweight direct database access  

### ⚠️ Compare with Entity Framework

For most cases, consider **Entity Framework Integration** instead:
- Entity Framework handles schema generation
- Better with complex relationships
- More flexible with migrations
- Standard ORM patterns

Use **MS SQL Direct** when you need raw SQL control or migrating legacy systems.

---

## Prerequisites

1. **SQL Server** (2016 or later)
2. **Connection String** to your database
3. **Table schema** (either existing or auto-generated)

---

## Basic Configuration

### Simple Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<Cat, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.ConnectionString = configuration["ConnectionString:Database"];
        sqlBuilder.Schema = "dbo";  // SQL Server schema
    });
});

var app = builder.Build();

// Create tables if they don't exist
await app.Services.WarmUpAsync();
```

### Configuration Breakdown

**ConnectionString**: SQL Server connection string

**Schema**: Database schema (default: `dbo`)

---

## Domain Model Setup

```csharp
public class Cat
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string? Color { get; set; }
    public DateTime BornOn { get; set; }
}
```

---

## Column Mapping

### Basic Column Definition

```csharp
services.AddRepository<Cat, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.ConnectionString = configuration["ConnectionString:Database"];
        sqlBuilder.Schema = "repo";
        
        // Map primary key
        sqlBuilder.WithPrimaryKey(x => x.Id, options =>
        {
            options.ColumnName = "CatId";
            options.IsAutoIncrement = false;  // GUID, not auto-increment
        });
        
        // Map regular columns
        sqlBuilder.WithColumn(x => x.Name, options =>
        {
            options.ColumnName = "CatName";
            options.IsNullable = false;
            options.MaxLength = 100;
        });
        
        sqlBuilder.WithColumn(x => x.Age, options =>
        {
            options.ColumnName = "CatAge";
            options.IsNullable = false;
        });
        
        sqlBuilder.WithColumn(x => x.Color, options =>
        {
            options.ColumnName = "CatColor";
            options.IsNullable = true;
        });
    });
});
```

### Column Options

```csharp
sqlBuilder.WithColumn(x => x.Property, options =>
{
    options.ColumnName = "DatabaseColumn";  // Map to different column name
    options.IsNullable = true;              // Allow NULL
    options.MaxLength = 255;                // For VARCHAR
    options.DataType = "NVARCHAR(MAX)";     // SQL data type
    options.IsAutoIncrement = false;        // Auto-increment (for int keys)
});
```

---

## Complete Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<Cat, Guid>(repositoryBuilder =>
{
    // Step 1: Configure MS SQL
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.ConnectionString = configuration["ConnectionString:Database"];
        sqlBuilder.Schema = "app";
        
        // Primary key
        sqlBuilder.WithPrimaryKey(x => x.Id, options =>
        {
            options.ColumnName = "CatId";
        });
        
        // Columns
        sqlBuilder
            .WithColumn(x => x.Name, opt => 
            { 
                opt.ColumnName = "Name"; 
                opt.MaxLength = 100; 
            })
            .WithColumn(x => x.Age, opt => 
            { 
                opt.ColumnName = "Age"; 
            })
            .WithColumn(x => x.Color, opt => 
            { 
                opt.ColumnName = "Color"; 
                opt.IsNullable = true; 
            });
    });
    
    // Step 2: Add business logic
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<CatBeforeInsertBusiness>();
});

var app = builder.Build();

// Step 3: Create tables
await app.Services.WarmUpAsync();

app.Run();
```

### Business Logic Interceptors

Example: Validate cat age

```csharp
public class CatBeforeInsertBusiness : IRepositoryBusiness<Cat, Guid>
{
    public async ValueTask<Cat?> BeforeInsertAsync(Cat entity)
    {
        if (entity.Age < 0 || entity.Age > 50)
            throw new InvalidOperationException("Invalid cat age");
        
        return entity;
    }
}
```

See [IRepositoryBusiness](https://rystem.net/mcp/resources/background-jobs.md) documentation for all lifecycle hooks.

---

## Using the Repository

### Inject and Use

```csharp
public class CatService(IRepository<Cat, Guid> repository)
{
    public async Task AddCatAsync(Cat cat)
    {
        await repository.InsertAsync(cat);
    }
    
    public async Task<Cat?> GetCatAsync(Guid catId)
    {
        return await repository.GetByKeyAsync(catId);
    }
    
    public async Task<IEnumerable<Cat>> GetCatsByColorAsync(string color)
    {
        return await repository.QueryAsync(x => x.Color == color);
    }
    
    public async Task UpdateCatAsync(Cat cat)
    {
        await repository.UpdateAsync(cat);
    }
    
    public async Task DeleteCatAsync(Guid catId)
    {
        await repository.DeleteAsync(catId);
    }
}
```

---

## Working with Existing Tables

### Map to Existing Schema

```csharp
services.AddRepository<LegacyUser, int>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.ConnectionString = configuration["ConnectionString:LegacyDb"];
        sqlBuilder.Schema = "legacy";
        
        // Map to existing table structure
        sqlBuilder.WithPrimaryKey(x => x.Id, opt => 
        { 
            opt.ColumnName = "user_id";  // Existing column
            opt.IsAutoIncrement = true; 
        });
        
        sqlBuilder.WithColumn(x => x.Username, opt => 
        { 
            opt.ColumnName = "user_name";  // Existing column
        });
        
        sqlBuilder.WithColumn(x => x.Email, opt => 
        { 
            opt.ColumnName = "email_address";  // Existing column
        });
    });
});
```

---

## Advanced Patterns

### String Primary Key

```csharp
public class Product
{
    public string Sku { get; set; }  // Product SKU as key
    public string Name { get; set; }
    public decimal Price { get; set; }
}

services.AddRepository<Product, string>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.WithPrimaryKey(x => x.Sku, options =>
        {
            options.ColumnName = "product_sku";
            options.MaxLength = 50;
        });
    });
});
```

### Int Primary Key with Auto-Increment

```csharp
public class Order
{
    public int OrderNumber { get; set; }
    public string CustomerName { get; set; }
}

services.AddRepository<Order, int>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.WithPrimaryKey(x => x.OrderNumber, options =>
        {
            options.ColumnName = "order_id";
            options.IsAutoIncrement = true;
        });
    });
});
```

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure MS SQL repository
builder.Services.AddRepository<Animal, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithMsSql(sqlBuilder =>
    {
        sqlBuilder.ConnectionString = builder.Configuration["ConnectionString:Database"];
        sqlBuilder.Schema = "zoo";
        
        sqlBuilder
            .WithPrimaryKey(x => x.Id, opt => 
            { 
                opt.ColumnName = "AnimalId"; 
            })
            .WithColumn(x => x.Name, opt => 
            { 
                opt.ColumnName = "AnimalName"; 
                opt.MaxLength = 100; 
            })
            .WithColumn(x => x.Species, opt => 
            { 
                opt.ColumnName = "Species"; 
            })
            .WithColumn(x => x.BirthYear, opt => 
            { 
                opt.ColumnName = "YearBorn"; 
            });
    });
    
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<AnimalValidationBusiness>();
});

var app = builder.Build();

// Initialize database
await app.Services.WarmUpAsync();

// Map endpoints
app.MapPost("/animals", async (IRepository<Animal, Guid> repo, Animal animal) =>
{
    await repo.InsertAsync(animal);
    return Results.Created($"/animals/{animal.Id}", animal);
});

app.MapGet("/animals/species/{species}", async (IRepository<Animal, Guid> repo, string species) =>
{
    var animals = await repo.QueryAsync(x => x.Species == species);
    return Results.Ok(animals);
});

app.Run();
```

---

## Automated REST API

Expose your MS SQL repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("MS SQL API")
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
- `GET /api/cat` - Query all cats
- `GET /api/cat/{id}` - Get cat by ID
- `POST /api/cat` - Create cat
- `PUT /api/cat/{id}` - Update cat
- `DELETE /api/cat/{id}` - Delete cat

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## WarmUpAsync()

**Critical**: Must be called after `Build()` to create tables if they don't exist.

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();  // Creates tables in SQL Server
app.Run();
```

---

## 💡 Best Practices

✅ Use **meaningful schema and table names**  
✅ Set **appropriate column constraints** (NOT NULL, MaxLength)  
✅ Use **string keys cautiously** - consider Guid for distributed systems  
✅ Monitor **query performance** - use indexes on frequently queried columns  
✅ Keep **column names descriptive** when mapping to existing tables  
✅ Test with **existing production schema** before migrating

---

## Comparison: MS SQL Direct vs Entity Framework

| Aspect | MS SQL Direct | Entity Framework |
|--------|--------------|------------------|
| Schema Control | Full control | ORM handles it |
| SQL Queries | Can write custom | LINQ to SQL |
| Complex Relationships | Possible but manual | Built-in support |
| Migrations | Manual | Automatic |
| Learning Curve | Steep | Moderate |
| Performance | Optimizable | Good defaults |

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [SQL Server Documentation](https://learn.microsoft.com/en-us/sql/sql-server/)
- [Entity Framework Alternative](https://rystem.net/mcp/tools/repository-setup.md)
- [Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)
