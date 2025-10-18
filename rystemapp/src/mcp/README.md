# MCP (Model Context Protocol) Documentation

This directory contains tools, resources, and prompts for the Rystem MCP server.

## Directory Structure

```
mcp/
├── tools/      # Executable tools for AI assistants
├── resources/  # Reference documentation and examples
└── prompts/    # Pre-configured prompts and templates
```

## Metadata Format

Each markdown file can include YAML frontmatter to provide metadata. The frontmatter is used to generate the MCP manifest and is automatically removed from the published files.

### Frontmatter Structure

```markdown
---
title: "Your Tool/Resource/Prompt Title"
description: "A concise description used by MCP servers and GitHub Copilot"
category: "optional-category"
tags: ["tag1", "tag2", "tag3"]
---

# Your Content Starts Here

The actual content of your tool/resource/prompt...
```

### Frontmatter Fields

- **title** (required): The display name shown in the MCP interface
- **description** (required): A brief description used in:
  - MCP manifest for AI assistants
  - GitHub Copilot suggestions
  - Tool/resource listings
- **category** (optional): For organizing items
- **tags** (optional): For filtering and search

### Example: Tool with Frontmatter

```markdown
---
title: "Domain-Driven Design (DDD) Pattern"
description: "Setup DDD pattern with entities, aggregates, value objects, and repositories using Rystem Framework"
category: "architecture"
tags: ["ddd", "pattern", "architecture", "entities"]
---

# Domain-Driven Design (DDD) Pattern

This tool provides guidance for implementing DDD patterns...
```

## Build Process

When you run `npm run build-mcp`:

1. ✅ Reads all `.md` files from `tools/`, `resources/`, and `prompts/`
2. ✅ Extracts metadata from YAML frontmatter
3. ✅ Removes frontmatter from the published files
4. ✅ Copies clean markdown to `public/mcp/`
5. ✅ Generates `public/mcp-manifest.json` with all metadata

## Fallback Behavior

If no frontmatter is provided, the script will:
- Use the first `# Heading` as the title
- Use the first `## Subheading` or `> Quote` as the description

However, **using frontmatter is recommended** for better control and MCP server compatibility.

## Best Practices

1. **Always include frontmatter** in new files
2. **Keep descriptions concise** (1-2 sentences)
3. **Use meaningful titles** that describe the purpose
4. **Add relevant tags** for better discoverability
5. **Test locally** with `npm run build-mcp` before committing

## Testing

```bash
# Build and verify the manifest
npm run build-mcp

# Check the generated manifest
cat public/mcp-manifest.json

# Verify frontmatter was removed
cat public/mcp/tools/your-file.md
```

## GitHub Copilot Integration

The description field is particularly important for GitHub Copilot, as it helps the AI understand when to suggest your tools and prompts.

**Good description:**
> "Setup DDD pattern with entities, aggregates, value objects, and repositories using Rystem Framework"

**Bad description:**
> "DDD stuff"
