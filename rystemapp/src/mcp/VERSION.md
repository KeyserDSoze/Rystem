# MCP Manifest Versioning Guide

Quick reference for updating the Rystem MCP manifest version.

## 📦 Current Version

The manifest version is automatically synced from `package.json`.

Current version: **Check `package.json`**

---

## 🔄 How to Update the Version

### Step 1: Update package.json

Use npm version commands (recommended):

```bash
# Bug fixes, documentation updates (0.0.1 → 0.0.2)
npm version patch

# New tools/resources/prompts (0.0.1 → 0.1.0)
npm version minor

# Breaking changes (0.0.1 → 1.0.0)
npm version major
```

**Or manually edit `package.json`:**
```json
{
  "name": "rystem-docs",
  "version": "0.0.2",  ← Change this
  ...
}
```

### Step 2: Rebuild the manifest

```bash
npm run build-mcp
```

This will show:
```
📦 Using version from package.json: 0.0.2
```

### Step 3: Build and deploy

```bash
# Build the entire site
npm run build

# Commit and push
git add .
git commit -m "chore: bump MCP version to 0.0.2"
git push
```

---

## 🔄 GitHub Copilot & VS Code Auto-Update

### How MCP Clients Handle Updates

**GitHub Copilot:**
- Checks for manifest updates periodically (every 5-30 minutes)
- Uses the `version` field to determine if cache should be invalidated
- May require 5-30 minutes to pick up changes after deployment

**VS Code MCP Extension:**
- Respects HTTP cache headers from the server
- Checks version field in manifest
- May cache for minutes to hours depending on configuration

### Force Update in Clients

**For users to see updates faster:**

1. **Increment version** (most important!)
   ```bash
   npm version patch
   npm run build-mcp
   npm run build
   git push
   ```

2. **Wait for deployment** (~2-5 minutes for GitHub Pages)

3. **Client-side refresh options:**

   **In VS Code:**
   - `Cmd/Ctrl + Shift + P` → "Developer: Reload Window"
   - Or restart VS Code completely

   **In GitHub Copilot:**
   - Restart VS Code to force manifest refresh
   - Or wait for automatic refresh (5-30 min)

### Cache Headers (Automatic)

GitHub Pages automatically sets appropriate cache headers. The `version` field in the manifest helps clients know when to refresh.

**Best practice:**
- Always increment version when adding/modifying tools
- Use semantic versioning (MAJOR.MINOR.PATCH)
- Document changes in git commit messages

---

## 📝 Version Numbering Guidelines

### Semantic Versioning

Follow `MAJOR.MINOR.PATCH` format:

**0.0.x** - Initial development
- Add/update individual tools: `0.0.1 → 0.0.2`
- Fix typos/bugs: patch version

**0.x.0** - Beta/Testing
- Add new categories of tools: `0.0.5 → 0.1.0`
- Significant content additions: minor version

**x.0.0** - Stable Release
- Breaking changes to tool interfaces: `0.9.0 → 1.0.0`
- Major restructuring: major version

### Examples

```bash
# Added project-setup tool
npm version patch  # 0.0.1 → 0.0.2
npm run build-mcp
git commit -m "feat: add project-setup tool"

# Added entire "deployment" category with 5 tools
npm version minor  # 0.0.2 → 0.1.0
npm run build-mcp
git commit -m "feat: add deployment tools category"

# Changed tool interface structure (breaking)
npm version major  # 0.9.0 → 1.0.0
npm run build-mcp
git commit -m "feat!: update tool interface format"
```

---

## 🚀 Complete Workflow Example

```bash
# 1. Add new content
echo "---
title: \"My New Tool\"
description: \"Does something useful\"
---
# My New Tool
..." > src/mcp/tools/my-new-tool.md

# 2. Increment version
npm version patch

# 3. Build everything
npm run build-docs
npm run build-mcp
npm run build

# 4. Test locally
npm run dev
# Open http://localhost:5173/mcp

# 5. Deploy
git add .
git commit -m "feat: add my-new-tool"
git push origin master

# 6. Wait for deployment (2-5 min)
# 7. Users will see updates within 5-30 minutes
# 8. Or they can manually reload VS Code
```

---

## 🐛 Troubleshooting

**Problem:** "Changes not showing in GitHub Copilot"

**Solutions:**
1. ✅ Did you increment the version? (`npm version patch`)
2. ✅ Did you rebuild? (`npm run build-mcp`)
3. ✅ Did you push to GitHub? (`git push`)
4. ⏱️ Wait 5-30 minutes for cache expiration
5. 🔄 Restart VS Code completely
6. 🔍 Check if GitHub Pages deployed successfully (Settings → Pages)

**Problem:** "Version not updating in manifest"

**Solutions:**
1. ✅ Check `package.json` has the new version
2. ✅ Run `npm run build-mcp` (not just `build`)
3. ✅ Check output: should show "📦 Using version from package.json: X.X.X"
4. ✅ Verify `public/mcp-manifest.json` has new version

---

## 📚 Related

- [MCP README](./README.md) - Full documentation
- [TEMPLATE.md](./TEMPLATE.md) - Template for new tools
- [GitHub Pages Deployment](../../.github/workflows/deploy-docs.yml)
