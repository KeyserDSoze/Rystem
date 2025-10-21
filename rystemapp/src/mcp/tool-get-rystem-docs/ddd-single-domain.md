---
title: DDD Single Domain
description: Domain-Driven Design for small applications with unified domain - flat structure with domains, business, infrastructures layers
---

# DDD Single Domain - Small Applications

**Purpose**: This tool explains how to apply Domain-Driven Design (DDD) principles for **small, single-domain applications** using Rystem Framework. Use this approach when your application has a cohesive business domain that doesn't need to be split into multiple bounded contexts.

---

## 🎯 When to Use Single Domain

Use single domain architecture when:
- ✅ The application has a **clear, unified business purpose**
- ✅ All features are **closely related** and share the same business rules
- ✅ Team size is **small to medium** (1-10 developers)
- ✅ Business complexity is **low to moderate**
- ✅ You want to **keep things simple** and avoid over-engineering

**Examples**:
- Task Manager (tasks, categories, users)
- Blog Platform (posts, comments, authors)
- Invoice System (invoices, customers, payments)
- Inventory Tracker (products, warehouses, stock movements)

---

## 📁 Project Structure

```
src/
├── domains/
│   └── [ProjectName].Core           # Domain models, interfaces
├── business/
│   └── [ProjectName].Business       # Services, use cases
├── infrastructures/
│   └── [ProjectName].Storage        # Repository implementations
├── applications/
│   ├── [ProjectName].Api            # REST API
│   └── [projectname].app            # React/React Native frontend
└── tests/
    └── [ProjectName].Test           # Unit & integration tests
```

### Key Characteristics
- **Flat structure**: All layers at root level
- **Single Core**: One `.Core` project contains all domain models
- **Single Business**: One `.Business` project contains all services
- **Single Storage**: One `.Storage` project for all data access
- **Unified API**: One `.Api` project serves all endpoints

---

## 🧱 DDD Layers Explained

### 1. Domain Layer (`[ProjectName].Core`)

**What it contains**:
- **Entities**: Objects with identity (e.g., `User`, `Order`, `Product`)
- **Value Objects**: Objects without identity (e.g., `Address`, `Money`, `Email`)
- **Aggregates**: Cluster of entities with a root (e.g., `Order` + `OrderItems`)
- **Domain Events**: Things that happened (e.g., `OrderPlaced`, `UserRegistered`)
- **Repository Interfaces**: Contracts for data access (e.g., `IUserRepository`)
- **Domain Services**: Pure business logic without state

**Example - Entity**:
```csharp
namespace TaskManager.Core.Entities;

public class Task : IEntity<Guid>
{
    public Guid Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DueDate { get; set; }
    
    // Business logic encapsulated
    public void MarkAsComplete()
    {
        if (Status == TaskStatus.Completed)
            throw new InvalidOperationException("Task is already completed");
            
        Status = TaskStatus.Completed;
        // Could raise domain event: TaskCompleted
    }
    
    public void Reopen()
    {
        if (Status != TaskStatus.Completed)
            throw new InvalidOperationException("Only completed tasks can be reopened");
            
        Status = TaskStatus.InProgress;
    }
}

public enum TaskStatus
{
    Todo,
    InProgress,
    Completed,
    Cancelled
}
```

**Example - Value Object**:
```csharp
namespace TaskManager.Core.ValueObjects;

public record Priority(int Value, string Name)
{
    public static Priority Low => new(1, "Low");
    public static Priority Medium => new(2, "Medium");
    public static Priority High => new(3, "High");
    public static Priority Critical => new(4, "Critical");
}
```

**Example - Repository Interface**:
```csharp
namespace TaskManager.Core.Interfaces;

public interface ITaskRepository : IRepository<Task>
{
    Task<IEnumerable<Task>> GetByStatusAsync(TaskStatus status);
    Task<IEnumerable<Task>> GetOverdueTasksAsync();
}
```

---

### 2. Business Layer (`[ProjectName].Business`)

**What it contains**:
- **Application Services**: Orchestrate use cases (e.g., `TaskService`)
- **Use Cases**: Specific business operations (e.g., `CreateTaskUseCase`)
- **DTOs**: Data Transfer Objects for API contracts
- **Validators**: Business rule validation
- **Mappers**: Entity ↔ DTO conversions

**Example - Service**:
```csharp
namespace TaskManager.Business.Services;

public class TaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<TaskService> _logger;
    
    public TaskService(ITaskRepository taskRepository, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _logger = logger;
    }
    
    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Title is required");
        
        // Create entity
        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            DueDate = dto.DueDate
        };
        
        // Persist
        var created = await _taskRepository.InsertAsync(task);
        
        _logger.LogInformation("Task created: {TaskId}", created.Id);
        
        // Return DTO
        return MapToDto(created);
    }
    
    public async Task CompleteTaskAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);
        if (task == null)
            throw new NotFoundException($"Task {taskId} not found");
        
        // Use domain logic
        task.MarkAsComplete();
        
        await _taskRepository.UpdateAsync(task);
    }
}
```

