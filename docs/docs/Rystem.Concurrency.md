In the namespace `System.Threading.Concurrent`, we have three classes: LockResponse, MemoryLock, and RaceConditionResponse. Below is the documentation for each class and its methods:

## LockResponse Class:

### Method Name: `LockResponse()`

**Description**: This is the constructor of the `LockResponse` class. It initializes the properties of LockResponse.

**Parameters**: 
- `executionTime` (TimeSpan) - The total time span it took to execute the given function. 
- `exceptions` (IList<Exception>) - List of any exceptions that occurred during the execution.

**Return Value**: No return value as this is a constructor method.

### Properties:
- **ExecutionTime** - Readonly TimeSpan property for execution time
- **Exceptions** - Readonly property for Any Exceptions during Execution. The type of this property is AggregateException and can be null.
- **InException** - Readonly boolean property to check if there are any exceptions. This is a derived property which checks if `Exceptions` property is not null.

### Usage Example:

```csharp
var exceptions = new List<Exception>(){ new Exception()}; 
var response = new LockResponse(TimeSpan.FromSeconds(2), exceptions);
```

## RaceConditionResponse Class:

### Method Name: `RaceConditionResponse()`

**Description**: This is the constructor of the `RaceConditionResponse` class. It initializes the properties of RaceConditionResponse.

**Parameters**: 
- `IsExecuted` (bool) - Boolean indicating whether the operation was executed. 
- `exceptions` (IList<Exception>) - List of any exceptions that occured during the execution.

**Return Value**: No return value as this is a constructor method.

### Properties:
- **IsExecuted** - Readonly Boolean property to indicate if the operation is Executed.
- **Exceptions** - Readonly property for Any Exceptions during Execution. The type of this property is AggregateException and can be null.
- **InException** - Readonly boolean property to check if there are any exceptions. This is a derived property which checks if `Exceptions` property is not null.

### Usage Example:

```csharp
var exceptions = new List<Exception>(){ new Exception()}; 
var response = new RaceConditionResponse(true, exceptions);
```

## MemoryLock Class:

### Method Name: `AcquireAsync()`

**Description**: This method tries to acquire a lock for the provided key.

**Parameters**: 
- `key` (string) - The key for which to acquire the lock.
- `maxWindow` (TimeSpan?, nullable) - The maximum time window for which the lock can exist.

**Return Value**: This method returns boolean wrapped inside a Task indicating whether the lock was acquired.

### Method Name: `IsAcquiredAsync()`

**Description**: This method checks if a lock is acquired for the provided key.

**Parameters**: 
- `key` (string) - The key for which to check the lock.

**Return Value**: Returns a boolean wrapped inside a Task indicating whether the lock is acquired for the provided key.

### Method Name: `ReleaseAsync()`

**Description**: This method releases the lock for the provided key.

**Parameters**: 
- `key` (string) - The key for which to release the lock.

**Return Value**: Returns a boolean wrapped inside a Task indicating whether the lock was released.

### Usage Example:

```csharp
var memLock = new MemoryLock();

var isAcquired = await memLock.AcquireAsync("test");
var keyExists = await memLock.IsAcquiredAsync("test");
var isReleased = await memLock.ReleaseAsync("test");
```

In the namespace `Microsoft.Extensions.DependencyInjection`, we have extension methods to add above concurrency classes to ServiceCollection for Dependency Injection. Depending on the project need, you can add the required class to your services collection as shown below:

```csharp
var services = new ServiceCollection();
services.AddLockExecutor(); //to add LockExecutor to services
services.AddLock();         //to add LockExecutor and MemoryLock to services
services.AddInMemoryLockable(); //to add MemoryLock to services
services.AddRaceCondition(); // to add RaceCondition related services
```
This library also offers ways to integrate with custom implementations of interfaces ILock, ILockable and IRaceCondition. The provided class must implement the respective interface. This can be done using the corresponding genric Add methods as shown below:

```csharp
public class CustomLock: ILock
{
    //implementation
}
services.AddLockExecutor<CustomLock>();

public class CustomLockable: ILockable
{
    //implementation
}
services.AddLockableIntegration<CustomLockable>();

public class CustomRaceCondition: IRaceCondition
{
    //implementation
}
services.AddRaceConditionExecutor<CustomRaceCondition>();
```