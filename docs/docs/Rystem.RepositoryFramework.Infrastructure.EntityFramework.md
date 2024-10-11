# EntityFrameworkOptions Class
This class provides options for Entity Framework operations.

## Properties

### DbSet
This property holds a function that takes a `DbContext` of generic type `TContext` and returns a reference to a `DbSet` of type `TEntityModel`. The `TContext` generic type constraint denotes that it must be of DbContext or derived from DbContext.

### References
This property holds a function that takes a `DbSet<TEntityModel>` and returns an `IQueryable<TEntityModel>`. This can be used to specify any additional data references your entity might have.

# RepositoryBuilderExtensions Class
This class contains extension methods to incorporate Entity Framework operations into Rystem Repository Framework.

## Methods

### WithEntityFramework for IRepositoryBuilder
This method integrates Entity Framework into the repository pattern. 

**Parameters**:
- `builder`: The IRepositoryBuilder object.
- `options`: Custom settings for your Entity Framework.
- `name`: The name for the debug factory.
- `lifetime`: Defines the lifetime of the service.

**Return value**: Returns an instance of IRepositoryBuilder that has been configured.
  
**Usage Example**:
```csharp
repositoryBuilder.WithEntityFramework<MyModel, int, MyEntityModel, MyDbContext>(options, "MyFactory", ServiceLifetime.Scoped);
```

### WithEntityFramework for ICommandBuilder
This method integrates the Entity Framework into the command pattern. 

**Parameters**:
- `builder`: The ICommandBuilder object.
- `options`: Settings for Entity Framework.
- `name`: The name for the debug factory.
- `lifetime`: Defines the lifetime of the service.

**Return Value**: Returns an instance of ICommandBuilder that has been configured.

**Usage Example**:
```csharp
commandBuilder.WithEntityFramework<MyModel, int, MyEntityModel, MyDbContext>(options, "MyFactory", ServiceLifetime.Scoped);
```

### WithEntityFramework for IQueryBuilder
This method integrates the Entity Framework into the query pattern.

**Parameters**:
- `builder`: IQueryBuilder object.
- `options`: Custom settings for your Entity Framework.
- `name`: The name for the debug factory.
- `lifetime`: Lifetime of the service.

**Return Value**: Returns an instance of IQueryBuilder that has been configured.
  
**Usage Example**:
```csharp
queryBuilder.WithEntityFramework<MyModel, int, MyEntityModel, MyDbContext>(options, "MyFactory", ServiceLifetime.Scoped);
```
 

The other set of methods in the `RepositoryBuilderExtensions` class are similar but return a `QueryTranslationBuilder` instead, allowing for translation between properties. They're used when the model you are reading from a database does not exactly match the model you are presenting in the repository.