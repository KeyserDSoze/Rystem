# Rystem Content Framework

A unified abstraction layer for **file storage** across multiple backends. Write your business logic once against `IContentRepository` and switch the underlying storage (Azure Blob, Azure File Share, SharePoint Online, in-memory) by changing only the DI registration.

## Packages

| Package | NuGet | Description |
|---------|-------|-------------|
| `Rystem.Content.Abstractions` | [![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Abstractions)](https://www.nuget.org/packages/Rystem.Content.Abstractions) | Core interface, models, migration tool |
| `Rystem.Content.Infrastructure.Azure.Storage.Blob` | [![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.Azure.Storage.Blob)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Azure.Storage.Blob) | Azure Blob Storage backend |
| `Rystem.Content.Infrastructure.Azure.Storage.File` | [![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.Azure.Storage.File)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.Azure.Storage.File) | Azure File Share backend |
| `Rystem.Content.Infrastructure.M365.Sharepoint` | [![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.M365.Sharepoint)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.M365.Sharepoint) | SharePoint Online backend |
| `Rystem.Content.Infrastructure.InMemory` | [![NuGet](https://img.shields.io/nuget/v/Rystem.Content.Infrastructure.InMemory)](https://www.nuget.org/packages/Rystem.Content.Infrastructure.InMemory) | In-memory backend (testing / dev) |

## Architecture

```
IContentRepository          ← inject this in your services
    │
    ├─ BlobStorageRepository       (Azure Blob Storage)
    ├─ FileStorageRepository       (Azure File Share)
    ├─ SharepointRepository        (Microsoft 365 SharePoint Online)
    └─ InMemoryRepository          (in-memory, for tests)
```

Multiple named backends can be registered simultaneously. Use `IContentRepositoryFactory` to resolve them by name when you have more than one.

## Quick example

```csharp
// Program.cs
await builder.Services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "assets";
        x.ConnectionString = configuration["Storage:ConnectionString"];
    }, "blob")
    .NoContext();

// Business class
public sealed class FileService
{
    private readonly IContentRepository _repo;

    public FileService(IContentRepositoryFactory factory)
        => _repo = factory.Create("blob");

    public Task<bool> UploadAsync(string name, byte[] data)
        => _repo.UploadAsync(name, data, new ContentRepositoryOptions
        {
            HttpHeaders = new ContentRepositoryHttpHeaders { ContentType = "image/png" }
        }).AsTask();
}
```

### Help the project

Reach out us on [Discord](https://discord.gg/tkWvy4WPjt)  
Contribute: https://www.buymeacoffee.com/keyserdsoze