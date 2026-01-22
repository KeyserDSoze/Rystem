### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Dynamics Dataverse Integration

This package provides Microsoft Dynamics Dataverse (formerly Common Data Service) integration for the Repository Framework, perfect for **enterprise applications, business process automation, and Microsoft ecosystem integration**.

### 🎯 When to Use Dynamics Dataverse

✅ **Dynamics Integration** - Connect to Dynamics 365 CE/Sales/Service  
✅ **Power Platform** - Integrate with Power Apps, Power Automate  
✅ **Enterprise CRM** - Centralized customer data management  
✅ **Business Process Automation** - Complex workflows and stages  
✅ **Microsoft Ecosystem** - SharePoint, Teams, Office 365 integration  
✅ **Multi-Tenant SaaS** - Built-in multi-tenancy support  

### ⚠️ Complexity Warning
Dataverse has a steep learning curve. Requires understanding of:
- Entity definitions and relationships
- Privilege escalation
- Solution management
- Organization service

---

## Prerequisites

1. **Azure AD Application Registration** for API access
2. **Dataverse Environment URL**
3. **Client ID and Client Secret**

---

## Basic Configuration

### Simple Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<User, string>(repositoryBuilder =>
{
    repositoryBuilder.WithDataverse(dataverseBuilder =>
    {
        // Connection settings
        dataverseBuilder.Settings.Prefix = "repo_";          // Entity prefix
        dataverseBuilder.Settings.SolutionName = "MyApp";    // Solution name
        
        // Azure AD credentials
        dataverseBuilder.Settings.SetConnection(
            environmentUrl: configuration["Dataverse:EnvironmentUrl"],
            credentials: new(
                clientId: configuration["Dataverse:ClientId"],
                clientSecret: configuration["Dataverse:ClientSecret"]
            ));
    });
});

var app = builder.Build();

// Initialize Dataverse tables
await app.Services.WarmUpAsync();
```

### Configuration Breakdown

**Prefix**: Prefixes all entity names. Example: `repo_user` table name

**SolutionName**: Groups related entities in a Dataverse solution

**EnvironmentUrl**: URL of your Dataverse environment (e.g., `https://myorg.crm.dynamics.com`)

**ClientId/ClientSecret**: Azure AD app credentials for API access

---

## Domain Model Setup

```csharp
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
    public List<Contact> Contacts { get; set; } = new();
}

public class Contact
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime AddedOn { get; set; }
}
```

---

## Complete Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Configure Dataverse repository
services.AddRepository<User, string>(repositoryBuilder =>
{
    // Step 1: Configure Dataverse connection
    repositoryBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "app_";
        dataverseBuilder.Settings.SolutionName = "MyCrmApp";
        
        dataverseBuilder.Settings.SetConnection(
            configuration["Dataverse:EnvironmentUrl"],
            new(
                configuration["Dataverse:ClientId"],
                configuration["Dataverse:ClientSecret"]
            ));
    });
    
    // Step 2: Add business logic interceptors
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<UserBeforeInsertBusiness>()
        .AddBusinessAfterInsert<UserAfterInsertBusiness>();
});

var app = builder.Build();

// Step 3: Initialize - creates tables if they don't exist
await app.Services.WarmUpAsync();

app.Run();
```

### Business Logic Interceptors

Example: Set creation timestamp before insert

```csharp
public class UserBeforeInsertBusiness : IRepositoryBusiness<User, string>
{
    public async ValueTask<User?> BeforeInsertAsync(User entity)
    {
        entity.CreatedOn = DateTime.UtcNow;
        entity.IsActive = true;
        return entity;
    }
}
```

See [IRepositoryBusiness](https://rystem.net/mcp/resources/background-jobs.md) documentation for all lifecycle hooks.

---

## Using the Repository

### Inject and Use

```csharp
public class UserService(IRepository<User, string> repository)
{
    public async Task CreateUserAsync(User user)
    {
        // Triggers business logic and saves to Dataverse
        await repository.InsertAsync(user);
    }
    
