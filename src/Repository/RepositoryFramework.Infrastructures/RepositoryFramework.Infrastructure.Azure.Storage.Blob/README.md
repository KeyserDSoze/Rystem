# Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob

Azure Blob Storage integration for Repository/CQRS services.

## Installation

```bash
dotnet add package Rystem.RepositoryFramework.Infrastructure.Azure.Storage.Blob
```

## Quick start

```csharp
builder.Services.AddRepository<Document, Guid>(repositoryBuilder =>
{
    repositoryBuilder.WithBlobStorage(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
        blobStorageBuilder.Settings.Prefix = "documents/";
    });
});
```

## Async setup variant

```csharp
await builder.Services.AddRepositoryAsync<Document, Guid>(async repositoryBuilder =>
{
    await repositoryBuilder.WithBlobStorageAsync(blobStorageBuilder =>
    {
        blobStorageBuilder.Settings.ConnectionString = builder.Configuration["ConnectionStrings:Storage"];
    });
});
```

## Expose as API

```csharp
builder.Services.AddApiFromRepositoryFramework().WithPath("api");

var app = builder.Build();
app.UseApiFromRepositoryFramework().WithNoAuthorization();
```
