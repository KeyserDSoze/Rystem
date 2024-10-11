# Rystem.Content.Infrastructure.Storage.Blob documentation

This documentation describes the main classes in the Rystem.Content.Infrastructure.Storage.Blob library. It covers public methods, their parameters, return types, and potential use cases.

---

## Class: ContentRepositoryBuilderExtensions

### Method: WithBlobStorageIntegrationAsync

- **Description:** This method adds a blob storage integration to the content repository. It's an asynchronous operation.

- **Parameters:**
   - `IContentRepositoryBuilder builder`: This is the builder being extended.
   - `Action<BlobStorageConnectionSettings> connectionSettings`: A delegate to configure the connection.
   - `string? name`: The name of the integration. This is optional and defaults to null.
   - `ServiceLifetime serviceLifetime`: The lifecycle of the blob storage integration. It's optional and defaults to 'Transient'.
   
- **Return Value:** returns the updated builder of type `Task<IContentRepositoryBuilder>`.

- **Usage Example:**
```csharp
services
    .AddContentRepository()
    .WithBlobStorageIntegrationAsync(x =>
    {
        x.ContainerName = "supertest";
        x.Prefix = "site/";
        x.ConnectionString = configuration["ConnectionString:Storage"];
        x.UploadOptions = new Azure.Storage.Blobs.Models.BlobUploadOptions()
        {
            AccessTier = Azure.Storage.Blobs.Models.AccessTier.Cool
        };
    },
    "blobstorage")
    .ToResult();
```

### Method: WithBlobStorageIntegration

- **Description:** This is a sync wrapper over `WithBlobStorageIntegrationAsync`. It also adds a blob storage integration to the content repository.

- **Parameters:** See the `WithBlobStorageIntegrationAsync` method. They are the same.

- **Return Value:** returns the updated builder of type `IContentRepositoryBuilder`.

- **Usage Example:** Same as above, but without awaiting the result.
```csharp
services
    .AddContentRepository()
    .WithBlobStorageIntegration(x =>
    {
        // settings configuration here.
    },
    "blobstorage");
```

---

## Class: BlobServiceClientWrapper

- **Properties:**
  - `BlobContainerClient ContainerClient`: Client to interact with the Blob container.
  - `BlobUploadOptions UploadOptions`: Options for uploading to the Blob container.
  - `string Prefix`: The prefix identifier to filter blobs in the blob storage.

---

## Class: BlobStorageConnectionSettings

This class defines the connection settings for Blob storage.

- **Properties:**
   - `Uri EndpointUri`: The URI endpoint of Blob storage.
   - `string ManagedIdentityClientId`: The client ID for a Managed Identity.
   - `string ConnectionString`: The connection string for Blob storage.
   - `string ContainerName`: The name of the Blob container.
   - `string Prefix`: The prefix identifier to filter blobs in the blob storage.
   - `bool IsPublic`: Whether the Blob storage is public.
   - `BlobClientOptions ClientOptions`: Client options for Blob storage.
   - `BlobUploadOptions UploadOptions`: Options for uploading to Blob storage.
   
- **Methods:**

### Method: BuildAsync

- **Description:** This method builds a Blob Service Client Asynchronously.

- **Parameters:** This method does not accept any input parameters.

- **Return Value:** Returns a `Task` of the build function for `BlobServiceClientWrapper`.

- **Usage Example:**
```csharp
=> BlobServiceClientFactory.GetClientAsync(this);
```
