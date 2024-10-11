# Documentation
## `ContentRepositoryBuilderExtensions` class

**Namespace**: `Microsoft.Extensions.DependencyInjection`

This class provides extension methods to integrate the content repository using different types of storage.

### `WithFileStorageIntegrationAsync` method

**Method Description**: This method is used to asynchronously add a file storage integration to the content repository.

**Parameters**:

- `builder` (IContentRepositoryBuilder): The builder to use for adding the integration.
- `connectionSettings` (Action): A method to set specific connection parameters for the file storage.
- `name` (string): An optional parameter that sets the name of storage integration. If not defined, the default is null.
- `serviceLifetime` (ServiceLifetime): An optional parameter that sets the life time of the service for the integration. The default is 'Transient'.

**Return Value**: The method returns an IContentRepositoryBuilder object, whose task completion represents the end of the integration setup.

**Usage Example**:

```csharp
builder.WithFileStorageIntegrationAsync(connectionSettings, "MyFileStorageIntegration", ServiceLifetime.Singleton);
```

### `WithFileStorageIntegration` method

**Method Description**: This method is used to add a file storage integration to the content repository. 

This is essentially a synchronous version of the `WithFileStorageIntegrationAsync` method. It calls that method and waits for the task to complete.

**Parameters**, **Return Value** and **Usage Example**: These are identical to the `WithFileStorageIntegrationAsync` method.

## `FileServiceClientWrapper` class

**Namespace**: `Rystem.Content.Infrastructure.Storage`

This class provides a wrapper to encapsulate the ShareClient for use with the content repository.

### Properties:
- `ShareClient` (ShareClient): The Azure Share Client object
- `Prefix` (string): The optional prefix to be used on all requests that is made through this client.

## `FileStorageConnectionSettings` class

**Namespace**: `Rystem.Content.Infrastructure.Storage`
    
This class sets the connection settings for the File Service Client.

### Properties:
- `EndpointUri` (Uri) : The Endpoint Uri for the connection
- `ManagedIdentityClientId` (string): Managed Identity client ID
- `ConnectionString` (string): Connection string
- `ShareName` (string): Name of the share storage
- `Prefix` (string): The prefix for the file in the storage. Helps to organize files in a structured manner in the storage
- `IsPublic` (bool): A flag specifying if it is public or not
- `ClientOptions` (ShareClientOptions): Options for the share client
- `ClientCreateOptions` (ShareCreateOptions): Options for creating a share service client 
- `Permissions` (List): A list that contains permissions (signed identifiers) 
- `Conditions` (ShareFileRequestConditions): conditions for request to a share file 
- `BuildAsync` (Task): A task that represents the asynchronous operation, contains a delegate that builds file service client wrapper.

**Usage Example**:

Setting FileStorageConnectionSettings:

```csharp
FileStorageConnectionSettings settings = new FileStorageConnectionSettings()
{
    EndpointUri = new Uri("http://my-end-point-url.com"),
    ManagedIdentityClientId = "1234",
    ConnectionString = "...", //some connection string
    ShareName = "ShareName",
    Prefix = "site/",
    IsPublic = true,
    //...set other properties as per requirement
};
```

## Information from Test Classes
From the test scenarios, it is clear that the package allows content to be stored in multiple storage systems, referred to as "integrations". These are "blobstorage", "inmemory", "sharepoint", and "filestorage". The `WithFileStorageIntegrationAsync` method is used to set up an integration for a file storage system. The file storage integration allows the content repository to store files in a file storage system on the Azure platform.