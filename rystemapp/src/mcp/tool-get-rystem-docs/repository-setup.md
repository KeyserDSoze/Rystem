---
title: Repository Pattern Setup
description: Configure data access with Rystem.RepositoryFramework - CQRS, multiple storage backends, business logic injection, and Factory pattern support
---

# Repository Pattern Setup

**Purpose**: This tool explains how to implement the **Repository Pattern** using **Rystem.RepositoryFramework**. This is the **REQUIRED tool** to use whenever you need to implement data access in a Rystem application.

---



## 🎯 What is Rystem.RepositoryFramework?This tool helps you configure and setup the Rystem Repository Framework in your .NET project. It provides guidance for implementing the Repository pattern and CQRS (Command Query Responsibility Segregation).



Rystem.RepositoryFramework is a powerful abstraction layer for data access that:## Usage

- ✅ Implements **Repository Pattern** and **CQRS** out of the box

- ✅ Supports **multiple storage backends** (SQL, NoSQL, Blob, Table Storage, Cosmos DB)Use this tool when you need to:

- ✅ Provides **unified API** for all storage types- Set up a new repository for your entities

- ✅ Includes **business logic injection** (before/after operations)- Configure multiple storage backends (Entity Framework, Cosmos DB, Azure Storage, etc.)

- ✅ Supports **multiple implementations** with Factory pattern- Implement CQRS patterns in your application

- ✅ Built-in **caching**, **translation**, and **batch operations**- Add caching layers to your repositories



**Key Benefit**: Write your repository once, swap storage implementations without changing business logic!## Installation



---```bash

dotnet add package Rystem.RepositoryFramework.Abstractions

## 📦 Installationdotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework

```

### Core Package (Required)

```bash## Basic Setup

dotnet add package Rystem.RepositoryFramework.Abstractions --version 9.1.3

``````csharp

// Program.cs

### Storage Backends (Choose One or More)builder.Services.AddRepository<User, string>(repositoryBuilder =>

{

**Entity Framework (SQL)**:    repositoryBuilder

```bash        .WithEntityFramework<ApplicationDbContext>(options =>

dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework --version 9.1.3        {

dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0            options.AddForSqlServer(builder.Configuration["ConnectionStrings:Default"]);

# Or PostgreSQL, SQLite, etc.        });

```});

```

**Azure Cosmos DB (NoSQL)**:

```bash## Advanced Configuration

dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql --version 9.1.3

``````csharp

// With caching

**Azure Blob Storage (NoSQL)**:builder.Services.AddRepository<Product, int>(repositoryBuilder =>

```bash{

dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob --version 9.1.3    repositoryBuilder

```        .WithEntityFramework<CatalogDbContext>()

        .WithCache(cache =>

**Azure Table Storage (NoSQL)**:        {

```bash            cache.WithDistributedCache();

dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table --version 9.1.3            cache.WithDefaultExpiration(TimeSpan.FromMinutes(5));

```        });

});

---

// With CQRS

## 🏗️ Core Conceptsbuilder.Services.AddRepositoryFrameworkCQRS<Order, Guid>(cqrs =>

{

### 1. Repository Interfaces (CQRS)    cqrs.WithCommand(command =>

    {

Rystem.RepositoryFramework is based on **CQRS** principles:        command.WithCosmosDb(/* config */);

    });

#### ICommand (Write Operations)    cqrs.WithQuery(query =>

```csharp    {

public interface ICommandPattern<T, TKey>        query.WithBlobStorage(/* config */);

    where TKey : notnull    });

{});

    Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default);```

    Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default);

    Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default);## Patterns

    IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);

}### Repository Pattern

```Use for standard CRUD operations with a single storage backend and optional caching.



#### IQuery (Read Operations)### CQRS Pattern

```csharpUse when you need different storage or optimization strategies for reads vs writes.

public interface IQueryPattern<T, TKey>

    where TKey : notnull## See Also

{

    Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default);- [Rystem.RepositoryFramework Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository)

    Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default);- [Entity Framework Integration](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/Rystem.RepositoryFramework.Infrastructure.EntityFramework)

    IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default);- [CQRS Implementation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository/Rystem.RepositoryFramework.Abstractions)

    ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
}
```

