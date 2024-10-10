# Rystem NuGet Package - System.Collection.Generics Namespace Documentation

## Class: AsyncEnumerable<T>

This class is a part of the System.Collection.Generics namespace in the Rystem libraries, and is responsible for handling the creation of async enumerables. 

### Method: Empty

**Method Name:** Empty  
**Description**: This is a predefined readonly property that creates an empty `IAsyncEnumerable<T>`.

**Parameters**: This method does not accept any parameters.

**Return Value**: This method returns an `IAsyncEnumerable<T>` that is empty. Type `T` is the type of elements in the enumerable.

**Usage Example:**  
Given that this property returns an empty `IAsyncEnumerable`, it can be used when we need to return an empty version of a list in an asynchronous operation, which may be useful when no data is available to populate the list. However, this is a static property so while demonstrating the usage, we would just show accessing this property, like so:

```csharp
var emptyAsyncList = AsyncEnumerable<string>.Empty;
```  

### Method: GetEmpty

**Method Name:** GetEmpty  
**Description**: This method is a private async enumerator function that was specifically created to initialize the `Empty` property.

**Parameters**: This method does not accept any parameters.

**Return Value**: This is a private method that doesn't return anything directly. Instead, it initializes the `Empty` property with an empty `IAsyncEnumerable<T>`.

**Usage Example:**  
Since this is a private method, it typically wouldn't be used directly by the end user. The purpose of this method is to create an empty enumerable for the above `Empty` method.