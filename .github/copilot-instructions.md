# Rystem Framework - GitHub Copilot Instructions

Follow Rystem Framework patterns and best practices when working in this repository.

## 📚 Documentation References

### Core Architecture
- **Project Setup**: https://rystem.net/mcp/tools/project-setup.md
- **DDD Single Domain**: https://rystem.net/mcp/tools/ddd-single-domain.md (for small apps)
- **DDD Multi-Domain**: https://rystem.net/mcp/tools/ddd-multi-domain.md (for enterprise apps)
- **Repository Pattern**: https://rystem.net/mcp/tools/repository-setup.md
- **Repository API Server**: https://rystem.net/mcp/tools/repository-api-server.md (auto-generate REST APIs)
- **Repository API Client (TypeScript)**: https://rystem.net/mcp/tools/repository-api-client-typescript.md
- **Repository API Client (.NET)**: https://rystem.net/mcp/tools/repository-api-client-dotnet.md
- **Package Installation**: https://rystem.net/mcp/tools/install-rystem.md

### Rystem Utilities
- **Discriminated Union**: https://rystem.net/mcp/tools/rystem-discriminated-union.md (AnyOf for type-safe unions)
- **Stopwatch**: https://rystem.net/mcp/tools/rystem-stopwatch.md (execution time monitoring)
- **LINQ Serializer**: https://rystem.net/mcp/tools/rystem-linq-serializer.md (serialize/deserialize expressions)
- **Reflection**: https://rystem.net/mcp/tools/rystem-reflection.md (reflection helpers and mocking)
- **Text Extensions**: https://rystem.net/mcp/tools/rystem-text-extensions.md (string/byte/stream conversions)
- **CSV & Minimization**: https://rystem.net/mcp/tools/rystem-csv.md (CSV and compact serialization)
- **JSON Extensions**: https://rystem.net/mcp/tools/rystem-json-extensions.md (ToJson/FromJson)
- **Task Extensions**: https://rystem.net/mcp/tools/rystem-task-extensions.md (NoContext, TaskManager)
- **ConcurrentList**: https://rystem.net/mcp/tools/rystem-concurrent-list.md (thread-safe List)
- **DI Factory Pattern**: https://rystem.net/mcp/tools/rystem-dependencyinjection-factory.md (named services with AddFactory)
- **Background Job**: https://rystem.net/mcp/tools/rystem-backgroundjob.md (CRON-based recurring tasks)
- **Async Lock**: https://rystem.net/mcp/tools/rystem-async-lock.md (key-based async locking)
- **Race Condition**: https://rystem.net/mcp/tools/rystem-race-condition.md (block duplicate operations)
- **In-Memory Queue**: https://rystem.net/mcp/tools/rystem-queue.md (batch processing with time/size limits)

### Content Repository (File Storage)
- **Content Repository Pattern**: https://rystem.net/mcp/tools/content-repository.md (unified interface for file storage)
- **Azure Blob Storage**: https://rystem.net/mcp/tools/content-repository-blob.md (large files, CDN, scalable storage)
- **Azure File Storage**: https://rystem.net/mcp/tools/content-repository-file.md (SMB shares, legacy apps, enterprise file sharing)
- **SharePoint Online**: https://rystem.net/mcp/tools/content-repository-sharepoint.md (Office 365, document collaboration)
- **In-Memory Storage**: https://rystem.net/mcp/tools/content-repository-inmemory.md (testing, caching, development)

### Social Authentication
- **Server Setup**: https://rystem.net/mcp/tools/auth-social-server.md (Google, Microsoft, Facebook, GitHub OAuth for APIs)
- **Blazor Client**: https://rystem.net/mcp/tools/auth-social-blazor.md (social login UI for Blazor Server/WASM)
- **TypeScript/React Client**: https://rystem.net/mcp/tools/auth-social-typescript.md (React hooks for social login)

### Best Practices
- **Background Jobs**: https://rystem.net/mcp/resources/background-jobs.md
- **Concurrency Control**: https://rystem.net/mcp/resources/concurrency.md
- **Content Repository**: https://rystem.net/mcp/resources/content-repo.md

### Templates & Prompts
- **Authentication Flow**: https://rystem.net/mcp/prompts/auth-flow.md
- **Service Setup with DI**: https://rystem.net/mcp/prompts/service-setup.md
- **Project Setup Prompt**: https://rystem.net/mcp/prompts/project-setup.md
- **Ready-to-Use Template**: https://rystem.net/mcp/prompts/project-setup-template.md
- **Standard Rystem Template**: https://rystem.net/mcp/prompts/project-setup-template-singledomain-classicrystem.md
- **Code Review - Alessandro Rapiti Style**: https://rystem.net/mcp/prompts/code-review-rapiti.md

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
→ Reference: https://rystem.net/mcp/prompts/project-setup.md

### Understanding DDD Architecture
→ Small Apps: https://rystem.net/mcp/tools/ddd-single-domain.md  
→ Enterprise Apps: https://rystem.net/mcp/tools/ddd-multi-domain.md

### Setting Up Data Access
→ Reference: https://rystem.net/mcp/tools/repository-setup.md

### Exposing Repositories as REST APIs
→ Reference: https://rystem.net/mcp/tools/repository-api-server.md

### Consuming Repositories from Frontend (TypeScript)
→ Reference: https://rystem.net/mcp/tools/repository-api-client-typescript.md

### Consuming Repositories from .NET Client (Blazor, MAUI, WPF)
→ Reference: https://rystem.net/mcp/tools/repository-api-client-dotnet.md

### Adding Authentication
→ Reference: https://rystem.net/mcp/prompts/auth-flow.md

### Scheduling Recurring Tasks
→ Reference: https://rystem.net/mcp/tools/rystem-backgroundjob.md

### Background Processing
→ Reference: https://rystem.net/mcp/resources/background-jobs.md

### Handling Concurrency
→ Reference: https://rystem.net/mcp/resources/concurrency.md

### Code Review & Best Practices
→ Reference: https://rystem.net/mcp/prompts/code-review-rapiti.md

### Working with File Storage
→ Main Pattern: https://rystem.net/mcp/tools/content-repository.md  
→ Large Files & CDN: https://rystem.net/mcp/tools/content-repository-blob.md  
→ SMB Shares & Legacy Apps: https://rystem.net/mcp/tools/content-repository-file.md  
→ Office 365 Integration: https://rystem.net/mcp/tools/content-repository-sharepoint.md  
→ Testing & Development: https://rystem.net/mcp/tools/content-repository-inmemory.md

### Implementing Social Login
→ API Backend: https://rystem.net/mcp/tools/auth-social-server.md  
→ Blazor Frontend: https://rystem.net/mcp/tools/auth-social-blazor.md  
→ React/TypeScript Frontend: https://rystem.net/mcp/tools/auth-social-typescript.md

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
