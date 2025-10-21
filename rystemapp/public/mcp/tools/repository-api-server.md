# Repository API Server - Auto-Generated REST APIs

**Purpose**: This tool explains how to **automatically expose repositories as REST APIs** without writing controllers manually. Rystem.RepositoryFramework can generate complete REST endpoints for your repositories with built-in authentication, authorization, and Swagger documentation.

---

## 🎯 What is Repository API Server?

Rystem.RepositoryFramework includes an **API Server** feature that:
- ✅ **Auto-generates REST APIs** for all registered repositories
- ✅ **No controllers needed** - endpoints are created automatically
- ✅ **Built-in Swagger/OpenAPI** documentation
- ✅ **Flexible authorization** - no auth, default policies, or custom per-repository
- ✅ **LINQ query support** in URL parameters
- ✅ **CORS configuration** out of the box

**Key Benefit**: Register your repository once, get a complete REST API for free!

---

## 📦 Installation

```bash
dotnet add package Rystem.RepositoryFramework.Api.Server --version 9.1.3
dotnet add package Rystem.Api.Server --version 9.1.3
```

---

## 🚀 Quick Start - No Authorization

The simplest setup with no authentication required:

### Step 1: Register Repositories and API Server

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register repositories
builder.Services.AddRepository<Order, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
});

builder.Services.AddRepository<Customer, Guid>(settings =>
{
    settings.WithEntityFramework<CustomersDbContext>();
});

// Add API Server
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("CargoLens API")
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger()
    .WithDocumentation()
    .WithDefaultCors("https://app.cargolens.com");

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use API from repositories - NO AUTHORIZATION
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

app.Run();
```

### Step 2: Generated Endpoints

The above code automatically generates these REST endpoints:

#### For `Order` entity:
```
GET     /api/v1/order/{id}                 - Get single order
GET     /api/v1/order/exist/{id}          - Check if order exists
POST    /api/v1/order/query               - Query orders with filter
POST    /api/v1/order                     - Create new order
PUT     /api/v1/order/{id}                - Update order
DELETE  /api/v1/order/{id}                - Delete order
POST    /api/v1/order/batch               - Batch operations
GET     /api/v1/order/count               - Count orders
GET     /api/v1/order/max                 - Get max value
GET     /api/v1/order/min                 - Get min value
GET     /api/v1/order/sum                 - Get sum
GET     /api/v1/order/average             - Get average
```

#### For `Customer` entity:
```
GET     /api/v1/customer/{id}
GET     /api/v1/customer/exist/{id}
POST    /api/v1/customer/query
POST    /api/v1/customer
PUT     /api/v1/customer/{id}
DELETE  /api/v1/customer/{id}
POST    /api/v1/customer/batch
... (all CRUD + aggregation operations)
```

### Step 3: Test with Swagger

Navigate to: `https://localhost:5001/swagger`

You'll see all endpoints documented and ready to test!

---

## 🔒 Authorization - Default Policy

Apply a default authorization policy to all endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://login.microsoftonline.com/{tenant-id}";
        options.Audience = "api://cargolens-api";
    });

// Add authorization
builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddRepository<Order, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
});

// Add API Server
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("CargoLens API")
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger()
    .WithDocumentation();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Use API with DEFAULT authorization
app.UseApiFromRepositoryFramework()
    .WithDefaultAuthorization();

app.Run();
```

**Result**: All endpoints require authentication (valid JWT token).

---

## 🔐 Authorization - Custom Policies

Apply different policies to different operations:

### Setup Policies

```csharp
var builder = WebApplication.CreateBuilder(args);

// Authentication setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* ... */);

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NormalUser", policy =>
    {
        policy.RequireClaim(ClaimTypes.Name);
    });
    
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireRole("Admin");
    });
    
    options.AddPolicy("SuperAdmin", policy =>
    {
        policy.RequireRole("SuperAdmin");
    });
});

// Register repositories
builder.Services.AddRepository<Order, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
});

builder.Services.AddRepository<Customer, Guid>(settings =>
{
    settings.WithEntityFramework<CustomersDbContext>();
});

// Add API Server
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("CargoLens API")
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Configure authorization per repository and method
app.UseEndpoints(endpoints =>
{
    // Order: SuperAdmin required for write operations
    endpoints.UseApiFromRepository<Order>()
        .SetPolicyForCommand()  // Insert, Update, Delete
        .With("SuperAdmin")
        .Build();
    
    // All other repositories: NormalUser for read, Admin for write
    endpoints.UseApiFromRepositoryFramework()
        .SetPolicyForAll()
        .With("NormalUser")  // Default for all operations
        .And()
        .SetPolicy(RepositoryMethods.Insert)
        .With("Admin")
        .And()
        .SetPolicy(RepositoryMethods.Update)
        .With("Admin")
        .And()
        .SetPolicy(RepositoryMethods.Delete)
        .With("Admin")
        .Build();
});

