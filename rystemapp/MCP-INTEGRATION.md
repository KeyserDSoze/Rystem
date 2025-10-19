# Rystem MCP Integration Guide

## 🎯 Overview

Rystem now provides a **static Model Context Protocol (MCP) server** deployed on GitHub Pages. This enables AI tools like GitHub Copilot, Claude, Cursor, and others to access Rystem Framework documentation, tools, and best practices directly.

## 🌐 Live Endpoints

All endpoints are available at `https://rystem.net/`:

| Endpoint | Description |
|----------|-------------|
| [`/mcp-server.json`](https://rystem.net/mcp-server.json) | Server initialization and capabilities |
| [`/mcp-tools-list.json`](https://rystem.net/mcp-tools-list.json) | List of available tools |
| [`/mcp-resources-list.json`](https://rystem.net/mcp-resources-list.json) | List of available resources |
| [`/mcp-prompts-list.json`](https://rystem.net/mcp-prompts-list.json) | List of available prompts |
| [`/mcp/tools/{name}.md`](https://rystem.net/mcp/tools/) | Individual tool content |
| [`/mcp/resources/{name}.md`](https://rystem.net/mcp/resources/) | Individual resource content |
| [`/mcp/prompts/{name}.md`](https://rystem.net/mcp/prompts/) | Individual prompt content |
| [`/.well-known/mcp.json`](https://rystem.net/.well-known/mcp.json) | MCP autodiscovery endpoint |

## 🔧 Setup Instructions

### GitHub Copilot (VS Code)

Add to your workspace `.vscode/settings.json` or global settings:

```json
{
  "github.copilot.chat.codeGeneration.instructions": [
    {
      "text": "Use Rystem Framework patterns and best practices from https://rystem.net/mcp-manifest.json"
    }
  ]
}
```

Or create `.github/copilot-instructions.md` in your repository:

```markdown
# Copilot Instructions

Use Rystem Framework patterns and best practices from https://rystem.net/mcp-manifest.json

When creating new projects, follow the architecture described in the project-setup tool.
When implementing repositories, use the repository-setup tool guidance.
For DDD patterns, reference the ddd tool.
```

### Claude Desktop

Add to `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or `%APPDATA%\Claude\claude_desktop_config.json` (Windows):

```json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.net/mcp-server.json",
      "type": "static",
      "description": "Rystem Framework documentation and tools"
    }
  }
}
```

### Cursor

Click this deeplink to install: [cursor://install-mcp?url=https://rystem.net/.well-known/mcp.json](cursor://install-mcp?url=https://rystem.net/.well-known/mcp.json)

Or manually add to Cursor settings:

```json
{
  "mcpServers": {
    "rystem": {
      "type": "http",
      "url": "https://rystem.net/mcp-server.json"
    }
  }
}
```

### Any MCP-Compatible Client

Use the standard MCP discovery endpoint:

```
https://rystem.net/.well-known/mcp.json
```

## 📚 Available Tools

### 🛠️ Tools (4)

1. **Domain-Driven Design (DDD) Pattern** (`ddd`)
   - Setup DDD pattern with entities, aggregates, value objects, and repositories using Rystem Framework

2. **Install Rystem Package** (`install-rystem`)
   - Guide to install and configure Rystem packages

3. **New Project Setup with Domain Architecture** (`project-setup`)
   - Complete guide to setup a new project with modular domain architecture, supporting both single and multiple domain structures

4. **Repository Pattern Setup** (`repository-setup`)
   - Setup repository pattern with Rystem.RepositoryFramework

### 📚 Resources (3)

1. **Background Jobs** (`background-jobs`)
   - Implementation patterns for background job processing

2. **Concurrency Control** (`concurrency`)
   - Concurrency management strategies

3. **Content Repository** (`content-repo`)
   - Content management system patterns

### 💬 Prompts (2)

1. **Authentication Flow Setup** (`auth-flow`)
   - Setup authentication and authorization flows

2. **Service Setup with Dependency Injection** (`service-setup`)
   - Configure services with dependency injection

## 🏗️ Architecture

### How It Works

1. **Static Generation**: All MCP responses are pre-generated as JSON files during build
2. **GitHub Pages**: Deployed as static files on GitHub Pages (no server required)
3. **No Runtime**: Everything is static - extremely fast and reliable
4. **Version Control**: All content is versioned with the main repository

### Build Process

```bash
# Generate documentation tree from .csproj/package.json metadata
npm run build-docs

# Generate MCP manifest and copy markdown files
npm run build-mcp

# Generate MCP server static responses
npm run build-mcp-server

# Or run all at once
npm run prebuild
```

### Generated Files

```
public/
├── mcp-server.json              # MCP Initialize response
├── mcp-tools-list.json          # Tools list
├── mcp-resources-list.json      # Resources list
├── mcp-prompts-list.json        # Prompts list
├── mcp-manifest.json            # UI manifest (existing)
├── mcp-server.html              # Human-readable landing page
├── MCP-SERVER.md                # Documentation
├── .well-known/
│   └── mcp.json                 # Autodiscovery endpoint
└── mcp/
    ├── tools/
    │   ├── ddd.md
    │   ├── install-rystem.md
    │   ├── project-setup.md
    │   └── repository-setup.md
    ├── resources/
    │   ├── background-jobs.md
    │   ├── concurrency.md
    │   └── content-repo.md
    └── prompts/
        ├── auth-flow.md
        └── service-setup.md
```

## 🔄 Adding New Content

### 1. Add Markdown File with Frontmatter

Create a new file in `src/mcp/tools/`, `src/mcp/resources/`, or `src/mcp/prompts/`:

```markdown
---
title: "My New Tool"
description: "Description of what this tool does"
category: "setup"
tags: ["tool", "setup", "guide"]
---

# My New Tool

Content here...
```

### 2. Rebuild

```bash
npm run build-mcp
npm run build-mcp-server
```

### 3. Deploy

Commit and push to GitHub - GitHub Actions will deploy automatically.

## 🧪 Testing

### Local Testing

```bash
# Start dev server
npm run dev

# Visit endpoints
open http://localhost:5174/mcp-server.html
open http://localhost:5174/mcp-server.json
open http://localhost:5174/mcp-tools-list.json
```

### Production Testing

After deployment, test at:
- https://rystem.net/mcp-server.html
- https://rystem.net/mcp-server.json
- https://rystem.net/.well-known/mcp.json

## 📈 Benefits

✅ **Zero Infrastructure**: No server required, hosted on GitHub Pages  
✅ **High Performance**: Static files served via CDN  
✅ **Always Available**: 99.9% uptime guaranteed by GitHub  
✅ **Version Controlled**: All content tracked in Git  
✅ **Easy Updates**: Just commit and push  
✅ **AI-Native**: Designed specifically for AI tool integration  
✅ **Standards-Based**: Follows MCP specification  

## 🔗 Related Links

- [MCP Specification](https://modelcontextprotocol.io)
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Claude API Documentation](https://docs.anthropic.com/claude/docs)
- [Rystem Framework GitHub](https://github.com/KeyserDSoze/Rystem)

## 📝 License

MIT License - Same as Rystem Framework

---

**Questions?** Open an issue on [GitHub](https://github.com/KeyserDSoze/Rystem/issues)
