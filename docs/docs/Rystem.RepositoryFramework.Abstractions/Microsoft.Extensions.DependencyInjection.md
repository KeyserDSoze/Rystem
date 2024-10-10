# Class Documentation

## Namespace: Microsoft.Extensions.DependencyInjection

This namespace contains extensions methods for an IServiceCollection to add repositories, commands and queries into the service dependency injection container with various other features. The classes here provide abstraction to functionality encapsulated by RepositoryFramework.

### ServiceCollectionExtensions

This class features methods that add Repositories, Commands, Queries and repository-related business into Microsoft's dependency injection container. These methods also provide the implementation for setting up the repository's settings, builder reference and custom separators for parsing default keys.

Method Name: **AddDefaultSeparatorForDefaultKeyInterface**

This method sets the default separator for the IDefaultKey interface.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the service is being added.
- separator (string): The separator string that will replace the default separator.

Return Value: It returns the same IServiceCollection instance which allows for fluent method chaining.

Usage Example:
```csharp
services.AddDefaultSeparatorForDefaultKeyInterface("|");
```

---

Method Name: **AddRepository**

This method adds a repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the repository is being added.
- builder (Action<IRepositoryBuilder<T, TKey>>): The builder to setup the repository.

Return Value: It returns the same IServiceCollection instance which allows for fluent method chaining.

Usage Example:
```csharp
services.AddRepository<User, int>((builder) => 
{
    builder.SetStorageType(StorageType.Cloud)
});
```

---

Method Name: **AddCommand**

This method adds a command repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the command is being added.
- builder (Action<ICommandBuilder<T, TKey>>): The builder to setup the command repository.

Return Value: It returns the same IServiceCollection instance which allows for fluent method chaining.

Usage Example:
```csharp
services.AddCommand<Order, Guid>((builder) =>
{
    builder.SetStorageType(StorageType.Local)
});
```
--- 

Method Name: **AddQuery**

This method adds a Query repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the Query repository is being added.
- builder (Action<IQueryBuilder<T, TKey>>): The builder to set up the Query repository.

Return Value: It returns the same IServiceCollection instance which allows for fluent method chaining.

Usage Example:
```csharp
services.AddQuery<Order, Guid>((builder) =>
{
    builder.SetStorageType(StorageType.Local)
});
```
---

Method Name: **ScanBusinessForRepositoryFramework**

This method adds all business classes from the specified assemblies for the repository or CQRS pattern to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the classes are being added.
- assemblies (Assembly[]): The assemblies where the business classes reside.

Return Value: It returns the same IServiceCollection instance which allows for fluent method chaining.

Usage Example:
```csharp
services.ScanBusinessForRepositoryFramework(typeof(SomeBusiness).Assembly);
```

---

Method Name: **AddBusinessForRepository**

This method adds a business repository for the specified model to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the business repository is being added.

Return Value: It returns a new instance of RepositoryBusinessBuilder for the supplied type and key parameters.

Usage Example:
```csharp
services.AddBusinessForRepository<Product, Guid>()
```

---

Method Name: **AddRepositoryAsync**

This method asynchronously adds a repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the repository is being added.
- builder (Func<IRepositoryBuilder<T, TKey>, ValueTask>): The builder to setup the repository asynchronously.

Return Value: It returns a Task of IServiceCollection which represents the asynchronous operation.

Usage Example:
```csharp
await services.AddRepositoryAsync<User, int>(async (builder) => 
{
    await builder.SetStorageTypeAsync(StorageType.Cloud)
});
```

---

Method Name: **AddCommandAsync**

This method asynchronously adds a command repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the command is being added.
- builder (Func<ICommandBuilder<T, TKey>, ValueTask>): The builder to setup the command repository asynchronously.

Return Value: It returns a Task of IServiceCollection which represents the asynchronous operation.

Usage Example:
```csharp
await services.AddCommandAsync<Order, Guid>(async (builder) =>
{
    await builder.SetStorageTypeAsync(StorageType.Local)
});
```

---

Method Name: **AddQueryAsync**

This method asynchronously adds a Query repository to the IServiceCollection.

Parameters:
- services (IServiceCollection): The IServiceCollection instance to which the Query repository is being added.
- builder (Func<IQueryBuilder<T, TKey>, ValueTask>): The builder to setup the Query repository asynchronously.

Return Value: It returns a Task of IServiceCollection which represents the asynchronous operation.

Usage Example:
```csharp
await services.AddQueryAsync<Order, Guid>(async (builder) =>
{
    await builder.SetStorageTypeAsync(StorageType.Local)
});
```