# CryptoExtensions Class

The `CryptoExtensions` class is a utility class that provides methods for hashing strings and objects.

## Method: `ToHash(this string message)`

This method computes the SHA512 hash of the provided string message.

### Parameters: 

- `message`: The string that needs to be hashed.

### Return Value: 

- A hexadecimal string representing the hash value of the given string. The return type is `string`.

### Usage Example: 

```csharp
string originalString = "This is a string to hash";
string hashedString = originalString.ToHash();
```

## Method: `ToHash<T>(this T message)`

This method computes the SHA512 hash of the JSON representation of the provided object.

### Parameters: 

- `message`: The object that needs to be hashed. 

### Return Value: 

- A hexadecimal string representing the hash value of the JSON string representation of the given object. The return type is `string`.

### Usage Example: 

```csharp
Foo objectToHash = new Foo{Values = new List<string> {"aa", "bb", "cc"}, X = true};
string hashedObject = objectToHash.ToHash();
```
This method is useful when you need to hash complex data structures, such as an entire class. Please note that the class should be serializable to JSON in order for this method to work.

Above examples show several practical cases. You can hash any string or an object. `ToHash()` method also uses internally `System.Text.Json` for JSON representation of the object, so all constraints for that library apply here too.

# Test Cases

Several test cases from the `HashTest` class provide insight into usage of the `ToHash()` methods.

- Comparing the hash of an object to itself:
```csharp
var foo = new Foo()
{
    Values = new List<string>() { "aa", "bb", "cc" },
    X = true
};
Assert.Equal(foo.ToHash(), foo.ToHash());
```
- Hashing a UUID:
```csharp
var message = Guid.NewGuid();
Assert.Equal(message.ToHash(), message.ToHash());
```
- Hashing a hard-coded UUID:
```csharp
var k = Guid.Parse("41e2c840-8ba1-4c0b-8a9b-781747a5de0c");
Assert.Equal("18edf95916c3aa4fd09a754e2e799fce252b0b7a76ffff76962175ad0f9921bc13bbd675954c1121d9177ffc222622c5adecf8544acb7a844117d6b1fab4590a", k.ToHash());
```

In the `AppUser` class (`Rystem.Test.UnitTest.System.Population.Random.Models`), the `ToHash()` method is used in a computed property `HashedMainGroup` to provide a hashed representation of `MainGroup`. 

- Usage in a class field:
```csharp
public string? HashedMainGroup => MainGroup?.ToHash();
```

In the `CsvTest` class (`Rystem.Test.UnitTest.Csv`), there is another example where `ToHash()` method is used to hash the `MainGroup` field of `AppUser` class:

```csharp
_users.Add(new AppUser
{
    MainGroup = Guid.NewGuid().ToString()
});
```