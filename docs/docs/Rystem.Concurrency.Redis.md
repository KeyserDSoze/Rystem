# Rystem.Concurrency.Redis

This library is useful for managing concurrency and distributed locks using Redis.

## Class: RedisConfiguration

This class holds the configuration needed to connect to a Redis server.

```csharp
public sealed class RedisConfiguration
{
    public string? ConnectionString { get; set; }
}
```

### Property: ConnectionString
- The ConnectionString property stores the connection string to your Redis server.
- It's a nullable string, meaning it can either store a connection string or `null`.

## Extension Class: ServiceCollectionExtensions

This class contains extension methods for `IServiceCollection` for adding various services including Redis lock services.

```csharp
public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisLock(this IServiceCollection services, Action<RedisConfiguration> configuration)
    
    public static IServiceCollection AddLockExecutor<TLock>(this IServiceCollection services)
        where TLock : class, ILock
    
    public static IServiceCollection AddRedisLockable(this IServiceCollection services, Action<RedisConfiguration> configuration)
    
    public static IServiceCollection AddRaceConditionWithRedis(this IServiceCollection services, Action<RedisConfiguration> configuration)
}
```

### Method: AddRedisLock

- This method adds Redis lock services to the `IServiceCollection`.

```csharp
public static IServiceCollection AddRedisLock(
  this IServiceCollection services, 
  Action<RedisConfiguration> configuration)
```
#### Parameters:
- `services` (`IServiceCollection`): The collection that this method extends.
- `configuration` (`Action<RedisConfiguration>`): The action used to configure the `RedisConfiguration`.

#### Return Value:
This method returns the `IServiceCollection` after adding the Redis lock services, allowing for further chaining of configuration methods.

#### Usage Example:
```csharp
IServiceCollection services = new ServiceCollection();
services.AddRedisLock(config =>
{
    config.ConnectionString = "Your Redis Connection String";
});
```

### Method: AddLockExecutor

- This method adds a lock executor service of a specific type to the `IServiceCollection`.

```csharp
public static IServiceCollection AddLockExecutor<TLock>(this IServiceCollection services)
    where TLock : class, ILock
```
#### Parameters:
- `services` (`IServiceCollection`): The collection that this method extends.

#### Return Value:
This method returns the `IServiceCollection` after adding the lock executor service, allowing for further chaining of configuration methods.

### Method: AddRedisLockable

- This method adds Redis lockable services to the `IServiceCollection`.

```csharp
public static IServiceCollection AddRedisLockable(
  this IServiceCollection services, 
  Action<RedisConfiguration> configuration)
```
#### Parameters:
- `services` (`IServiceCollection`): The collection that this method extends.
- `configuration` (`Action<RedisConfiguration>`): The action used to configure the `RedisConfiguration`.

#### Return Value:
This method returns the `IServiceCollection` after adding the Redis lockable services, allowing for further chaining of configuration methods.

### Method: AddRaceConditionWithRedis

- This method adds services for managing race conditions with Redis to the `IServiceCollection`.

```csharp
public static IServiceCollection AddRaceConditionWithRedis(
  this IServiceCollection services, 
  Action<RedisConfiguration> configuration)
```
#### Parameters:
- `services` (`IServiceCollection`): The collection that this method extends.
- `configuration` (`Action<RedisConfiguration>`): The action used to configure the `RedisConfiguration`.

#### Return Value:
This method returns the `IServiceCollection` after adding the race condition management services, allowing for further chaining of configuration methods.

## Class: RedisLock

The RedisLock class implements the `ILockable` interface and provides methods for acquiring and releasing a lock via the Redis server. The lock is obtained with a given key, and if the lock is successfully obtained or released, the methods return `true`.

```csharp
public sealed class RedisLock : ILockable
{
    public RedisLock(IConnectionMultiplexer connectionMultiplexer)
    
    public Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null)
    
    public async Task<bool> IsAcquiredAsync(string key)
    
    public async Task<bool> ReleaseAsync(string key)
}
```

### Method: AcquireAsync
- This method tries to acquire a lock with the given key.

```csharp
public Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null)
```
#### Parameters:
- `key` (`string`): The key which is associated with the lock.
- `maxWindow` (`TimeSpan?`): Specifies the maximum time window for which the lock can be held. Default is `null`.

#### Return Value:
The method returns a `Task<bool>`. It returns `true` if the operation is successful.

### Method: IsAcquiredAsync
- This method checks if a lock with a given key is acquired.

```csharp
public async Task<bool> IsAcquiredAsync(string key)
```
#### Parameters:
- `key` (`string`): The key which is associated with the lock.

#### Return Value:
The method returns a `Task<bool>`. It returns `true` if the lock with the given key is acquired, else it returns `false`.

### Method: ReleaseAsync
- This method tries to release a lock with the given key.

```csharp
public async Task<bool> ReleaseAsync(string key)
```
#### Parameters:
- `key` (`string`): The key which is associated with the lock. 

#### Return Value:
The method returns a `Task<bool>`. It returns `true` if the operation is successful.