**Example - DTO**:
```csharp
namespace TaskManager.Business.Dtos;

public record CreateTaskDto(
    string Title,
    string Description,
    DateTime? DueDate
);

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? DueDate
);
```

---

### 3. Infrastructure Layer (`[ProjectName].Storage`)

**What it contains**:
- **DbContext**: Entity Framework configuration
- **Repository Implementations**: Concrete data access (using Rystem.RepositoryFramework)
- **Migrations**: Database schema changes
- **Configurations**: Entity mappings, indexes

**Example - DbContext**:
```csharp
namespace TaskManager.Storage;

public class TaskManagerDbContext : DbContext
{
    public TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Task> Tasks { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.Status).HasConversion<string>();
            entity.HasIndex(t => t.Status);
        });
    }
}
```

**Example - Repository (Rystem)**:
```csharp
namespace TaskManager.Storage.Repositories;

// With Rystem.RepositoryFramework, you don't need to implement the repository manually
// Just register it in DI:

// In Program.cs or Startup:
services.AddRepository<Task, TaskRepository>(builder =>
{
    builder.WithEntityFramework<TaskManagerDbContext>();
});

// For custom queries, extend the repository:
public class TaskRepository : ITaskRepository
{
    private readonly IRepository<Task> _baseRepository;
    private readonly TaskManagerDbContext _context;
    
    public TaskRepository(IRepository<Task> baseRepository, TaskManagerDbContext context)
    {
        _baseRepository = baseRepository;
        _context = context;
    }
    
    public async Task<IEnumerable<Task>> GetByStatusAsync(TaskStatus status)
    {
        return await _context.Tasks
            .Where(t => t.Status == status)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Task>> GetOverdueTasksAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Tasks
            .Where(t => t.DueDate.HasValue && t.DueDate < now && t.Status != TaskStatus.Completed)
            .ToListAsync();
    }
}
```

---

### 4. API Layer (`[ProjectName].Api`)

**What it contains**:
- **Controllers**: REST endpoints
- **Middleware**: Authentication, error handling
- **Configuration**: appsettings.json, DI setup
- **Swagger**: API documentation

**Example - Controller**:
```csharp
namespace TaskManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly TaskService _taskService;
    
    public TasksController(TaskService taskService)
    {
        _taskService = taskService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetAll()
    {
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }
    
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskDto dto)
    {
        var created = await _taskService.CreateTaskAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
    
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _taskService.CompleteTaskAsync(id);
        return NoContent();
    }
}
```

**Example - Program.cs**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Rystem services
builder.Services.AddRystem(builder =>
{
    builder.AddRepository<Task, TaskRepository>(repo =>
    {
        repo.WithEntityFramework<TaskManagerDbContext>();
    });
});

// Add DbContext
builder.Services.AddDbContext<TaskManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Add business services
builder.Services.AddScoped<TaskService>();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

### 5. Frontend Layer (`[projectname].app`)

**What it contains**:
- **Components**: Reusable UI components
- **Pages**: Route-level components
- **Services**: API client, state management
- **Hooks**: Custom React hooks
- **i18n**: Multi-language support

**Example - Structure**:
```
[projectname].app/
├── src/
│   ├── components/
│   │   ├── TaskCard.tsx
│   │   ├── TaskForm.tsx
│   │   └── TaskList.tsx
│   ├── pages/
│   │   ├── TasksPage.tsx
│   │   └── TaskDetailPage.tsx
│   ├── services/
│   │   ├── api/
│   │   │   └── taskApi.ts
│   │   └── store/
│   │       └── taskStore.ts
│   ├── hooks/
│   │   └── useTasks.ts
│   ├── i18n/
│   │   ├── en.json
│   │   └── it.json
│   ├── App.tsx
│   └── main.tsx
└── package.json
```

---

## 🔄 Dependencies Between Layers

```
┌─────────────────────┐
│   [ProjectName].Api │  ← Entry point
└──────────┬──────────┘
           │ depends on
           ↓
┌──────────────────────────┐
│  [ProjectName].Business  │  ← Orchestration
└──────────┬───────────────┘
           │ depends on
           ↓
┌──────────────────────────┐
│    [ProjectName].Core    │  ← Domain models
└──────────────────────────┘
           ↑
           │ depends on
┌──────────┴───────────────┐
│  [ProjectName].Storage   │  ← Data access
└──────────────────────────┘
```

**Rules**:
- ✅ `Business` can reference `Core`
- ✅ `Storage` can reference `Core`
- ✅ `Api` can reference `Business` and `Core`
- ✅ `Test` can reference all layers
- ❌ `Core` should NOT reference any other layer (pure domain)
- ❌ `Business` should NOT reference `Storage` (uses interfaces from Core)

---

## 🎯 Best Practices for Single Domain

