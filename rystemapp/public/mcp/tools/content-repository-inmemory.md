# Content Repository - In-Memory Storage Integration

Store files **in memory** using the unified Content Repository interface.

**Best For:**
- Unit testing
- Development/debugging
- Temporary caching
- CI/CD pipelines
- No external dependencies

---

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.InMemory --version 9.1.3
```

---

## Configuration

### Basic Setup

```csharp
services
    .AddContentRepository()
    .WithInMemoryIntegration("inmemory");
```

**Note:** No connection string or configuration needed! Files are stored in RAM.

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
    
    public async Task CacheFileAsync(string fileName, byte[] data)
    {
        await _contentRepository.UploadAsync(fileName, data);
    }
}
```

### Multiple Integrations

```csharp
public class FileService
{
    private readonly IContentRepository _cache;
    
    public FileService(IContentRepositoryFactory factory)
    {
        _cache = factory.Create("inmemory");
    }
}
```

---

## Complete Example

```csharp
public class InMemoryTest
{
    private readonly IContentRepositoryFactory _factory;
    
    public InMemoryTest(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ExecuteAsync()
    {
        var _contentRepository = _factory.Create("inmemory");
        
        // Create test data
        var file = new byte[] { 1, 2, 3, 4, 5 };
        var name = "test-file.bin";
        var contentType = "application/octet-stream";
        
        var metadata = new Dictionary<string, string>
        {
            { "test", "value" },
            { "created", DateTime.UtcNow.ToString("o") }
        };
        
        var tags = new Dictionary<string, string>
        {
            { "environment", "test" },
            { "temporary", "true" }
        };
        
        // Check if exists (should be false initially)
        var exists = await _contentRepository.ExistAsync(name);
        Assert.False(exists);
        
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
        metadata.Add("updated", "true");
        
        var updated = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
        {
            Metadata = metadata
        });
        
        Assert.True(updated);
        
        // Verify update
        properties = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All);
        Assert.Equal("true", properties.Options.Metadata["updated"]);
        
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

### Unit Testing

```csharp
public class FileServiceTests
{
    private readonly IContentRepository _contentRepository;
    
    public FileServiceTests()
    {
        var services = new ServiceCollection();
        services
            .AddContentRepository()
            .WithInMemoryIntegration("test");
        
        var provider = services.BuildServiceProvider();
        _contentRepository = provider.GetRequiredService<IContentRepository>();
    }
    
    [Fact]
    public async Task UploadFile_ShouldStoreInMemory()
    {
        // Arrange
        var fileName = "test.txt";
        var fileData = Encoding.UTF8.GetBytes("Hello, World!");
        
        // Act
        var success = await _contentRepository.UploadAsync(fileName, fileData);
        
        // Assert
        Assert.True(success);
        
        var exists = await _contentRepository.ExistAsync(fileName);
        Assert.True(exists);
        
        var downloaded = await _contentRepository.DownloadAsync(fileName);
        Assert.Equal(fileData, downloaded.Data);
    }
    
    [Fact]
    public async Task DeleteFile_ShouldRemoveFromMemory()
    {
        // Arrange
        var fileName = "test.txt";
        var fileData = Encoding.UTF8.GetBytes("Test");
        await _contentRepository.UploadAsync(fileName, fileData);
        
        // Act
        var deleted = await _contentRepository.DeleteAsync(fileName);
        
        // Assert
        Assert.True(deleted);
        
        var exists = await _contentRepository.ExistAsync(fileName);
        Assert.False(exists);
    }
}
```

### Temporary File Cache

```csharp
public class FileCacheService
{
    private readonly IContentRepository _cache;
    private readonly IContentRepository _permanent;
    
    public FileCacheService(IContentRepositoryFactory factory)
    {
        _cache = factory.Create("inmemory");       // Fast in-memory cache
        _permanent = factory.Create("blobstorage"); // Permanent Azure Blob
    }
    
    public async Task<byte[]> GetFileAsync(string fileName)
    {
        // Try cache first
        var cached = await _cache.DownloadAsync(fileName);
        if (cached != null)
        {
            return cached.Data;
        }
        
        // Load from permanent storage
        var permanent = await _permanent.DownloadAsync(fileName);
        if (permanent != null)
        {
            // Cache for next time
            await _cache.UploadAsync(fileName, permanent.Data);
            return permanent.Data;
        }
        
        return Array.Empty<byte>();
    }
    
