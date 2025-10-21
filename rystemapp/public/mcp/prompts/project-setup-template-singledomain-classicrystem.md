═══════════════════════════════════════════════════════════════════
🚀 RYSTEM STANDARD APPLICATION TEMPLATE - SINGLE DOMAIN NoSQL
═══════════════════════════════════════════════════════════════════

This is a pre-configured template for a standard Rystem application with:
- Single Domain Architecture
- NoSQL Database (Azure Blob Storage via Rystem.RepositoryFramework)
- Social Authentication
- React Frontend with MUI
- Azure Cloud Deployment (Web App + Static Web App)

───────────────────────────────────────────────────────────────────

BACKEND API (.NET 10)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
API Framework: .NET 10

API Features:
✓ Authentication & Authorization
✓ Background Jobs & Scheduled Tasks
  → Use Rystem.BackgroundJob (CRON-based scheduling)
  → MCP resource: background-jobs
  
✓ Concurrency Control (Distributed Locks)
  → Use Rystem.Concurrency (Redis/SQL Server locks)
  → MCP resource: concurrency

✓ File Upload/Download
✓ Email Notifications
✓ Logging & Monitoring
✓ API Versioning
✓ Health Checks

FRONTEND
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Framework: React

UI Library: MUI (Material UI)

Multi-Language Support: Yes (with i18next)
Default Languages: en, it

Frontend Features:
✓ Authentication UI (Login/Register/Forgot Password)
✓ Dashboard with Analytics
✓ Dark Mode / Light Mode
✓ Responsive Design (Mobile/Tablet/Desktop)
✓ Data Export (PDF/Excel/CSV)
✓ Advanced Filtering & Search
✓ File Upload with Preview
✓ Charts & Graphs

ARCHITECTURE (Domain-Driven Design)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Domain Type: Single Domain
  → One unified domain for the entire application
  → Use MCP tool: project-setup (single domain mode)

DATABASE & STORAGE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  IMPORTANT: NoSQL Architecture with Azure Blob Storage

Primary Database: NoSQL (Azure Blob Storage)
  → Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob
  → Document-based storage (JSON serialization)
  → Use MCP tool: repository-setup (Blob Storage configuration)

Data Access Pattern: Repository Pattern
  → Rystem.RepositoryFramework with Azure Blob Storage
  → NoSQL pattern: Each entity stored as JSON blob
  → Partitioning strategy: By entity type
  → Use MCP tool: repository-setup

Key Points for NoSQL with Blob Storage:
  ✓ No SQL migrations needed
  ✓ Schema-less (flexible data models)
  ✓ Automatic JSON serialization
  ✓ Container per entity type
  ✓ Blob name = Entity ID
  ✓ Supports queries via Rystem.RepositoryFramework
  ✓ Supports batch operations
  ✓ Built-in versioning with blob snapshots

CONTENT STORAGE (Files, Images, Documents)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Use Rystem.Content Library (Recommended)
  → MCP resource: content-repo
  
  Storage Provider: Azure Blob Storage
    → Same storage account as NoSQL database
    → Separate container for files/media
    → Supports metadata and custom properties

CLOUD INFRASTRUCTURE (AZURE)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Deployment Strategy:
  ✓ Azure Web App (Backend API)
    → .NET 10 runtime
    → App Service Plan: Basic/Standard
    → Application Insights enabled
    → Always On enabled
    
  ✓ Azure Static Web Apps (Frontend SPA)
    → Automatic deployment from GitHub
    → Global CDN distribution
    → Free SSL certificate
    → Custom domain support
    
  ✓ Azure Blob Storage Account
    → For NoSQL database (Rystem.RepositoryFramework)
    → For content/media files (Rystem.Content)
    → Lifecycle management policies
    → Redundancy: LRS or GRS

Additional Azure Services:
  ✓ Azure Monitor & Application Insights
    → API telemetry and monitoring
    → Performance tracking
    → Error logging
    
  ✓ Azure Key Vault
    → Store connection strings
    → Store API keys and secrets
    → Store OAuth client secrets

NO Docker/Kubernetes (using Azure PaaS services instead)
NO Redis Cache (using in-memory cache for development)

AUTHENTICATION & SECURITY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Social Authentication with Rystem.Authentication.Social (Recommended)
  → Use MCP prompt: auth-flow
  
  Supported Providers:
  ✓ Google
  ✓ Microsoft
  
  Frontend Integration: React (use API endpoints)

Security Features:
✓ JWT Authentication
✓ Role-Based Access Control (RBAC)
✓ Refresh Token Rotation
✓ HTTPS Only
✓ CORS Configuration
✓ Rate Limiting

