# Documentation for Rystem.RepositoryFramework.MigrationTools

## Class: MigrationOptions

Represents options for data migration. This class holds configuration such as the number of concurrent inserts, source factory name, and destination factory name.

### Members

#### NumberOfConcurrentInserts

- **Desciption**: This property sets the maximum number of simultaneous insert operations on the repository.
- **Type**: int
- **Default Value**: 10

#### SourceFactoryName

- **Desciption**: This property determines the source from where data will be migrated.
- **Type**: string

#### DestinationFactoryName
- **Desciption**: This property determines the destination where data will be migrated.
- **Type**: string

---

## Class Extension: ServiceCollectionExtensions

A static extension class for IServiceCollection to aid in setting up the data migration services.

### Method: AddMigrationManager

Sets up the data migration service, injects an instance of IMigrationManager<T, TKey> for migrating data from source to destination repositories.

- **Parameters**
  1. services: An instance of `IServiceCollection`, represents the .NET Core's Dependency Injection container.
  2. options: A delegate that configures the `MigrationOptions<T, TKey>`, where T is the model used for the repository and TKey is the key to retrieve, update or delete your data from the repository.
  3. name: Optional parameter. Represents the factory name.

- **Return Value**: The method returns the updated `IServiceCollection`.

- **Usage Example**

  ```csharp
  var services = new ServiceCollection();
  services.AddMigrationManager<RepositoryModel, Guid>(
      options =>
      {
        options.DestinationFactoryName = "destinationRepository";
        options.SourceFactoryName = "sourceRepository";
        options.NumberOfConcurrentInserts = 15; 
      },
      "migrationFactory"
  );
  ```

  **Note**:

  It throws `ArgumentException` if the same value is provided for `SourceFactoryName` and `DestinationFactoryName`, as you cannot migrate from and to the same source. Always set these properties with different values.