# Rystem Nuget Package: EnumerableExtensions Class Documentation

This document provides a comprehensive guide on the usage and functionality of the methods in the EnumerableExtensions class under Rystem Nuget package. These extension methods provide additional functionality to the standard IEnumerable interface in C#. 

## Class: EnumerableExtensions 

Location: `System.Collections`

This is a static class, meaning you do not need to instantiate objects from it, and you can directly call its methods.

The class contains the following public methods:

1. ElementAt
2. SetElementAt
3. RemoveElementAt

### Method: ElementAt
This method retrieves an element at a specific index from an IEnumerable object.

- **Parameters**
  - `entities` (IEnumerable): The collection from where an element will be retrieved.
  - `index` (int): The position of the element to be retrieved from the collection.

- **Return Value**
  - Returns an object (`object?`) which is the element at the specified index. If the index is out of range, it returns null.

- **Usage Example** 

```csharp
IEnumerable list = new List<int> {1, 2, 3, 4, 5};
var element = list.ElementAt(2); 
//element now contains the value 3
```
  
### Method: SetElementAt
Replaces an element at a specific index from an IEnumerable object.

- **Parameters**
    - `entities` (IEnumerable): The collection where an element will be replaced.
    - `index` (int): The position of the element to be replaced in the collection.
    - `value` (object?): The new value that will replace the current value at the specified index. 

- **Return Value**
    - Returns a boolean. If the element is successfully replaced it returns true, else false.

- **Usage Example** 

```csharp
IEnumerable list = new List<int> {1, 2, 3, 4, 5};
bool isReplaced = list.SetElementAt(2, 9); 
//List value will be {1, 2, 9, 4, 5}
```

### Method: RemoveElementAt
Removes an element at a specific index from an IEnumerable object.

- **Parameters**
  - `entities` (IEnumerable): The collection from where an element will be removed.
  - `index` (int): The position of the element to be removed from the collection.
  - `newEntities` (out IEnumerable): Output parameter that contains the list after the removal.
  - `value` (out object?): Output parameter that contains the removed element.

- **Return Value**
  - Returns a boolean (`bool`). If the element is successfully removed it returns true, else false.

- **Usage Example** 

```csharp
IEnumerable list = new List<int> {1, 2, 3, 4, 5};
list.RemoveElementAt(2, out IEnumerable newList, out object removedValue);
// newList will be {1, 2, 4, 5}
// removedValue will be 3
```

These methods provide more flexibility and control when dealing with collections in your projects.