# `ConcurrentList<T>` Class

The `ConcurrentList<T>` is a thread-safe implementation of the `IList<T>` interface. This class wraps all actions on the underlying list, with each method locking on the `_trafficLight` object to avoid race conditions and maintain thread safety.

### `Add(T item)` Method

**Purpose**: Adds an item to the `ConcurrentList<T>`.

**Parameters**:
- `T item`: The item of type `T` that should be added to the list.

**Return Value**: This method doesn't return a value.

**Usage Example**:

```csharp
var concurrentList = new ConcurrentList<int>();
concurrentList.Add(5);
```

### `Clear()` Method

**Purpose**: Removes all items from the `ConcurrentList<T>`.

**Parameters**: This method doesn't need any parameters.

**Return Value**: This method doesn't return a value.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>();
concurrentList.Add(5);
concurrentList.Clear();
```

### `Contains(T item)` Method

**Purpose**: Determines whether the `ConcurrentList<T>` contains a specific item.

**Parameters**:
- `T item`: The item to locate in the `ConcurrentList<T>`.

**Return Value**: A boolean value indicating whether the item is found in the `ConcurrentList<T>`.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>();
concurrentList.Add(5);
bool contains = concurrentList.Contains(5); // true
```

### `CopyTo(T[] array, int arrayIndex)` Method

**Purpose**: Copies the elements of the `ConcurrentList<T>` to an array, starting at a specific array index.

**Parameters**:
- `T[] array`: The one-dimensional array that is the destination of the elements copied from `ConcurrentList<T>`.
- `int arrayIndex`: The zero-based index in array at which copying begins.

**Return Value**: This method doesn't return a value.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
var targetArray = new int[5];
concurrentList.CopyTo(targetArray, 0);
```

### `GetEnumerator()` Method

**Purpose**: Returns an enumerator that iterates through the `ConcurrentList<T>`.

**Parameters**: This method doesn't need any parameters.

**Return Value**: An `IEnumerator<T>` that can be used to iterate through the collection.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
var enumerator = concurrentList.GetEnumerator();
while(enumerator.MoveNext()){
    Console.WriteLine(enumerator.Current);
}
```
### `IndexOf(T item)` Method

**Purpose**: Determines the index of a specific item in the `ConcurrentList<T>`.

**Parameters**:
- `T item`: The object to locate in the `ConcurrentList<T>`.

**Return Value**: The index of `item` if found in the list; otherwise, -1.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
int index = concurrentList.IndexOf(3); // 2
```

### `Insert(int index, T item)` Method

**Purpose**: Inserts an item to the `ConcurrentList<T>` at the specified index.

**Parameters**:
- `int index`: The zero-based index at which `item` should be inserted.
- `T item`: The object to insert into the `ConcurrentList<T>`.

**Return Value**: This method doesn't return a value.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
concurrentList.Insert(0, 0); // 0, 1, 2, 3, 4, 5
```

### `Remove(T item)` Method

**Purpose**: Removes the first occurrence of a specific object from the `ConcurrentList<T>`.

**Parameters**:
- `T item`: The object to remove from the `ConcurrentList<T>`.

**Return Value**: A boolean value indicating if the operation was successful.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
bool isRemoved = concurrentList.Remove(3); // true if 3 was successfully removed
``` 

### `RemoveAt(int index)` Method

**Purpose**: Removes the `ConcurrentList<T>` element at the specified index.

**Parameters**:
- `int index`: The zero-based index of the element to remove.

**Return Value**: This method doesn't return a value.

**Usage Example**:

```csharp 
var concurrentList = new ConcurrentList<int>(){1, 2, 3, 4, 5};
concurrentList.RemoveAt(2); // Removes 3 from the list
```

Using the test cases provided, it's clear to see how these methods are used to safely manipulate and manage data concurrently, avoiding potential issues with regular Lists and arrays in a multi-threaded environment.