#### IRepository (Combined)
```csharp
public interface IRepositoryPattern<T, TKey> : ICommandPattern<T, TKey>, IQueryPattern<T, TKey>
    where TKey : notnull
{
    // Includes all methods from ICommand and IQuery
}
```

**⚠️ Important**: Always inject `IRepository<T, TKey>`, NOT `IRepositoryPattern<T, TKey>`!

---

## 🚀 Quick Start Examples

### Example 1: Simple Repository with Entity Framework

#### Step 1: Define Your Entity
```csharp
namespace CargoLens.Orders.Core.Entities;

public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}
```

#### Step 2: Register Repository in DI
```csharp
// Program.cs or Startup.cs
using Rystem;

services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("OrdersDb")));

services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});
```

#### Step 3: Use in Your Service
```csharp
namespace CargoLens.Orders.Business.Services;

public class OrderService
{
    private readonly IRepository<Order, Guid> _orderRepository;
    
    public OrderService(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        return await _orderRepository.GetAsync(orderId);
    }
    
    public async Task<State<Order, Guid>> CreateOrderAsync(Order order)
    {
        return await _orderRepository.InsertAsync(order.Id, order);
    }
    
    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        var query = await _orderRepository
            .Where(x => x.Status == OrderStatus.Pending)
            .ToListAsync();
        
        return query.Select(x => x.Value!).ToList();
    }
}
```

---

### Example 2: Complex Key (Multi-Property)

Many entities have composite keys. Rystem handles this elegantly.

#### Option 1: Using Record (Recommended)
```csharp
// Define composite key as record
public record OrderItemKey(Guid OrderId, int ItemNumber);

public class OrderItem
{
    public Guid OrderId { get; set; }
    public int ItemNumber { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

// Register
services.AddRepository<OrderItem, OrderItemKey>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// Use
var item = await _repository.GetAsync(new OrderItemKey(orderId, 1));
```

#### Option 2: Using IDefaultKey (Auto-Parsing)
```csharp
public class OrderItemKey : IDefaultKey
{
    public Guid OrderId { get; set; }
    public int ItemNumber { get; set; }
}

// Set custom separator (optional, default is |||)
IDefaultKey.SetDefaultSeparator("$$$");

// Key will be serialized as: {OrderId}$$${ItemNumber}
var keyString = key.AsString();
var parsedKey = IDefaultKey.Parse(keyString);
```

#### Option 3: Using IKey (Custom Parsing)
```csharp
public class OrderItemKey : IKey
{
    public Guid OrderId { get; set; }
    public int ItemNumber { get; set; }
    
    public static IKey Parse(string keyAsString)
    {
        var parts = keyAsString.Split('$');
        return new OrderItemKey 
        { 
            OrderId = Guid.Parse(parts[0]), 
            ItemNumber = int.Parse(parts[1]) 
        };
    }
    
    public string AsString()
    {
        return $"{OrderId}${ItemNumber}";
    }
}
```

#### Option 4: Using Built-in Key<T1, T2, ...>
Rystem provides generic key types for 2-5 properties:

```csharp
// For 2 properties
services.AddRepository<OrderItem, Key<Guid, int>>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// Usage
var key = new Key<Guid, int>(orderId, itemNumber);
var item = await _repository.GetAsync(key);

// Available: Key<T1, T2>, Key<T1, T2, T3>, Key<T1, T2, T3, T4>, Key<T1, T2, T3, T4, T5>
```

---

### Example 3: CQRS (Separate Read and Write)

When you have different storage for reads and writes:

