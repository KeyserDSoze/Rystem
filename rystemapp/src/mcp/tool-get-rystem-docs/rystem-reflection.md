---
title: Reflection Utilities
description: Advanced reflection helpers for C# - includes NameOfCallingClass(), FetchProperties() caching, IsTheSameTypeOrASon() type checking, CreateInstance() mocking for abstracts/interfaces, and IsNullable() for nullability detection
---

# Reflection Utilities

Advanced **reflection helpers** for C# to simplify common reflection tasks with performance optimizations.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Name of Calling Class

Get the name of the class that called your method:

```csharp
using Rystem;

public class MyService
{
    public void DoSomething()
    {
        // Get calling class name
        string callerName = ReflectionHelper.NameOfCallingClass(deep: 1, fullName: false);
        
        Console.WriteLine($"Called by: {callerName}");
    }
}
```

**Parameters:**
- **`deep`**: How many levels up the call stack
  - `deep = 1`: Immediate caller
  - `deep = 2`: Caller of the caller
  - etc.
- **`fullName`**: If `true`, returns full namespace + class name

**Example:**

```csharp
public class Controller
{
    public void ProcessRequest()
    {
        var service = new Service();
        service.DoWork(); // Will print "Controller"
    }
}

public class Service
{
    public void DoWork()
    {
        string caller = ReflectionHelper.NameOfCallingClass(1, fullName: false);
        Console.WriteLine($"Called by: {caller}"); // Outputs: "Controller"
    }
}
```

---

## Cached Property, Field, and Constructor Access

Improve performance by **caching** reflection metadata:

### Fetch Properties

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Fetch and cache properties
var properties = typeof(Product).FetchProperties();

foreach (var prop in properties)
{
    Console.WriteLine($"Property: {prop.Name}, Type: {prop.PropertyType}");
}
```

### Fetch Constructors

```csharp
var constructors = typeof(Product).FetchConstructors();

foreach (var ctor in constructors)
{
    Console.WriteLine($"Constructor with {ctor.GetParameters().Length} parameters");
}
```

### Fetch Fields

```csharp
public class Config
{
    public static readonly string Version = "1.0";
    private int _counter;
}

var fields = typeof(Config).FetchFields();

foreach (var field in fields)
{
    Console.WriteLine($"Field: {field.Name}, Type: {field.FieldType}");
}
```

**Benefits:**
- ✅ **Performance**: Results are cached for subsequent calls
- ✅ **Consistency**: Always returns the same cached collection
- ✅ **Convenience**: No need to manually cache reflection metadata

---

## Type Relationship Checking

Check if a type is a **parent, child, or same type** as another:

### IsTheSameTypeOrASon

Check if an object is the same type or a **derived type**:

```csharp
public class Sulo { }
public class Zalo : Sulo { }
public class Folli : Sulo { }

Zalo zalo = new();
Zalo zalo2 = new();
Folli folli = new();
Sulo sulo = new();
object quo = new();
int x = 2;
decimal y = 3;

// Same type or child
Assert.True(zalo.IsTheSameTypeOrASon(sulo));      // Zalo is son of Sulo ✅
Assert.True(folli.IsTheSameTypeOrASon(sulo));     // Folli is son of Sulo ✅
Assert.True(zalo.IsTheSameTypeOrASon(zalo2));     // Zalo is same as Zalo ✅
Assert.True(zalo.IsTheSameTypeOrASon(quo));       // Zalo is son of object ✅
Assert.False(sulo.IsTheSameTypeOrASon(zalo));     // Sulo is NOT son of Zalo ❌
```

### IsTheSameTypeOrAParent

Check if an object is the same type or a **parent type**:

```csharp
Assert.True(sulo.IsTheSameTypeOrAParent(zalo));   // Sulo is parent of Zalo ✅
Assert.False(y.IsTheSameTypeOrAParent(x));        // decimal is NOT parent of int ❌
```

---

## Mock Abstracts and Interfaces

Create **instances of abstract classes or interfaces** at runtime:

### Mocking an Abstract Class

```csharp
public abstract class Alzio
{
    private protected string X { get; }
    public string O => X;
    public string A { get; set; }
    
    public Alzio(string x)
    {
        X = x;
    }
}

// Create instance of abstract class
var mocked = typeof(Alzio).CreateInstance("AAA") as Alzio;
mocked.A = "rrrr";