### 1. Keep Core Pure
```csharp
// ✅ GOOD - Pure domain logic
public class Order
{
    public decimal CalculateTotal()
    {
        return Items.Sum(i => i.Quantity * i.Price);
    }
}

// ❌ BAD - Infrastructure concerns in domain
public class Order
{
    public async Task SaveToDatabase() // NO!
    {
        // This belongs in Storage layer
    }
}
```

### 2. Use Repository Interfaces
```csharp
// ✅ GOOD - Interface in Core, implementation in Storage
// Core:
public interface IOrderRepository : IRepository<Order> { }

// Storage:
public class OrderRepository : IOrderRepository
{
    // Rystem.RepositoryFramework handles implementation
}
```

### 3. Encapsulate Business Logic
```csharp
// ✅ GOOD - Logic in entity
public class Order
{
    public OrderStatus Status { get; private set; }
    
    public void Cancel()
    {
        if (Status == OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel shipped orders");
        
        Status = OrderStatus.Cancelled;
    }
}

// ❌ BAD - Logic in service
public class OrderService
{
    public void CancelOrder(Order order)
    {
        if (order.Status == OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot cancel shipped orders");
        
        order.Status = OrderStatus.Cancelled; // Direct property access
    }
}
```

### 4. Use DTOs for API Contracts
```csharp
// ✅ GOOD - Separate domain and API models
public record CreateOrderDto(Guid CustomerId, List<OrderItemDto> Items);
public class OrderService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var order = MapToEntity(dto);
        // ...
        return MapToDto(order);
    }
}

// ❌ BAD - Exposing entities directly
public class OrderService
{
    public async Task<Order> CreateOrderAsync(Order order) // NO!
    {
        // Exposes internal structure
    }
}
```

---

## 📦 Required Rystem Packages

### Core Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

### Business Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

### Storage Project
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
<PackageReference Include="Rystem.RepositoryFramework.Infrastructure.EntityFramework" Version="9.1.3" />
```

### API Project
```xml
<PackageReference Include="Rystem.DependencyInjection.Web" Version="9.1.3" />
<PackageReference Include="Rystem.Api.Server" Version="9.1.3" />
```

---

## 🚀 Quick Start Example

Create a simple Task Manager application:

```bash
# 1. Create solution
dotnet new sln -n TaskManager

# 2. Create Core
cd src/domains
dotnet new classlib -n TaskManager.Core -f net9.0
dotnet add TaskManager.Core package Rystem.DependencyInjection -v 9.1.3

# 3. Create Business
cd ../business
dotnet new classlib -n TaskManager.Business -f net9.0
dotnet add TaskManager.Business package Rystem.DependencyInjection -v 9.1.3
dotnet add TaskManager.Business reference ../domains/TaskManager.Core/TaskManager.Core.csproj

# 4. Create Storage
cd ../infrastructures
dotnet new classlib -n TaskManager.Storage -f net9.0
dotnet add TaskManager.Storage package Rystem.DependencyInjection -v 9.1.3
dotnet add TaskManager.Storage package Rystem.RepositoryFramework.Infrastructure.EntityFramework -v 9.1.3
dotnet add TaskManager.Storage reference ../domains/TaskManager.Core/TaskManager.Core.csproj

# 5. Create API
cd ../applications
dotnet new webapi -n TaskManager.Api -f net9.0
dotnet add TaskManager.Api package Rystem.DependencyInjection.Web -v 9.1.3
dotnet add TaskManager.Api package Rystem.Api.Server -v 9.1.3
dotnet add TaskManager.Api reference ../business/TaskManager.Business/TaskManager.Business.csproj
dotnet add TaskManager.Api reference ../infrastructures/TaskManager.Storage/TaskManager.Storage.csproj

# 6. Create React App
npx create-vite taskmanager.app --template react-ts
```

---

## 📚 When to Upgrade to Multi-Domain

Consider migrating to **multi-domain architecture** when:
- 🔴 The codebase is becoming too large (>50 entities)
- 🔴 Different teams work on different features
- 🔴 Business logic has distinct, independent areas
- 🔴 You need to deploy features independently
- 🔴 Different parts have different scalability requirements

→ See the `ddd-multi-domain` tool for advanced architecture

---

## 🔗 Related Tools

- **ddd-multi-domain**: For complex applications with multiple bounded contexts
- **repository-setup**: Configure Rystem.RepositoryFramework
- **project-setup**: Scaffold complete project structure
- **auth-flow**: Add authentication

---

## ✅ Summary

**Single Domain DDD** is perfect for:
- ✅ Small to medium applications
- ✅ Cohesive business domain
- ✅ Simple team structure
- ✅ Quick development cycles

**Key Principles**:
1. Keep domain logic in `Core` (pure)
2. Orchestrate use cases in `Business`
3. Implement data access in `Storage`
4. Expose APIs in `Api`
5. Use Rystem packages for DI and Repository Pattern

**Result**: Clean, maintainable, testable codebase ready for growth! 🚀
