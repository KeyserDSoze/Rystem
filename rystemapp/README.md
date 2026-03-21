# Rystem Documentation Site

This directory contains the static documentation site for the Rystem framework.

## 🚀 Quick Start

```bash
# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview
```

## 📁 Structure

```
rystemapp/
├── src/
│   ├── components/      # React components
│   ├── pages/           # Page components
│   ├── generated/       # Auto-generated from README files
│   └── mcp/             # MCP static files
│       ├── tools/       # MCP tools
│       ├── resources/   # MCP resources
│       └── prompts/     # MCP prompts
├── scripts/
│   ├── build-docs.ts    # Scans and generates doc index
│   └── build-mcp.ts     # Generates MCP manifest
└── public/              # Static assets
```

## 🛠️ Scripts

- `npm run dev` - Start development server
- `npm run build-docs` - Generate documentation index from README files
- `npm run build-mcp` - Generate MCP manifest and copy markdown files
- `npm run build-mcp-server` - Generate static MCP server responses (JSON-RPC)
- `npm run prebuild` - Run all build scripts (docs + mcp + mcp-server)
- `npm run build` - Build production site (runs prebuild first)
- `npm run preview` - Preview production build locally

## 🤖 MCP Integration

Rystem now provides a **Model Context Protocol (MCP)** static server for AI tools integration!

### Quick Setup

**GitHub Copilot:**
```json
{
  "github.copilot.chat.codeGeneration.instructions": [
    { "text": "Use Rystem patterns from https://rystem.net/mcp-manifest.json" }
  ]
}
```

**Claude Desktop:**
```json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.net/mcp-server.json",
      "type": "static"
    }
  }
}
```

### Documentation

- 📘 [MCP Integration Guide](./MCP-INTEGRATION.md) - Complete setup instructions
- 📝 [MCP Examples](./MCP-EXAMPLES.md) - Usage examples with different AI tools
- 🧪 [Test MCP Server](./TEST-MCP.md) - Testing and troubleshooting

### Endpoints

- `https://rystem.net/mcp-server.json` - Server info
- `https://rystem.net/mcp-tools-list.json` - Tools list
- `https://rystem.net/mcp-resources-list.json` - Resources list
- `https://rystem.net/mcp-prompts-list.json` - Prompts list
- `https://rystem.net/.well-known/mcp.json` - Autodiscovery

## 📚 Adding Documentation

Documentation is automatically generated from README.md files in the `../src/` directory. Simply add or update README files in your packages, and run `npm run build-docs` to regenerate the index.

## 🔧 Adding MCP Content

1. Create a markdown file in `src/mcp/tools/`, `src/mcp/resources/`, or `src/mcp/prompts/`
2. Add YAML frontmatter with title, description, category, and tags
3. Run `npm run build-mcp && npm run build-mcp-server`
4. Content will be available in the MCP server and web UI

## 🔧 Adding MCP Content

1. Add Markdown files to `src/mcp/tools/`, `src/mcp/resources/`, or `src/mcp/prompts/`
2. Run `npm run build-mcp` to update the manifest
3. The content will be available on the MCP page

## 🌐 Deployment

The site is automatically deployed to GitHub Pages when changes are pushed to the master branch via GitHub Actions.