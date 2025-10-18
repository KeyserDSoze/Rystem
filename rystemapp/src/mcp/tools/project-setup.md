---
title: "New Project Setup with Domain Architecture"
description: "Complete guide to setup a new project with modular domain architecture, supporting both single and multiple domain structures using Rystem Framework"
category: "setup"
tags: ["project-setup", "architecture", "ddd", "domain-driven-design", "structure"]
---

# New Project Setup with Domain Architecture

Complete guide to create a new project following a **modular domain architecture** (Domain-Driven Design light), where each domain can be independent with its own infrastructure, business logic, and API.

## ⚠️ IMPORTANT: Project Name Required

**Before starting, you MUST provide your project name!**

Throughout this guide, we use `[ProjectName]` as a placeholder. **Replace ALL occurrences** of `[ProjectName]` with your actual project name.

**Example:** If your project is called "CargoLens", replace:
- `[ProjectName].Api` → `CargoLens.Api`
- `[ProjectName].Core` → `CargoLens.Core`
- `[projectname].app` → `cargolens.app` (lowercase for frontend)

**📝 What is your project name?** ___________________________

---

## 🏗️ Project Types

The project can be of two types:

- **Single Domain:** One main domain
- **Multiple Domain:** Multiple separate but coexisting domains in the same application ecosystem

---

## 📁 General Structure

```
src/
├── applications/              # Backend API (ASP.NET Core) and React frontend
│   ├── [ProjectName].Api     # Main REST API for domains
│   └── [projectname].app     # React frontend application (TypeScript)
│
├── business/                  # Business logic
│   └── [ProjectName].Business    # Services, use cases, orchestrations
│
├── domains/                   # Domain models and contracts
│   └── [ProjectName].Core    # Entities, value objects, repository interfaces, DTOs, etc.
│
├── infrastructures/           # Physical storage implementations, integrations, etc.
│   └── [ProjectName].Storage     # Storage implementation for domain (e.g., EF, SQL, Blob, etc.)
│
├── tests/                     # Integration and unit tests
│   └── [ProjectName].Test
```

---

## 🧩 Single Domain Architecture

When the application is **single domain**, the structure remains flat:

- Everything revolves around a single **core domain** (`[ProjectName].Core`)
- Other layers (business, infrastructures, API, app, test) directly reference that domain

### Example Structure

```
src/
├── domains/
│   └── [ProjectName].Core
├── business/
│   └── [ProjectName].Business
├── infrastructures/
│   └── [ProjectName].Storage
├── applications/
│   ├── [ProjectName].Api
│   └── [projectname].app
├── tests/
│   └── [ProjectName].Test
```

Each project is **nominally unique**, without domain suffixes.

---

## 🧱 Multiple Domain Architecture

When the application manages multiple domains (e.g., **Orders**, **Shipments**, **Customers**), **the general structure changes significantly**: each domain becomes a **completely isolated folder** containing its own vertical slice of the architecture.

### 🔄 Key Difference

In multiple domain architecture, **each domain has its own folder at the root level** (`src/[DomainName]`), and inside each domain folder you'll find the same structure as a single domain project.

### Example Structure

```
src/
├── Orders/                           # Orders Domain (isolated)
│   ├── domains/
│   │   └── [ProjectName].Orders.Core
│   ├── business/
│   │   └── [ProjectName].Orders.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Orders.Storage
│   ├── applications/
│   │   └── [ProjectName].Orders.Api      # API specific for Orders domain
│   └── tests/
│       └── [ProjectName].Orders.Test
│
├── Shipments/                        # Shipments Domain (isolated)
│   ├── domains/
│   │   └── [ProjectName].Shipments.Core
│   ├── business/
│   │   └── [ProjectName].Shipments.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Shipments.Storage
│   ├── applications/
│   │   └── [ProjectName].Shipments.Api   # API specific for Shipments domain
│   └── tests/
│       └── [ProjectName].Shipments.Test
│
├── Customers/                        # Customers Domain (isolated)
│   ├── domains/
│   │   └── [ProjectName].Customers.Core
│   ├── business/
│   │   └── [ProjectName].Customers.Business
│   ├── infrastructures/
│   │   └── [ProjectName].Customers.Storage
│   ├── applications/
│   │   └── [ProjectName].Customers.Api   # API specific for Customers domain
│   └── tests/
│       └── [ProjectName].Customers.Test
│
└── app/                              # Frontend Domain (if unified frontend)
    └── [projectname].app             # React app with all domain modules
    # OR micro-frontends aggregator if using micro-frontend architecture
```

