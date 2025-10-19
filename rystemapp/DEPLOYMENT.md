# Rystem MCP Server Deployment

This project provides Model Context Protocol (MCP) integration for Rystem Framework with dual deployment strategy.

## 🌐 Deployment Architecture

### Production Endpoints

| Service | URL | Purpose | Technology |
|---------|-----|---------|------------|
| **Website** | https://rystem.net | Static documentation site | GitHub Pages |
| **MCP Server** | https://rystem.vercel.app/mcp | Dynamic MCP protocol server | Vercel Serverless |

### Why Two Deployments?

- **GitHub Pages** (`rystem.net`): 
  - Custom domain already configured
  - Serves static documentation
  - Hosts static MCP JSON files for legacy clients
  - Zero cost, fast CDN delivery

- **Vercel** (`rystem.vercel.app`):
  - Full MCP protocol implementation
  - Serverless functions with TypeScript SDK
  - Dynamic content generation
  - Better for API endpoints

## 🚀 Local Development

### Start Documentation Site
```bash
npm run dev
# Opens on http://localhost:5173
```

### Start MCP Server
```bash
npm run dev:api
# Opens on http://localhost:3000
# MCP endpoint: http://localhost:3000/mcp
```

### Test MCP Server
```bash
# In another terminal
npx tsx test-mcp-local.ts
```

## 📦 Build Process

```bash
# Build everything (docs + MCP + site)
npm run build

# Outputs to:
# - dist/ (static site for GitHub Pages)
# - public/mcp-*.json (static MCP responses)
# - api/mcp.ts (Vercel serverless function)
```

## 🔄 Deployment Flow

When you push to `master`:

1. **GitHub Actions** triggers
2. Builds the project
3. **Deploys to GitHub Pages** → `rystem.net`
4. **Deploys to Vercel** → `rystem.vercel.app`

### GitHub Actions Workflow

```yaml
- build → Test & compile
- deploy-pages → GitHub Pages (rystem.net)
- deploy-vercel → Vercel (rystem.vercel.app)
```

## 🔧 Configuration Files

- **`vercel.json`**: Vercel deployment config
  - Rewrites `/mcp` → `/api/mcp`
  - CORS headers
  - Serverless function settings

- **`.github/workflows/deploy-docs.yml`**: GitHub Actions
  - Automated deployment on push
  - Parallel deployment to both platforms

- **`api/mcp.ts`**: Vercel serverless function
  - Full MCP protocol implementation
  - Reads from `public/mcp-manifest.json`
  - Dynamically loads markdown content

- **`server-local.ts`**: Local development server
  - Same logic as production
  - Serves static files + MCP API
  - Hot reload with tsx

## 📖 Using the MCP Server

### Claude Desktop

Add to `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "rystem": {
      "url": "https://rystem.vercel.app/mcp",
      "transport": {
        "type": "streamable-http"
      }
    }
  }
}
```

### Cursor

Use the deeplink:
```
cursor://anysphere.cursor-deeplink/mcp/install?name=rystem&config=eyJ1cmwiOiJodHRwczovL3J5c3RlbS52ZXJjZWwuYXBwL21jcCJ9
```

### MCP Inspector

```bash
npx @modelcontextprotocol/inspector https://rystem.vercel.app/mcp
```

## 🔑 Required Secrets

For GitHub Actions deployment, these secrets must be configured:

- `VERCEL_TOKEN` - Vercel authentication token
- `VERCEL_ORG_ID` - Vercel organization ID
- `VERCEL_PROJECT_ID` - Vercel project ID

## 📚 Available MCP Items

- **Tools** (4): `ddd`, `install-rystem`, `project-setup`, `repository-setup`
- **Resources** (3): `background-jobs`, `concurrency`, `content-repo`
- **Prompts** (2): `auth-flow`, `service-setup`

## 🛠️ Development Scripts

```json
{
  "dev": "vite",                    // Start docs site
  "dev:api": "...",                 // Start MCP server locally
  "build-docs": "...",              // Generate docs index
  "build-mcp": "...",               // Generate MCP manifest
  "build-mcp-server": "...",        // Generate static responses
  "build": "...",                   // Full production build
  "preview": "vite preview"         // Preview production build
}
```

## 📊 Project Structure

```
rystemapp/
├── api/
│   └── mcp.ts                    # Vercel serverless function
├── public/
│   ├── mcp/                      # MCP content (tools, resources, prompts)
│   ├── mcp-manifest.json         # MCP items registry
│   ├── mcp-server.json           # Static initialize response
│   └── ...                       # Other static MCP files
├── src/                          # React documentation site
├── scripts/
│   ├── build-docs.ts             # Generate docs index
│   ├── build-mcp.ts              # Generate MCP manifest
│   └── build-mcp-server.ts       # Generate static responses
├── server-local.ts               # Local dev server
├── test-mcp-local.ts             # MCP testing script
├── vercel.json                   # Vercel config
└── package.json
```

## 🔮 Future: Custom Domain on Vercel

When you're ready to use `mcp.rystem.net` or similar:

1. Add custom domain in Vercel dashboard
2. Update DNS records
3. Update `vercel.json`:
   ```json
   {
     "alias": ["mcp.rystem.net"]
   }
   ```
4. Update documentation URLs

## 📝 Notes

- **Production MCP Server**: Always use `rystem.vercel.app/mcp`
- **Documentation Site**: Always use `rystem.net`
- **Local Testing**: Use `localhost:3000/mcp`
- **GitHub Copilot**: Uses `.github/copilot-instructions.md` (not MCP)

---

For more information, see:
- [MCP Protocol Docs](https://modelcontextprotocol.io)
- [Rystem Framework](https://rystem.net)
- [Vercel Deployment](https://vercel.com/docs)
