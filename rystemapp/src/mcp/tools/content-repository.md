---
title: Content Repository Pattern
description: Unified interface for file storage across multiple providers - upload, download, delete files with metadata and tags support for Azure Blob Storage, Azure File Storage, SharePoint, and In-Memory storage
---

# Content Repository Pattern

**Unified interface** for managing **file storage** across multiple storage providers (Azure Blob Storage, Azure File Storage, SharePoint, In-Memory).

---

## What is Content Repository?

Content Repository provides a **consistent API** for file operations regardless of the underlying storage provider. Switch between Azure Blob, SharePoint, or local file storage without changing your business logic.

**Key Features:**
- ✅ Upload, download, delete files
- ✅ Metadata and tags support
- ✅ Multiple storage providers
- ✅ Migration tool between providers
- ✅ Factory pattern for multiple integrations

---

## Installation

```bash
# Core package
dotnet add package Rystem.Content.Abstractions --version 9.1.3

# Choose your integration(s)
dotnet add package Rystem.Content.Infrastructure.Storage.Blob --version 9.1.3
dotnet add package Rystem.Content.Infrastructure.Storage.File --version 9.1.3
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint --version 9.1.3
dotnet add package Rystem.Content.Infrastructure.InMemory --version 9.1.3
```

---

## Quick Start

### 1. Register Single Integration

```csharp
services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");
```

### 2. Use in Business Class

```csharp
public class FileService
{
    private readonly IContentRepository _contentRepository;
    
    public FileService(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }
    
    public async Task UploadFileAsync(string fileName, byte[] data)
    {
        await _contentRepository.UploadAsync(fileName, data);
    }
    
    public async Task<byte[]> DownloadFileAsync(string fileName)
    {
        var result = await _contentRepository.DownloadAsync(fileName);
        return result?.Data ?? Array.Empty<byte>();
    }
}
```

---

## Core Operations

### Upload File

```csharp
var data = File.ReadAllBytes("document.pdf");

var success = await _contentRepository.UploadAsync(
    path: "folder/document.pdf",
    data: data,
    options: new ContentRepositoryOptions
    {
        HttpHeaders = new ContentRepositoryHttpHeaders
        {
            ContentType = "application/pdf"
        },
        Metadata = new Dictionary<string, string>
        {
            { "uploadedBy", "user123" },
            { "department", "sales" }
        },
        Tags = new Dictionary<string, string>
        {
            { "version", "1.0" },
            { "status", "draft" }
        }
    },
    overwrite: true
);
```

### Download File

```csharp
var result = await _contentRepository.DownloadAsync(
    path: "folder/document.pdf",
    informationRetrieve: ContentInformationType.All
);

if (result != null)
{
    byte[] fileData = result.Data;
    string contentType = result.Options.HttpHeaders.ContentType;
    var metadata = result.Options.Metadata;
    var tags = result.Options.Tags;
}
```

### Check if File Exists

```csharp
bool exists = await _contentRepository.ExistAsync("folder/document.pdf");
```

### Get File Properties

```csharp
var properties = await _contentRepository.GetPropertiesAsync(
    path: "folder/document.pdf",
    informationRetrieve: ContentInformationType.All
);

if (properties != null)
{
    Uri fileUri = properties.Uri;
    var metadata = properties.Options.Metadata;
    var tags = properties.Options.Tags;
    string contentType = properties.Options.HttpHeaders.ContentType;
}
```

### Update File Properties

```csharp
var success = await _contentRepository.SetPropertiesAsync(
    path: "folder/document.pdf",
    options: new ContentRepositoryOptions
    {
        Metadata = new Dictionary<string, string>
        {
            { "status", "published" },
            { "publishedDate", DateTime.UtcNow.ToString("o") }
        }
    }
);
```

### Delete File

```csharp
var success = await _contentRepository.DeleteAsync("folder/document.pdf");
```

### List Files

```csharp
await foreach (var file in _contentRepository.ListAsync(
    prefix: "folder/",
    downloadContent: false,
    informationRetrieve: ContentInformationType.All
))
{
    Console.WriteLine($"File: {file.Path}");
    Console.WriteLine($"Size: {file.Data?.Length ?? 0} bytes");
    Console.WriteLine($"Content-Type: {file.Options.HttpHeaders.ContentType}");
}
```

