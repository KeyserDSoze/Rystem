# Rystem.Content.Infrastructure.M365.Sharepoint Documentation

This package aids the integration of Sharepoint storage with the Rystem Content Repository.

## 1. Class: ContentRepositoryBuilderExtensions

This static class provides extensions to implement Sharepoint storage integration.

### Method: WithSharepointIntegrationAsync

This method adds a Sharepoint storage integration to content repository. You should use an App Registration with Permission Type: Application and Permissions: Files.ReadWrite.All or Sites.ReadWrite.All.

#### Parameters:
- `IContentRepositoryBuilder builder`: The repository builder where the integration is to be added.
- `Action<SharepointConnectionSettings> connectionSettings`: Action to configure the connection settings for Sharepoint.
- `string name`: (Optional) The name of the connection.
- `ServiceLifetime serviceLifetime`: (Optional) Determines the lifetime of the service in the dependency injection container.

#### Return Value:
This returns a task that upon execution returns a builder `Task<IContentRepositoryBuilder>`

#### Usage Example:

```
var configuration = new ConfigurationBuilder()
   .AddJsonFile("appsettings.test.json")
   .AddEnvironmentVariables()
   .Build();

services
    .AddContentRepository()
    .WithSharepointIntegrationAsync(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
    })
    .ToResult();
```

### Method: WithSharepointIntegration

This method adds a Sharepoint storage integration to content repository. It is a non-async version of `WithSharepointIntegrationAsync` method.

#### Parameters:
- `IContentRepositoryBuilder builder`: The repository builder where the integration is to be added.
- `Action<SharepointConnectionSettings> connectionSettings`: Action to configure the connection settings for Sharepoint.
- `string name`: (Optional) The name of the connection.
- `ServiceLifetime serviceLifetime`: (Optional) Determines the lifetime of the service in the dependency injection container.

#### Return Value:
Return builder `IContentRepositoryBuilder`.

#### Usage Example:

```
services
    .AddContentRepository()
    .WithSharepointIntegration(x =>
    {
        x.TenantId = configuration["Sharepoint:TenantId"];
        x.ClientId = configuration["Sharepoint:ClientId"];
        x.ClientSecret = configuration["Sharepoint:ClientSecret"];
    })
    .ToResult();
```

## 2. Class: SharepointConnectionSettings

This class provides configurations for Sharepoint integration. 

#### Properties:

##### ClientId, ClientSecret, TenantId
These are strings used for OpenID Connect authentication to SharePoint.

##### SiteId, DocumentLibraryId
These are identifiers for the SharePoint Site and Document Library. 

##### SiteName, DocumentLibraryName
Alternative to using Ids to identify the SharePoint site and document library, you can use the Site Name and Document Library Name.

##### OnlyDocumentLibrary
This boolean property can be set to true if you want to map only to a Document Library.

#### Methods:

There are a series of mapping methods provided in this class that allows you to setup your connection to SharePoint in various ways depending on if you want to refer to sites and document libraries by name or by ID. They include:

- MapWithSiteIdAndDocumentLibraryId
- MapWithSiteIdAndDocumentLibraryName
- MapWithSiteNameAndDocumentLibraryName 
- MapWithRootSiteAndDocumentLibraryName 
- MapOnlyDocumentLibraryId 
- MapOnlyDocumentLibraryName 


## 3. Class: SharepointClientWrapper
This sealed class which implements IFactoryOptions, holds the function to create a GraphServiceClient and ids for SharePoint's site and document library.

#### Properties:
- `Func<GraphServiceClient> Creator`: Function that creates a GraphServiceClient.
- `string SiteId`: ID of the SharePoint site.
- `string DocumentLibraryId`: ID of the SharePoint Document Library.

By referring to the provided test classes, developers can gain insights on how the methods should be used in different use-case scenarios. This is crucial to ensuring that the library functions as expected and that all edge cases are handled appropriately.