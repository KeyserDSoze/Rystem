# RepositoryFramework.Cache Documentation

The `RepositoryFramework.Cache` namespace, part of the `Rystem.RepositoryFramework.Cache` NuGet package, provides classes for managing caching within a repository framework. 

**Dependencies**:
- Rystem.RepositoryFramework.Abstractions, minimum version: 6.2.0
- Microsoft.Extensions.Caching.Abstractions, minimum version: 8.0.0
- Microsoft.Extensions.Caching.Memory, minimum version: 8.0.1

Below are the descriptions of classes and their methods within this namespace.

## Class: CacheOptions<T, TKey>
This class provides settings for cache management in your repository. Settings include the expiration time for the cached data, and the allowed commands to perform operations like update/insert, delete or get on the cache.

### Properties

1. **ExpiringTime** - _TimeSpan_

   The duration after which the cached data expires.

2. **HasCommandPattern** - _bool_

   A flag indicating if Update, Insert, or Delete methods are allowed to perform operations on the cache.

3. **HasCache(method)** - _bool_

   Checks if the cache is allowed on the repository according to specified method.

4. **Methods** - _RepositoryMethods_

   Flags to set the allowed operations on the cache.

5. **Default** - _CacheOptions<T, TKey>_

   Gets the default cache options.

## Class: DistributedCacheOptions<T, TKey>
This class extends `CacheOptions<T, TKey>`, providing settings for cache management in a distributed (multi-instance) environment.

### Properties

1. **Default** - _DistributedCacheOptions<T, TKey>_

   Gets the default distributed cache options.

## RepositoryBuilderExtensions Class

This class provides extension methods to add caching mechanisms to repository, command and query patterns for both single-instance and multi-instance environments.

### Methods

1. **WithCache**, **WithDistributedCache** (for Repository, Command & Query builder)

   These methods are used to add the cache mechanism (a normal or distributed cache) to the specified repository/command/query builder.

   - **Parameters**:
     - `builder`: The builder instance to which you're adding cache mechanism.
     - `options`: Optional settings for your cache.
     - `name`: Optional name.
     - `lifetime`: Service Lifetime. Default is Singleton.

   - **Return Value**:
     - Returns the builder instance (IRepositoryBuilder, ICommandBuilder or IQueryBuilder) with applied caching mechanism.

   - **Usage Example**:
     ```csharp
     var builder = new RepositoryBuilder<MyModel, string>();
     builder.WithCache(options =>
     {
         options.ExpiringTime = TimeSpan.FromMinutes(30);
     });
     ```

2. **WithInMemoryCache** (for Repository, Command & Query builder)

   These methods are used to add in-memory cache mechanism to the specified repository/command/query builder.

   - **Parameters**:
     - `builder`: The builder instance to which you're adding cache mechanism.
     - `options`: Optional settings for your cache.
     - `name`: Optional name.

   - **Return Value**:
     - Returns the builder instance (IRepositoryBuilder, ICommandBuilder or IQueryBuilder) with applied in-memory caching mechanism.

   - **Usage Example**:
     ```csharp
     var builder = new RepositoryBuilder<MyModel, string>();
     builder.WithInMemoryCache(options =>
     {
         options.ExpiringTime = TimeSpan.FromMinutes(30);
     });
     ```

These classes also includes several private methods such as `AddCacheManager`, `WithCache`, and `WithDistributedCache` that are used internally by the public methods.

This documentation should help you understand the capabilities of the `RepositoryFramework.Cache` namespace. When you're ready to add caching to your repository, start with the `WithCache` or `WithDistributedCache` methods.