TESTING & QUALITY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ Unit Tests (xUnit with Rystem.Test.XUnit)
✓ Integration Tests (with in-memory Blob Storage emulator)
✓ API Tests (Rystem.Test)
✓ Frontend Tests (Vitest + React Testing Library)

DEVELOPMENT ENVIRONMENT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Local Development:
  ✓ Azurite (Azure Storage Emulator) for local Blob Storage
  ✓ In-Memory cache for development
  ✓ Local HTTPS certificates
  ✓ Environment-based configuration (appsettings.Development.json)

CI/CD:
  ✓ GitHub Actions
    → Build and test on push
    → Deploy API to Azure Web App
    → Deploy SPA to Azure Static Web Apps
    → Environment variables from GitHub Secrets

RYSTEM PACKAGES USED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Backend:
  ✓ Rystem.DependencyInjection (v9.1.3)
  ✓ Rystem.DependencyInjection.Web (v9.1.3)
  ✓ Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob (v9.1.3)
  ✓ Rystem.Authentication.Social (v9.1.3)
  ✓ Rystem.BackgroundJob (v9.1.3)
  ✓ Rystem.Concurrency (v9.1.3)
  ✓ Rystem.Content.Abstractions (v9.1.3)
  ✓ Rystem.Content.Infrastructure.Storage.Blob (v9.1.3)
  ✓ Rystem.Api.Server (v9.1.3)

Testing:
  ✓ Rystem.Test.XUnit (v9.1.3)

MCP TOOLS & RESOURCES TO USE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
During setup, the AI will use:
  1. project-setup tool → Single domain structure
  2. ddd tool → Domain models and entities
  3. repository-setup tool → Blob Storage NoSQL configuration
  4. auth-flow prompt → Social authentication setup
  5. background-jobs resource → Background job configuration
  6. concurrency resource → Distributed lock setup
  7. content-repo resource → Content storage setup

FOLDER STRUCTURE (Generated)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
src/
├── domains/
│   └── [ProjectName].Core            # Entities, interfaces, DTOs
├── business/
│   └── [ProjectName].Business        # Services, use cases
├── infrastructures/
│   └── [ProjectName].Storage         # Blob Storage repository implementations
├── applications/
│   ├── [ProjectName].Api             # ASP.NET Core Web API
│   └── [projectname].app             # React SPA with MUI
└── tests/
    └── [ProjectName].Test            # Unit & integration tests

DEPLOYMENT CHECKLIST
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Azure Resources to Create:
  ☐ Azure Blob Storage Account
  ☐ Azure Web App (for API)
  ☐ Azure Static Web App (for SPA)
  ☐ Azure Key Vault
  ☐ Application Insights

Configuration Secrets (store in Key Vault):
  ☐ Blob Storage Connection String
  ☐ Google OAuth Client ID & Secret
  ☐ Microsoft OAuth Client ID & Secret
  ☐ JWT Secret Key

GitHub Secrets (for CI/CD):
  ☐ AZURE_WEBAPP_PUBLISH_PROFILE
  ☐ AZURE_STATIC_WEB_APPS_API_TOKEN

ADDITIONAL NOTES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
NoSQL Design Considerations:
  - Design entities for your domain
  - No foreign keys or complex joins
  - Denormalize data where appropriate
  - Use embedding for one-to-many relationships
  - Use separate containers for many-to-many
  - Leverage Rystem.RepositoryFramework query capabilities
  - Consider blob naming strategy for efficient queries

Performance:
  - Use Application Insights for monitoring
  - Implement caching for frequently accessed data
  - Use CDN for static assets
  - Optimize blob storage access patterns




═══════════════════════════════════════════════════════════════════
⚠️  FILL IN THESE REQUIRED FIELDS AT THE END:
═══════════════════════════════════════════════════════════════════

PROJECT INFORMATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Project Name: [YOUR_PROJECT_NAME_HERE]

Description: [YOUR_DESCRIPTION_HERE - Detailed description of what your application does, features needed, who will use it]

⚠️  IMPORTANT - FUD.md (Functional User Documentation):
   
   Option 1: Provide description above
   → The AI will create docs/FUD.md from your description
   
   Option 2: If docs/FUD.md already exists
   → The AI will read it and use it as the source of truth
   → You can leave the description field empty or provide a summary
   
   The AI will:
   1. Check if docs/FUD.md exists
   2. If YES → Read FUD.md and use it for all requirements
   3. If NO → Create docs/FUD.md from your description above
   4. Use FUD.md as the single source of truth for development
   5. Generate entities, services, and UI based on FUD.md

═══════════════════════════════════════════════════════════════════