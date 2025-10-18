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
- `npm run build-mcp` - Generate MCP manifest
- `npm run build` - Build production site (runs build-docs and build-mcp first)
- `npm run preview` - Preview production build locally

## 📚 Adding Documentation

Documentation is automatically generated from README.md files in the `../src/` directory. Simply add or update README files in your packages, and run `npm run build-docs` to regenerate the index.

## 🔧 Adding MCP Content

1. Add Markdown files to `src/mcp/tools/`, `src/mcp/resources/`, or `src/mcp/prompts/`
2. Run `npm run build-mcp` to update the manifest
3. The content will be available on the MCP page

## 🌐 Deployment

The site is automatically deployed to GitHub Pages when changes are pushed to the master branch via GitHub Actions.
