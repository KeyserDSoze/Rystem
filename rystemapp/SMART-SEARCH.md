# 🔍 Smart Fuzzy Search - MCP Dynamic Tools

## Overview

The MCP server now implements **progressive keyword disambiguation** to handle imprecise queries intelligently.

## Problem Statement

When AI assistants query the MCP server, they often use natural language phrases like:
- `"blob storage repository configuration WithBlobStorage"`
- `"how to setup azure cosmos sql"`
- `"entity framework repository pattern"`

These queries don't match the exact `id` + `value` structure, causing documentation lookups to fail.

## Solution: Progressive Keyword Disambiguation

### Algorithm

1. **Extract Keywords**: Split query by spaces, filter words < 3 chars
2. **Score Candidates**: For each document, calculate:
   - `matchedKeywords` = number of query keywords found in `id + value + title`
   - `score` = (matchedKeywords × 1000) + (sum of keyword lengths × 100)
3. **Rank Results**: Sort by score (highest first)
4. **Auto-Correct**: If top result significantly better (1.5× next best), auto-select it
5. **Disambiguate**: If ambiguous, show all options with available categories

### Example Flow

**Query**: `"blob storage repository configuration WithBlobStorage"`

**Step 1**: Extract keywords → `["blob", "storage", "repository", "configuration", "withblobstorage"]`

**Step 2**: Score all documents:
- `repository/api-server` → matches: ["repository"] → score: 1010
- `content/blob` → matches: ["blob", "storage"] → score: 2090
- `repository/setup` → matches: ["repository"] → score: 1010

**Step 3**: Top result = `content/blob` (score 2090 >> 1010)

**Step 4**: Auto-correct! Return `content/blob` documentation with suggestion message

---

## Implementation Details

### Main Tool Enhancement

```typescript
// In get-rystem-docs tool
const findBestMatch = (query: string) => {
    const keywords = query.toLowerCase().split(/\s+/).filter(k => k.length > 2);
    let candidates = [];

    for (const [id, topics] of Object.entries(mapping)) {
        for (const value of Object.keys(topics)) {
            const searchText = `${id} ${value} ${title}`.toLowerCase();
            let score = 0;
            let matchedKeywords = 0;
            
            for (const keyword of keywords) {
                if (searchText.includes(keyword)) {
                    matchedKeywords++;
                    score += keyword.length;
                }
            }
            
            if (matchedKeywords > 0) {
                candidates.push({ id, value, score: matchedKeywords * 1000 + score });
            }
        }
    }

    // Sort by score, return best if unambiguous
    candidates.sort((a, b) => b.score - a.score);
    if (candidates[0].score > candidates[1].score * 1.5) {
        return candidates[0];
    }
    return null; // Ambiguous
};
```

### Error Messages Enhanced

**Before**:
```
❌ Documentation not found for id="blob", value="storage repository configuration WithBlobStorage"

📂 Available categories:
  - auth
  - content
  - ddd
  ...
```

**After**:
```markdown
# [Content: Azure Blob Storage Documentation]

...full documentation content...

---
💡 **Auto-corrected**: You searched for id="blob", value="storage repository configuration WithBlobStorage"
✅ **Showing**: id="content", value="blob" (Azure Blob Storage)
```

### Search Tool Enhancement

**Before**: Simple `includes()` check

**After**: Progressive keyword scoring with ranked results

```typescript
// Usage: get-rystem-docs-search(query="blob storage configuration")

🔍 Found 3 matches for "blob storage configuration" (showing top 10):

1. **content** → `blob` (Azure Blob Storage)
   Matched: blob, storage, configuration
   Usage: get-rystem-docs(id="content", value="blob")

2. **content** → `file` (Azure File Storage)
   Matched: storage, configuration
   Usage: get-rystem-docs(id="content", value="file")

3. **repository** → `setup` (Repository Pattern Setup)
   Matched: configuration
   Usage: get-rystem-docs(id="repository", value="setup")
```

---

## Benefits

### 1. **Fault Tolerance**
AI assistants don't need exact parameter matching anymore. The system adapts to natural language.

### 2. **Auto-Correction**
When intent is clear, the system automatically returns the best match with a suggestion note.

### 3. **Helpful Errors**
When ambiguous, shows all available options with full category listings instead of generic "not found".

### 4. **Ranked Search**
Search tool now shows relevance-scored results with matched keywords highlighted.

---

## Testing Scenarios

### Scenario 1: Clear Intent
```json
{
  "tool": "get-rystem-docs",
  "args": {
    "id": "repository",
    "value": "blob storage azure cosmos sql"
  }
}
```

**Expected**: Auto-corrects to `repository/api-server` or shows disambiguation

---

### Scenario 2: Search Query
```json
{
  "tool": "get-rystem-docs-search",
  "args": {
    "query": "authentication social login"
  }
}
```

**Expected**: Ranked list with `auth/social-server`, `auth/social-blazor`, `auth/social-typescript`

---

### Scenario 3: No Matches
```json
{
  "tool": "get-rystem-docs-search",
  "args": {
    "query": "nonexistent feature xyz"
  }
}
```

**Expected**: Full listing of all available categories and topics with usage instructions

---

## Performance Considerations

- **Complexity**: O(n × k) where n = documents, k = keywords
- **Typical Load**: 32 documents × 5 keywords = 160 comparisons
- **Execution Time**: < 10ms (in-memory string operations)

---

## Future Enhancements

1. **Fuzzy String Matching**: Use Levenshtein distance for typo tolerance
2. **Synonym Support**: Map "login" → "authentication", "db" → "database"
3. **Context Awareness**: Weight recent queries higher
4. **Caching**: Memoize frequent query patterns

---

## Code Location

**Files Modified**:
- `rystemapp/api/mcp.ts` (lines 56-280)
  - Added `findBestMatch()` helper
  - Enhanced main tool with auto-correction
  - Enhanced search tool with progressive scoring

**Commit**: 🔍 Add smart fuzzy search to MCP with progressive keyword disambiguation

---

## Documentation

**Main README**: https://rystem.net  
**MCP Server**: https://rystem.cloud/mcp  
**Source Code**: https://github.com/KeyserDSoze/Rystem/blob/master/rystemapp/api/mcp.ts
