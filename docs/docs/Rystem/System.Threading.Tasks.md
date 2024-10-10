# `RystemTask` Class

## Class Overview:
This static class that provides configuration for task execution. 

| Property | Description |
| -------- | ----------- |
| WaitYourStartingThread | An option to determine whether the starting thread should wait till the task has finished |

# `RystemTaskExtensions` Class

## Class Overview:
This static class provides extension methods to `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, and `IAsyncEnumerable<T>`, providing properties such as NoContext and ToListAsync, amongst others.

## Methods:

### `NoContext()`:

**Method Name**: NoContext

**Description**: Ensures that the task continues on the captured context or not, depending on `WaitYourStartingThread` value.

**Parameters**: None.

**Return Value**: A `ConfiguredTaskAwaitable` that represents the configured task.

**Usage Example**: 
```csharp
// example task
Task exampleTask = Task.Run(() => { ... });
exampleTask.NoContext();
```

### `ToListAsync<T>()`:

**Method Name**: ToListAsync

**Description**: Converts an `IAsyncEnumerable<T>` to a `ValueTask<List<T>>`.

**Parameters**: 
- `items (IAsyncEnumerable<T>)` : The async enumerable we want to convert.

**Return Value**: A `ValueTask<List<T>>` that represents the list of items.

**Usage Example**: 
```csharp
IAsyncEnumerable<int> numbers = GetNumbers();
ValueTask<List<int>> numberList = numbers.ToListAsync();
```

### `ToResult()`:

**Method Name**: ToResult

**Description**: Gets the result of the Task/ValueTask synchronously, and blocks the execution until the task has completed. 

**Parameters**: None.

**Return Value**: Returns the result of the Task/ValueTask if it is of type `Task<T>` or `ValueTask<T>`, otherwise nothing.

**Usage Example**: 
```csharp
Task<int> exampleTask = GetNumberAsync();
int result = exampleTask.ToResult();
```

# `TaskManager` Class

## Class Overview:
This static class manages various tasks based on a specific function provided, and provides control over cancellation, concurrency and execution frequency.

## Methods:

### `WhenAll()`:

**Method Name**: WhenAll

**Description**: Executes a list of tasks concurrently and waits for all tasks to complete.

**Parameters**: 
- `task (Func<int, CancellationToken, Task>)` : Function that generates the tasks.
- `times (int)` : The number of times the task should be executed.
- `concurrentTask (int)` : The max number of tasks that will be run concurrently.
- `runEverytimeASlotIsFree (bool)` : If set to true, a new task will be started immediately when a task finishes. If false, waits till all concurrent tasks finish to start a new round.
- `cancellationToken (CancellationToken)` : A cancellation token to cancel the tasks.

**Return Value**: Returns a Task representing the asynchronous operation of executing all tasks.

**Usage Example**: 
```csharp
TaskManager.WhenAll(
    async (int i, CancellationToken token) => { ... }, //a task-taking function 
    100,  // execute 100 times
    10,  // at most 10 concurrent tasks
    true,  // don't wait for all tasks to finish to start new ones
    token // cancellation token
);
```

### `WhenAtLeast()`:

**Method Name**: WhenAtLeast

**Description**: Executes a list of tasks concurrently and waits for a minimum number of tasks to complete.

**Parameters**: 
- `task (Func<int, CancellationToken, Task>)` : Function that generates the tasks.
- `times (int)` : The number of times the task should be executed.
- `atLeast (int)` : The min number of tasks that should be completed to finish.
- `concurrentTask (int)` : The max number of tasks that will be run concurrently.
- `cancellationToken (CancellationToken)` : A cancellation token to cancel the tasks.

**Return Value**: Returns a Task representing the asynchronous operation of executing a minimum number of tasks.

**Usage Example**: 
```csharp
TaskManager.WhenAtLeast(
    async (int i, CancellationToken token) => { ... }, //a task-taking function 
    100,  // execute 100 times
    70, // wait for at least 70 tasks to complete
    10,  // at most 10 concurrent tasks
    token // cancellation token
);
```