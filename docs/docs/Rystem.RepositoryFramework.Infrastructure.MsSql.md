# Documentation

## Class: `PropertyHelper<T>`

This class provides a set of methods manipulating entities of a certain type. It is used to map the properties of an entity to their `SqlParameter` representations, and to set the properties of an entity from a `SqlDataReader`.

### Method: `SetEntityForDatabase`

This method maps an entity consisting of properties of the type `T` into an `SqlParameter` object.

**Parameters**
- `T entity`: This is an object of type `T`. Its public properties' values are used to create an `SqlParameter`.

**Return Value**  
This method returns an instance of `SqlParameter`. The `SqlParameter` is set with the `value` of the entity's property, with the `ColumnName` as the parameter’s name.

**Usage Example**

```csharp
var propertyHelper = new PropertyHelper<YourEntity>(propertyInfo); // Assume propertyInfo is an instance of PropertyInfo.
SqlParameter sqlParameter = propertyHelper.SetEntityForDatabase(new YourEntity{ Property1 = "A value" });
```

### Method: `SetEntity`

This method sets the value of the property in an object of the type `T` using data from a `SqlDataReader`.

**Parameters**
- `SqlDataReader reader`: This parameter is used to read the data that will be set into the entity.
- `T entity`: This object's property identified by the `ColumnName` will be set to the received value.

**Return Value**  
This method does not return any value. It sets the property of an entity object of type `T`.

**Usage Example**

```csharp
var propertyHelper = new PropertyHelper<YourEntity>(propertyInfo); // Assume propertyInfo is an instance of PropertyInfo.
SqlDataReader sqlDataReader = command.ExecuteReader();
propertyHelper.SetEntity(sqlDataReader, new YourEntity());
```


## Class: `RepositoryBuilderExtensions`

This static class provides extension methods for several interfaces to add and configure the MsSql service.

### Method: `WithMsSql<T, TKey>`

This is an extension method available for classes implementing the `IRepositoryBuilder<T, TKey>` interface. It configures an MsSql service for the repository.

**Parameters**
- `IRepositoryBuilder<T, TKey> builder`: The repository builder to be extended.
- `Action<IMsSqlRepositoryBuilder<T, TKey>> sqlBuilder`: An action to further configure the MsSql repository.
- `string? name`: Optional name for the factory.
- `ServiceLifetime lifetime`: Optional parameter specifying the service's lifetime.

**Return Value**  
This method returns an instance of `IRepositoryBuilder<T, TKey>`.

**Usage Example**

```csharp
IRepositoryBuilder<YourEntity, int> builder;
builder.WithMsSql<YourEntity, int>(sqlBuilderAction, "MyFactory");
```

The same concept applies for `WithMsSql<T, TKey>` method overload on `ICommandBuilder<T, TKey>` and `IQueryBuilder<T, TKey> interface. These methods set up MsSql service for command pattern and query pattern respectively.