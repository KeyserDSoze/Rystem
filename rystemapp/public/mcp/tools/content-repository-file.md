# Content Repository - Azure File Storage Integration

Store files in **Azure File Storage** using the unified Content Repository interface.

**Best For:**
- SMB/CIFS file shares
- Legacy application migration
- Enterprise file sharing
- Lift-and-shift scenarios

---

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.Storage.File --version 9.1.3
```

---

## Configuration

### Basic Setup

```csharp
await services
    .AddContentRepository()
    .WithFileStorageIntegrationAsync(x =>
    {
        x.ShareName = "my-share";
        x.ConnectionString = configuration["ConnectionString:Storage"];
    }, "filestorage")
    .NoContext();
```

### With Prefix

Add a **prefix** to all file paths:

```csharp
await services
    .AddContentRepository()
    .WithFileStorageIntegrationAsync(x =>
    {
        x.ShareName = "company-files";
        x.Prefix = "departments/sales/";
        x.ConnectionString = configuration["ConnectionString:Storage"];
    }, "filestorage")
    .NoContext();
```

---

## Usage

### Single Integration

```csharp
public class DocumentService
{
    private readonly IContentRepository _contentRepository;
    
    public DocumentService(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }
    
    public async Task SaveDocumentAsync(string documentName, byte[] data)
    {
        await _contentRepository.UploadAsync(
            $"documents/{documentName}",
            data,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "application/pdf"
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
    private readonly IContentRepository _fileStorage;
    
    public FileService(IContentRepositoryFactory factory)
    {
        _fileStorage = factory.Create("filestorage");
    }
}
```

---

## Complete Example

```csharp
public class FileStorageTest
{
    private readonly IContentRepositoryFactory _factory;
    
    public FileStorageTest(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ExecuteAsync()
    {
        var _contentRepository = _factory.Create("filestorage");
        
        // Read file
        var file = await File.ReadAllBytesAsync("report.xlsx");
        var name = "reports/2024/Q1/report.xlsx";
        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        var metadata = new Dictionary<string, string>
        {
            { "department", "finance" },
            { "quarter", "Q1" },
            { "year", "2024" }
        };
        
        var tags = new Dictionary<string, string>
        {
            { "reportType", "quarterly" },
            { "status", "final" }
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
        metadata.Add("reviewed", "true");
        
        var updated = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
        {
            Metadata = metadata
        });
        
        Assert.True(updated);
        
        // Verify update
        properties = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All);
        Assert.Equal("true", properties.Options.Metadata["reviewed"]);
        
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

### Department File Sharing

```csharp
public class DepartmentFileService
{
    private readonly IContentRepository _fileShare;
    private readonly ILogger<DepartmentFileService> _logger;
    
    public DepartmentFileService(IContentRepositoryFactory factory, ILogger<DepartmentFileService> logger)
    {
        _fileShare = factory.Create("departments");
        _logger = logger;
    }
    
    public async Task ShareFileWithDepartmentAsync(string department, string fileName, byte[] fileData)
    {
        var path = $"{department}/shared/{fileName}";
        
        var success = await _fileShare.UploadAsync(
            path,
            fileData,
            new ContentRepositoryOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "department", department },
                    { "sharedBy", "admin" },
                    { "sharedDate", DateTime.UtcNow.ToString("o") }
                },
                Tags = new Dictionary<string, string>
                {
                    { "access", "department-wide" }
                }
            }
        );
        
        if (success)
            _logger.LogInformation("File shared with {Department}: {FileName}", department, fileName);
    }
    
    public async Task<List<string>> ListDepartmentFilesAsync(string department)
    {
        var files = new List<string>();
        
        await foreach (var file in _fileShare.ListAsync(
            prefix: $"{department}/",
            downloadContent: false
        ))
        {
            files.Add(file.Path);
        }
        
        return files;
    }
}
```

### Legacy Application Migration

```csharp
public class LegacyFileService
{
    private readonly IContentRepository _fileStorage;
    
    public LegacyFileService(IContentRepositoryFactory factory)
    {
        _fileStorage = factory.Create("legacy-files");
    }
    
    public async Task MigrateLegacyFileAsync(string sourcePath)
    {
        // Read from local file system (legacy)
        var fileData = await File.ReadAllBytesAsync(sourcePath);
        var fileName = Path.GetFileName(sourcePath);
        
        // Upload to Azure File Storage
        var cloudPath = $"migrated/{fileName}";
        
        await _fileStorage.UploadAsync(
            cloudPath,
            fileData,
            new ContentRepositoryOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "sourcePath", sourcePath },
                    { "migratedAt", DateTime.UtcNow.ToString("o") },
                    { "originalSize", fileData.Length.ToString() }
                },
                Tags = new Dictionary<string, string>
                {
                    { "migration", "legacy-to-cloud" }
                }
            }
        );
    }
}
```

### Report Archive

```csharp
public class ReportArchiveService
{
    private readonly IContentRepository _archive;
    
    public ReportArchiveService(IContentRepositoryFactory factory)
    {
        _archive = factory.Create("report-archive");
    }
    
    public async Task ArchiveReportAsync(string reportType, int year, int month, byte[] reportData)
    {
        var path = $"{reportType}/{year}/{month:D2}/report.pdf";
        
        await _archive.UploadAsync(
            path,
            reportData,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "application/pdf"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "reportType", reportType },
                    { "year", year.ToString() },
                    { "month", month.ToString() },
                    { "archivedAt", DateTime.UtcNow.ToString("o") }
                }
            }
        );
    }
    
    public async Task<byte[]?> RetrieveReportAsync(string reportType, int year, int month)
    {
        var path = $"{reportType}/{year}/{month:D2}/report.pdf";
        
        var result = await _archive.DownloadAsync(path);
        
        return result?.Data;
    }
}
```

---

## Configuration Options

```csharp
public class FileStorageOptions
{
    public string ShareName { get; set; }          // Required: File share name
    public string Prefix { get; set; }             // Optional: Path prefix for all files
    public string ConnectionString { get; set; }   // Required: Azure Storage connection string
}
```

---

## Benefits

- ✅ **SMB Compatible**: Mount as network drive
- ✅ **Enterprise Ready**: AD/LDAP integration
- ✅ **Lift-and-Shift**: Easy migration from on-premises
- ✅ **Metadata Support**: Rich file properties
- ✅ **Hierarchical**: Folder structure support
- ✅ **Cross-Platform**: Windows, Linux, macOS

---

## Related Tools

- **[Content Repository Pattern](https://rystem.net/mcp/tools/content-repository.md)** - Main documentation
- **[Azure Blob Storage](https://rystem.net/mcp/tools/content-repository-blob.md)** - Blob storage integration
- **[SharePoint](https://rystem.net/mcp/tools/content-repository-sharepoint.md)** - SharePoint integration

---

## References

- **NuGet Package**: [Rystem.Content.Infrastructure.Storage.File](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Storage.File) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **Azure Docs**: https://learn.microsoft.com/azure/storage/files/
