---
title: Discriminated Union with AnyOf
description: Implement type-safe discriminated unions in C# with AnyOf - supports up to 8 types, automatic JSON serialization/deserialization with intelligent type detection, pattern matching, and selector attributes
---

# Discriminated Union with AnyOf

## What is a Discriminated Union?

A **discriminated union** is a type that can hold **one of several predefined types** at a time. It provides a way to represent and operate on data that may take different forms, ensuring **type safety** and improving **code readability**.

Unlike traditional inheritance or interfaces, discriminated unions allow you to work with completely unrelated types in a type-safe manner.

**Example:**

```csharp
AnyOf<int, string, bool> value;
```

The `value` can hold an integer, a string, or a boolean, but **never more than one type at a time**.

---

## Why Use AnyOf Instead of Interfaces?

Traditional C# requires a common interface or base class to work with multiple types polymorphically:

```csharp
// Traditional approach - requires interface
public interface IResult { }
public class SuccessResult : IResult { public string Data { get; set; } }
public class ErrorResult : IResult { public int Code { get; set; } }

public IResult GetResult() => new SuccessResult { Data = "OK" };
```

**With AnyOf**, you don't need interfaces:

```csharp
// AnyOf approach - no interfaces needed
public class SuccessResult { public string Data { get; set; } }
public class ErrorResult { public int Code { get; set; } }

public AnyOf<SuccessResult, ErrorResult> GetResult()
{
    if (success)
        return new SuccessResult { Data = "OK" };
    else
        return new ErrorResult { Code = 500 };
}
```

**Benefits:**
- ✅ No need to define interfaces or base classes
- ✅ Works with completely unrelated types
- ✅ Type safety at compile time
- ✅ Automatic JSON serialization with intelligent type detection
- ✅ Pattern matching support

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Quick Start

### Basic Usage

```csharp
using Rystem;

// Define a union that can be int OR string
AnyOf<int, string> value;

// Implicit conversion from int
value = 42;

// Implicit conversion from string
value = "Hello";

// Check which type is currently stored
if (value.Is<int>())
{
    int number = value.AsT0; // Access as first type (int)
    Console.WriteLine($"Integer: {number}");
}
else if (value.Is<string>())
{
    string text = value.AsT1; // Access as second type (string)
    Console.WriteLine($"String: {text}");
}
```

### Returning from Methods

```csharp
public AnyOf<int, string> GetSomething(bool check)
{
    if (check)
        return 42;           // Returns int
    else
        return "Hello";      // Returns string
}

var result = GetSomething(true);
result.Switch(
    i => Console.WriteLine($"Got integer: {i}"),
    s => Console.WriteLine($"Got string: {s}")
);
```

---

## Supported Types

AnyOf supports up to **8 types**:

- `AnyOf<T0, T1>` - 2 types
- `AnyOf<T0, T1, T2>` - 3 types
- `AnyOf<T0, T1, T2, T3>` - 4 types
- ... up to ...
- `AnyOf<T0, T1, T2, T3, T4, T5, T6, T7>` - 8 types

**Example with 4 types:**

```csharp
public AnyOf<int, string, bool, DateTime> GetValue(int choice)
{
    return choice switch
    {
        1 => 42,
        2 => "Hello",
        3 => true,
        4 => DateTime.Now,
        _ => throw new ArgumentException()
    };
}
```

---

## Accessing Values

### Type Checking

```csharp
AnyOf<int, string, bool> value = "Hello";

// Check if value is of specific type
bool isInt = value.Is<int>();        // false
bool isString = value.Is<string>();  // true
bool isBool = value.Is<bool>();      // false

// Check by index
bool isFirstType = value.IsT0;       // false (int)
bool isSecondType = value.IsT1;      // true (string)
bool isThirdType = value.IsT2;       // false (bool)
```

### Retrieving Values

```csharp
AnyOf<int, string, bool> value = "Hello";

// Access by type index
string text = value.AsT1;  // "Hello"
// int number = value.AsT0; // Throws exception if not int

// Safe access with TryGet
if (value.TryGet<string>(out var result))
{
    Console.WriteLine($"Got string: {result}");
}
```

---

## Pattern Matching

### Match Method (Returns Value)

The `Match` method allows you to provide **delegates for each possible type**, returning a value based on the stored type.

```csharp
var union = new AnyOf<int, string>(42);

var result = union.Match(
    i => $"Integer: {i}",
    s => $"String: {s}"
);

Console.WriteLine(result); // Outputs: "Integer: 42"
```

**With 3 types:**

```csharp
var union = new AnyOf<int, string, bool>(true);

var result = union.Match(
    i => $"Number: {i}",
    s => $"Text: {s}",
    b => $"Boolean: {b}"
);

Console.WriteLine(result); // Outputs: "Boolean: True"
```