```csharp
// Write to SQL Server
services.AddCommand<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// Read from In-Memory Cache or different DB
services.AddQuery<Order, Guid>(builder =>
{
    builder.WithInMemory(options =>
    {
        options.PopulateWithRandomData(100, 10); // For testing
    });
    // Or read from read-replica
    // builder.WithEntityFramework<OrdersReadDbContext>();
});

// Inject separately
public class OrderService
{
    private readonly ICommand<Order, Guid> _orderCommand;
    private readonly IQuery<Order, Guid> _orderQuery;
    
    public OrderService(
        ICommand<Order, Guid> orderCommand,
        IQuery<Order, Guid> orderQuery)
    {
        _orderCommand = orderCommand;
        _orderQuery = orderQuery;
    }
    
    public async Task CreateOrderAsync(Order order)
    {
        await _orderCommand.InsertAsync(order.Id, order);
    }
    
    public async Task<Order?> GetOrderAsync(Guid id)
    {
        return await _orderQuery.GetAsync(id);
    }
}
```

---

## 🏭 Factory Pattern (Multiple Implementations)

Rystem integrates with **IFactory** from `Rystem.DependencyInjection` to support multiple repository implementations for the same entity.

### Use Case
You want:
- Primary storage: SQL Server
- Cache: In-Memory or Redis
- Backup: Azure Blob Storage

### Setup
```csharp
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
}, "primary"); // Factory key

services.AddRepository<Order, Guid>(builder =>
{
    builder.WithInMemory();
}, "cache"); // Factory key

services.AddRepository<Order, Guid>(builder =>
{
    builder.WithBlobStorage(options =>
    {
        options.ConnectionString = configuration["Azure:Storage"];
        options.ContainerName = "orders-backup";
    });
}, "backup"); // Factory key
```

### Usage
```csharp
public class OrderService
{
    private readonly IFactory<IRepository<Order, Guid>> _repositoryFactory;
    
    public OrderService(IFactory<IRepository<Order, Guid>> repositoryFactory)
    {
        _repositoryFactory = repositoryFactory;
    }
    
    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        // Try cache first
        var cacheRepo = _repositoryFactory.Create("cache");
        var cached = await cacheRepo.GetAsync(orderId);
        if (cached != null) return cached;
        
        // Fallback to primary
        var primaryRepo = _repositoryFactory.Create("primary");
        var order = await primaryRepo.GetAsync(orderId);
        
        // Store in cache
        if (order != null)
        {
            await cacheRepo.InsertAsync(orderId, order);
        }
        
        return order;
    }
    
    public async Task BackupOrderAsync(Guid orderId)
    {
        var primaryRepo = _repositoryFactory.Create("primary");
        var backupRepo = _repositoryFactory.Create("backup");
        
        var order = await primaryRepo.GetAsync(orderId);
        if (order != null)
        {
            await backupRepo.InsertAsync(orderId, order);
        }
    }
}
```

**Default Repository**: The **last registered** repository is injected by default when you use `IRepository<T, TKey>` directly.

```csharp
// This will inject the "backup" repository (last one registered)
public OrderService(IRepository<Order, Guid> repository)
{
    _repository = repository;
}
```

---

## 🔧 Query Operations

Rystem provides powerful LINQ-like query capabilities:

### Basic Queries
```csharp
// Get single item
var order = await _repository.GetAsync(orderId);

// Check existence
var exists = await _repository.ExistAsync(orderId);

// Get all
var allOrders = await _repository.QueryAsync().ToListAsync();

// Filter
var pendingOrders = await _repository
    .Where(x => x.Status == OrderStatus.Pending)
    .ToListAsync();

// Multiple conditions
var recentOrders = await _repository
    .Where(x => x.Status == OrderStatus.Confirmed && x.CreatedAt > DateTime.UtcNow.AddDays(-7))
    .ToListAsync();
```

### Sorting and Paging
```csharp
// Order by
var orderedOrders = await _repository
    .Where(x => x.Status == OrderStatus.Shipped)
    .OrderBy(x => x.CreatedAt)
    .ToListAsync();

// Order by descending
var latestOrders = await _repository
    .OrderByDescending(x => x.CreatedAt)
    .Top(10)
    .ToListAsync();

// Pagination
var page = await _repository
    .Where(x => x.Status == OrderStatus.Delivered)
    .OrderByDescending(x => x.CreatedAt)
    .PageAsync(pageNumber: 1, pageSize: 20);

// Skip and Take
var orders = await _repository
    .OrderBy(x => x.OrderNumber)
    .Skip(50)
    .Top(25)
    .ToListAsync();
```

