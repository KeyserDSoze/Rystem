# DataverseOptions Class

The `DataverseOptions` class is specialized in managing options for the Microsoft Dataverse, acting as a structure containing various properties and methods to configure, control and access different aspects of a Dataverse interaction.

## Method: SetConnection

**Purpose**: This method sets the connection parameters for Dataverse.

**Parameters**:
- `string environment`: Your environment's name.
- `DataverseAppRegistrationAccount identity`: Your DataverseAppRegistrationAccount instance which contains your application identity.

**Return Value**: This method does not return a value.

**Usage Example**:

```
DataVerseOptions<MyClass, string> options = new DataVerseOptions<MyClass, string>();
options.SetConnection("my-environment", new DataverseAppRegistrationAccount());
```

## Method: SetDataverseEntity

**Purpose**: This method is used to set a Microsoft.Xrm.Sdk.Entity from your model and key.

**Parameters**:
- `Microsoft.Xrm.Sdk.Entity dataverseEntity`: Your entity from Dataverse to be set with the properties from your model.
- `T entity`: Your model instance.
- `TKey key`: Your model key.

**Return Value**: This method does not return a value.

**Usage Example**:

```
dataverseOptions.SetDataverseEntity(dataverseEntity, myEntity, "entityKey");
```

## Method: SetEntity

**Purpose**: This method sets your entity using a Microsoft.Xrm.Sdk.Entity and returns the Key.

**Parameters**:
- `Microsoft.Xrm.Sdk.Entity dataverseEntity`: The entity from Dataverse.
- `T entity`: Your model instance.

**Return Value**: Returns the TKey for the entity.

**Usage Example**:

```
var key = dataverseOptions.SetEntity(dataverseEntity, myEntity);
```

## Method: CheckIfExistColumnsAsync

**Purpose**: This method performs an asynchronous check to verify if columns exist for your model in Dataverse.

**Parameters**: This method takes no parameters.

**Return Value**: A `Task` indicating the completion of the operation. 

**Usage Example**:

```
await dataverseOptions.CheckIfExistColumnsAsync();
```

## Method: GetClient

**Purpose**: This method creates a ServiceClient for Dataverse based on your DataverseOptions. 

**Parameters**: This method takes no parameters.

**Return Value**: It returns an instance of a ServiceClient for Dataverse.

**Usage Example**:

```
var client = dataverseOptions.GetClient();
``` 

# RepositoryBuilderExtensions Class

The `RepositoryBuilderExtensions` class is about managing the repositories, commands and queries in the Microsoft Extensions Dependency Injection framework.

## Method: WithDataverse (IRepositoryBuilder variation)

**Purpose**: This method sets up a default Dataverse service for your repository pattern.

**Parameters**:
- `IRepositoryBuilder<T, TKey> builder`: Your IRepositoryBuilder instance.
- `Action<IDataverseRepositoryBuilder<T, TKey>> dataverseBuilder`: An action with the settings for your Dataverse.
- `string? name`: Optional. The name for your factory.

**Return Value**: It returns an instance of IRepositoryBuilder for Dataverse.

**Usage Example**:

```csharp
repositoryBuilder.WithDataverse(builderAction, "MyRepository");
```

Please note that there are additional `WithDataverse` methods for `ICommandBuilder<T, TKey>` and `IQueryBuilder<T, TKey>`. The setup and usage for these methods follows the same pattern as `IRepositoryBuilder<T, TKey>`.

# Note
The classes are assumed to be part of the `RepositoryFramework.Infrastructure.Dynamics.Dataverse` and `Microsoft.Extensions.DependencyInjection` namespaces.

