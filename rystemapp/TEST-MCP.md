# Test Rystem MCP Server

Quick test script to verify MCP server endpoints are working.

## Usage

```bash
# Make this script executable
chmod +x test-mcp.sh

# Run tests
./test-mcp.sh
```

Or test manually:

```bash
# Test Initialize endpoint
curl https://rystem.net/mcp-server.json | jq

# Test Tools List
curl https://rystem.net/mcp-tools-list.json | jq

# Test Resources List
curl https://rystem.net/mcp-resources-list.json | jq

# Test Prompts List
curl https://rystem.net/mcp-prompts-list.json | jq

# Test autodiscovery
curl https://rystem.net/.well-known/mcp.json | jq

# Test tool content
curl https://rystem.net/mcp/tools/project-setup.md

# Test manifest (used by UI)
curl https://rystem.net/mcp-manifest.json | jq
```

## Expected Responses

### Initialize (/mcp-server.json)
```json
{
  "jsonrpc": "2.0",
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "tools": {},
      "resources": {},
      "prompts": {}
    },
    "serverInfo": {
      "name": "rystem-mcp",
      "version": "0.0.1"
    }
  },
  "id": 1
}
```

### Tools List (/mcp-tools-list.json)
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      {
        "name": "ddd",
        "description": "Setup DDD pattern...",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      },
      ...
    ]
  },
  "id": 1
}
```

## Test with MCP Inspector

```bash
npx @modelcontextprotocol/inspector
```

Then connect to: `https://rystem.net/mcp-server.json`

## Local Testing

Before deployment, test locally:

```bash
# Start dev server
npm run dev

# Test local endpoints
curl http://localhost:5174/mcp-server.json | jq
curl http://localhost:5174/mcp-tools-list.json | jq
```

## Troubleshooting

### CORS Issues
If testing from browser, ensure CORS headers are properly configured in GitHub Pages.

### 404 Errors
Verify files exist in `public/` directory and are deployed to GitHub Pages.

### Invalid JSON
Run `npm run build-mcp-server` to regenerate files.

### Outdated Content
Ensure you've run the full build:
```bash
npm run prebuild
npm run build
```