app.Run();
```

### Available Repository Methods

```csharp
RepositoryMethods.Get          // GET /{id}
RepositoryMethods.Exist        // GET /exist/{id}
RepositoryMethods.Query        // POST /query
RepositoryMethods.Insert       // POST /
RepositoryMethods.Update       // PUT /{id}
RepositoryMethods.Delete       // DELETE /{id}
RepositoryMethods.Batch        // POST /batch
RepositoryMethods.Count        // GET /count
RepositoryMethods.Max          // GET /max
RepositoryMethods.Min          // GET /min
RepositoryMethods.Sum          // GET /sum
RepositoryMethods.Average      // GET /average
```

---

## 🚫 Excluding Repositories from API

Sometimes you want a repository for internal use only (not exposed as API):

```csharp
builder.Services.AddRepository<InternalAuditLog, Guid>(settings =>
{
    settings.WithEntityFramework<AuditDbContext>();
    settings.SetNotExposable();  // ⚠️ NOT exposed as API
});

builder.Services.AddRepository<Order, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
    // This WILL be exposed as API (default)
});
```

**Result**: 
- `Order` endpoints are generated ✅
- `InternalAuditLog` endpoints are NOT generated ❌

---

## 🔍 Query API - Using LINQ Expressions

The most powerful feature: **Query with LINQ expressions in HTTP requests**!

### Query Endpoint

```
POST /api/v1/order/query
Content-Type: application/json
```

### Simple Filter

```json
{
  "filter": "ƒ => (ƒ.Status == 2)",
  "top": 10,
  "skip": 0,
  "orderBy": "CreatedAt",
  "ascending": false
}
```

**Explanation**: 
- `ƒ` is the entity variable
- `ƒ.Status == 2` filters where Status equals 2 (e.g., Shipped)
- `top` and `skip` for pagination
- `orderBy` sorts by property name

### Complex Filter with AND/OR

```json
{
  "filter": "ƒ => ((ƒ.Status == 2) AndAlso (ƒ.CustomerId == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")))"
}
```

### String Operations

```json
{
  "filter": "ƒ => (ƒ.OrderNumber.Contains(\"ORD-\"))"
}
```

```json
{
  "filter": "ƒ => Not(String.IsNullOrWhiteSpace(ƒ.CustomerName))"
}
```

### Date Comparisons

```json
{
  "filter": "ƒ => (ƒ.CreatedAt > Convert.ToDateTime(\"2024-01-01\"))"
}
```

### Collections

```json
{
  "filter": "ƒ => (ƒ.Tags.Any(x => (x == \"urgent\")))"
}
```

### Nested Properties

```json
{
  "filter": "ƒ => (ƒ.Customer.Address.City == \"Milan\")"
}
```

### Complex Example

```json
{
  "filter": "ƒ => ((((ƒ.Status == 2) AndAlso (ƒ.TotalAmount > 1000)) OrElse (ƒ.Priority == \"High\")) AndAlso (ƒ.CreatedAt > Convert.ToDateTime(\"2024-01-01\")))",
  "top": 20,
  "skip": 0,
  "orderBy": "TotalAmount",
  "ascending": false
}
```

**Translation**: 
```
Get orders where:
  (Status is Shipped AND TotalAmount > 1000) 
  OR Priority is High
  AND CreatedAt after Jan 1, 2024
Order by TotalAmount descending
Take 20 results
```

---

## 📝 CRUD Operations Examples

### Create (Insert)

```http
POST /api/v1/order
Content-Type: application/json

{
  "key": "bf46510b-b7e6-4ba2-88da-cef208aa81f2",
  "value": {
    "orderNumber": "ORD-12345",
    "customerId": "550e8400-e29b-41d4-a716-446655440000",
    "status": 0,
    "createdAt": "2024-10-21T10:00:00Z",
    "totalAmount": 1500.50
  }
}
```

### Read (Get)

```http
GET /api/v1/order/bf46510b-b7e6-4ba2-88da-cef208aa81f2
```

Response:
```json
{
  "id": "bf46510b-b7e6-4ba2-88da-cef208aa81f2",
  "orderNumber": "ORD-12345",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": 0,
  "createdAt": "2024-10-21T10:00:00Z",
  "totalAmount": 1500.50
}
```

### Update

```http
PUT /api/v1/order/bf46510b-b7e6-4ba2-88da-cef208aa81f2
Content-Type: application/json

