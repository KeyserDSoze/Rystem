# Rystem.Content.Abstractions Documentation

## Class: ContentMigrationExceptionResult

This class handles the outcome of a failed migration in the content system, containing the exception information that arose and the path of the migration.

### Properties:

**1. Exception**
  - **Type**: Exception
  - **Description**: Contains the exception that resulted from a failed migration.
    
**2. Path**
  - **Type**: ContentMigrationPath
  - **Description**: Contains the migration path for which the exception was thrown.

## Class: ContentMigrationPath

This class denotes a migration path in the content system, including source and destination. It also includes a method to check if the source and destination are the same.

### Properties:

**1. From**
  - **Type**: string
  - **Description**: The source of the migration path.

**2. To**
  - **Type**: string
  - **Description**: The destination of the migration path.

**3. IsTheSame**
  - **Type**: bool
  - **Description**: A checker method conditional to return true if "From" and "To" are equal which means the source and destination are the same.

## Class: ContentMigrationResult

This class compiles the results of content migration, including paths that were migrated, those not migrated, blocked by predicate paths, and paths not migrated due to errors.

### Properties:

**1. MigratedPaths**

    - Type: List<ContentMigrationPath>
    - Description: List of migration paths that were migrated successfully.

**2. NotMigratedPaths**

    - Type: List<ContentMigrationPath>
    - Description: List of migration paths that were not migrated.

**3. NotContentPaths**

    - Type: List<ContentMigrationPath>
    - Description: List of paths that were not considered to be content and thus did not migrate.

**4. BlockedByPredicatePaths**

    - Type: List<ContentMigrationPath>
    - Description: List of migration paths that were blocked by a predicate and thus did not migrate.

**5. NotMigratedPathsForErrors**

    - Type: List<ContentMigrationExceptionResult>
    - Description: List of failed migrations due to exceptions.

## Class: ContentMigrationSettings

This class contains various settings that control the behaviour of the migration process during content migration.

### Properties:

**1. Prefix**

    - Type: string
    - Description: The prefix applied to migration paths.

**2. Predicate**

    - Type: Func<ContentRepositoryDownloadResult, bool>
    - Description: A function to filter out paths from migration. If the function returns true for a given path, it will be included in the migration; otherwise, it will be excluded.

**3. OnErrorContinue**

    - Type: bool
    - Description: If set to true, the migration process will continue even if an error is encountered. Default value is true.

**4. OverwriteIfExists**

    - Type: bool
    - Description: If set to true, existing content at the destination path will be overwritten if the same content is being migrated.

**5. ModifyDestinationPath**

    - Type: Func<string, string>
    - Description: A function to modify the destination path for a given source path when migrating content.

## Class: ServiceCollectionExtensions

This class includes extension methods for the IServiceCollection class.

### Methods:

**1. AddContentRepository**

    - Description: This method configures content repository services and registers them to the provided IServiceCollection.
    - Parameters: 
        - IServiceCollection services: An instance of IServiceCollection to which the content repository services will be added.
    - Returns: IContentRepositoryBuilder object that is used to configure content repository.
    - Usage Example:
    ```markdown
    var services = new ServiceCollection();
    services.AddContentRepository();
    ```

For the remaining classes, the properties serve to encapsulate various details of the content repository, download results, response headers, etc., and do not have notable behaviors needing to be explained. With these classes, you can manage content, handle requests and responses in content operations, set up necessary options regarding caching, encoding, languages, etc., and generally manage all data traffic related to content operations.
