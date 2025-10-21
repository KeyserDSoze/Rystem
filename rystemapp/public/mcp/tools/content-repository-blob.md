# Content Repository - Azure Blob Storage Integration

Store files in **Azure Blob Storage** using the unified Content Repository interface.

**Best For:**
- Large files (videos, images, backups)
- CDN integration for public access
- Scalable cloud storage
- Static website hosting

---

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Storage.Blob --version 9.1.3
```

---

## Configuration

### Basic Setup

```csharp
await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "my-container";
        x.ConnectionString = configuration["ConnectionString:Storage"];
    }, "blobstorage")
    .NoContext();
```

### With Prefix

Add a **prefix** to all file paths (like a virtual folder):

```csharp
await services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "documents";
        x.Prefix = "uploads/2024/";  // All files will be under this prefix
        x.ConnectionString = configuration["ConnectionString:Storage"];
    }, "blobstorage")
    .NoContext();
```

---

## Usage

### Single Integration

```csharp
public class FileService
{
    private readonly IContentRepository _contentRepository;
    
    public FileService(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }
    
    public async Task UploadImageAsync(string imageName, byte[] imageData)
    {
        await _contentRepository.UploadAsync(
            $"images/{imageName}",
            imageData,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "image/png"
                }
            }
        );
    }
}
```

### Multiple Integrations

```csharp
public class FileService
{
    private readonly IContentRepository _blobStorage;
    
    public FileService(IContentRepositoryFactory factory)
    {
        _blobStorage = factory.Create("blobstorage");
    }
}
```

---

## Complete Example

```csharp
public class BlobStorageTest
{
    private readonly IContentRepositoryFactory _factory;
    
    public BlobStorageTest(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ExecuteAsync()
    {
        var _contentRepository = _factory.Create("blobstorage");
        
        // Read file
        var file = await File.ReadAllBytesAsync("document.pdf");
        var name = "folder/document.pdf";
        var contentType = "application/pdf";
        
        var metadata = new Dictionary<string, string>
        {
            { "uploadedBy", "john.doe" },
            { "department", "sales" }
        };
        
        var tags = new Dictionary<string, string>
        {
            { "version", "1.0" },
            { "status", "published" }
        };
        
        // Check if exists
        var exists = await _contentRepository.ExistAsync(name);
        if (exists)
        {
            await _contentRepository.DeleteAsync(name);
        }
        
        // Upload file
        var uploaded = await _contentRepository.UploadAsync(name, file, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = contentType
            },
            Metadata = metadata,
            Tags = tags
        }, overwrite: true);
        
        Assert.True(uploaded);
        
        // Verify exists
        exists = await _contentRepository.ExistAsync(name);
        Assert.True(exists);
        
        // Get properties
        var properties = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All);
        
        Assert.NotNull(properties.Uri);
        Assert.Equal(contentType, properties.Options.HttpHeaders.ContentType);
        
        foreach (var kvp in metadata)
        {
            Assert.Equal(kvp.Value, properties.Options.Metadata[kvp.Key]);
        }
        
        foreach (var kvp in tags)
        {
            Assert.Equal(kvp.Value, properties.Options.Tags[kvp.Key]);
        }
        
        // Update metadata
        metadata.Add("modifiedBy", "jane.smith");
        
        var updated = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
        {
            Metadata = metadata
        });
        
        Assert.True(updated);
        
        // Verify update
        properties = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All);
        Assert.Equal("jane.smith", properties.Options.Metadata["modifiedBy"]);
        
        // Download file
        var downloaded = await _contentRepository.DownloadAsync(name);
        Assert.NotNull(downloaded);
        Assert.Equal(file.Length, downloaded.Data.Length);
        
        // Delete file
        var deleted = await _contentRepository.DeleteAsync(name);
        Assert.True(deleted);
        
        // Verify deleted
        exists = await _contentRepository.ExistAsync(name);
        Assert.False(exists);
    }
}
```

---

## Real-World Examples

### Image Upload API

```csharp
[HttpPost("upload/image")]
public async Task<IActionResult> UploadImage(IFormFile file)
{
    if (!file.ContentType.StartsWith("image/"))
        return BadRequest("Only images allowed");
    
    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    
    var fileName = $"images/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    
    var success = await _contentRepository.UploadAsync(
        fileName,
        memoryStream.ToArray(),
        new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = file.ContentType,
                CacheControl = "public, max-age=31536000" // 1 year
            },
            Metadata = new Dictionary<string, string>
            {
                { "originalFileName", file.FileName },
                { "uploadedBy", User.Identity?.Name ?? "anonymous" }
            }
        }
    );
    
    if (!success)
        return StatusCode(500);
    
    var properties = await _contentRepository.GetPropertiesAsync(fileName);
    
    return Ok(new 
    { 
        fileName, 
        url = properties?.Uri?.ToString() 
    });
}
```

### Backup Service

```csharp
public class BackupService
{
    private readonly IContentRepository _blobStorage;
    private readonly ILogger<BackupService> _logger;
    
