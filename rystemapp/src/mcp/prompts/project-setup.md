---
title: Application Setup with Rystem
description: Interactive prompt to create complete applications with .NET API backend and React/Next.js frontend following Domain-Driven Design and FUD.md approach
---

# Application Setup with Rystem Framework

**Purpose**: This prompt guides you through creating a complete application using Rystem Framework. It will help you set up a modern, scalable application with .NET API backend and React/React Native frontend, following Domain-Driven Design principles.

---

**What this prompt does**:
- Guides you through all architectural decisions
- Uses Rystem MCP tools to generate the complete project structure
- Sets up a professional, production-ready codebase
- Configures multi-language support automatically
- Follows best practices for DDD, dependency injection, and repository patterns
- **Creates FUD (Functional User Documentation)** as the foundation for development

**How it works**:
1. You provide your choices below
2. The AI creates/updates FUD.md documentation based on your description
3. The AI uses FUD.md as the source of truth for what to build
4. The AI uses Rystem MCP tools to scaffold the entire application
5. You get a fully configured project ready for development

---

## � FUD.md - Functional User Documentation (CRITICAL)

**⚠️ IMPORTANT: Everything starts from FUD.md**

The **FUD (Functional User Documentation)** is the foundation of the entire project. It describes what the application must do from a functional perspective.

### Location and Structure

**Single Domain Projects:**
```
docs/
└── FUD.md                    # Main functional documentation
```

**Multiple Domain Projects:**
```
docs/
├── FUD.md                    # Overall application description
├── FUD-Orders.md             # Orders domain functionality
├── FUD-Shipments.md          # Shipments domain functionality
├── FUD-Customers.md          # Customers domain functionality
└── FUD-[DomainName].md       # One FUD per domain
```

### What FUD.md Contains

The FUD document includes:
- **Application Overview**: What the app does, who uses it
- **User Roles**: Different types of users and their permissions
- **Core Features**: List of all features grouped by area
- **User Stories**: Detailed user stories (As a [role], I want to [action], so that [benefit])
- **Business Rules**: Validation rules, constraints, policies
- **Workflows**: Step-by-step process flows
- **Data Requirements**: What data needs to be stored
- **Integrations**: External systems or APIs needed
- **Non-Functional Requirements**: Performance, security, scalability needs

### AI Workflow with FUD.md

**STEP 1: Create or Update FUD.md**
- If `docs/FUD.md` doesn't exist → AI creates it from your description
- If `docs/FUD.md` exists → AI updates it with new requirements
- For multiple domains → AI creates FUD.md + FUD-{DomainName}.md for each domain

**STEP 2: Use FUD.md as Source of Truth**
- AI reads FUD.md to understand what to build
- All entities, services, endpoints are derived from FUD.md
- All business logic reflects the rules in FUD.md
- All tests verify the requirements in FUD.md

**STEP 3: Keep FUD.md Updated**
- When requirements change → Update FUD.md first
- Then regenerate/update code based on new FUD.md
- FUD.md is the single source of truth

### Example FUD.md Structure

```markdown
# FUD - [Project Name]

## Overview
[High-level description of what the application does]

## User Roles
- **Admin**: Full system access, user management
- **Manager**: View reports, approve actions
- **User**: Basic features, CRUD operations

## Core Features

### Feature 1: [Feature Name]
**Description**: [What this feature does]

**User Stories**:
- As a [role], I want to [action], so that [benefit]
- As a [role], I want to [action], so that [benefit]

**Business Rules**:
- [Rule 1]
- [Rule 2]

### Feature 2: [Feature Name]
...

## Data Model
[Description of main entities and their relationships]

## Integrations
[External systems, APIs, services]

## Non-Functional Requirements
- Performance: [requirements]
- Security: [requirements]
- Scalability: [requirements]
```

---

## �📋 Configuration Choices

Please provide your choices for each section below:

### 1. Project Information

**Project Name**: `_________________________`  
*Example: CargoLens, InventoryHub, OrderFlow*  
*This will be used for all namespaces and project names*

**Application Description**: `_________________________`  
*Describe what your application does, what features it needs, who will use it*  
*Be as detailed as possible - this will be used to create/update docs/FUD.md*  
*Example: "A cargo tracking system that monitors shipments in real-time across multiple carriers. Users can create shipments, track their status, receive notifications, and generate reports. Admins can manage users and configure system settings."*  

**→ The AI will automatically create or update docs/FUD.md (and docs/FUD-{DomainName}.md for multiple domains) based on your description**

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

0. **🎯 FIRST: Create/Update FUD.md Documentation**
   - Check if `docs/FUD.md` exists
   - If NOT exists → Create `docs/FUD.md` from your Application Description
   - If EXISTS → Read and validate against your requirements
   - For Multiple Domains → Create `docs/FUD-{DomainName}.md` for each domain
   - FUD.md becomes the **single source of truth** for what to build
   - All subsequent steps are based on FUD.md requirements

