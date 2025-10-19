# Rystem Framework - GitHub Copilot Instructions

Follow Rystem Framework patterns and best practices when working in this repository.

## 📚 Documentation References

### Core Architecture
- **Project Setup**: https://rystem.net/mcp/tools/project-setup.md
- **Domain-Driven Design**: https://rystem.net/mcp/tools/ddd.md
- **Repository Pattern**: https://rystem.net/mcp/tools/repository-setup.md
- **Package Installation**: https://rystem.net/mcp/tools/install-rystem.md

### Best Practices
- **Background Jobs**: https://rystem.net/mcp/resources/background-jobs.md
- **Concurrency Control**: https://rystem.net/mcp/resources/concurrency.md
- **Content Repository**: https://rystem.net/mcp/resources/content-repo.md

### Templates & Prompts
- **Authentication Flow**: https://rystem.net/mcp/prompts/auth-flow.md
- **Service Setup with DI**: https://rystem.net/mcp/prompts/service-setup.md

## 🏗️ Project Structure Guidelines

### Single Domain Architecture
```
src/
├── domains/[ProjectName].Core           # Entities, value objects, interfaces
├── business/[ProjectName].Business      # Services, use cases
├── infrastructures/[ProjectName].Storage # Repository implementations
├── applications/[ProjectName].Api       # REST API
└── tests/[ProjectName].Test            # Unit & integration tests
```

### Multiple Domain Architecture
```
src/
├── [DomainName]/                       # Each domain isolated
│   ├── domains/[ProjectName].[DomainName].Core
│   ├── business/[ProjectName].[DomainName].Business
│   ├── infrastructures/[ProjectName].[DomainName].Storage
│   ├── applications/[ProjectName].[DomainName].Api
│   └── tests/[ProjectName].[DomainName].Test
└── app/[projectname].app               # Frontend aggregator
```

## 📦 Required NuGet Packages

**Always include in Core/Domain projects:**
```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

**For Repository Pattern:**
```xml
<PackageReference Include="Rystem.RepositoryFramework.Infrastructure.EntityFramework" Version="9.1.3" />
```

**For API Projects:**
```xml
<PackageReference Include="Rystem.DependencyInjection.Web" Version="9.1.3" />
<PackageReference Include="Rystem.Api.Server" Version="9.1.3" />
```

## 🎯 Code Conventions

### .csproj Configuration
```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

### C# Best Practices
- Use **C# 12** features (primary constructors, collection expressions, etc.)
- Enable **nullable reference types**
- Use **record types** for DTOs and value objects
- Use **minimal APIs** for endpoints when possible
- Follow **DDD patterns**: Aggregates, Entities, Value Objects, Domain Events

### Naming Conventions
- **Single Domain**: `[ProjectName].Core`, `[ProjectName].Business`, etc.
- **Multiple Domains**: `[ProjectName].[DomainName].Core`, etc.
- **Frontend**: `[projectname].app` (lowercase)

## 🔧 Common Patterns

### Repository Setup
```csharp
// In Core project - Interface
public interface IUserRepository : IRepository<User> { }

// In Storage project - Implementation
public class UserRepository : IUserRepository
{
    // Rystem.RepositoryFramework handles implementation
}

// In API - Dependency Injection
services.AddRepository<User, UserRepository>(repositoryBuilder => 
{
    repositoryBuilder.WithEntityFramework<AppDbContext>();
});
```

### DDD Aggregate
```csharp
// Aggregate Root
public class Order : IEntity<Guid>
{
    public Guid Id { get; init; }
    public OrderStatus Status { get; private set; }
    private List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public void AddItem(Product product, int quantity)
    {
        // Business logic here
        _items.Add(new OrderItem(product.Id, quantity, product.Price));
    }
}

// Value Object
public record OrderItem(Guid ProductId, int Quantity, decimal Price);
```

### Background Job
```csharp
services.AddBackgroundJob<EmailNotificationJob>(options =>
{
    options.Cron = "0 */5 * * * *"; // Every 5 minutes
});
```

### Concurrency Control
```csharp
services.AddConcurrency(options =>
{
    options.WithRedis("connection-string");
});

// Usage
await concurrency.TryLockAsync("order-123", async () =>
{
    // Critical section
});
```

## 🚀 Quick Start Commands

### Create New Single Domain Project
```bash
# From project root
dotnet new sln -n [ProjectName]
mkdir -p src/domains src/business src/infrastructures src/applications src/tests

# Create projects
cd src/domains && dotnet new classlib -n [ProjectName].Core -f net9.0
cd ../business && dotnet new classlib -n [ProjectName].Business -f net9.0
cd ../infrastructures && dotnet new classlib -n [ProjectName].Storage -f net9.0
cd ../applications && dotnet new webapi -n [ProjectName].Api -f net9.0
cd ../tests && dotnet new xunit -n [ProjectName].Test -f net9.0

# Add packages
dotnet add [ProjectName].Core package Rystem.DependencyInjection -v 9.1.3
```

### Create React Frontend
```bash
cd src/applications
npx create-vite [projectname].app --template react-ts
```

## 📖 When to Reference What

### Creating New Projects
→ Reference: https://rystem.net/mcp/tools/project-setup.md

### Implementing Domain Models
→ Reference: https://rystem.net/mcp/tools/ddd.md

### Setting Up Data Access
→ Reference: https://rystem.net/mcp/tools/repository-setup.md

### Adding Authentication
→ Reference: https://rystem.net/mcp/prompts/auth-flow.md

### Background Processing
→ Reference: https://rystem.net/mcp/resources/background-jobs.md

### Handling Concurrency
→ Reference: https://rystem.net/mcp/resources/concurrency.md

## 💡 Tips

- **Always start** with the project structure from `project-setup.md`
- **Follow DDD** principles from `ddd.md` for domain models
- **Use Rystem.RepositoryFramework** instead of manual EF Core repositories
- **Leverage dependency injection** with Rystem.DependencyInjection
- **Check the documentation** at https://rystem.net before asking for clarification

---

📚 **Full Documentation**: https://rystem.net  
🔧 **MCP Server**: https://rystem.net/mcp  
💻 **GitHub**: https://github.com/KeyserDSoze/Rystem