### 📱 Frontend Architecture in Multiple Domains

#### Option 1: Unified Frontend (Recommended)
Create a dedicated **`app`** domain folder containing a single React application that aggregates all backend domains:

```
src/
├── app/
│   └── [projectname].app/
│       ├── src/
│       │   ├── domains/
│       │   │   ├── orders/        # Orders UI module
│       │   │   ├── shipments/     # Shipments UI module
│       │   │   └── customers/     # Customers UI module
│       │   ├── shared/
│       │   └── app.tsx
│       └── package.json
```

#### Option 2: Micro-Frontends
If each domain has its own frontend, **still create the `app` domain** to host the aggregator/shell:

```
src/
├── Orders/
│   └── applications/
│       ├── [ProjectName].Orders.Api
│       └── [projectname].orders.app    # Orders micro-frontend
├── Shipments/
│   └── applications/
│       ├── [ProjectName].Shipments.Api
│       └── [projectname].shipments.app # Shipments micro-frontend
└── app/
    └── [projectname].shell.app         # Shell/Aggregator for micro-frontends
```

**Important:** Even with micro-frontends, the `app` domain contains the **shell application** that orchestrates and loads the individual micro-frontends.

### 🎯 Benefits of Domain Folders

- **Complete Isolation**: Each domain is a self-contained unit
- **Independent Deployment**: Domains can be deployed separately
- **Team Organization**: Different teams can own different domain folders
- **Clear Boundaries**: No confusion about which code belongs to which domain
- **Scalability**: Easy to add/remove domains without affecting others

---

## ⚙️ Naming Rules

| Project Type         | Single Domain              | Multiple Domain (Folder-Based)           |
|---------------------|---------------------------|------------------------------------------|
| **Folder Structure**| `src/[layer]/[project]`   | `src/[DomainName]/[layer]/[project]`    |
| Domain              | [ProjectName].Core        | [ProjectName].[DomainName].Core         |
| Business Layer      | [ProjectName].Business    | [ProjectName].[DomainName].Business     |
| Infrastructure Layer| [ProjectName].Storage     | [ProjectName].[DomainName].Storage      |
| Test                | [ProjectName].Test        | [ProjectName].[DomainName].Test         |
| API                 | [ProjectName].Api         | [ProjectName].[DomainName].Api          |
| Frontend App        | [projectname].app         | [projectname].app (in `src/app/`)       |

**Key Points:**
- **Single Domain**: All layers at root level (`src/domains/`, `src/business/`, etc.)
- **Multiple Domain**: Each domain in its own folder (`src/Orders/`, `src/Shipments/`, etc.)
- **Frontend**: Always in dedicated `src/app/` folder for multiple domains

---

## 🧠 Design Guidelines

### 1. Domain-Driven Design (DDD Light)

Each domain defines:
- **Entities and Value Objects** in the `Core` layer
- **Use cases and services** in the `Business` layer
- **Concrete repositories and database** in the `Storage` layer

### 2. Dependency Direction

```
API → Business → Core
Storage → Core
Tests → Business/Storage
```

Rules:
- `Business` depends on `Core`
- `Storage` depends on `Core`
- `API` depends on `Business` and `Core`
- No layer depends on `App` or `Tests`

