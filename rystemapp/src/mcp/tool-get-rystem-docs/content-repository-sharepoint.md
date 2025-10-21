---
title: Content Repository - SharePoint Online
description: Store files in SharePoint Online with Content Repository - perfect for document collaboration, Office 365 integration, and enterprise content management with metadata support
---

# Content Repository - SharePoint Online Integration

Store files in **SharePoint Online** using the unified Content Repository interface.

**Best For:**
- Document collaboration with Office 365
- Enterprise content management
- Team sites and document libraries
- Compliance and governance
- Microsoft ecosystem integration

---

## Installation

```bash
dotnet add package Rystem.Content.Infrastructure.M365.Sharepoint --version 9.1.3
```

---

## Prerequisites

### Azure AD App Registration

1. Go to **Azure Portal** → **Azure Active Directory** → **App registrations**
2. Create new registration
3. Copy **Tenant ID**, **Client ID**
4. Create **Client Secret** (copy the value)
5. Grant API permissions:
   - **Microsoft Graph** → `Sites.ReadWrite.All`
   - Grant admin consent

### Get Site and Library IDs

Get SharePoint site information:

```
https://<tenant>.sharepoint.com/sites/<site-url>/_api/site/id
```

---

## Configuration

### By Site Name and Document Library Name

```csharp
await services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithSiteNameAndDocumentLibraryName("TeamSite", "Documents");
    }, "sharepoint")
    .NoContext();
```

### By Root Site

```csharp
await services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithRootSiteAndDocumentLibraryName("SharedDocuments");
    }, "sharepoint")
    .NoContext();
```

### By Site ID and Library ID

```csharp
await services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
        x.MapWithSiteIdAndDocumentLibraryId(
            configuration["Sharepoint:SiteId"],
            configuration["Sharepoint:DocumentLibraryId"]
        );
    }, "sharepoint")
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
    
    public async Task UploadDocumentAsync(string documentName, byte[] data)
    {
        await _contentRepository.UploadAsync(
            $"contracts/{documentName}",
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
public class MultiSiteService
{
    private readonly IContentRepository _hrSite;
    private readonly IContentRepository _salesSite;
    
    public MultiSiteService(IContentRepositoryFactory factory)
    {
        _hrSite = factory.Create("hr-sharepoint");
        _salesSite = factory.Create("sales-sharepoint");
    }
}
```

---

## Complete Example

```csharp
public class SharepointTest
{
    private readonly IContentRepositoryFactory _factory;
    
    public SharepointTest(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task ExecuteAsync()
    {
        var _contentRepository = _factory.Create("sharepoint");
        
        // Read file
        var file = await File.ReadAllBytesAsync("contract.pdf");
        var name = "legal/contracts/contract-2024.pdf";
        var contentType = "application/pdf";
        
        var metadata = new Dictionary<string, string>
        {
            { "department", "legal" },
            { "contractType", "employment" },
            { "year", "2024" }
        };
        
        var tags = new Dictionary<string, string>
        {
            { "status", "active" },
            { "confidential", "true" }
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
        metadata.Add("reviewedBy", "legal-team");
        
        var updated = await _contentRepository.SetPropertiesAsync(name, new ContentRepositoryOptions
        {
            Metadata = metadata
        });
        
        Assert.True(updated);
        
        // Verify update
        properties = await _contentRepository.GetPropertiesAsync(name, ContentInformationType.All);
        Assert.Equal("true", properties.Options.Metadata["reviewed"]);
        Assert.Equal("legal-team", properties.Options.Metadata["reviewedBy"]);
        
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

### Contract Management

```csharp
public class ContractService
{
    private readonly IContentRepository _sharepoint;
    private readonly ILogger<ContractService> _logger;
    
    public ContractService(IContentRepositoryFactory factory, ILogger<ContractService> logger)
    {
        _sharepoint = factory.Create("legal-docs");
        _logger = logger;
    }
    
    public async Task UploadContractAsync(string contractName, byte[] pdfData, string contractType)
    {
        var year = DateTime.UtcNow.Year;
        var path = $"Contracts/{year}/{contractType}/{contractName}.pdf";
        
        var success = await _sharepoint.UploadAsync(
            path,
            pdfData,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "application/pdf"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "contractType", contractType },
                    { "year", year.ToString() },
                    { "uploadedBy", "contract-system" },
                    { "uploadedDate", DateTime.UtcNow.ToString("o") }
                },
                Tags = new Dictionary<string, string>
                {
                    { "department", "legal" },
                    { "status", "pending-review" }
                }
            }
        );
        
        if (success)
            _logger.LogInformation("Contract uploaded to SharePoint: {Path}", path);
        else
            _logger.LogError("Failed to upload contract: {Path}", path);
    }
    
    public async Task<List<string>> ListActiveContractsAsync()
    {
        var contracts = new List<string>();
        
        await foreach (var file in _sharepoint.ListAsync(
            prefix: "Contracts/",
            downloadContent: false,
            informationRetrieve: ContentInformationType.Tags
        ))
        {
            if (file.Options.Tags.TryGetValue("status", out var status) && status == "active")
            {
                contracts.Add(file.Path);
            }
        }
        
        return contracts;
    }
}
```

### Team Document Collaboration

```csharp
public class TeamDocumentService
{
    private readonly IContentRepository _teamSite;
    
