---
title: Install Rystem Packages
description: Quick guide to install Rystem NuGet packages with correct versions - includes core packages, repository framework, authentication, and infrastructure components
---

# Install Rystem Package

**Purpose**: This tool provides step-by-step instructions for installing Rystem packages and their dependencies.

---

## Core Packages

### Rystem (Core Library)
```bash
dotnet add package Rystem
```

Essential utilities, extensions, and helpers for .NET development.

### Rystem.DependencyInjection
```bash
dotnet add package Rystem.DependencyInjection
```

Advanced dependency injection features and service factory patterns.

### Repository Framework
```bash
# Abstractions (required)
dotnet add package Rystem.RepositoryFramework.Abstractions

# Choose your storage backend
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Cosmos.Sql
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Table
```

### Background Jobs
```bash
dotnet add package Rystem.BackgroundJob
```

### Queue Management
```bash
dotnet add package Rystem.Queue
```

### Concurrency Control
```bash
dotnet add package Rystem.Concurrency
# For distributed locks with Redis
dotnet add package Rystem.Concurrency.Redis
```

### Content Repository
```bash
dotnet add package Rystem.Content.Abstractions
# Choose your storage
dotnet add package Rystem.Content.Infrastructure.Storage.Blob
dotnet add package Rystem.Content.Infrastructure.Storage.File
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint
```

### API Integration
```bash
dotnet add package Rystem.Api.Client
dotnet add package Rystem.Api.Server
```

## Quick Start Example

### 1. Install Core Package
```bash
dotnet new webapi -n MyRystemApp
cd MyRystemApp
dotnet add package Rystem
dotnet add package Rystem.RepositoryFramework.Abstractions
dotnet add package Rystem.RepositoryFramework.Infrastructure.EntityFramework
```

### 2. Configure Services
```csharp
// Program.cs
using Rystem;

var builder = WebApplication.CreateBuilder(args);

// Add Rystem services
builder.Services.AddRepository<User, string>(repository =>
{
    repository.WithEntityFramework<ApplicationDbContext>();
});

var app = builder.Build();
app.Run();
```

### 3. Use in Your Code
```csharp
public class UserService
{
    private readonly IRepository<User, string> _userRepository;

    public UserService(IRepository<User, string> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserAsync(string id)
    {
        return await _userRepository.GetAsync(id);
    }
}
```

## Version Compatibility

- .NET 6.0 or higher
- .NET 7.0 recommended
- .NET 8.0 fully supported

## See Also

- [NuGet Gallery](https://www.nuget.org/packages?q=Rystem)
- [GitHub Repository](https://github.com/KeyserDSoze/Rystem)
- [Documentation](https://rystem.net)