1. **Use `project-setup` tool** to create the domain-driven architecture
   - Generate folder structure (single or multiple domain)
   - Create all .csproj files with correct references
   - Set up .NET 10 projects with Rystem packages
   - Create `docs/` folder structure

2. **Use DDD tools** to understand and apply Domain-Driven Design principles:
   - **`ddd-single-domain`**: For small applications with unified domain (1-10 entities, cohesive business)
     - Explains flat structure: `domains/`, `business/`, `infrastructures/`, `applications/`
     - Shows how to implement entities, value objects, repositories
     - Provides examples of pure domain logic, DTOs, services
     - Perfect for simple use cases: Task Manager, Blog, Invoice System
   
   - **`ddd-multi-domain`**: For enterprise applications with multiple bounded contexts
     - Explains domain isolation: `src/Orders/`, `src/Shipments/`, `src/Customers/`
     - Shows domain communication patterns (REST APIs, events)
     - Provides examples of bounded contexts, aggregates, domain events
     - Perfect for complex systems: E-Commerce, ERP, Logistics, Healthcare

3. **Set up domain models** (based on FUD.md and chosen DDD approach)
   - Create entities, value objects, aggregates from FUD.md data requirements
   - Define repository interfaces from FUD.md features
   - Set up domain events from FUD.md workflows
   - Implement business rules from FUD.md
   - Follow patterns from `ddd-single-domain` or `ddd-multi-domain`

3. **Use `repository-setup` tool** to configure data access
   - Set up Entity Framework Core or Blob Storage (based on database choice)
   - Configure DbContext with entities from FUD.md
   - Implement repository pattern or CQRS with Rystem.RepositoryFramework

4. **Configure Frontend**
   - Create React/React Native app with TypeScript
   - Install UI library (MUI/Tamagui)
   - Set up i18next for multi-language support
   - Configure routing and state management
   - Create base layout and navigation
   - Build screens/components based on FUD.md user stories

5. **Set up Authentication** *(if selected)*
   - Use `auth-flow` prompt for complete setup with Rystem.Authentication.Social
   - Configure OAuth providers (Google, Facebook, Microsoft, etc.)
   - Implement user roles from FUD.md
   - JWT tokens for API
   - Login/Register UI components
   - Protected routes based on FUD.md permissions

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

9. **Verify Against FUD.md**
   - Cross-check all features from FUD.md are implemented
   - Ensure all user stories have corresponding code
   - Validate business rules are enforced
   - Confirm all data requirements are covered

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

- **ddd-single-domain** - Domain-Driven Design for small applications (unified domain)
- **ddd-multi-domain** - Domain-Driven Design for enterprise applications (multiple bounded contexts)
- **install-rystem** - Installs Rystem packages with correct versions
- **repository-setup** - Configures data access with Rystem.RepositoryFramework
- **repository-api-server** - Auto-generates REST APIs from repositories (no controllers needed)
- **repository-api-client-typescript** - Consumes REST APIs from TypeScript/JavaScript apps
- **repository-api-client-dotnet** - Consumes REST APIs from .NET/C# apps (Blazor, MAUI, WPF)
- **auth-flow** - Adds authentication with Rystem.Authentication.Social (if selected)
- **background-jobs** - Configures Rystem.BackgroundJob (if selected)
- **concurrency** - Sets up Rystem.Concurrency (if selected)
- **content-repo** - Implements Rystem.Content for file storage (if selected)

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
- [Repository API Server](./repository-api-server.md)
- [Repository API Client (TypeScript)](./repository-api-client-typescript.md)
- [Repository API Client (.NET)](./repository-api-client-dotnet.md)
- [DDD Single Domain](./ddd-single-domain.md)
- [DDD Multi-Domain](./ddd-multi-domain.md)
- [Discriminated Union (AnyOf)](./rystem-discriminated-union.md)
- [Stopwatch Utilities](./rystem-stopwatch.md)
- [LINQ Expression Serializer](./rystem-linq-serializer.md)
- [Reflection Utilities](./rystem-reflection.md)
- [Text Extensions](./rystem-text-extensions.md)
- [CSV & Minimization](./rystem-csv.md)
- [JSON Extensions](./rystem-json-extensions.md)
- [Task Extensions](./rystem-task-extensions.md)
- [ConcurrentList](./rystem-concurrent-list.md)
- [DI Factory Pattern](./rystem-dependencyinjection-factory.md)
- [Background Jobs](../resources/background-jobs.md)
- [Concurrency Control](../resources/concurrency.md)
- [Content Repository](../resources/content-repo.md)

---


NOW WAIT FOR TEMPLATE, DO NOTHING BEFORE THE TEMPLATE THAT EXPLAINS THE PROJECT