    public TeamDocumentService(IContentRepositoryFactory factory)
    {
        _teamSite = factory.Create("team-site");
    }
    
    public async Task ShareDocumentWithTeamAsync(string teamName, string documentName, byte[] documentData)
    {
        var path = $"Teams/{teamName}/Shared/{documentName}";
        
        await _teamSite.UploadAsync(
            path,
            documentData,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = GetContentType(documentName)
                },
                Metadata = new Dictionary<string, string>
                {
                    { "team", teamName },
                    { "sharedBy", "document-system" },
                    { "sharedDate", DateTime.UtcNow.ToString("o") }
                },
                Tags = new Dictionary<string, string>
                {
                    { "access", "team-only" },
                    { "collaborative", "true" }
                }
            }
        );
    }
    
    private string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }
}
```

### Compliance and Audit Trail

```csharp
public class ComplianceDocumentService
{
    private readonly IContentRepository _compliance;
    private readonly IAuditLogger _auditLogger;
    
    public ComplianceDocumentService(IContentRepositoryFactory factory, IAuditLogger auditLogger)
    {
        _compliance = factory.Create("compliance-docs");
        _auditLogger = auditLogger;
    }
    
    public async Task ArchiveComplianceDocumentAsync(
        string documentName, 
        byte[] data, 
        string department,
        DateTime retentionUntil)
    {
        var path = $"Compliance/{DateTime.UtcNow.Year}/{department}/{documentName}";
        
        var success = await _compliance.UploadAsync(
            path,
            data,
            new ContentRepositoryOptions
            {
                HttpHeaders = new ContentRepositoryHttpHeaders
                {
                    ContentType = "application/pdf"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "department", department },
                    { "archivedDate", DateTime.UtcNow.ToString("o") },
                    { "retentionUntil", retentionUntil.ToString("o") },
                    { "compliance", "true" }
                },
                Tags = new Dictionary<string, string>
                {
                    { "retention-policy", "7-years" },
                    { "confidential", "true" }
                }
            }
        );
        
        if (success)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Action = "Document Archived",
                Resource = path,
                Department = department,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
```

### Multi-Site Document Distribution

```csharp
public class DocumentDistributionService
{
    private readonly IContentRepositoryFactory _factory;
    
    public DocumentDistributionService(IContentRepositoryFactory factory)
    {
        _factory = factory;
    }
    
    public async Task DistributeToAllSitesAsync(string documentName, byte[] data)
    {
        var sites = new[] { "hr-site", "finance-site", "it-site" };
        
        await TaskManager.WhenAll(
            async (index, cancellationToken) =>
            {
                var siteName = sites[index];
                var repository = _factory.Create(siteName);
                
                await repository.UploadAsync(
                    $"Shared/{documentName}",
                    data,
                    new ContentRepositoryOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "distributedTo", siteName },
                            { "distributedDate", DateTime.UtcNow.ToString("o") }
                        }
                    }
                );
            },
            times: sites.Length,
            concurrentTasks: 3,
            runEverytimeASlotIsFree: true
        ).NoContext();
    }
}
```

---

## Configuration Options

```csharp
public class SharepointOptions
{
    public string TenantId { get; set; }           // Required: Azure AD Tenant ID
    public string ClientId { get; set; }           // Required: App Registration Client ID
    public string ClientSecret { get; set; }       // Required: App Registration Secret
    
    // Choose ONE mapping method:
    public void MapWithSiteNameAndDocumentLibraryName(string siteName, string libraryName);
    public void MapWithRootSiteAndDocumentLibraryName(string libraryName);
    public void MapWithSiteIdAndDocumentLibraryId(string siteId, string libraryId);
}
```

---

## Benefits

- ✅ **Office 365 Integration**: Works with Microsoft ecosystem
- ✅ **Collaboration**: Real-time co-authoring with Office Online
- ✅ **Versioning**: Built-in document version control
- ✅ **Compliance**: DLP, retention policies, eDiscovery
- ✅ **Search**: Powerful enterprise search
- ✅ **Permissions**: Granular access control

---

## Related Tools

- **[Content Repository Pattern](https://rystem.net/mcp/tools/content-repository.md)** - Main documentation
- **[Azure Blob Storage](https://rystem.net/mcp/tools/content-repository-blob.md)** - Alternative cloud storage
- **[Content Migration](https://rystem.net/mcp/tools/content-repository.md#migration-tool)** - Migrate from/to SharePoint

---

## References

- **NuGet Package**: [Rystem.Content.Infrastructure.M365.Sharepoint](https://www.nuget.org/packages/Rystem.Content.Infrastructure.M365.Sharepoint) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **SharePoint API**: https://learn.microsoft.com/sharepoint/dev/