---

## Multiple Integrations (Factory Pattern)

When you need **multiple storage providers** in the same application:

### Registration

```csharp
await services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory")
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "documents";
        x.ConnectionString = configuration["ConnectionString:Storage"];
    }, "azure")
    .NoContext();
```

### Usage with Factory

```csharp
public class FileService
{
    private readonly IContentRepository _memoryStorage;
    private readonly IContentRepository _azureStorage;
    
    public FileService(IContentRepositoryFactory factory)
    {
        _memoryStorage = factory.Create("inmemory");
        _azureStorage = factory.Create("azure");
    }
    
    public async Task CacheFileAsync(string fileName, byte[] data)
    {
        // Store in memory for fast access
        await _memoryStorage.UploadAsync(fileName, data);
        
        // Also persist to Azure
        await _azureStorage.UploadAsync(fileName, data);
    }
}
```

---

## Migration Tool

Migrate files **between storage providers** with filtering and path modification:

### Setup Multiple Providers

```csharp
await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "old-storage";
        x.ConnectionString = configuration["ConnectionString:OldStorage"];
    }, "source")
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithSiteNameAndDocumentLibraryName("CompanySite", "Documents");
    }, "destination")
    .NoContext();
```

### Migrate Files

```csharp
public class MigrationService
{
    private readonly IContentMigration _contentMigration;
    
    public MigrationService(IContentMigration contentMigration)
    {
        _contentMigration = contentMigration;
    }
    
    public async Task MigrateToSharepointAsync()
    {
        await _contentMigration.MigrateAsync(
            sourceKey: "source",
            destinationKey: "destination",
            settings: config =>
            {
                // Overwrite existing files
                config.OverwriteIfExists = true;
                
                // Only migrate files in specific folder
                config.Prefix = "documents/2024/";
                
                // Filter files
                config.Predicate = file =>
                {
                    // Skip files larger than 10MB
                    return file.Data?.Length < 10 * 1024 * 1024;
                };
                
                // Modify destination path
                config.ModifyDestinationPath = path =>
                {
                    // Rename folder structure
                    return path.Replace("documents/2024/", "archive/2024/");
                };
            }
        );
    }
}
```

---

## ContentInformationType

Control what information is retrieved:

```csharp
public enum ContentInformationType
{
    None,           // No metadata/tags
    Metadata,       // Only metadata
    Tags,           // Only tags
    All             // Metadata + tags
}
```

**Example:**

```csharp
// Get only file data (fastest)
var result = await _contentRepository.DownloadAsync(
    "file.pdf", 
    ContentInformationType.None
);

// Get file data + metadata
var result = await _contentRepository.DownloadAsync(
    "file.pdf", 
    ContentInformationType.Metadata
);

// Get everything
var result = await _contentRepository.DownloadAsync(
    "file.pdf", 
    ContentInformationType.All
);
```

---

## Real-World Examples

### File Upload API

```csharp
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IContentRepository _contentRepository;
    
    public FilesController(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }
    
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string category)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        
        var fileName = $"{category}/{Guid.NewGuid()}_{file.FileName}";
        
        var success = await _contentRepository.UploadAsync(
            fileName,
            memoryStream.ToArray(),
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = file.ContentType
                },
                Metadata = new Dictionary<string, string>
                {
                    { "originalFileName", file.FileName },
                    { "uploadedBy", User.Identity?.Name ?? "anonymous" },
                    { "uploadedAt", DateTime.UtcNow.ToString("o") }
                }
            }
        );
        
        return success ? Ok(new { fileName }) : StatusCode(500);
    }
    
    [HttpGet("download/{*path}")]
    public async Task<IActionResult> DownloadFile(string path)
    {
        var result = await _contentRepository.DownloadAsync(path, ContentInformationType.All);
        
        if (result == null)
            return NotFound();
        
        return File(result.Data, result.Options.HttpHeaders.ContentType ?? "application/octet-stream");
    }
}
```

### Document Management System