### Switch Method (Performs Actions)

The `Switch` method allows you to **perform different actions** based on the stored type without returning a value.

```csharp
var union = new AnyOf<int, string>("Hello");

union.Switch(
    i => Console.WriteLine($"Integer: {i}"),
    s => Console.WriteLine($"String: {s}")
);
// Outputs: "String: Hello"
```

### Async Pattern Matching

**Async Match Example:**

```csharp
var union = new AnyOf<int, string>("Hello");

var result = await union.MatchAsync(
    async i => await Task.FromResult($"Integer: {i}"),
    async s => await Task.FromResult($"String: {s}")
);

Console.WriteLine(result); // Outputs: "String: Hello"
```

**Async Switch Example:**

```csharp
await union.SwitchAsync(
    async i => 
    { 
        await Task.Delay(100); 
        Console.WriteLine($"Integer: {i}"); 
    },
    async s => 
    { 
        await Task.Delay(100); 
        Console.WriteLine($"String: {s}"); 
    }
);
```

---

## JSON Serialization and Deserialization

One of the **most powerful features** of AnyOf is its **automatic JSON integration** with **intelligent type detection**.

### How It Works: Signature-Based Deserialization

When deserializing JSON, the library uses a **"Signature"** mechanism:

1. Analyzes the **property names** present in the JSON
2. Matches them to a predefined signature for each class in the union
3. Instantiates the correct class based on the match

**Example:**

```csharp
public class FirstClass
{
    public string FirstProperty { get; set; }
    public string SecondProperty { get; set; }
}

public class SecondClass
{
    public string FirstProperty { get; set; }
    public string SecondProperty { get; set; }
}

public class Container
{
    public AnyOf<FirstClass, SecondClass> Value { get; set; }
}
```

**Serialization:**

```csharp
var container = new Container
{
    Value = new FirstClass 
    { 
        FirstProperty = "First", 
        SecondProperty = "Second" 
    }
};

var json = container.ToJson();
// {"Value":{"FirstProperty":"First","SecondProperty":"Second"}}
```

**Deserialization:**

```csharp
var deserialized = json.FromJson<Container>();
// Automatically detects and deserializes as FirstClass
```

### Complete Example

```csharp
public class TestClass
{
    public AnyOf<FirstClass, SecondClass> OneClass_String { get; set; }
    public AnyOf<SecondClass, FirstClass> SecondClass_OneClass { get; set; }
    public AnyOf<FirstClass, string> OneClass_string__2 { get; set; }
    public AnyOf<bool, int> Bool_Int { get; set; }
    public AnyOf<decimal, bool> Decimal_Bool { get; set; }
}

var testClass = new TestClass
{
    OneClass_String = new FirstClass 
    { 
        FirstProperty = "OneClass_String.FirstProperty", 
        SecondProperty = "OneClass_String.SecondProperty" 
    },
    SecondClass_OneClass = new SecondClass
    {
        FirstProperty = "SecondClass_OneClass.FirstProperty",
        SecondProperty = "SecondClass_OneClass.SecondProperty"
    },
    OneClass_string__2 = "ExampleString",
    Bool_Int = 3,
    Decimal_Bool = true
};

// Serialize
var json = testClass.ToJson();

// Deserialize with automatic type detection
var deserialized = json.FromJson<TestClass>();

// Access values
Console.WriteLine(deserialized.OneClass_String.AsT0.FirstProperty); 
// Outputs: OneClass_String.FirstProperty
```

---

## Handling Ambiguous Classes with Attributes

When two classes have the **same properties** (same signature), you need attributes to disambiguate them.

### AnyOfJsonSelector Attribute

Use `[AnyOfJsonSelector]` to specify which **property value** identifies each class.

**Example:**

```csharp
public class ChosenClass
{
    public AnyOf<TheFirstChoice, TheSecondChoice>? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public class TheFirstChoice
{
    [AnyOfJsonSelector("first")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}

public class TheSecondChoice
{
    [AnyOfJsonSelector("third", "second")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}
```

**How it works:**

1. **If `Type == "first"`** → Deserializes as `TheFirstChoice`
2. **If `Type == "third"` or `Type == "second"`** → Deserializes as `TheSecondChoice`

**Test Case 1:**

```csharp
var testClass = new ChosenClass
{
    FirstProperty = new TheSecondChoice
    {
        Type = "first",  // Selector matches TheFirstChoice
        Flexy = 1,
    }
};

var json = testClass.ToJson();
var deserialized = json.FromJson<ChosenClass>();

Assert.True(deserialized.FirstProperty.Is<TheFirstChoice>());
```

**Test Case 2:**

