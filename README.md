## Rystem

Rystem is a open-source framework to improve .NET with powerful utilities, patterns, and integrations.

## 📚 Documentation

- **Complete Documentation**: [https://rystem.net](https://rystem.net)
- **MCP Server** (Model Context Protocol for AI): [https://rystem.cloud/mcp](https://rystem.cloud/mcp)

### 🤖 AI-Powered Development with MCP

Rystem provides a **Model Context Protocol (MCP) server** that enables AI assistants (like GitHub Copilot, Claude, etc.) to access comprehensive documentation, best practices, and code examples directly during development.

**Access the MCP server at**: `https://rystem.cloud/mcp`

Features:
- 📖 **32+ Documentation Topics**: DDD patterns, Repository framework, Authentication, Content management, and more
- 🔍 **Smart Search**: Find documentation by keyword across all topics
- 💡 **Code Examples**: Ready-to-use code snippets and templates
- 🎯 **Best Practices**: Alessandro Rapiti's proven patterns and conventions

### Help the project

Reach out us on [Discord](https://discord.gg/tkWvy4WPjt)

### Contribute

Support the project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Get Started

### 🎯 Core Libraries

#### Rystem (core library)
Essential utilities for .NET: JSON extensions, LINQ serialization, reflection helpers, text utilities, stopwatch, discriminated unions, and more.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Core/Rystem)

#### Rystem.DependencyInjection (core library)
Advanced dependency injection with Factory pattern, named services, decorators, and automatic assembly scanning.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Core/Rystem.DependencyInjection)

#### Rystem.DependencyInjection.Web (core library)
Web-specific DI extensions for ASP.NET Core applications.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Core/Rystem.DependencyInjection.Web)

---

### 🔐 Authentication

#### Rystem.Authentication.Social
Social login integration for Google, Microsoft, Facebook, GitHub, LinkedIn, X (Twitter), Instagram, and Pinterest with automatic token management.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Authentication/Rystem.Authentication.Social)

#### Rystem.Authentication.Social.Blazor
Social authentication UI components for Blazor Server and Blazor WebAssembly with automatic routing protection.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Authentication/Rystem.Authentication.Social.Blazor)

#### Rystem.Authentication.Social.React
React hooks and components for social authentication in TypeScript/JavaScript applications.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Authentication/rystem.authentication.social.react)

---

### 🚀 API Framework

#### Rystem.Api.Server
Auto-generate REST APIs from repositories without writing controllers - includes authentication, LINQ queries, and Swagger documentation.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Api/Rystem.Api.Server)

#### Rystem.Api.Client
Consume REST APIs with automatic retry logic, circuit breaker, and authentication handling.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Api/Rystem.Api.Client)

---

### ⚙️ Background Processing

#### Concurrency (Async Lock and Race Condition)
Prevent race conditions with key-based async locking - supports in-memory and Redis distributed locks.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/Concurrency/Rystem.Concurrency)

#### Background Job
Schedule recurring tasks with CRON expressions - run automated jobs, data sync, cleanup tasks, and notifications.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/BackgroundJob/Rystem.BackgroundJob)

#### Queue
Buffer operations in-memory and process in batches - collect items until buffer size or time limit is reached.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Extensions/Queue/Rystem.Queue)

---

### 📦 Data Access

#### Repository Framework (Repository pattern and CQRS)
Complete repository pattern implementation with CQRS support, multiple storage backends, business logic injection, and Factory pattern.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Repository)

#### Content Repository (Upload/Download files)
Unified interface for file storage across Azure Blob Storage, Azure File Storage, SharePoint Online, and in-memory storage with metadata and tags support.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Content)

---

### 🌍 Localization

Simplified localization for .NET applications with resource file management and multi-language support.

📖 [Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Localization)

---

## 🏗️ Architecture Patterns

Rystem follows **Domain-Driven Design (DDD)** principles and supports:

- ✅ **Single Domain**: Flat structure for small applications
- ✅ **Multi-Domain**: Isolated bounded contexts for enterprise applications
- ✅ **CQRS**: Command/Query separation with repository framework
- ✅ **Event-Driven**: Domain events and messaging patterns
- ✅ **Microservices**: Complete domain isolation with API communication

📖 Learn more at [https://rystem.net](https://rystem.net)

---

## 📄 License

MIT License - see [LICENSE](LICENSE.txt) for details

---

## 🙏 Credits

Created and maintained by **Alessandro Rapiti** (KeyserDSoze)

Special thanks to all [contributors](https://github.com/KeyserDSoze/Rystem/graphs/contributors)