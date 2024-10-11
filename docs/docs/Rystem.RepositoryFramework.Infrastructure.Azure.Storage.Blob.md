# Documentation for RepositoryFramework Infrastructure - Azure Storage Blob

### ```BlobContainerClientWrapper``` Class

#### Properties
- **Client**: This is the object of `BlobContainerClient` from Azure Storage Blobs that provides all blob service operations.
- **Prefix**: This is an optional `string` that can contain a common prefix shared by the blob names in the blob containers.

### ```BlobStorageConnectionSettings``` Class

#### Properties
- **EndpointUri**: This `Uri` property allows you to set the Blob service endpoint from the Azure Storage account.
- **ManagedIdentityClientId**: This `string` property corresponds to the client ID of the Managed Identity in Azure. 
- **ConnectionString**: Use this `string` property to set the connection string from the Azure Storage account. 
- **ContainerName**: This `string` property allows to set the name of the Blob Container in the Azure storage account. 
- **Prefix**: This is an optional `string` which can contain a common prefix shared by the blob names in the blob containers.
- **ClientOptions**: These are the options for configuring this blob client according to the `BlobClientOptions` class given in Azure Storage SDK.
- **ModelType**: This `Type` property refers to the Model's Type that the blob storage will relate to.

### ```RepositoryBuilderExtensions```  Class

This class contains several public methods that can be used to add/alter the blob storage in your repository/command/query builder. It contains various utility methods that enable easy addition and management of blob storage. 

**Methods:**

#### ```WithBlobStorage<T, TKey>```
This method adds a default blob storage service for your repository pattern.
- **Parameters**: 
  - _builder_: (`IRepositoryBuilder<T, TKey>`) The Repository Builder of specific model and key type.
  - _blobStorageBuilder_: (`Action<IBlobStorageRepositoryBuilder<T, TKey>>`) The settings for your blob storage.
  - _name_: (`string`) An optional parameter for the Factory name.
- **Return Value**: Returns an updated instance of `IRepositoryBuilder<T, TKey>`.
- **Usage Example**: 
```csharp
builder.WithBlobStorage(blobStorageBuilder, "MyFactoryName");
```

#### ```WithBlobStorage<T, TKey> (for ICommandBuilder)```
This method adds a default blob storage service for your command pattern.
- **Parameters** and **Return Value** are identical to the above method. The only difference is in the First Parameter where `ICommandBuilder<T, TKey>` is used instead of `IRepositoryBuilder<T, TKey>`.
- **Usage Example**: 
```csharp
commandBuilder.WithBlobStorage(blobStorageBuilder, "MyFactoryName");
```
#### ```WithBlobStorage<T, TKey> (for IQueryBuilder)```
This method adds a default blob storage service for your query pattern.
- **Parameters** and **Return Value** are identical to the first method. The only difference is in the First Parameter where `IQueryBuilder<T, TKey>`  is used instead of `IRepositoryBuilder<T, TKey>`.
- **Usage Example**: 
```csharp
queryBuilder.WithBlobStorage(blobStorageBuilder, "MyFactoryName");
```

The same three methods are available with the Async versions. They perform the same task in an asynchronous manner using the `async/await` pattern. The return type is a Task of `IRepositoryBuilder<T, TKey>`, `ICommandBuilder<T, TKey>`, or `IQueryBuilder<T, TKey>` accordingly.

**Usage Example for Async methods**: 
```csharp
await builder.WithBlobStorageAsync(blobStorageBuilder, "MyFactoryName");
```