### Aggregations
```csharp
// Count
var count = await _repository
    .Where(x => x.Status == OrderStatus.Pending)
    .CountAsync();

// Max
var maxOrderId = await _repository.MaxAsync(x => x.Id);

// Min
var minCreationDate = await _repository.MinAsync(x => x.CreatedAt);

// Sum (requires numeric property)
var totalQuantity = await _repository
    .Where(x => x.Status == OrderStatus.Delivered)
    .SumAsync(x => x.TotalQuantity);

// Average
var avgOrderValue = await _repository.AverageAsync(x => x.TotalAmount);
```

### First and Single
```csharp
// First or default
var firstOrder = await _repository
    .Where(x => x.CustomerId == customerId)
    .OrderBy(x => x.CreatedAt)
    .FirstOrDefaultAsync();

// Single or default (expects 0 or 1 result)
var order = await _repository
    .Where(x => x.OrderNumber == "ORD-12345")
    .SingleOrDefaultAsync();
```

---

## 🔄 Batch Operations

Execute multiple operations in a single call:

```csharp
var batchOperation = _repository.CreateBatchOperation();

// Add operations
for (var i = 0; i < 100; i++)
{
    var order = new Order 
    { 
        Id = Guid.NewGuid(), 
        OrderNumber = $"ORD-{i:D5}",
        Status = OrderStatus.Pending 
    };
    batchOperation.Insert(order.Id, order);
}

// Execute all at once
var results = await batchOperation.ExecuteAsync().ToListAsync();

// Check results
foreach (var result in results)
{
    if (!result.State.IsOk)
    {
        Console.WriteLine($"Failed: {result.Key} - {result.State.Message}");
    }
}
```

### Batch with Mixed Operations
```csharp
var batch = _repository.CreateBatchOperation();

// Insert
batch.Insert(newOrder.Id, newOrder);

// Update
batch.Update(existingOrder.Id, existingOrder);

// Delete
batch.Delete(oldOrderId);

await batch.ExecuteAsync().ToListAsync();
```

---

## 🌍 Translation (Property Mapping)

When your database schema differs from your domain model:

```csharp
// Domain model
public class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; }
    public string CustomerEmail { get; set; }
}

// Database model
public class OrderEntity
{
    public Guid Identificativo { get; set; }
    public string NumeroOrdine { get; set; }
    public string EmailCliente { get; set; }
}

// Configure translation
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
    
    builder.Translate<OrderEntity>()
        .With(x => x.Id, x => x.Identificativo)
        .With(x => x.OrderNumber, x => x.NumeroOrdine)
        .With(x => x.CustomerEmail, x => x.EmailCliente);
});
```

Now when you query:
```csharp
// This query on Order model...
var orders = await _repository
    .Where(x => x.CustomerEmail.Contains("@gmail.com"))
    .ToListAsync();

// ...is automatically translated to OrderEntity:
// context.Orders.Where(x => x.EmailCliente.Contains("@gmail.com"))
```

**How to Use Translation in Repository Implementation**:
```csharp
public class OrderRepository : IRepository<Order, Guid>
{
    private readonly OrdersDbContext _context;
    
    public async IAsyncEnumerable<Entity<Order, Guid>> QueryAsync(
        IFilterExpression filter, 
        CancellationToken cancellationToken = default)
    {
        // Apply translation and convert to IQueryable
        await foreach (var entity in filter.ApplyAsAsyncEnumerable(_context.Orders))
        {
            yield return new Entity<Order, Guid>
            {
                Key = entity.Identificativo,
                Value = new Order
                {
                    Id = entity.Identificativo,
                    OrderNumber = entity.NumeroOrdine,
                    CustomerEmail = entity.EmailCliente
                }
            };
        }
    }
}
```

---

## 💼 Business Logic Injection

Execute custom logic before or after repository operations:

### Create Business Class
```csharp
public class OrderBusinessLogic : 
    IRepositoryBusinessBeforeInsert<Order, Guid>,
    IRepositoryBusinessAfterInsert<Order, Guid>,
    IRepositoryBusinessBeforeUpdate<Order, Guid>,
    IRepositoryBusinessBeforeDelete<Order, Guid>
{
    private readonly ILogger<OrderBusinessLogic> _logger;
    
    public OrderBusinessLogic(ILogger<OrderBusinessLogic> logger)
    {
        _logger = logger;
    }
    
    // Before insert - validation
    public Task<State<Order, Guid>> BeforeInsertAsync(
        Entity<Order, Guid> entity, 
        CancellationToken cancellationToken = default)
    {
        var order = entity.Value;
        
        // Validation
        if (string.IsNullOrWhiteSpace(order.OrderNumber))
        {
            return Task.FromResult(State.Error<Order, Guid>("Order number is required"));
        }
        
        // Auto-generate values
        if (order.CreatedAt == default)
        {
            order.CreatedAt = DateTime.UtcNow;
        }
        
        _logger.LogInformation("Creating order {OrderNumber}", order.OrderNumber);
        
        return Task.FromResult(State.Ok(entity));
    }
    
    // After insert - side effects
    public Task<State<Order>> AfterInsertAsync(
        State<Order, Guid> state, 
        Entity<Order, Guid> entity, 
        CancellationToken cancellationToken = default)
    {
        if (state.IsOk)
        {
            _logger.LogInformation("Order created successfully: {OrderId}", entity.Key);
            // Send event, notification, etc.
        }
        
        return Task.FromResult(state);
    }
    
    // Before update - prevent invalid transitions
    public Task<State<Order, Guid>> BeforeUpdateAsync(
        Entity<Order, Guid> entity, 
        Entity<Order, Guid>? oldEntity,
        CancellationToken cancellationToken = default)
    {
        if (oldEntity?.Value?.Status == OrderStatus.Cancelled)
        {
            return Task.FromResult(State.Error<Order, Guid>("Cannot update cancelled orders"));
        }
        
        return Task.FromResult(State.Ok(entity));
    }
    
    // Before delete - soft delete
    public Task<State<Order, Guid>> BeforeDeleteAsync(
        Entity<Order, Guid> entity, 
        CancellationToken cancellationToken = default)
    {
        // Instead of deleting, mark as cancelled
        entity.Value!.Status = OrderStatus.Cancelled;
        return Task.FromResult(State.Error<Order, Guid>("Soft delete - order cancelled"));
    }
}
```

### Register Business Logic
```csharp
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
    
    builder
        .AddBusiness()
            .AddBusinessBeforeInsert<OrderBusinessLogic>()
            .AddBusinessAfterInsert<OrderBusinessLogic>()
            .AddBusinessBeforeUpdate<OrderBusinessLogic>()
            .AddBusinessBeforeDelete<OrderBusinessLogic>();
});
```

### Separate Business Registration (Clean Architecture)
If you want to keep business logic in a different project:

```csharp
// In Infrastructure project - just repository setup
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// In Business project - add business logic
services.AddBusinessForRepository<Order, Guid>(builder =>
{
    builder
        .AddBusiness()
            .AddBusinessBeforeInsert<OrderBusinessLogic>()
            .AddBusinessAfterInsert<OrderBusinessLogic>()
            .AddBusinessBeforeUpdate<OrderBusinessLogic>()
            .AddBusinessBeforeDelete<OrderBusinessLogic>();
});
```

**Available Interfaces**:
- `IRepositoryBusinessBeforeInsert<T, TKey>`
- `IRepositoryBusinessAfterInsert<T, TKey>`
- `IRepositoryBusinessBeforeUpdate<T, TKey>`
- `IRepositoryBusinessAfterUpdate<T, TKey>`
- `IRepositoryBusinessBeforeDelete<T, TKey>`
- `IRepositoryBusinessAfterDelete<T, TKey>`

**⚠️ Important**: Business logic classes must be **public** for DI to instantiate them!

---

## 🗄️ Storage Backend Examples

### 1. Entity Framework (SQL Server, PostgreSQL, SQLite)

