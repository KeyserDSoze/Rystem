# Documentation

## CsvEngine Class

The `CsvEngine` class is an internal sealed class that helps with converting an `IEnumerable<T>` object into a string in CSV form.

### Method: Convert

Convert transforms IEnumerable of T into a CSV String.

**Method Name:** `Convert<T>`
- **Parameters:** 
  1. **values** (IEnumerable<T>) : Sequence of any consumer-provided generic object type.
 
- **Return Value:** This method returns a string which represents the given Enumerable objects in CSV format.
  
 > Note: "T" can be any class that the user wants to convert into CSV.
 
**Usage Example:**

```csharp
// Assuming T is a class Product with fields Name (string) and Price (int)
var products = new List<Product>
{
    new Product {Name = "Product1", Price = 50},
    new Product {Name = "Product2", Price = 60}
};

var csv = CsvEngine.Convert(products);
```

## CsvEngineExtensions Class

The `CsvEngineExtensions` class is a public static class which extends the functionality of IEnumerable&lt;T&gt; with a function `ToCsv`.

### Method: ToCsv

The `ToCsv` method is an extension method for `IEnumerable<T>` that converts the sequence into a CSV formatted string.

**Method Name:** `ToCsv<T>`
- **Parameters:**
  1. **values** (`IEnumerable<T>`): This is an extension method parameter representing the sequence of generic objects which need to be converted into CSV format.

- **Return Value:** This method returns a string representing the sequence in CSV format.

**Usage Example:**
```csharp
var users = new List<User>
{
    new User {Name = "John", Age = 24},
    new User {Name = "Jane", Age = 20}
};

string csvData = users.ToCsv();
```

## Test Case: CsvTest

The `CsvTest` class houses the test cases for the `CsvEngine` conversion methods. It creates a list of objects of Custom classes such as `CsvModel`, `CsvInnerModel` and `AppUser`. It then validates if the CSV conversion was successful for various types of fields, including nullable and complex types. 

``` 
 [Fact]
    public void Test()
    {
        var value = _models.ToCsv();
        Assert.NotEmpty(value);
        value = _users.ToCsv();
        Assert.NotEmpty(value);
    }
``` 
The test verifies if the CSV conversion works as expected by ensuring the string return from the conversion is not empty.