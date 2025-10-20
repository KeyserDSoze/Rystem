# Rystem MCP Server

Model Context Protocol (MCP) integration for Rystem Framework documentation and tools.

## 🌐 Domain Architecture

Rystem uses two domains for different purposes:

- **📖 Documentation Site**: `https://rystem.net` (GitHub Pages)
  - Static documentation, guides, and API references
  - Static MCP JSON files (legacy support)
  
- **⚡ MCP Server**: `https://rystem.cloud/mcp` (Vercel)
  - Live Model Context Protocol server
  - Dynamic tools, resources, and prompts
  - JSON-RPC 2.0 over HTTP

## � Quick Start - MCP Server

**Use this endpoint for all AI tools:**

```
https://rystem.cloud/mcp
```

### Connect to Your AI Tool

**Claude Desktop** - Add to `claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.cloud/mcp",
      "transport": {
        "type": "streamable-http"
      }
    }
  }
}
```

**Cursor** - Click this deeplink:
```
cursor://anysphere.cursor-deeplink/mcp/install?name=rystem&config=eyJ1cmwiOiJodHRwczovL3J5c3RlbS5jbG91ZC9tY3AifQ==
```

**VS Code** - Run this command:
```bash
code --add-mcp '{"name":"rystem","type":"http","url":"https://rystem.cloud/mcp"}'
```

**GitHub Copilot** - The MCP server provides context automatically when configured

**MCP Inspector** - Test the server interactively:
```bash
npx @modelcontextprotocol/inspector https://rystem.cloud/mcp
```

## 🔧 Technical Details

- **Protocol**: JSON-RPC 2.0
- **Transport**: Streamable HTTP
- **Version**: 0.0.1 (MCP Protocol 2024-11-05)
- **Runtime**: Node.js 20.x on Vercel Edge Functions
- **Region**: iad1 (US East - Washington D.C.)

## � Available Content

### Tools (2)
- **Install Rystem Package**: Description
- **Repository Pattern Setup**: Description

### Resources (3)
- **Background Jobs**: Overview
- **Concurrency Control**: Overview
- **Content Repository**: Overview

### Prompts (3)
- **Authentication Flow Setup**: Context
- **Application Setup with Rystem Framework**: 📋 Configuration Choices
- **Service Setup with Dependency Injection**: Context

## 📁 Static JSON Files (Legacy)

For clients that don't support full MCP protocol, static JSON files are available at `https://rystem.net/`:

- **Server Info**: `mcp-server.json`
- **Tools List**: `mcp-tools-list.json`
- **Resources List**: `mcp-resources-list.json`
- **Prompts List**: `mcp-prompts-list.json`

### Content Files

Individual content files:
- Tools: `https://rystem.net/mcp/tools/{name}.md`
- Resources: `https://rystem.net/mcp/resources/{name}.md`
- Prompts: `https://rystem.net/mcp/prompts/{name}.md`

## 🧪 Testing

### Test with cURL

```bash
# Initialize connection
curl -X POST https://rystem.cloud/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2024-11-05",
      "capabilities": {},
      "clientInfo": {
        "name": "test-client",
        "version": "1.0.0"
      }
    }
  }'

# List available tools
curl -X POST https://rystem.cloud/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/list",
    "params": {}
  }'
```

### Local Development

Run the MCP server locally:

```bash
cd rystemapp
npm run dev:api
```

Server will start on `http://localhost:3000/mcp`

## 🔄 Updates

This documentation is automatically generated during build:

```bash
npm run build-mcp
```

The MCP server content is dynamically loaded from markdown files in `/mcp/{tools,resources,prompts}/`.

---

**Last Updated**: 2025-10-20  
**MCP Protocol Version**: 2024-11-05  
**Documentation**: https://rystem.net  
**MCP Server**: https://rystem.cloud/mcp
