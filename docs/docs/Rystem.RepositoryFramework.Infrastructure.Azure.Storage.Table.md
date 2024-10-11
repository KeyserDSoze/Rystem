# Documentation

## Class: TableStorageConnectionSettings

This class is responsible for setting up the connection to an Azure Table Storage instance. It contains properties that are used to define the connection settings.

### Properties:

- **EndpointUri (Uri?)**: The endpoint URL of the Azure Table Storage. This field can be null.
- **ManagedIdentityClientId (string?)**: The client ID that is used for Azure Managed Identity authentication. This can be null if not using Managed Identity authentication.
- **ConnectionString (string?)**: The connection string to the Azure Table Storage. This field can be null.
- **TableName (string?)**: The name of the table in Azure Table Storage to connect to.
- **ClientOptions (TableClientOptions)**: The client options that will be used when creating a connection to the Azure Table Storage.
- **ModelType (Type)**: The model type that the table storage will managed. 

## Class: TableStorageSettings&lt;T, TKey&gt;

This class contains settings that are relevant to the functionality of the Azure Table Storage instance. Here, `T` represents the model and `TKey` represents the key to manage data.

### Properties:

- **PartitionKeyFunction (Func&lt;T, string&gt;)**: A function to generate the partition key from the model.
- **PartitionKeyFromKeyFunction (Func&lt;TKey, string&gt;)**: A function to generate the partition key from the key.
- **RowKeyFunction (Func&lt;T, string&gt;)**: A function to generate the row key from the model.
- **RowKeyFromKeyFunction (Func&lt;TKey, string&gt;)?**: A function to generate the row key from the key. This field can be null.
- **TimestampFunction (Func&lt;T, DateTime&gt;)?**: A function to generate the timestamp for the model. This field can be null.
- **PartitionKey (string)**: The partition key for the Table Storage.
- **RowKey (string?)**: The row key for the Table Storage. This field can be null.
- **Timestamp (string?)**: The timestamp for the Table Storage. This field can be null.

## Extensions for Repository/Command/Query Builders

These extension methods are available for repository, command and query builders respectively to add a default Azure Table Storage service to the pattern.

- **WithTableStorageAsync**
- **WithTableStorage**

Taking `WithTableStorageAsync` in `IRepositoryBuilderExtensions` for example:

### WithTableStorageAsync&lt;T, TKey&gt;

This method allows you to integrate Azure Table Storage with your repository pattern. It sets up a Table Storage and build options for it.

#### Parameters:

- **builder (IRepositoryBuilder&lt;T, TKey&gt;)**: The repository builder on which to add Table Storage.
- **tableStorageBuilder (Action&lt;ITableStorageRepositoryBuilder&lt;T, TKey&gt;&gt;)**: An action that configures the Table Storage Repository Builder.
- **name (string?)**: The name of the factory. This field can be null.

#### Returns:

- **IRepositoryBuilder&lt;T, TKey&gt;**: Returns the updated builder.

#### Usage Example:

```csharp
IRepositoryBuilder<Article, int> repositoryBuilder = ... // Your repository builder
repositoryBuilder.WithTableStorageAsync(builder =>
{
    // Configure the table storage builder here
}, "MyFactoryName");
```
The same principles apply to the two other types of builders (`ICommandBuilder` and `IQueryBuilder`). The only difference is on the type of builder passed as the first parameter.

These methods (both async and un-async), add and configure Azure Table Storage service to the provided builder; configuring repository, command or query builders to use Azure Table Storage as a backend. The caller passes and action to configure the `ITableStorageRepositoryBuilder` which provides required parameters for setting up the service.

Please notice that, as to the builder parameter, it is always of a generic type T (representing the model) and TKey (representing the deliverable data from the repository). So when such methods are called in context, the actual types replacing T and TKey must be provided. Also, TKey should not be a nullable type (given by the constraint `where TKey : not-null`).