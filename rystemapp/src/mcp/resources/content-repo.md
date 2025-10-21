---
title: Content Repository
description: Upload, download, and manage files with Rystem.Content - unified interface for Azure Blob Storage, File System, and SharePoint
---

# Content Repository

**Purpose**: This resource explains how to upload, download, and manage files using Rystem Content Repository.

---

## Overview

Rystem Content Repository provides a unified interface for file storage across different backends including Azure Blob Storage, File System, and SharePoint.

---

## Installation

```bash
# Core abstractions
dotnet add package Rystem.Content.Abstractions

# Choose your storage backend
dotnet add package Rystem.Content.Infrastructure.Storage.Blob
dotnet add package Rystem.Content.Infrastructure.Storage.File
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint
dotnet add package Rystem.Content.Infrastructure.InMemory
```

## Configuration

### Azure Blob Storage
```csharp
builder.Services.AddContentRepository(content =>
{
    content.WithAzureBlobStorage(options =>
    {
        options.ConnectionString = builder.Configuration["Azure:Storage:ConnectionString"];
        options.ContainerName = "documents";
    });
});
```

### File System
```csharp
builder.Services.AddContentRepository(content =>
{
    content.WithFileSystem(options =>
    {
        options.BasePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    });
});
```

### SharePoint
```csharp
builder.Services.AddContentRepository(content =>
{
    content.WithSharePoint(options =>
    {
        options.SiteUrl = "https://yourtenant.sharepoint.com/sites/yoursite";
        options.ClientId = builder.Configuration["SharePoint:ClientId"];
        options.ClientSecret = builder.Configuration["SharePoint:ClientSecret"];
    });
});
```

## Usage

### Upload a File
```csharp
public class DocumentService
{
    private readonly IContentRepository _contentRepository;

    public DocumentService(IContentRepository contentRepository)
    {
        _contentRepository = contentRepository;
    }

    public async Task<string> UploadDocumentAsync(
        Stream fileStream, 
        string fileName, 
        string contentType)
    {
        var result = await _contentRepository.UploadAsync(
            fileStream,
            fileName,
            new ContentMetadata
            {
                ContentType = contentType,
                Tags = new[] { "document", "user-uploaded" }
            });

        return result.Uri;
    }
}
```

### Download a File
```csharp
public async Task<Stream> DownloadDocumentAsync(string uri)
{
    var content = await _contentRepository.DownloadAsync(uri);
    return content.Stream;
}
```

### List Files
```csharp
public async Task<IEnumerable<ContentInfo>> ListDocumentsAsync(string folder)
{
    var files = await _contentRepository.ListAsync(folder);
    return files;
}
```

### Delete a File
```csharp
public async Task DeleteDocumentAsync(string uri)
{
    await _contentRepository.DeleteAsync(uri);
}
```

## Advanced Features

### Metadata Management
```csharp
var metadata = new ContentMetadata
{
    ContentType = "application/pdf",
    Tags = new[] { "invoice", "2024" },
    CustomProperties = new Dictionary<string, string>
    {
        ["Department"] = "Finance",
        ["Year"] = "2024"
    }
};

await _contentRepository.UploadAsync(stream, "invoice.pdf", metadata);
```

### Search by Tags
```csharp
var files = await _contentRepository.SearchAsync(new SearchCriteria
{
    Tags = new[] { "invoice", "2024" },
    FromDate = new DateTime(2024, 1, 1)
});
```

### Streaming Large Files
```csharp
public async Task UploadLargeFileAsync(string filePath)
{
    using var fileStream = File.OpenRead(filePath);
    await _contentRepository.UploadAsync(
        fileStream,
        Path.GetFileName(filePath),
        new ContentMetadata
        {
            ContentType = "application/octet-stream",
            UseChunkedUpload = true,
            ChunkSize = 4 * 1024 * 1024 // 4MB chunks
        });
}
```

## Use Cases

- **Document Management** - Store and retrieve business documents
- **Media Storage** - Images, videos, and audio files
- **Backup Storage** - Application data backups
- **User Uploads** - Profile pictures, attachments
- **Report Storage** - Generated reports and exports

## Multiple Storage Backends

```csharp
builder.Services
    .AddContentRepository("primary", content =>
    {
        content.WithAzureBlobStorage(/* config */);
    })
    .AddContentRepository("backup", content =>
    {
        content.WithFileSystem(/* config */);
    });

// Usage
public class DocumentService
{
    private readonly IContentRepository _primary;
    private readonly IContentRepository _backup;

    public DocumentService(
        [FromKeyedServices("primary")] IContentRepository primary,
        [FromKeyedServices("backup")] IContentRepository backup)
    {
        _primary = primary;
        _backup = backup;
    }
}
```

## See Also

- [Rystem.Content Documentation](https://github.com/KeyserDSoze/Rystem/tree/master/src/Content)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/)
- [SharePoint Integration](https://github.com/KeyserDSoze/Rystem/tree/master/src/Content/Rystem.Content.Infrastructure.M365.Sharepoint)
