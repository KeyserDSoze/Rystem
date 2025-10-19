# Rystem MCP Usage Examples

Real-world examples of how to use the Rystem MCP server with different AI tools.

## 🤖 GitHub Copilot Chat

### Example 1: Ask about project setup

```
@workspace How do I create a new Rystem project with multiple domains?

# Copilot will use the project-setup tool from rystem.net
```

### Example 2: Request DDD implementation

```
@workspace Show me how to implement a DDD aggregate in Rystem

# Copilot references the ddd tool
```

### Example 3: Repository pattern

```
@workspace How do I set up a repository with Entity Framework in Rystem?

# Uses repository-setup tool
```

## 💬 Claude Desktop

After configuring the MCP server in Claude Desktop, you can ask:

### Example 1: Architecture guidance

```
I'm building a new e-commerce platform. Help me set up the project 
structure using Rystem Framework with separate domains for Orders, 
Customers, and Products.
```

Claude will reference the `project-setup` tool and provide guidance following the multiple domain architecture pattern.

### Example 2: Background jobs

```
How do I implement background job processing in my Rystem application?
```

Claude will use the `background-jobs` resource to provide implementation details.

## 🔄 Cursor

### Example 1: Quick commands

```
/rystem setup a new single domain project called "TaskManager"
```

Cursor will use the project-setup tool to generate the complete structure.

### Example 2: Code generation

```
/rystem create a User aggregate with email and password properties
```

Cursor uses the DDD tool to generate proper aggregate structure.

## 📝 VS Code with Copilot

### Workspace Context

Add to `.vscode/settings.json`:

```json
{
  "github.copilot.chat.codeGeneration.instructions": [
    {
      "text": "Use Rystem Framework patterns from https://rystem.net/mcp-manifest.json"
    }
  ]
}
```

Then use inline chat:

```csharp
// Create a repository for the Customer entity
// Copilot will generate code following Rystem patterns
```

## 🎯 Common Use Cases

### 1. Starting a New Project

**Question to AI:**
```
Create a new Rystem project called "InventorySystem" with two domains: 
Products and Warehouses. Show me the complete folder structure and 
initial setup commands.
```

**AI Response will include:**
- Complete folder structure for multiple domains
- dotnet CLI commands to create projects
- Package references to Rystem.DependencyInjection
- Project references between layers

### 2. Implementing Repository Pattern

**Question to AI:**
```
I have a Product entity with Id, Name, and Price. Show me how to 
create a repository using Rystem.RepositoryFramework with Entity Framework.
```

**AI Response will include:**
- Entity definition with proper attributes
- IRepository<Product> interface usage
- DbContext configuration
- Dependency injection setup

### 3. Setting Up Authentication

**Question to AI:**
```
How do I add OAuth authentication to my Rystem API using the 
authentication flow patterns?
```

**AI Response will include:**
- Rystem.Authentication.Social.Abstractions usage
- OAuth provider configuration
- Middleware setup
- Token handling

### 4. Background Job Processing

**Question to AI:**
```
I need to send email notifications in the background. Show me how 
to implement this using Rystem.BackgroundJob.
```

**AI Response will include:**
- BackgroundJob service registration
- Job implementation
- Scheduling configuration
- Error handling

### 5. Concurrency Control

**Question to AI:**
```
My application has multiple users editing the same order. How do I 
implement optimistic concurrency control with Rystem?
```

**AI Response will include:**
- Rystem.Concurrency usage
- Version tracking
- Conflict resolution
- Redis integration for distributed scenarios

## 🔍 Advanced Queries

### Query Chaining

```
@workspace First, set up a new project called "BlogEngine" with 
Posts and Comments domains. Then, show me how to implement the 
Post aggregate with DDD patterns and create a repository for it.
```

AI will:
1. Use `project-setup` to create structure
2. Use `ddd` to implement Post aggregate
3. Use `repository-setup` to create repository

### Context-Aware Generation

```
@workspace Look at my current User.cs entity and refactor it to 
follow Rystem DDD patterns with proper aggregate root, value objects, 
and domain events.
```

AI will:
1. Analyze your existing code
2. Reference `ddd` tool for patterns
3. Generate refactored code following Rystem conventions

## 📊 Metrics and Validation

After using MCP, you can verify the integration is working:

### Check Tool Usage

In GitHub Copilot logs:
```
> GitHub Copilot Chat: Used context from rystem.net/mcp-manifest.json
> Retrieved tool: project-setup
> Applied Rystem Framework patterns
```

### Verify Generated Code

Generated code should include:
- ✅ Proper Rystem.DependencyInjection usage
- ✅ Correct project structure (domains/business/infrastructures)
- ✅ Repository pattern with Rystem.RepositoryFramework
- ✅ DDD patterns (aggregates, value objects, domain events)

## 🎓 Learning Path

### Beginner

1. Ask AI to explain Rystem Framework basics
2. Request simple project setup (single domain)
3. Implement basic repository
4. Add simple background job

### Intermediate

1. Create multiple domain architecture
2. Implement DDD aggregates with domain events
3. Add concurrency control
4. Setup authentication flow

### Advanced

1. Implement complex multi-domain scenarios
2. Use advanced repository patterns
3. Optimize with caching strategies
4. Build custom MCP tools for your project

## 💡 Tips

1. **Be Specific**: Include domain names, entity names, and requirements
2. **Reference Patterns**: Mention "using Rystem patterns" in your queries
3. **Iterate**: Ask follow-up questions to refine generated code
4. **Validate**: Always review generated code against Rystem documentation
5. **Context**: Keep relevant Rystem code files open for better context

## 🚨 Troubleshooting

### AI not using Rystem patterns

**Solution**: Explicitly mention "using Rystem Framework" in your query

### Outdated patterns suggested

**Solution**: Reference specific tools: "use the project-setup tool from rystem.net"

### Missing context

**Solution**: Ensure MCP configuration is correct and endpoints are accessible

## 🔗 Resources

- [MCP Integration Guide](./MCP-INTEGRATION.md)
- [Test MCP Server](./TEST-MCP.md)
- [Rystem Documentation](https://rystem.net)

---

**Need Help?** Open an issue on [GitHub](https://github.com/KeyserDSoze/Rystem/issues)