```csharp
// DbContext
public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Order> Orders { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
        });
    }
}

// Registration
services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("OrdersDb")));

services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});
```

### 2. Azure Cosmos DB (NoSQL)

```csharp
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithCosmosDb(options =>
    {
        options.ConnectionString = configuration["CosmosDb:ConnectionString"];
        options.DatabaseName = "OrdersDb";
        options.ContainerName = "orders";
        options.PartitionKey = "/customerId"; // Partition key path
    });
});
```

### 3. Azure Blob Storage (NoSQL - JSON documents)

```csharp
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithBlobStorage(options =>
    {
        options.ConnectionString = configuration["Azure:Storage"];
        options.ContainerName = "orders";
    });
});
```

### 4. Azure Table Storage (NoSQL - Key-Value)

```csharp
services.AddRepository<Order, OrderKey>(builder =>
{
    builder.WithTableStorage(options =>
    {
        options.ConnectionString = configuration["Azure:Storage"];
        options.TableName = "Orders";
        options.PartitionKey = "CustomerId";
        options.RowKey = "OrderId";
        options.PartitionKeyFunction = x => x.CustomerId.ToString();
        options.RowKeyFunction = x => x.OrderId.ToString();
    });
});
```

### 5. In-Memory (Testing)

```csharp
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithInMemory(options =>
    {
        // Pre-populate with random data
        options
            .PopulateWithRandomData(count: 100, numberOfEntities: 10)
            .WithPattern(x => x.Value!.OrderNumber, @"ORD-[0-9]{5}");
    });
});
```

---

## 🎯 Real-World Example: Complete Order Repository

```csharp
// 1. Entity
namespace CargoLens.Orders.Core.Entities;

public class Order
{
    public Guid Id { get; init; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItem> Items { get; set; } = new();
    
    public void MarkAsConfirmed() => Status = OrderStatus.Confirmed;
    public void MarkAsShipped() => Status = OrderStatus.Shipped;
}

// 2. Repository Interface (Optional - for DDD)
namespace CargoLens.Orders.Core.Interfaces;

public interface IOrderRepository : IRepository<Order, Guid>
{
    // Custom methods if needed
    Task<List<Order>> GetOrdersByCustomerAsync(Guid customerId);
}

// 3. Repository Implementation (if custom methods)
namespace CargoLens.Orders.Storage.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IRepository<Order, Guid> _baseRepository;
    private readonly OrdersDbContext _context;
    
    public OrderRepository(
        IRepository<Order, Guid> baseRepository,
        OrdersDbContext context)
    {
        _baseRepository = baseRepository;
        _context = context;
    }
    
    // Delegate all IRepository methods to base
    public Task<State<Order, Guid>> InsertAsync(Guid key, Order value, CancellationToken cancellationToken = default)
        => _baseRepository.InsertAsync(key, value, cancellationToken);
    
    public Task<State<Order, Guid>> UpdateAsync(Guid key, Order value, CancellationToken cancellationToken = default)
        => _baseRepository.UpdateAsync(key, value, cancellationToken);
    
    public Task<State<Order, Guid>> DeleteAsync(Guid key, CancellationToken cancellationToken = default)
        => _baseRepository.DeleteAsync(key, cancellationToken);
    
    public IAsyncEnumerable<BatchResult<Order, Guid>> BatchAsync(BatchOperations<Order, Guid> operations, CancellationToken cancellationToken = default)
        => _baseRepository.BatchAsync(operations, cancellationToken);
    
    public Task<State<Order, Guid>> ExistAsync(Guid key, CancellationToken cancellationToken = default)
        => _baseRepository.ExistAsync(key, cancellationToken);
    
    public Task<Order?> GetAsync(Guid key, CancellationToken cancellationToken = default)
        => _baseRepository.GetAsync(key, cancellationToken);
    
    public IAsyncEnumerable<Entity<Order, Guid>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        => _baseRepository.QueryAsync(filter, cancellationToken);
    
    public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        => _baseRepository.OperationAsync(operation, filter, cancellationToken);
    
    // Custom method
    public async Task<List<Order>> GetOrdersByCustomerAsync(Guid customerId)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}

// 4. Registration
services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("OrdersDb")));

services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
    
    builder
        .AddBusiness()
            .AddBusinessBeforeInsert<OrderBusinessLogic>();
});

// If custom repository interface
services.AddScoped<IOrderRepository, OrderRepository>();

// 5. Usage in Service
namespace CargoLens.Orders.Business.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    
    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    
    public async Task<Guid> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = dto.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(MapToOrderItem).ToList()
        };
        
        var result = await _orderRepository.InsertAsync(order.Id, order);
        
        if (!result.IsOk)
            throw new InvalidOperationException(result.Message);
        
        return order.Id;
    }
    
    public async Task<List<OrderDto>> GetCustomerOrdersAsync(Guid customerId)
    {
        var orders = await _orderRepository.GetOrdersByCustomerAsync(customerId);
        return orders.Select(MapToDto).ToList();
    }
}
```