{
  "orderNumber": "ORD-12345",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "status": 2,
  "createdAt": "2024-10-21T10:00:00Z",
  "totalAmount": 1500.50
}
```

### Delete

```http
DELETE /api/v1/order/bf46510b-b7e6-4ba2-88da-cef208aa81f2
```

### Check Existence

```http
GET /api/v1/order/exist/bf46510b-b7e6-4ba2-88da-cef208aa81f2
```

Response:
```json
{
  "exists": true
}
```

---

## 🔢 Aggregation Operations

### Count

```http
GET /api/v1/order/count?filter=ƒ => (ƒ.Status == 2)
```

Response:
```json
{
  "count": 42
}
```

### Max

```http
GET /api/v1/order/max?property=TotalAmount
```

### Min

```http
GET /api/v1/order/min?property=TotalAmount
```

### Sum

```http
GET /api/v1/order/sum?property=TotalAmount&filter=ƒ => (ƒ.Status == 3)
```

### Average

```http
GET /api/v1/order/average?property=TotalAmount
```

---

## 📦 Batch Operations

Execute multiple operations in a single request:

```http
POST /api/v1/order/batch
Content-Type: application/json

{
  "operations": [
    {
      "type": "insert",
      "key": "guid-1",
      "value": { /* order data */ }
    },
    {
      "type": "update",
      "key": "guid-2",
      "value": { /* updated order data */ }
    },
    {
      "type": "delete",
      "key": "guid-3"
    }
  ]
}
```

Response:
```json
{
  "results": [
    {
      "key": "guid-1",
      "success": true,
      "message": "Inserted successfully"
    },
    {
      "key": "guid-2",
      "success": true,
      "message": "Updated successfully"
    },
    {
      "key": "guid-3",
      "success": false,
      "message": "Order not found"
    }
  ]
}
```

---

## 🌐 CORS Configuration

### Default CORS (Single Origin)

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDefaultCors("https://app.cargolens.com");
```

### Multiple Origins

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("CargoLensPolicy", policy =>
    {
        policy.WithOrigins(
            "https://app.cargolens.com",
            "https://admin.cargolens.com",
            "http://localhost:3000"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("CargoLensPolicy");
```

---

## 📚 Real-World Example: Complete Setup

```csharp
// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Rystem;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddDbContext<CustomersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CustomersDb")));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["AzureAd:Authority"];
        options.Audience = builder.Configuration["AzureAd:ClientId"];
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("User", policy =>
    {
        policy.RequireClaim("roles", "User");
    });
    
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireClaim("roles", "Admin");
    });
});

// Repositories
builder.Services.AddRepository<Order, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
});

builder.Services.AddRepository<Customer, Guid>(settings =>
{
    settings.WithEntityFramework<CustomersDbContext>();
});

builder.Services.AddRepository<Shipment, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
});

// Internal repository - NOT exposed
builder.Services.AddRepository<AuditLog, Guid>(settings =>
{
    settings.WithEntityFramework<OrdersDbContext>();
    settings.SetNotExposable();
});

// API Server
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("CargoLens API")
    .WithPath("api")
    .WithVersion("v1")
    .WithSwagger()
    .WithDocumentation()
    .WithDefaultCors("https://app.cargolens.com");

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CargoLens API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Configure API authorization
app.UseEndpoints(endpoints =>
{
    // Health check
    endpoints.MapHealthChecks("/healthz");
    
    // Order API: Admin only for write operations
    endpoints.UseApiFromRepository<Order>()
        .SetPolicyForCommand()
        .With("Admin")
        .Build();
    
    // Customer API: Admin only for write operations
    endpoints.UseApiFromRepository<Customer>()
        .SetPolicyForCommand()
        .With("Admin")
        .Build();
    
    // Shipment API: User can read, Admin can write
    endpoints.UseApiFromRepository<Shipment>()
        .SetPolicyForQuery()
        .With("User")
        .And()
        .SetPolicyForCommand()
        .With("Admin")
        .Build();
    
    // All other repositories: Default authorization
    endpoints.UseApiFromRepositoryFramework()
        .SetPolicyForAll()
        .With("User")
        .Build();
});

