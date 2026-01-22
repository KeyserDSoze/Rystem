### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Azure Blob Storage Integration

This package provides Azure Blob Storage integration for the Repository Framework, perfect for **scalable file storage, large files, CDN distribution, and cloud-native applications**.

### 🎯 When to Use Azure Blob Storage

✅ **Large Files** - Efficiently store and retrieve large documents, videos, images  
✅ **CDN Distribution** - Deliver content worldwide with Azure CDN  
✅ **Scalable Storage** - Grow without infrastructure constraints  
✅ **Object Storage** - Store any type of binary data (JSON, XML, archives)  
✅ **Cloud-Native Apps** - Built-in redundancy and disaster recovery  
✅ **Media Management** - Images, videos, backups, analytics data  

### ⚠️ NOT for Structured Data
Blob Storage is for unstructured data. For relational queries, use **Entity Framework** or **Cosmos SQL**.

---

## Basic Configuration

### Simple Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobBuilder =>
    {
        blobBuilder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
        blobBuilder.Settings.Prefix = "documents/";  // Optional folder prefix
    });
});

var app = builder.Build();
```

### Understanding Configuration

**ConnectionString**: Azure Storage connection string (from Azure Portal or local development)

**Prefix**: Optional folder path within the container. Examples:
```csharp
blobBuilder.Settings.Prefix = "";                    // Root
blobBuilder.Settings.Prefix = "documents/";          // Specific folder
blobBuilder.Settings.Prefix = "2024/01/";            // Date-based organization
```

---

## Working with Blob Data

### Domain Model

```csharp
public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Complete Configuration

```csharp
services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    // Step 1: Configure Blob Storage
    repositoryBuilder.WithBlobStorage(blobBuilder =>
    {
        blobBuilder.Settings.ConnectionString = configuration["ConnectionString:Storage"];
        blobBuilder.Settings.Prefix = "documents/";
    });
    
    // Step 2: Add business logic interceptors (optional)
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<DocumentBeforeInsertBusiness>()
        .AddBusinessAfterInsert<DocumentAfterInsertBusiness>();
});

var app = builder.Build();
```

### Business Logic Interceptors

Example: Validate file size before insert

```csharp
public class DocumentBeforeInsertBusiness : IRepositoryBusiness<Document, Guid>
{
    public async ValueTask<Document?> BeforeInsertAsync(Document entity)
    {
        if (entity.Content?.Length > 100_000_000)  // 100MB limit
            throw new InvalidOperationException("File too large");
        
        return entity;
    }
}
```

See [IRepositoryBusiness](https://rystem.net/mcp/resources/background-jobs.md) documentation for all available lifecycle hooks.

---

## Using the Repository

### Inject and Use

```csharp
public class DocumentService(IRepository<Document, Guid> repository)
{
    public async Task UploadDocumentAsync(string fileName, byte[] content)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Content = content,
            ContentType = "application/pdf",
            CreatedAt = DateTime.UtcNow
        };
        
        // Upload to Blob Storage
        await repository.InsertAsync(document);
    }
    
    public async Task<Document?> GetDocumentAsync(Guid documentId)
    {
        return await repository.GetByKeyAsync(documentId);
    }
    
    public async Task DeleteDocumentAsync(Guid documentId)
    {
        await repository.DeleteAsync(documentId);
    }
}
```

---

## Advanced Patterns

### Organization by Prefix

```csharp
// Project documents
repositoryBuilder.WithBlobStorage(b =>
{
    b.Settings.Prefix = "projects/documents/";
});

// User profiles
repositoryBuilder.WithBlobStorage(b =>
{
    b.Settings.Prefix = "users/profiles/";
});

// Backups
repositoryBuilder.WithBlobStorage(b =>
{
    b.Settings.Prefix = "backups/daily/";
});
```

### Multiple Storage Accounts

```csharp
// Hot storage for frequent access
services.AddRepository<RecentDocument, Guid>(builder =>
{
    builder.WithBlobStorage(b =>
    {
        b.Settings.ConnectionString = configuration["Storage:Hot"];
        b.Settings.Prefix = "recent/";
    });
});

// Cold storage for archives
services.AddRepository<ArchivedDocument, Guid>(builder =>
{
    builder.WithBlobStorage(b =>
    {
        b.Settings.ConnectionString = configuration["Storage:Cold"];
        b.Settings.Prefix = "archived/";
    });
});
```

---

## Complete Example

```csharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Configure Blob Storage Repository
services.AddRepository<FileDocument, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobBuilder =>
    {
        blobBuilder.Settings.ConnectionString = configuration["ConnectionString:BlobStorage"];
        blobBuilder.Settings.Prefix = "files/";
    });
    
    // Add validation business logic
    repositoryBuilder.AddBusiness()
        .AddBusinessBeforeInsert<FileValidationBusiness>();
});

var app = builder.Build();

// Map API endpoints
app.MapPost("/upload", async (IRepository<FileDocument, Guid> repo, HttpRequest request) =>
{
    var stream = request.Body;
    var content = new byte[stream.Length];
    await stream.ReadExactlyAsync(content);
    
    var doc = new FileDocument
    {
        Id = Guid.NewGuid(),
        Content = content,
        FileName = request.Query["name"],
        CreatedAt = DateTime.UtcNow
    };
    
    await repo.InsertAsync(doc);
    return Results.Ok(doc.Id);
});

app.Run();
```

---

## Automated REST API

Expose your Blob Storage repository as a REST API:

```csharp
builder.Services.AddApiFromRepositoryFramework()
    .WithDescriptiveName("Blob Storage API")
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
- `GET /api/document` - List all documents
- `GET /api/document/{id}` - Download document
- `POST /api/document` - Upload document
- `PUT /api/document/{id}` - Replace document
- `DELETE /api/document/{id}` - Delete document

See [Repository API Server Documentation](https://rystem.net/mcp/tools/repository-api-server.md) for advanced configuration.

---

## 💡 Best Practices

✅ Use **meaningful prefixes** to organize data logically  
✅ Implement **business validation** to prevent oversized uploads  
✅ Use **CDN** for frequently accessed files  
✅ Monitor **storage costs** for large datasets  
✅ Implement **access control** via Azure Security

---

## References

- [Repository Pattern Documentation](https://rystem.net/mcp/tools/repository-setup.md)
- [Content Repository Pattern](https://rystem.net/mcp/tools/content-repository-blob.md)
- [Azure Blob Storage Docs](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)