---

## 📚 Common Patterns

### Pattern 1: Repository + Business Logic + Factory
```csharp
// Primary database
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
    builder.AddBusiness()
        .AddBusinessBeforeInsert<OrderValidationBusiness>()
        .AddBusinessAfterInsert<OrderEventPublisherBusiness>();
}, "primary");

// Read replica or cache
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersReadDbContext>();
}, "readonly");
```

### Pattern 2: CQRS with Different Databases
```csharp
// Write to SQL
services.AddCommand<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
});

// Read from Cosmos DB (denormalized for fast queries)
services.AddQuery<Order, Guid>(builder =>
{
    builder.WithCosmosDb(options => { /* ... */ });
});
```

### Pattern 3: Layered Caching
```csharp
// L1 Cache: In-Memory
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithInMemory();
}, "memory");

// L2 Cache: Redis
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithDistributedCache();
}, "redis");

// Primary: Database
services.AddRepository<Order, Guid>(builder =>
{
    builder.WithEntityFramework<OrdersDbContext>();
}, "database");
```

---

## ⚠️ Important Notes

1. **Always use `IRepository<T, TKey>`**, NOT `IRepositoryPattern<T, TKey>` when injecting
2. **Always use `ICommand<T, TKey>`**, NOT `ICommandPattern<T, TKey>` when injecting
3. **Always use `IQuery<T, TKey>`**, NOT `IQueryPattern<T, TKey>` when injecting
4. **Business logic classes must be `public`** for DI to instantiate them
5. **Factory pattern uses `IFactory<IRepository<T, TKey>>`** from Rystem.DependencyInjection
6. **Last registered** repository is the default when injecting `IRepository<T, TKey>` directly
7. **Version 9.1.3** is the current stable version for all Rystem packages

---

## 🔗 Related Resources

- **ddd-single-domain**: How to organize repositories in single-domain architecture
- **ddd-multi-domain**: How to organize repositories in multi-domain architecture
- **repository-api-server**: Auto-generate REST APIs from repositories (no controllers needed)
- **repository-api-client-typescript**: Consume repositories from TypeScript/JavaScript apps
- **repository-api-client-dotnet**: Consume repositories from .NET/C# apps (Blazor, MAUI, WPF)
- **install-rystem**: How to install Rystem packages
- **Background Jobs**: Use with repositories for async operations
- **Concurrency**: Lock repositories during critical operations

---

## 📖 Further Reading

- [Rystem.RepositoryFramework GitHub](https://github.com/KeyserDSoze/RepositoryFramework)
- [Unit Tests Examples](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Test)
- [Entity Framework Integration](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Test/RepositoryFramework.Test.Infrastructure.EntityFramework)

---

## ✅ Summary

**Rystem.RepositoryFramework** provides:
- ✅ Unified repository pattern for all storage types
- ✅ Built-in CQRS support
- ✅ Multiple implementations with Factory pattern
- ✅ Business logic injection (before/after hooks)
- ✅ Powerful LINQ-like queries
- ✅ Batch operations
- ✅ Translation for schema mapping
- ✅ Support for complex keys

**Use this tool whenever you need to implement data access in a Rystem application!** 🚀