    public async Task SaveFileAsync(string fileName, byte[] data)
    {
        // Save to permanent storage
        await _permanent.UploadAsync(fileName, data);
        
        // Also cache
        await _cache.UploadAsync(fileName, data);
    }
}
```

### Development Mock

```csharp
public class DevelopmentFileService
{
    private readonly IContentRepository _files;
    
    public DevelopmentFileService(IContentRepositoryFactory factory, IHostEnvironment env)
    {
        // Use in-memory in Development, Azure Blob in Production
        _files = env.IsDevelopment() 
            ? factory.Create("inmemory")
            : factory.Create("production-blob");
    }
    
    public async Task UploadUserAvatarAsync(Guid userId, byte[] imageData)
    {
        await _files.UploadAsync(
            $"avatars/{userId}.jpg",
            imageData,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "image/jpeg"
                }
            }
        );
    }
}

// Startup configuration
if (builder.Environment.IsDevelopment())
{
    services
        .AddContentRepository()
        .WithInMemoryIntegration("inmemory");
}
else
{
    await services
        .AddContentRepository()
        .WithBlobStorageIntegrationAsync(x =>
        {
            x.ContainerName = "user-avatars";
            x.ConnectionString = configuration["Azure:Storage"];
        }, "production-blob")
        .NoContext();
}
```

### CI/CD Pipeline Testing

```csharp
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real storage with in-memory for tests
                services
                    .AddContentRepository()
                    .WithInMemoryIntegration("test-storage");
            });
        });
    }
    
    [Fact]
    public async Task UploadFile_ShouldSucceed()
    {
        var client = _factory.CreateClient();
        
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.bin");
        
        var response = await client.PostAsync("/api/files/upload", content);
        
        response.EnsureSuccessStatusCode();
    }
}
```

### Session Storage

```csharp
public class SessionFileService
{
    private readonly IContentRepository _sessionStorage;
    private readonly IHttpContextAccessor _httpContext;
    
    public SessionFileService(IContentRepositoryFactory factory, IHttpContextAccessor httpContext)
    {
        _sessionStorage = factory.Create("session");
        _httpContext = httpContext;
    }
    
    public async Task SaveSessionFileAsync(string fileName, byte[] data)
    {
        var sessionId = _httpContext.HttpContext?.Session.Id;
        var path = $"{sessionId}/{fileName}";
        
        await _sessionStorage.UploadAsync(
            path,
            data,
            new ContentRepositoryOptions
            {
                Tags = new Dictionary<string, string>
                {
                    { "expiresAt", DateTime.UtcNow.AddHours(1).ToString("o") }
                }
            }
        );
    }
    
    public async Task<byte[]?> GetSessionFileAsync(string fileName)
    {
        var sessionId = _httpContext.HttpContext?.Session.Id;
        var path = $"{sessionId}/{fileName}";
        
        var result = await _sessionStorage.DownloadAsync(path);
        return result?.Data;
    }
}
```

---

## Benefits

- ✅ **No Dependencies**: Works without Azure, AWS, or any external service
- ✅ **Fast**: Files stored in RAM for instant access
- ✅ **Simple Setup**: No configuration needed
- ✅ **Perfect for Tests**: No cleanup, fresh state each test run
- ✅ **Development**: No costs, no credentials
- ✅ **CI/CD**: Works in any environment

---

## Limitations

- ⚠️ **Not Persistent**: Data lost when application restarts
- ⚠️ **Memory Usage**: Large files consume RAM
- ⚠️ **Single Instance**: Not shared across app instances
- ⚠️ **Production**: Not suitable for production use

---

## Related Tools

- **[Content Repository Pattern](https://rystem.net/mcp/tools/content-repository.md)** - Main documentation
- **[Azure Blob Storage](https://rystem.net/mcp/tools/content-repository-blob.md)** - Production storage
- **[Azure File Storage](https://rystem.net/mcp/tools/content-repository-file.md)** - File share storage

---

## References

- **NuGet Package**: [Rystem.Content.Infrastructure.InMemory](https://www.nuget.org/packages/Rystem.Content.Infrastructure.InMemory) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
