# Application Setup with Rystem Framework

**Purpose**: This prompt guides you through creating a complete application using Rystem Framework. It will help you set up a modern, scalable application with .NET API backend and React/React Native frontend, following Domain-Driven Design principles.

**What this prompt does**:
- Guides you through all architectural decisions
- Uses Rystem MCP tools to generate the complete project structure
- Sets up a professional, production-ready codebase
- Configures multi-language support automatically
- Follows best practices for DDD, dependency injection, and repository patterns

**How it works**:
1. You provide your choices below
2. The AI uses Rystem MCP tools to scaffold the entire application
3. You get a fully configured project ready for development

---

## 📋 Configuration Choices

Please provide your choices for each section below:

### 1. Project Information

**Project Name**: `_________________________`  
*Example: CargoLens, InventoryHub, OrderFlow*  
*This will be used for all namespaces and project names*

**Application Description**: `_________________________`  
*Describe what your application does in 1-2 sentences*  
*Example: "A cargo tracking system that monitors shipments in real-time across multiple carriers"*

---

### 2. Backend API

**API Framework**: `.NET 10` *(default - always use latest)*

**API Features** *(check all that apply)*:
- [ ] Authentication & Authorization
- [ ] Background Jobs
- [ ] Real-time (SignalR)
- [ ] File Upload/Download
- [ ] External API Integration
- [ ] Email Notifications
- [ ] Caching (Redis)

---

### 3. Frontend

**Framework**: `☐ React` | `☐ React Native`

**UI Library**:
- If React: `MUI` *(default - Material UI)*
- If React Native: `Tamagui` *(default)*
- Custom: `_________________________` *(optional override)*

**Multi-Language**: `Yes` *(default - always enabled with i18next)*

**Default Languages**: `en, it` *(default - add more as needed)*

**Frontend Features** *(check all that apply)*:
- [ ] Authentication UI (Login/Register)
- [ ] Dashboard/Analytics
- [ ] Real-time Updates
- [ ] Offline Support (PWA for React, local storage for React Native)
- [ ] Push Notifications
- [ ] Dark Mode
- [ ] Responsive Design

---

### 4. Architecture

**Domain Architecture**: `☐ Single Domain` | `☐ Multiple Domains`

**If Multiple Domains**, specify domain names:
1. `_________________________` *(e.g., Orders)*
2. `_________________________` *(e.g., Shipments)*
3. `_________________________` *(e.g., Customers)*
4. `_________________________` *(optional)*

**Database**: `☐ SQL Server` | `☐ PostgreSQL` | `☐ SQLite` *(default: SQL Server)*

**Additional Infrastructure** *(check all that apply)*:
- [ ] Azure Blob Storage
- [ ] Azure Service Bus
- [ ] Redis Cache
- [ ] Elasticsearch
- [ ] Docker Support
- [ ] Kubernetes Manifests

---

## 🚀 Setup Process

After you've filled in your choices above, the AI will:

1. **Use `project-setup` tool** to create the domain-driven architecture
   - Generate folder structure (single or multiple domain)
   - Create all .csproj files with correct references
   - Set up .NET 10 projects with Rystem packages

2. **Use `ddd` tool** to set up domain models
   - Create entities, value objects, aggregates
   - Define repository interfaces
   - Set up domain events (if needed)

3. **Use `repository-setup` tool** to configure data access
   - Set up Entity Framework Core
   - Configure DbContext
   - Implement repository pattern or CQRS with Rystem.RepositoryFramework

4. **Configure Frontend**
   - Create React/React Native app with TypeScript
   - Install UI library (MUI/Tamagui)
   - Set up i18next for multi-language support
   - Configure routing and state management
   - Create base layout and navigation

5. **Set up Authentication** *(if selected)*
   - Use `auth-flow` prompt for complete setup with Rystem.Authentication.Social
   - Configure OAuth providers (Google, Facebook, Microsoft, etc.)
   - JWT tokens for API
   - Login/Register UI components
   - Protected routes

6. **Configure Content Storage** *(if selected)*
   - Use `content-repo` resource for Rystem.Content implementation
   - Set up Azure Blob Storage, SharePoint, or File System
   - Unified API for file operations across providers

7. **Configure Additional Features**
   - Background jobs with Rystem.BackgroundJob *(use `background-jobs` resource)*
   - Concurrency control with Rystem.Concurrency *(use `concurrency` resource)*
   - Caching with Rystem.Cache *(if selected)*
   - SignalR hubs *(if selected)*
   - Email notifications *(if selected)*

8. **Create Development Environment**
   - Docker Compose for local development
   - appsettings.json configurations
   - Environment variables setup
   - README with setup instructions

---

## 📚 Example Configuration

Here's a complete example to help you understand:

```
Project Name: CargoLens
Application Description: A cargo tracking system that monitors shipments in real-time across multiple carriers

API Framework: .NET 10
API Features: ✓ Authentication, ✓ Background Jobs, ✓ Real-time, ✓ Caching

Frontend Framework: React
UI Library: MUI
Multi-Language: Yes
Default Languages: en, it, es
Frontend Features: ✓ Authentication UI, ✓ Dashboard, ✓ Real-time Updates, ✓ Dark Mode

Domain Architecture: Multiple Domains
Domains:
  1. Orders
  2. Shipments  
  3. Customers
  4. Tracking

Database: SQL Server
Additional Infrastructure: ✓ Azure Blob Storage, ✓ Redis Cache, ✓ Docker Support
```

---

## 🎯 What You'll Get

After the setup is complete, you'll have:

### Backend Structure
```
src/
├── Orders/                          # (if multiple domains)
│   ├── domains/CargoLens.Orders.Core
│   ├── business/CargoLens.Orders.Business
│   ├── infrastructures/CargoLens.Orders.Storage
│   ├── applications/CargoLens.Orders.Api
│   └── tests/CargoLens.Orders.Test
├── Shipments/                       # (if multiple domains)
│   └── ... (same structure)
└── app/                             # Frontend
    └── cargolens.app/
        ├── src/
        │   ├── domains/             # UI modules per domain
        │   │   ├── orders/
        │   │   ├── shipments/
        │   │   └── customers/
        │   ├── shared/              # Shared components
        │   │   ├── components/
        │   │   ├── hooks/
        │   │   ├── i18n/           # Multi-language
        │   │   └── theme/          # MUI/Tamagui theme
        │   ├── App.tsx
        │   └── main.tsx
        └── package.json
```

### Key Files Configured
- ✅ All .csproj with Rystem packages
- ✅ DbContext with Entity Framework
- ✅ Repository implementations
- ✅ API controllers with Swagger
- ✅ React/React Native app with routing
- ✅ i18next configuration with language files
- ✅ MUI/Tamagui theme setup
- ✅ Docker Compose for development
- ✅ README with setup instructions

---

## 🔧 Technologies Used

### Backend
- **.NET 10** - Latest .NET framework
- **Rystem Framework** - DI, Repository, Background Jobs, etc.
- **Entity Framework Core** - ORM
- **ASP.NET Core Web API** - REST API
- **Swagger/OpenAPI** - API documentation

### Frontend (React)
- **React 18+** with TypeScript
- **Vite** - Build tool
- **MUI (Material-UI)** - UI components
- **React Router** - Navigation
- **i18next** - Internationalization
- **React Query** - Data fetching
- **Zustand/Redux** - State management

### Frontend (React Native)
- **React Native** with TypeScript
- **Tamagui** - UI components
- **React Navigation** - Navigation
- **i18next** - Internationalization
- **React Query** - Data fetching
- **Zustand** - State management

---

## 📖 Next Steps After Setup

1. **Review Generated Code**: Check all generated files
2. **Configure Database**: Update connection strings in appsettings.json
3. **Run Migrations**: `dotnet ef migrations add Initial` and `dotnet ef database update`
4. **Install Frontend Dependencies**: `cd app/[projectname].app && npm install`
5. **Start Development**: Run API and frontend concurrently
6. **Customize**: Adjust generated code to your specific needs

---

## 🔗 Related MCP Tools

This prompt uses these Rystem MCP tools automatically:

- **project-setup** - Creates domain architecture
- **ddd** - Sets up domain models
- **repository-setup** - Configures data access
- **auth-flow** - Adds authentication (if selected)
- **service-setup** - Configures dependency injection

All tools are documented at: https://rystem.cloud/mcp

---

## ⚠️ Before You Start

Make sure you have:
- [ ] .NET 10 SDK installed
- [ ] Node.js 20+ installed
- [ ] VS Code or Visual Studio
- [ ] Git installed
- [ ] Docker Desktop (if using Docker)

---

**Ready to start?** Fill in your choices above and let's build your application! 🚀

---
---

# 📘 Technical Reference Guide

The sections below provide detailed technical information about the architecture, naming conventions, and step-by-step commands. This is reference material that the AI will use during setup.

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

---
---

# 📋 Ready-to-Use Configuration Template

**Copy the section below, delete/modify options you don't need, and paste it to start your setup:**