```csharp
var testClass = new ChosenClass
{
    FirstProperty = new TheSecondChoice
    {
        Type = "third",  // Selector matches TheSecondChoice
        Flexy = 1,
    }
};

var json = testClass.ToJson();
var deserialized = json.FromJson<ChosenClass>();

Assert.True(deserialized.FirstProperty.Is<TheSecondChoice>());
```

---

## Advanced Attributes

### AnyOfJsonDefault - Set Default Class

Mark a class as the **default choice** when no other match is found:

```csharp
[AnyOfJsonDefault]
public sealed class RunResult : ApiBaseResponse
{
    // Default fallback class
}
```

### AnyOfJsonRegexSelector - Regex Matching

Use **regex patterns** to match property values:

```csharp
public sealed class FifthGetClass
{
    [AnyOfJsonRegexSelector("fift[^.]*.")]
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}
```

- If `FirstProperty` matches the regex `fift[^.]*.`, deserializes as `FifthGetClass`

### AnyOfJsonClassSelector - Class-Level Selector

Apply selectors at the **class level** instead of property level:

```csharp
[AnyOfJsonClassSelector(nameof(FirstProperty), "first.F")]
public sealed class FirstGetClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}
```

- If `FirstProperty == "first.F"`, deserializes as `FirstGetClass`

### AnyOfJsonRegexClassSelector - Class-Level Regex

Apply **regex selectors** at the class level:

```csharp
[AnyOfJsonRegexClassSelector(nameof(FirstProperty), "secon[^.]*.[^.]*")]
public sealed class SecondGetClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}
```

- If `FirstProperty` matches `secon[^.]*.[^.]*`, deserializes as `SecondGetClass`

---

## Real-World Use Cases

### API Response Handling

```csharp
public class SuccessResponse
{
    public string Data { get; set; }
    public int StatusCode { get; set; }
}

public class ErrorResponse
{
    public string Message { get; set; }
    public int ErrorCode { get; set; }
}

public class ApiClient
{
    public async Task<AnyOf<SuccessResponse, ErrorResponse>> CallApiAsync()
    {
        var response = await httpClient.GetAsync("/api/endpoint");
        
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();
            return new SuccessResponse 
            { 
                Data = data, 
                StatusCode = (int)response.StatusCode 
            };
        }
        else
        {
            return new ErrorResponse 
            { 
                Message = "Request failed", 
                ErrorCode = (int)response.StatusCode 
            };
        }
    }
}

// Usage
var result = await apiClient.CallApiAsync();
result.Switch(
    success => Console.WriteLine($"Success: {success.Data}"),
    error => Console.WriteLine($"Error: {error.Message}")
);
```

### Domain Events

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public decimal Total { get; set; }
}

public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; }
}

public class OrderShippedEvent
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; }
}

public class EventHandler
{
    public void HandleEvent(AnyOf<OrderCreatedEvent, OrderCancelledEvent, OrderShippedEvent> domainEvent)
    {
        domainEvent.Switch(
            created => ProcessOrderCreated(created),
            cancelled => ProcessOrderCancelled(cancelled),
            shipped => ProcessOrderShipped(shipped)
        );
    }
}
```

### Validation Results

```csharp
public class ValidationSuccess
{
    public string Message { get; set; }
}

public class ValidationError
{
    public List<string> Errors { get; set; }
}

public class Validator
{
    public AnyOf<ValidationSuccess, ValidationError> Validate(User user)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(user.Email))
            errors.Add("Email is required");
        
        if (user.Age < 18)
            errors.Add("User must be 18 or older");
        
        if (errors.Any())
            return new ValidationError { Errors = errors };
        
        return new ValidationSuccess { Message = "Validation successful" };
    }
}

// Usage
var result = validator.Validate(user);
var message = result.Match(
    success => success.Message,
    error => string.Join(", ", error.Errors)
);
```

---

## Benefits of Using AnyOf

1. **Type Safety**: Ensures only predefined types are used at compile time
2. **No Interfaces Required**: Works with completely unrelated types
3. **Automatic JSON Support**: Intelligent type detection during deserialization
4. **Code Clarity**: Reduces boilerplate for type management
5. **Pattern Matching**: Clean, functional-style code with `Match` and `Switch`
6. **Flexible Disambiguation**: Multiple attributes for complex scenarios

---

## Related Tools

- **[Repository Pattern Setup](https://rystem.net/mcp/tools/repository-setup.md)** - Use AnyOf for flexible repository return types
- **[DDD Single Domain](https://rystem.net/mcp/tools/ddd-single-domain.md)** - Use AnyOf for domain events
- **[JSON Extensions](https://rystem.net/mcp/tools/rystem-json-extensions.md)** - JSON serialization utilities

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