    public async Task<User?> GetUserAsync(string userId)
    {
        return await repository.GetByKeyAsync(userId);
    }
    
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await repository.QueryAsync(x => x.IsActive);
    }
    
    public async Task UpdateUserAsync(User user)
    {
        await repository.UpdateAsync(user);
    }
    
    public async Task DeactivateUserAsync(string userId)
    {
        var user = await repository.GetByKeyAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            await repository.UpdateAsync(user);
        }
    }
}
```

---

## Entity Relationships

### One-to-Many Relationship

```csharp
public class Account
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    // One-to-many: Account has many Contacts
    public List<Contact> Contacts { get; set; } = new();
}

public class Contact
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AccountId { get; set; }  // Foreign key
}

// Configuration
services.AddRepository<Account, string>(builder =>
{
    builder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.SetConnection(...);
        // Relationships configured automatically from model
    });
});
```

---

## Advanced Patterns

### Multi-Environment Setup

```csharp
// Development
var devConfig = services.AddRepository<User, string>(builder =>
{
    builder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.EnvironmentUrl = configuration["Dataverse:Dev:Url"];
    });
});

// Production
var prodConfig = services.AddRepository<User, string>(builder =>
{
    builder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.EnvironmentUrl = configuration["Dataverse:Prod:Url"];
    });
});
```

### Privilege Escalation

For operations requiring elevated privileges:

```csharp
public class AdminUserService(IRepository<User, string> repository)
{
    public async Task DeleteUserAsync(string userId)
    {
        // Dataverse handles privilege verification
        await repository.DeleteAsync(userId);
    }
}
```

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Setup Dataverse repository
builder.Services.AddRepository<CrmUser, string>(repositoryBuilder =>
{
    repositoryBuilder.WithDataverse(dataverseBuilder =>
    {
        dataverseBuilder.Settings.Prefix = "crm_";
        dataverseBuilder.Settings.SolutionName = "CrmIntegration";
        
        dataverseBuilder.Settings.SetConnection(
            builder.Configuration["Dataverse:EnvironmentUrl"],
            new(
                builder.Configuration["Dataverse:ClientId"],
                builder.Configuration["Dataverse:ClientSecret"]
            ));
    });
    
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<CrmUserValidationBusiness>();
});

var app = builder.Build();

// Initialize Dataverse
await app.Services.WarmUpAsync();

// Map endpoints
app.MapPost("/users", async (IRepository<CrmUser, string> repo, CrmUser user) =>
{
    await repo.InsertAsync(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapGet("/users/{id}", async (IRepository<CrmUser, string> repo, string id) =>
{
    var user = await repo.GetByKeyAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.Run();
```

---

## Automated REST API

Expose your Dataverse repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Dataverse API")
    .WithPath("/api")
    .WithSwagger()
    .WithVersion("v1")
    .WithDocumentation()
    .WithDefaultCors("*");

var app = builder.Build();

app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

Automatically generates endpoints:
- `GET /api/user` - Query all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## WarmUpAsync()

**Critical**: Must be called after `Build()` to initialize Dataverse connection and create tables.

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();  // Creates entities in Dataverse
app.Run();
```

---

## 💡 Best Practices

✅ Use **meaningful prefixes** to organize entities  
✅ Leverage **Dataverse workflows** for business automation  
✅ Implement **privilege-based access control**  
✅ Use **solution management** for version control  
✅ Monitor **API call limits** (Dataverse has throttling)  
✅ Test in **sandbox environment** before production  

---

## Troubleshooting

### Common Issues

**"Authentication failed"** → Check ClientId/ClientSecret and environment URL

**"Entity not found"** → Run `WarmUpAsync()` or check prefix settings

**"Privilege violation"** → Verify Azure AD app has required Dataverse permissions

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [Microsoft Dataverse Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/)
- [Dynamics 365 CE Integration](https://learn.microsoft.com/en-us/dynamics365/customer-engagement/developer/use-web-api)
- [Azure AD App Registration](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)