app.Run();
```

**Generated Endpoints**:
```
✅ /api/v1/order/*        - Admin required for POST/PUT/DELETE
✅ /api/v1/customer/*     - Admin required for POST/PUT/DELETE
✅ /api/v1/shipment/*     - User for GET, Admin for POST/PUT/DELETE
❌ /api/v1/auditlog/*     - NOT exposed (SetNotExposable)
```

---

## 📋 Configuration Options

### AddApiFromRepositoryFramework Options

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("API Display Name")
    .WithPath("api")                // Base path (default: "api")
    .WithVersion("v1")              // Version (default: "v1")
    .WithSwagger()                  // Enable Swagger UI
    .WithDocumentation()            // Enable XML documentation
    .WithDefaultCors("origin");     // Single CORS origin
```

### UseApiFromRepositoryFramework Options

```csharp
// No authorization (default)
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

// Default authorization (requires authenticated user)
app.UseApiFromRepositoryFramework()
    .WithDefaultAuthorization();

// Custom per-repository authorization
app.UseEndpoints(endpoints =>
{
    endpoints.UseApiFromRepository<Order>()
        .SetPolicyForCommand()      // All write operations
        .With("PolicyName")
        .And()
        .SetPolicyForQuery()        // All read operations
        .With("PolicyName")
        .And()
        .SetPolicy(RepositoryMethods.Insert)  // Specific method
        .With("PolicyName")
        .Build();
});
```

---

## 🎯 Best Practices

### 1. Always Use Policies for Production

```csharp
// ❌ BAD - No authorization in production
app.UseApiFromRepositoryFramework()
    .WithNoAuthorization();

// ✅ GOOD - Proper authorization
app.UseEndpoints(endpoints =>
{
    endpoints.UseApiFromRepositoryFramework()
        .SetPolicyForAll()
        .With("User")
        .And()
        .SetPolicyForCommand()
        .With("Admin")
        .Build();
});
```

### 2. Hide Internal Repositories

```csharp
// ✅ GOOD - Internal repositories not exposed
builder.Services.AddRepository<AuditLog, Guid>(settings =>
{
    settings.WithEntityFramework<AuditDbContext>();
    settings.SetNotExposable();
});
```

### 3. Use Different Policies for Different Entities

```csharp
// ✅ GOOD - Fine-grained control
endpoints.UseApiFromRepository<Order>()
    .SetPolicyForCommand()
    .With("OrderAdmin")
    .Build();

endpoints.UseApiFromRepository<Customer>()
    .SetPolicyForCommand()
    .With("CustomerAdmin")
    .Build();
```

### 4. Enable Swagger Only in Development

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

### 5. Use Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrdersDbContext>();

app.MapHealthChecks("/healthz");
```

---

## ⚠️ Important Notes

1. **Package Required**: `Rystem.RepositoryFramework.Api.Server` version 9.1.3
2. **Automatic Swagger**: Swagger is auto-configured when you use `.WithSwagger()`
3. **Default Behavior**: Repositories are exposed by default unless you use `.SetNotExposable()`
4. **LINQ Syntax**: Query filters use C# LINQ expression syntax (converted from string)
5. **Authorization Precedence**: Specific repository policies override global policies
6. **CORS**: Always configure CORS for production frontends

---

## 🔗 Related Resources

- **repository-setup**: How to configure repositories
- **repository-api-client-typescript**: Consume these APIs from TypeScript/JavaScript apps
- **repository-api-client-dotnet**: Consume these APIs from .NET/C# apps (Blazor, MAUI, WPF)
- **ddd-single-domain**: Organizing repositories in single-domain architecture
- **ddd-multi-domain**: Organizing repositories in multi-domain architecture
- **auth-flow**: Setting up authentication with Rystem.Authentication.Social

---

## 📖 Further Reading

- [Rystem.RepositoryFramework.Api.Server GitHub](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Api.Server)
- [API Server Examples](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Test/RepositoryFramework.Api.Server.Test)
- [Rystem.Api.Server Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Api/Rystem.Api.Server)

---

## ✅ Summary

**Rystem.RepositoryFramework.Api.Server** provides:
- ✅ Zero-code REST API generation
- ✅ Complete CRUD operations for all repositories
- ✅ LINQ query support in HTTP requests
- ✅ Flexible authorization (none, default, custom per-repository)
- ✅ Automatic Swagger/OpenAPI documentation
- ✅ Batch operations support
- ✅ Aggregation operations (Count, Max, Min, Sum, Average)
- ✅ CORS configuration
- ✅ Option to hide internal repositories

**Use this tool to quickly expose your repositories as production-ready REST APIs!** 🚀