### 3. React App Modular Structure

The frontend (`[projectname].app`) is structured in modules:

```
src/
├── domains/
│   ├── orders/
│   ├── shipments/
│   └── customers/
├── shared/
└── app.tsx
```

Each frontend module reflects a backend domain.

### 4. Repository Pattern and Dependency Injection

Each storage implements interfaces defined in the `Core` through `Rystem.RepositoryFramework` or `EF Core`.

---

## 🚀 Creating a New Domain

### Single Domain Project
When using a single domain, the structure is already defined (no additional domains needed).

### Multiple Domain Project
When adding a new domain to a multiple domain architecture:

1. **Create the domain folder** at root level:
   ```bash
   mkdir src/[DomainName]
   ```

2. **Inside the domain folder, replicate the single domain structure**:
   ```
   src/[DomainName]/
   ├── domains/
   │   └── [ProjectName].[DomainName].Core
   ├── business/
   │   └── [ProjectName].[DomainName].Business
   ├── infrastructures/
   │   └── [ProjectName].[DomainName].Storage
   ├── applications/
   │   └── [ProjectName].[DomainName].Api
   └── tests/
       └── [ProjectName].[DomainName].Test
   ```

3. **Update the frontend** (`src/app/[projectname].app`) to include the new domain module

4. **Optional**: Configure API Gateway or service mesh to route requests to the new domain API

**Example**: Adding a "Payments" domain:
```bash
mkdir src/Payments
cd src/Payments
mkdir domains business infrastructures applications tests
# Then create projects inside each folder following the naming convention
```

---

## 📦 Required NuGet Package

### ⚠️ Important

**Always add this package to domain libraries:**

```xml
<PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
```

This package provides essential dependency injection and service registration capabilities for Rystem Framework.

---

## 📄 .csproj Template for .NET 9

Always create `.csproj` files in all project folders targeting **.NET 9**:

### Core Project (Domain)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
  </ItemGroup>

</Project>
```

### Business Project

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\domains\[ProjectName].Core\[ProjectName].Core.csproj" />
    <!-- Or for multiple domains: -->
    <!-- <ProjectReference Include="..\domains\[ProjectName].[DomainName].Core\[ProjectName].[DomainName].Core.csproj" /> -->
  </ItemGroup>

</Project>
```

### Storage Project

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Rystem.DependencyInjection" Version="9.1.3" />
    <PackageReference Include="Rystem.RepositoryFramework.Infrastructure.EntityFramework" Version="9.1.3" />
    <!-- Or other Rystem infrastructure packages as needed -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\domains\[ProjectName].Core\[ProjectName].Core.csproj" />
  </ItemGroup>

</Project>
```

### API Project

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Rystem.DependencyInjection.Web" Version="9.1.3" />
    <PackageReference Include="Rystem.Api.Server" Version="9.1.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\business\[ProjectName].Business\[ProjectName].Business.csproj" />
    <ProjectReference Include="..\infrastructures\[ProjectName].Storage\[ProjectName].Storage.csproj" />
  </ItemGroup>

</Project>
```

### Test Project

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Rystem.Test.XUnit" Version="9.1.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\business\[ProjectName].Business\[ProjectName].Business.csproj" />
    <ProjectReference Include="..\infrastructures\[ProjectName].Storage\[ProjectName].Storage.csproj" />
  </ItemGroup>

