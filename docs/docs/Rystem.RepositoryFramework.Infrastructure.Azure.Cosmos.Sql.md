# Class Documentation

## 1. CosmosSettings
This class is used for database creation and container creation. It includes the throughput of the resources in the Azure Cosmos DB service, which is the standard pricing for the resources.

   - ***Method Name***: ThroughputProperties
     - **Parameters**: None, it's a public property
     - **Return Value**: ThroughputProperties object (It represents the throughput of the resources in the Azure Cosmos DB service).
     - **Usage Example**: 
      ```csharp
      CosmosSettings settings = new CosmosSettings();
      var throughput = settings.ThroughputProperties;
      ```

   - ***Method Name***: RequestOptions
     - **Parameters**: None, it's a public property
     - **Return Value**: RequestOptions object (It holds the default cosmos request options).
     - **Usage Example**: 
     ```csharp
     CosmosSettings settings = new CosmosSettings();
     var requestOptions = settings.RequestOptions;
     ```

## 2. CosmosSqlConnectionSettings
The class assumes settings for the cosmos DB and container.

   - ***Method Name***: EndpointUri
     - **Parameters**: None, it's a public property
     - **Return Value**: Uri object (The endpoint URI of the Azure Cosmos DB service)
     - **Usage Example**: 
     ```csharp
     CosmosSqlConnectionSettings settings = new CosmosSqlConnectionSettings();
     Uri endpoint = settings.EndpointUri;
     ```

Continue following this format for each class and associated method.


## Test Classes
|||
---
In the test classes, you will get further context on how these classes and their methods are used and how the input data is provided or consumed. Make use of this information to incorporate it into your documentation. For instance, if a test class exceptional handling for any method, ensure you cover that in potential error and troubleshooting section.
