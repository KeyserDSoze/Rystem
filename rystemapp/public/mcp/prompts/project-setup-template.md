═══════════════════════════════════════════════════════════════════
🚀 NEW APPLICATION SETUP
═══════════════════════════════════════════════════════════════════

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
⚠️  FILL IN THESE REQUIRED FIELDS AT THE END:
═══════════════════════════════════════════════════════════════════

PROJECT INFORMATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Project Name: [Your project name here]

Application Description: [Detailed description of what your app does, features needed, who will use it]

⚠️  IMPORTANT - FUD.md (Functional User Documentation):
   
   Option 1: Provide description above
   → The AI will create docs/FUD.md from your description
   → For Multiple Domains: AI will create docs/FUD-{DomainName}.md for each domain
   
   Option 2: If docs/FUD.md already exists
   → The AI will read it and use it as the source of truth
   → You can leave the description field empty or provide a summary
   
   The AI will:
   1. Check if docs/FUD.md (and docs/FUD-{DomainName}.md) exists
   2. If YES → Read FUD.md files and use them for all requirements
   3. If NO → Create FUD.md files from your description above
   4. Use FUD.md as the single source of truth for development
   5. Generate entities, services, UI, and tests based on FUD.md

═══════════════════════════════════════════════════════════════════