```
═══════════════════════════════════════════════════════════════════
🚀 NEW APPLICATION SETUP WITH RYSTEM FRAMEWORK
═══════════════════════════════════════════════════════════════════

PROJECT INFORMATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Project Name: [Your project name here - e.g., CargoLens]
Application Description: [What does your app do? - e.g., A cargo tracking system that monitors shipments in real-time]

BACKEND API (.NET 10)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
API Framework: .NET 10

API Features (mark with ✓ or delete unwanted):
☐ Authentication & Authorization

☐ Background Jobs & Scheduled Tasks
  ☐ Use Rystem.BackgroundJob (Recommended - CRON-based scheduling)
    → Use MCP resource: background-jobs for implementation
  ☐ Hangfire
  ☐ Quartz.NET
  ☐ Custom implementation

☐ Concurrency Control (Distributed Locks)
  ☐ Use Rystem.Concurrency (Recommended - Redis/SQL Server locks)
    → Use MCP resource: concurrency for implementation
  ☐ Custom implementation

☐ Real-time Communication (SignalR)
☐ File Upload/Download
☐ External API Integration
☐ Email Notifications
☐ Caching (Redis)
☐ Logging & Monitoring
☐ API Rate Limiting
☐ WebHooks
☐ API Versioning
☐ Health Checks

FRONTEND
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Framework: ☐ React | ☐ React Native

UI Library:
  If React → MUI (Material UI)
  If React Native → Tamagui
  Custom Override: [leave empty to use defaults]

Multi-Language Support: Yes (with i18next)
Default Languages: en, it [add more: es, fr, de, etc.]

Frontend Features (mark with ✓ or delete unwanted):
☐ Authentication UI (Login/Register/Forgot Password)
☐ Dashboard with Analytics
☐ Real-time Updates (WebSocket/SignalR)
☐ Offline Support (PWA for React / Local storage for React Native)
☐ Push Notifications
☐ Dark Mode / Light Mode
☐ Responsive Design (Mobile/Tablet/Desktop)
☐ Data Export (PDF/Excel/CSV)
☐ Advanced Filtering & Search
☐ Drag & Drop Interface
☐ File Upload with Preview
☐ Charts & Graphs

ARCHITECTURE (Domain-Driven Design)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Domain Type: ☐ Single Domain | ☐ Multiple Domains

If Single Domain:
  - One unified domain for the entire application

If Multiple Domains (list your domain names):
  Domain 1: [e.g., Orders - Order management and processing]
  Domain 2: [e.g., Shipments - Shipment tracking and logistics]
  Domain 3: [e.g., Customers - Customer profiles and management]
  Domain 4: [e.g., Inventory - Stock and warehouse management]
  Domain 5: [add more if needed]

DATABASE & STORAGE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Primary Database: ☐ SQL Server | ☐ PostgreSQL | ☐ SQLite | ☐ MySQL

Data Access Pattern:
☐ Repository Pattern (Rystem.RepositoryFramework)
  → Use MCP tool: repository-setup
☐ CQRS (Command Query Responsibility Segregation with Rystem.RepositoryFramework)
  → Use MCP tool: repository-setup with CQRS configuration
☐ Custom / Entity Framework Core only

CONTENT STORAGE (Files, Images, Documents)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
☐ Use Rystem.Content Library (Recommended - unified API for multiple storage providers)
  → Use MCP resource: content-repo for implementation guide
  
  Storage Providers (select one or more):
  ☐ Azure Blob Storage
  ☐ Azure Storage Files
  ☐ Microsoft 365 SharePoint
  ☐ Local File System
  ☐ In-Memory (for testing)

☐ Custom Implementation (direct SDK usage)
  ☐ Azure Blob Storage
  ☐ AWS S3
  ☐ Local File System

ADDITIONAL INFRASTRUCTURE (mark with ✓ or delete unwanted)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Message Queues & Events:
☐ Azure Service Bus
☐ RabbitMQ
☐ Redis Pub/Sub

Caching:
☐ Redis Cache
☐ In-Memory Cache
☐ Distributed Cache

Search:
☐ Elasticsearch
☐ Azure Cognitive Search

DevOps & Containers:
☐ Docker Support (Dockerfile + docker-compose.yml)
☐ Kubernetes Manifests
☐ GitHub Actions CI/CD
☐ Azure DevOps Pipelines

Monitoring & Logging:
☐ Application Insights
☐ Serilog
☐ ELK Stack (Elasticsearch, Logstash, Kibana)

AUTHENTICATION & SECURITY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Authentication Type:
☐ JWT Authentication (Manual implementation)
☐ OAuth 2.0 / OpenID Connect
☐ Azure AD / Entra ID

☐ Social Authentication with Rystem.Authentication.Social (Recommended)
  → Use MCP prompt: auth-flow for complete setup
  
  Supported Providers (mark what you need):
  ☐ Google
  ☐ Facebook  
  ☐ Microsoft
  ☐ GitHub
  ☐ Twitter
  ☐ LinkedIn
  ☐ Apple
  
  Frontend Integration:
  ☐ Blazor Server (Rystem.Authentication.Social.Blazor)
  ☐ Blazor WebAssembly (Rystem.Authentication.Social.Blazor)
  ☐ React/React Native (use API endpoints)

Security Features:
☐ Two-Factor Authentication (2FA)
☐ Role-Based Access Control (RBAC)
☐ Permission-Based Authorization
☐ API Key Authentication
☐ Refresh Token Rotation
☐ Account Lockout Policy

TESTING & QUALITY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
☐ Unit Tests (xUnit with Rystem.Test.XUnit)
☐ Integration Tests
☐ API Tests (Rystem.Test)
☐ Frontend Tests (Jest/Vitest + React Testing Library)
☐ End-to-End Tests (Playwright/Cypress)

ADDITIONAL NOTES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[Any specific requirements, integrations, or custom features you need]




═══════════════════════════════════════════════════════════════════
✅ READY! Paste your completed configuration above to start setup
═══════════════════════════════════════════════════════════════════
```