Console.WriteLine(mocked.O); // Outputs: "AAA"
Console.WriteLine(mocked.A); // Outputs: "rrrr"
```

### Alternative Syntax

```csharp
// From null instance
Alzio alzio = null!;
var mocked = alzio.CreateInstance("AAA");
mocked.A = "rrrr";

// Using Mocking class
var mocked = Mocking.CreateInstance<Alzio>("AAA");
```

### Mocking an Interface

```csharp
public interface IService
{
    string Name { get; set; }
    int Calculate(int x, int y);
}

// Create instance of interface
var mocked = Mocking.CreateInstance<IService>();
mocked.Name = "TestService";

// Note: Methods will have default behavior (return default values)
```

**Use Cases:**
- Testing without mock frameworks
- Dynamic type creation
- Plugin systems
- Proxy generation

---

## Check Nullability

Detect if properties, fields, or parameters are **nullable reference types**:

### Example Class

```csharp
private sealed class InModel
{
    public string? A { get; set; }
    public string B { get; set; }
    public string? C;
    public string D;
    
    public InModel(string? b, string c)
    {
        A = b;
        B = c;
    }
    
    public void SetSomething(string? b, string c)
    {
        A = b;
        B = c;
    }
}
```

### Check Constructor Parameters

```csharp
var type = typeof(InModel);
var constructorParameters = type.GetConstructors().First().GetParameters().ToList();

Assert.True(constructorParameters[0].IsNullable());   // string? b ✅
Assert.False(constructorParameters[1].IsNullable());  // string c ❌
```

### Check Method Parameters

```csharp
var methodParameters = type.GetMethod(nameof(InModel.SetSomething))
                           .GetParameters()
                           .ToList();

Assert.True(methodParameters[0].IsNullable());   // string? b ✅
Assert.False(methodParameters[1].IsNullable());  // string c ❌
```

### Check Properties

```csharp
var properties = type.GetProperties().ToList();

Assert.True(properties[0].IsNullable());   // string? A ✅
Assert.False(properties[1].IsNullable());  // string B ❌
```

### Check Fields

```csharp
var fields = type.GetFields().ToList();

Assert.True(fields[0].IsNullable());   // string? C ✅
Assert.False(fields[1].IsNullable());  // string D ❌
```

---

## Real-World Examples

### Validation with Nullability Check

```csharp
public class Validator
{
    public List<string> ValidateNullability<T>(T instance)
    {
        var errors = new List<string>();
        var properties = typeof(T).FetchProperties();
        
        foreach (var prop in properties)
        {
            var value = prop.GetValue(instance);
            
            if (value == null && !prop.IsNullable())
            {
                errors.Add($"Property {prop.Name} cannot be null");
            }
        }
        
        return errors;
    }
}
```

### Plugin System with Abstract Mocking

```csharp
public abstract class Plugin
{
    public abstract string Name { get; }
    public abstract void Execute();
}

public class PluginManager
{
    public Plugin LoadPlugin(Type pluginType)
    {
        if (!pluginType.IsSubclassOf(typeof(Plugin)))
            throw new ArgumentException("Type must inherit from Plugin");
        
        return pluginType.CreateInstance() as Plugin;
    }
}
```

### Type Hierarchy Checker

```csharp
public class TypeAnalyzer
{
    public bool CanAssign(object source, Type targetType)
    {
        return source.IsTheSameTypeOrASon(Activator.CreateInstance(targetType));
    }
    
    public bool IsCompatible(Type parent, Type child)
    {
        var parentInstance = parent.CreateInstance();
        var childInstance = child.CreateInstance();
        
        return childInstance.IsTheSameTypeOrASon(parentInstance);
    }
}
```

---

## Benefits

- ✅ **Performance**: Cached reflection metadata
- ✅ **Type Safety**: Check type relationships at runtime
- ✅ **Testing**: Mock abstracts/interfaces without frameworks
- ✅ **Nullability**: Detect nullable reference types
- ✅ **Debugging**: Identify calling classes

---

## Related Tools

- **[Discriminated Union](https://rystem.net/mcp/tools/rystem-discriminated-union.md)** - Type-safe unions
- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Uses reflection for entity mapping
- **[DDD](https://rystem.net/mcp/tools/ddd-single-domain.md)** - Domain model reflection

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **Unit Tests**: [ReflectionTest.cs](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Reflection/ReflectionTest.cs)