</Project>
```

---

## 🎯 Step-by-Step Setup

### Single Domain Project

1. **Create Solution and Folders**
   ```bash
   dotnet new sln -n [ProjectName]
   mkdir src
   cd src
   mkdir applications business domains infrastructures tests
   ```

2. **Create Core Domain**
   ```bash
   cd domains
   dotnet new classlib -n [ProjectName].Core -f net9.0
   dotnet add [ProjectName].Core package Rystem.DependencyInjection -v 9.1.3
   cd ..
   dotnet sln add domains/[ProjectName].Core/[ProjectName].Core.csproj
   ```

3. **Create Business Layer**
   ```bash
   cd business
   dotnet new classlib -n [ProjectName].Business -f net9.0
   dotnet add [ProjectName].Business package Rystem.DependencyInjection -v 9.1.3
   dotnet add [ProjectName].Business reference ../domains/[ProjectName].Core/[ProjectName].Core.csproj
   cd ..
   dotnet sln add business/[ProjectName].Business/[ProjectName].Business.csproj
   ```

4. **Create Storage Layer**
   ```bash
   cd infrastructures
   dotnet new classlib -n [ProjectName].Storage -f net9.0
   dotnet add [ProjectName].Storage package Rystem.DependencyInjection -v 9.1.3
   dotnet add [ProjectName].Storage package Rystem.RepositoryFramework.Infrastructure.EntityFramework -v 9.1.3
   dotnet add [ProjectName].Storage reference ../domains/[ProjectName].Core/[ProjectName].Core.csproj
   cd ..
   dotnet sln add infrastructures/[ProjectName].Storage/[ProjectName].Storage.csproj
   ```

5. **Create API**
   ```bash
   cd applications
   dotnet new webapi -n [ProjectName].Api -f net9.0
   dotnet add [ProjectName].Api package Rystem.DependencyInjection.Web -v 9.1.3
   dotnet add [ProjectName].Api package Rystem.Api.Server -v 9.1.3
   dotnet add [ProjectName].Api reference ../business/[ProjectName].Business/[ProjectName].Business.csproj
   dotnet add [ProjectName].Api reference ../infrastructures/[ProjectName].Storage/[ProjectName].Storage.csproj
   cd ..
   dotnet sln add applications/[ProjectName].Api/[ProjectName].Api.csproj
   ```

6. **Create React App**
   ```bash
   cd applications
   npx create-vite [projectname].app --template react-ts
   cd ../..
   ```

7. **Create Test Project**
   ```bash
   cd tests
   dotnet new xunit -n [ProjectName].Test -f net9.0
   dotnet add [ProjectName].Test package Rystem.Test.XUnit -v 9.1.3
   dotnet add [ProjectName].Test reference ../business/[ProjectName].Business/[ProjectName].Business.csproj
   dotnet add [ProjectName].Test reference ../infrastructures/[ProjectName].Storage/[ProjectName].Storage.csproj
   cd ..
   dotnet sln add tests/[ProjectName].Test/[ProjectName].Test.csproj
   ```

### Multiple Domain Project

For multiple domains, **each domain gets its own root folder** with the complete structure inside:

1. **Create Solution**
   ```bash
   dotnet new sln -n [ProjectName]
   mkdir src
   cd src
   ```

2. **Create First Domain Folder (e.g., Orders)**
   ```bash
   mkdir Orders
   cd Orders
   mkdir domains business infrastructures applications tests
   ```

3. **Create Core Domain**
   ```bash
   cd domains
   dotnet new classlib -n [ProjectName].Orders.Core -f net9.0
   dotnet add [ProjectName].Orders.Core package Rystem.DependencyInjection -v 9.1.3
   cd ..
   dotnet sln ../../[ProjectName].sln add domains/[ProjectName].Orders.Core/[ProjectName].Orders.Core.csproj
   ```

4. **Create Business Layer**
   ```bash
   cd business
   dotnet new classlib -n [ProjectName].Orders.Business -f net9.0
   dotnet add [ProjectName].Orders.Business package Rystem.DependencyInjection -v 9.1.3
   dotnet add [ProjectName].Orders.Business reference ../domains/[ProjectName].Orders.Core/[ProjectName].Orders.Core.csproj
   cd ..
   dotnet sln ../../[ProjectName].sln add business/[ProjectName].Orders.Business/[ProjectName].Orders.Business.csproj
   ```

5. **Create Storage Layer**
   ```bash
   cd infrastructures
   dotnet new classlib -n [ProjectName].Orders.Storage -f net9.0
   dotnet add [ProjectName].Orders.Storage package Rystem.DependencyInjection -v 9.1.3
   dotnet add [ProjectName].Orders.Storage package Rystem.RepositoryFramework.Infrastructure.EntityFramework -v 9.1.3
   dotnet add [ProjectName].Orders.Storage reference ../domains/[ProjectName].Orders.Core/[ProjectName].Orders.Core.csproj
   cd ..
   dotnet sln ../../[ProjectName].sln add infrastructures/[ProjectName].Orders.Storage/[ProjectName].Orders.Storage.csproj
   ```

6. **Create API**
   ```bash
   cd applications
   dotnet new webapi -n [ProjectName].Orders.Api -f net9.0
   dotnet add [ProjectName].Orders.Api package Rystem.DependencyInjection.Web -v 9.1.3
   dotnet add [ProjectName].Orders.Api package Rystem.Api.Server -v 9.1.3
   dotnet add [ProjectName].Orders.Api reference ../business/[ProjectName].Orders.Business/[ProjectName].Orders.Business.csproj
   dotnet add [ProjectName].Orders.Api reference ../infrastructures/[ProjectName].Orders.Storage/[ProjectName].Orders.Storage.csproj
   cd ..
   dotnet sln ../../[ProjectName].sln add applications/[ProjectName].Orders.Api/[ProjectName].Orders.Api.csproj
   ```

7. **Create Test Project**
   ```bash
   cd tests
   dotnet new xunit -n [ProjectName].Orders.Test -f net9.0
   dotnet add [ProjectName].Orders.Test package Rystem.Test.XUnit -v 9.1.3
   dotnet add [ProjectName].Orders.Test reference ../business/[ProjectName].Orders.Business/[ProjectName].Orders.Business.csproj
   dotnet add [ProjectName].Orders.Test reference ../infrastructures/[ProjectName].Orders.Storage/[ProjectName].Orders.Storage.csproj
   cd ..
   dotnet sln ../../[ProjectName].sln add tests/[ProjectName].Orders.Test/[ProjectName].Orders.Test.csproj
   ```

8. **Go back to src and repeat for other domains** (e.g., Shipments, Customers)
   ```bash
   cd ..  # Back to src/
   # Repeat steps 2-7 for each additional domain
   ```

9. **Create Frontend App Domain**
   ```bash
   mkdir app
   cd app
   npx create-vite [projectname].app --template react-ts
   cd ..
   ```

**Result Structure:**
```
src/
├── Orders/
│   ├── domains/
│   ├── business/
│   ├── infrastructures/
│   ├── applications/
│   └── tests/
├── Shipments/
│   ├── domains/
│   ├── business/
│   ├── infrastructures/
│   ├── applications/
│   └── tests/
└── app/
    └── [projectname].app/
```

---

## 🔧 Configuration Tips

1. **Enable Implicit Usings**: Already configured in `.csproj` templates
2. **Enable Nullable Reference Types**: Set `<Nullable>enable</Nullable>`
3. **Use Rystem.DependencyInjection**: For service registration and module organization
4. **Repository Framework**: Use `Rystem.RepositoryFramework` for data access patterns
5. **Test Framework**: Use `Rystem.Test.XUnit` for enhanced testing capabilities

---

## 📚 Next Steps

After project creation:

1. Define entities and interfaces in `Core`
2. Implement business logic in `Business`
3. Configure database context in `Storage`
4. Setup API endpoints in `Api`
5. Build React components in `app`
6. Write tests in `Test` projects

---

## 🔗 Related Resources

- [Repository Pattern Setup](./repository-setup.md)
- [Domain-Driven Design Pattern](./ddd.md)
- [Rystem.DependencyInjection Documentation](https://github.com/KeyserDSoze/Rystem)