---

## 💡 Quick Start Examples

### Example 1: Simple Single Domain App
```
Project Name: TaskManager
Description: A task management application with real-time collaboration

Backend: .NET 10
API Features: ✓ Authentication (Rystem.Authentication.Social), ✓ Real-time (SignalR)

Frontend: React
UI Library: MUI
Multi-Language: Yes (en, it)
Frontend Features: ✓ Authentication UI, ✓ Dashboard, ✓ Real-time Updates, ✓ Dark Mode

Architecture: Single Domain
Database: PostgreSQL
Data Access: ✓ Repository Pattern (Rystem.RepositoryFramework)
Infrastructure: ✓ Redis Cache, ✓ Docker Support

Authentication: ✓ Social Login (Google, Microsoft) via Rystem.Authentication.Social
```

### Example 2: Complex Multiple Domain E-Commerce
```
Project Name: ShopHub
Description: Enterprise e-commerce platform with inventory and order management

Backend: .NET 10
API Features: ✓ Authentication, ✓ Background Jobs (Rystem.BackgroundJob), 
              ✓ Email Notifications, ✓ File Upload, ✓ Concurrency Control (Rystem.Concurrency)

Frontend: React
UI Library: MUI
Multi-Language: Yes (en, it, es, fr, de)
Frontend Features: ✓ Authentication UI, ✓ Dashboard, ✓ Offline Support, ✓ Dark Mode, ✓ Charts & Graphs

Architecture: Multiple Domains
Domains:
  1. Products - Product catalog and management
  2. Orders - Order processing and fulfillment
  3. Customers - Customer profiles and preferences
  4. Inventory - Stock and warehouse management
  5. Payments - Payment processing and invoicing

Database: SQL Server
Data Access: ✓ CQRS Pattern (Rystem.RepositoryFramework)
Content Storage: ✓ Rystem.Content (Azure Blob Storage + SharePoint)
Infrastructure: ✓ Azure Service Bus, ✓ Redis Cache, ✓ Elasticsearch, 
                ✓ Docker Support, ✓ Kubernetes, ✓ GitHub Actions

Authentication: ✓ Social Login (Google, Facebook, Microsoft) via Rystem.Authentication.Social
Background Jobs: ✓ Order processing, Inventory sync, Email notifications via Rystem.BackgroundJob
Concurrency: ✓ Distributed locks for inventory updates via Rystem.Concurrency
```

### Example 3: Mobile App with React Native
```
Project Name: FitTracker
Description: Mobile fitness tracking app with social features

Backend: .NET 10
API Features: ✓ Authentication (Rystem.Authentication.Social), 
              ✓ File Upload (Rystem.Content), ✓ Background Jobs

Frontend: React Native
UI Library: Tamagui
Multi-Language: Yes (en, it, es)
Frontend Features: ✓ Authentication UI, ✓ Offline Support, ✓ Push Notifications, ✓ Dark Mode

Architecture: Single Domain
Database: PostgreSQL
Data Access: ✓ Repository Pattern (Rystem.RepositoryFramework)
Content Storage: ✓ Rystem.Content (Azure Blob Storage for photos/videos)
Infrastructure: ✓ Redis Cache, ✓ Docker Support

Authentication: ✓ Social Login (Google, Apple, Facebook) via Rystem.Authentication.Social
Background Jobs: ✓ Daily workout reminders, Weekly reports via Rystem.BackgroundJob
```
Multi-Language: Yes (en, it, es)
Frontend Features: ✓ Authentication UI, ✓ Offline Support, ✓ Push Notifications, ✓ Dark Mode

Architecture: Single Domain
Database: PostgreSQL
Infrastructure: ✓ Azure Blob Storage, ✓ Redis Cache, ✓ Docker Support
```

---

**📌 Remember**: After filling the template, paste it back and the AI will use Rystem MCP tools to generate your complete application! 🚀