```csharp
public class DocumentService
{
    private readonly IContentRepository _contentRepository;
    private readonly ILogger<DocumentService> _logger;
    
    public DocumentService(IContentRepository contentRepository, ILogger<DocumentService> logger)
    {
        _contentRepository = contentRepository;
        _logger = logger;
    }
    
    public async Task<Guid> CreateDocumentAsync(string category, byte[] content, string fileName)
    {
        var documentId = Guid.NewGuid();
        var path = $"{category}/{documentId}/{fileName}";
        
        var success = await _contentRepository.UploadAsync(path, content, new ContentRepositoryOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "documentId", documentId.ToString() },
                { "category", category },
                { "originalFileName", fileName },
                { "createdAt", DateTime.UtcNow.ToString("o") }
            },
            Tags = new Dictionary<string, string>
            {
                { "version", "1" },
                { "status", "draft" }
            }
        });
        
        if (!success)
            throw new Exception("Failed to upload document");
        
        _logger.LogInformation("Document {DocumentId} created", documentId);
        
        return documentId;
    }
    
    public async Task<List<DocumentInfo>> ListDocumentsByCategoryAsync(string category)
    {
        var documents = new List<DocumentInfo>();
        
        await foreach (var file in _contentRepository.ListAsync(
            prefix: $"{category}/",
            downloadContent: false,
            informationRetrieve: ContentInformationType.All
        ))
        {
            documents.Add(new DocumentInfo
            {
                DocumentId = Guid.Parse(file.Options.Metadata["documentId"]),
                FileName = file.Options.Metadata["originalFileName"],
                Path = file.Path,
                Status = file.Options.Tags.GetValueOrDefault("status", "unknown"),
                CreatedAt = DateTime.Parse(file.Options.Metadata["createdAt"])
            });
        }
        
        return documents;
    }
}
```

### Multi-Tenant File Storage

```csharp
public class TenantFileService
{
    private readonly IContentRepositoryFactory _factory;
    
    public TenantFileService(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task UploadTenantFileAsync(Guid tenantId, string fileName, byte[] data)
    {
        // Each tenant has its own storage
        var repository = _factory.Create($"tenant-{tenantId}");
        
        await repository.UploadAsync(fileName, data, new ContentRepositoryOptions
        {
            Metadata = new Dictionary<string, string>
            {
                { "tenantId", tenantId.ToString() }
            }
        });
    }
}

// Registration
services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "tenant-a-files";
        x.ConnectionString = configuration["Storage:TenantA"];
    }, "tenant-a")
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "tenant-b-files";
        x.ConnectionString = configuration["Storage:TenantB"];
    }, "tenant-b");
```

---

## Available Integrations

| Integration | Package | Use Case |
|------------|---------|----------|
| **Azure Blob Storage** | `Rystem.Content.Infrastructure.Storage.Blob` | Large files, CDN, public access |
| **Azure File Storage** | `Rystem.Content.Infrastructure.Storage.File` | SMB shares, legacy apps |
| **SharePoint Online** | `Rystem.Content.Infrastructure.M365.Sharepoint` | Document collaboration, Office 365 |
| **In-Memory** | `Rystem.Content.Infrastructure.InMemory` | Testing, caching |

---

## Benefits

- ✅ **Provider Agnostic**: Switch storage providers without code changes
- ✅ **Unified API**: Same interface for all providers
- ✅ **Metadata & Tags**: Store custom properties
- ✅ **Migration**: Built-in tool for moving files
- ✅ **Factory Pattern**: Multiple providers in one app
- ✅ **Testing**: In-memory provider for unit tests

---

## Related Tools

- **[Azure Blob Storage Integration](https://rystem.net/mcp/tools/content-repository-blob.md)** - Blob storage setup
- **[Azure File Storage Integration](https://rystem.net/mcp/tools/content-repository-file.md)** - File storage setup
- **[SharePoint Integration](https://rystem.net/mcp/tools/content-repository-sharepoint.md)** - SharePoint setup
- **[In-Memory Integration](https://rystem.net/mcp/tools/content-repository-inmemory.md)** - In-memory setup
- **[Text Extensions](https://rystem.net/mcp/tools/rystem-text-extensions.md)** - Stream/byte utilities

---

## References

- **NuGet Packages**: 
  - [Rystem.Content.Abstractions](https://www.nuget.org/packages/Rystem.Content.Abstractions) v9.1.3
  - [Rystem.Content.Infrastructure.*](https://www.nuget.org/packages?q=Rystem.Content.Infrastructure) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