    public BackupService(IContentRepositoryFactory factory, ILogger<BackupService> logger)
    {
        _blobStorage = factory.Create("backups");
        _logger = logger;
    }
    
    public async Task BackupDatabaseAsync(string databaseName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var backupPath = $"backups/{databaseName}/{timestamp}.bak";
        
        // Generate backup (pseudo-code)
        byte[] backupData = await GenerateBackupAsync(databaseName);
        
        var success = await _blobStorage.UploadAsync(
            backupPath,
            backupData,
            new ContentRepositoryOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "databaseName", databaseName },
                    { "backupDate", timestamp },
                    { "size", backupData.Length.ToString() }
                },
                Tags = new Dictionary<string, string>
                {
                    { "type", "database-backup" },
                    { "retention", "30-days" }
                }
            }
        );
        
        if (success)
            _logger.LogInformation("Backup completed: {Path}", backupPath);
        else
            _logger.LogError("Backup failed: {Path}", backupPath);
    }
}
```

### CDN File Delivery

```csharp
public class CdnService
{
    private readonly IContentRepository _cdn;
    
    public CdnService(IContentRepositoryFactory factory)
    {
        _cdn = factory.Create("cdn");
    }
    
    public async Task<string> PublishAssetAsync(string assetName, byte[] content, string contentType)
    {
        var path = $"assets/{assetName}";
        
        await _cdn.UploadAsync(path, content, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders
            {
                ContentType = contentType,
                CacheControl = "public, max-age=31536000, immutable"
            },
            Tags = new Dictionary<string, string>
            {
                { "environment", "production" },
                { "publishedAt", DateTime.UtcNow.ToString("o") }
            }
        });
        
        var properties = await _cdn.GetPropertiesAsync(path);
        
        return properties?.Uri?.ToString() ?? string.Empty;
    }
}
```

---

## Configuration Options

```csharp
public class BlobStorageOptions
{
    public string ContainerName { get; set; }      // Required: Blob container name
    public string Prefix { get; set; }             // Optional: Path prefix for all files
    public string ConnectionString { get; set; }   // Required: Azure Storage connection string
}
```

---

## Benefits

- ✅ **Scalable**: Handles petabytes of data
- ✅ **CDN Integration**: Built-in Azure CDN support
- ✅ **Cost-Effective**: Pay for what you use
- ✅ **Metadata & Tags**: Rich file properties
- ✅ **Access Tiers**: Hot, Cool, Archive
- ✅ **Public Access**: Optional anonymous access

---

## Related Tools

- **[Content Repository Pattern](https://rystem.net/mcp/tools/content-repository.md)** - Main documentation
- **[Azure File Storage](https://rystem.net/mcp/tools/content-repository-file.md)** - File storage integration
- **[SharePoint](https://rystem.net/mcp/tools/content-repository-sharepoint.md)** - SharePoint integration

---

## References

- **NuGet Package**: [Rystem.Content.Infrastructure.Storage.Blob](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Storage.Blob) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **Azure Docs**: https://learn.microsoft.com/azure/storage/blobs/
