# Complex Object Parameters in Tools

## 📦 Overview

PlayFramework fully supports **complex object parameters** in tools, enabling rich, structured data exchange between LLMs and your application logic. You can use **any .NET type**: classes, records, structs, collections, nested objects, and more.

## 🎯 Key Features

- ✅ **Any .NET Type**: Classes, records, structs, enums, primitives
- ✅ **Nested Objects**: Objects within objects (unlimited depth)
- ✅ **Collections**: Lists, arrays, dictionaries, sets
- ✅ **Multiple Parameters**: Mix primitives and complex objects freely
- ✅ **Automatic Serialization**: JSON-based with `System.Text.Json`
- ✅ **Type Safety**: Full compile-time type checking
- ✅ **Default Values**: Support for optional parameters with defaults

## 🚀 Quick Start

### Simple Complex Object

```csharp
// Domain model
public record Address(string Street, string City, string Country);

// Service interface
public interface IUserService
{
    Task<string> UpdateAddressAsync(string userId, Address newAddress);
}

// PlayFramework configuration
services.AddPlayFramework(builder =>
{
    builder
        .AddScene(sceneBuilder =>
        {
            sceneBuilder
                .WithName("UserManagement")
                .WithService<IUserService>(serviceBuilder =>
                {
                    serviceBuilder.WithMethod(
                        x => x.UpdateAddressAsync(default, default),
                        "updateAddress",
                        "Update user's address");
                });
        });
});
```

### LLM Function Call

The LLM will call the tool with structured JSON:

```json
{
  "name": "updateAddress",
  "arguments": {
    "userId": "user-123",
    "newAddress": {
      "street": "Via Roma 123",
      "city": "Milano",
      "country": "Italia"
    }
  }
}
```

## 📋 Supported Types

### Primitives
```csharp
string name
int age
decimal price
bool isActive
DateTime createdAt
Guid id
```

### Classes & Records
```csharp
public class User
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public record Address(string Street, string City);
```

### Enums
```csharp
public enum Status { Pending, Active, Completed }

Task ProcessAsync(string id, Status status);
```

### Collections
```csharp
List<string> tags
string[] categories
Dictionary<string, object> metadata
HashSet<int> ids
```

### Nested Objects
```csharp
public record Customer(
    string Name,
    Address HomeAddress,
    Address? BillingAddress,
    List<ContactMethod> ContactMethods);

public record Address(string Street, string City, string Country);
public record ContactMethod(string Type, string Value);
```

## 🎨 Real-World Examples

### Example 1: E-Commerce Order

```csharp
// Domain models
public record Customer(
    string Name,
    string Email,
    Address ShippingAddress);

public record Address(
    string Street,
    string City,
    string PostalCode,
    string Country);

public record OrderItem(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record PaymentInfo(
    string Method,
    string CardLast4Digits);

// Service
public interface IOrderService
{
    Task<Order> CreateOrderAsync(
        Customer customer,
        List<OrderItem> items,
        PaymentInfo payment,
        string? notes = null);
}

// Setup
services.AddPlayFramework(builder =>
{
    builder
        .AddScene(sceneBuilder =>
        {
            sceneBuilder
                .WithName("OrderProcessing")
                .WithService<IOrderService>(serviceBuilder =>
                {
                    serviceBuilder.WithMethod(
                        x => x.CreateOrderAsync(default, default, default, default),
                        "createOrder",
                        "Create a new order with customer info, items, and payment details");
                });
        });
});
```

**LLM Call:**
```json
{
  "name": "createOrder",
  "arguments": {
    "customer": {
      "name": "Mario Rossi",
      "email": "mario@example.com",
      "shippingAddress": {
        "street": "Via Roma 123",
        "city": "Milano",
        "postalCode": "20100",
        "country": "Italia"
      }
    },
    "items": [
      {
        "productId": "prod-123",
        "productName": "Laptop Dell XPS 15",
        "quantity": 1,
        "unitPrice": 1499.99
      },
      {
        "productId": "prod-456",
        "productName": "Mouse Logitech MX Master",
        "quantity": 2,
        "unitPrice": 99.99
      }
    ],
    "payment": {
      "method": "CreditCard",
      "cardLast4Digits": "1234"
    },
    "notes": "Please deliver between 9-12 AM"
  }
}
```

### Example 2: Calendar Event Creation

```csharp
// Domain models
public record EventDateTime(DateTime Start, DateTime End, string TimeZone);

public record Attendee(
    string Email,
    string Name,
    AttendeeStatus Status,
    bool Optional);

public enum AttendeeStatus { Pending, Accepted, Declined, Tentative }

public record Reminder(
    int MinutesBefore,
    ReminderMethod Method);

public enum ReminderMethod { Email, SMS, Popup }

public record RecurrenceRule(
    RecurrenceFrequency Frequency,
    int Interval,
    DateTime? Until,
    int? Count);

public enum RecurrenceFrequency { Daily, Weekly, Monthly, Yearly }

// Service
public interface ICalendarService
{
    Task<CalendarEvent> CreateEventAsync(
        string title,
        string? description,
        EventDateTime dateTime,
        List<Attendee> attendees,
        string? location,
        List<Reminder>? reminders,
        RecurrenceRule? recurrence);
}

// Setup
services.AddPlayFramework(builder =>
{
    builder
        .AddScene(sceneBuilder =>
        {
            sceneBuilder
                .WithName("Calendar")
                .WithService<ICalendarService>(serviceBuilder =>
                {
                    serviceBuilder.WithMethod(
                        x => x.CreateEventAsync(
                            default, // title
                            default, // description
                            default, // dateTime
                            default, // attendees
                            default, // location
                            default, // reminders
                            default), // recurrence
                        "createCalendarEvent",
                        "Create a calendar event with attendees, reminders, and recurrence");
                });
        });
});
```

**LLM Call:**
```json
{
  "name": "createCalendarEvent",
  "arguments": {
    "title": "Weekly Team Standup",
    "description": "Weekly sync meeting for the development team",
    "dateTime": {
      "start": "2024-01-15T10:00:00",
      "end": "2024-01-15T10:30:00",
      "timeZone": "Europe/Rome"
    },
    "attendees": [
      {
        "email": "alice@company.com",
        "name": "Alice",
        "status": "Accepted",
        "optional": false
      },
      {
        "email": "bob@company.com",
        "name": "Bob",
        "status": "Pending",
        "optional": true
      }
    ],
    "location": "Conference Room A",
    "reminders": [
      {
        "minutesBefore": 15,
        "method": "Email"
      },
      {
        "minutesBefore": 5,
        "method": "Popup"
      }
    ],
    "recurrence": {
      "frequency": "Weekly",
      "interval": 1,
      "until": "2024-12-31T23:59:59",
      "count": null
    }
  }
}
```

### Example 3: Batch Processing with Metadata

```csharp
// Domain models
public record BatchItem(
    string Id,
    string Name,
    string Category,
    Dictionary<string, object> Metadata,
    List<string> Tags);

public record ProcessingOptions(
    bool ValidateSchema,
    bool EnableParallelProcessing,
    int MaxRetries,
    TimeSpan Timeout);

// Service
public interface IBatchService
{
    Task<BatchResult> ProcessItemsAsync(
        List<BatchItem> items,
        ProcessingOptions options);
}

// Setup
services.AddPlayFramework(builder =>
{
    builder
        .AddScene(sceneBuilder =>
        {
            sceneBuilder
                .WithName("BatchProcessing")
                .WithService<IBatchService>(serviceBuilder =>
                {
                    serviceBuilder.WithMethod(
                        x => x.ProcessItemsAsync(default, default),
                        "processBatch",
                        "Process a batch of items with custom options");
                });
        });
});
```

**LLM Call:**
```json
{
  "name": "processBatch",
  "arguments": {
    "items": [
      {
        "id": "item-1",
        "name": "Product A",
        "category": "Electronics",
        "metadata": {
          "weight": 2.5,
          "color": "Black",
          "manufacturer": "ACME Corp",
          "inStock": true
        },
        "tags": ["featured", "bestseller", "new"]
      },
      {
        "id": "item-2",
        "name": "Product B",
        "category": "Books",
        "metadata": {
          "pages": 350,
          "author": "John Doe",
          "isbn": "978-1234567890",
          "publishYear": 2023
        },
        "tags": ["fiction", "award-winner"]
      }
    ],
    "options": {
      "validateSchema": true,
      "enableParallelProcessing": true,
      "maxRetries": 3,
      "timeout": "00:05:00"
    }
  }
}
```

## 🔧 How It Works

### Serialization Flow

```
1. LLM generates function call with JSON arguments
   ↓
2. PlayFramework receives FunctionCallContent
   ↓
3. ServiceMethodTool.ExecuteAsync() is called
   ↓
4. DeserializeArguments() parses JSON
   ↓
5. For each parameter:
   - Extract JsonElement from arguments dictionary
   - Deserialize using System.Text.Json
   - Handle special cases (default values, nullables)
   ↓
6. Invoke method with deserialized objects
   ↓
7. Return result (serialized back to JSON)
```

### Deserialization Code

From `ServiceMethodTool.cs`:

```csharp
private static object?[] DeserializeArguments(string argumentsJson, ParameterInfo[] parameters)
{
    // Parse JSON into dictionary
    var argsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
    
    var args = new object?[parameters.Length];
    
    for (int i = 0; i < parameters.Length; i++)
    {
        var param = parameters[i];
        
        if (argsDict.TryGetValue(param.Name!, out var value))
        {
            // ✅ Deserialize ANY type with System.Text.Json
            args[i] = JsonSerializer.Deserialize(value.GetRawText(), param.ParameterType);
        }
        else if (param.HasDefaultValue)
        {
            args[i] = param.DefaultValue; // Use default if not provided
        }
        else if (param.ParameterType.IsValueType)
        {
            args[i] = Activator.CreateInstance(param.ParameterType); // Default struct
        }
        else
        {
            args[i] = null; // Null for reference types
        }
    }
    
    return args;
}
```

## 📊 Type Mapping

### JSON to .NET Mapping

| JSON Type | .NET Type | Example |
|-----------|-----------|---------|
| `string` | `string` | `"Hello"` |
| `number` | `int`, `long`, `decimal`, `double` | `42`, `3.14` |
| `boolean` | `bool` | `true`, `false` |
| `null` | `null`, `Nullable<T>` | `null` |
| `object` | `class`, `record`, `struct` | `{ "name": "John" }` |
| `array` | `List<T>`, `T[]`, `IEnumerable<T>` | `[1, 2, 3]` |

### Advanced Types

**Nullable Types:**
```csharp
Address? optionalAddress  // null or object
int? optionalAge         // null or integer
```

**Dictionaries:**
```json
{
  "metadata": {
    "key1": "value1",
    "key2": 123,
    "key3": true
  }
}
```
Maps to: `Dictionary<string, object>` or `Dictionary<string, JsonElement>`

**DateTimes:**
```json
{
  "createdAt": "2024-01-15T10:30:00Z"
}
```
Maps to: `DateTime` (ISO 8601 format)

**Enums:**
```json
{
  "status": "Active"
}
```
Maps to: `enum Status { Pending, Active, Completed }`

## 🧪 Testing Complex Objects

```csharp
[Fact]
public async Task ExecuteAsync_WithComplexObjects_DeserializesCorrectly()
{
    // Arrange
    services.AddPlayFramework(builder =>
    {
        builder
            .AddScene(sceneBuilder =>
            {
                sceneBuilder
                    .WithService<IUserService>(serviceBuilder =>
                    {
                        serviceBuilder.WithMethod(
                            x => x.CreateUserAsync(
                                default,  // string name
                                default,  // Address address
                                default,  // ContactInfo contact
                                default), // Preferences preferences
                            "createUser",
                            "Create user with full profile");
                    });
            });
    });

    // Mock chat client that returns complex object arguments
    services.AddSingleton<IChatClient>(sp => new MockComplexObjectChatClient());

    var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();

    // Act
    await foreach (var response in sceneManager.ExecuteAsync("Create user", settings))
    {
        // Process responses...
    }

    // Assert - Verify complex objects were deserialized correctly
    var userService = serviceProvider.GetRequiredService<IUserService>() as UserService;
    Assert.NotNull(userService.LastCreatedUser);
    Assert.Equal("Via Roma 123", userService.LastCreatedUser.Address.Street);
}
```

## 🛡️ Best Practices

### 1. Use Immutable Types

```csharp
// ✅ Good: Records are immutable and concise
public record Address(string Street, string City, string Country);

// ❌ Avoid: Mutable classes are error-prone
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}
```

### 2. Provide Clear Descriptions

```csharp
serviceBuilder.WithMethod(
    x => x.CreateOrderAsync(default, default, default),
    "createOrder",
    "Create a new order. Parameters: " +
    "customer (name, email, address), " +
    "items (list of products with quantities), " +
    "payment (method and card info)");
```

### 3. Use Optional Parameters

```csharp
Task<Result> CreateUserAsync(
    string name,
    Address address,
    ContactInfo? contact = null,  // Optional
    string? notes = null);         // Optional
```

### 4. Validate Complex Objects

```csharp
public async Task<Order> CreateOrderAsync(Customer customer, List<OrderItem> items)
{
    // Validate deserialized objects
    if (string.IsNullOrWhiteSpace(customer.Email))
        throw new ArgumentException("Customer email is required");
    
    if (items.Count == 0)
        throw new ArgumentException("At least one item is required");
    
    // Process order...
}
```

### 5. Use Value Objects

```csharp
// ✅ Good: Encapsulates validation logic
public record Email
{
    private readonly string _value;
    
    public Email(string value)
    {
        if (!value.Contains("@"))
            throw new ArgumentException("Invalid email");
        _value = value;
    }
    
    public override string ToString() => _value;
}

// Usage
Task CreateUserAsync(string name, Email email, Address address);
```

### 6. Document Expected Structure

```csharp
/// <summary>
/// Creates a new order.
/// </summary>
/// <param name="customer">
/// Customer information including:
/// - Name (required)
/// - Email (required, valid email format)
/// - ShippingAddress (street, city, country)
/// </param>
/// <param name="items">
/// List of order items, each containing:
/// - ProductId (required)
/// - Quantity (required, > 0)
/// - UnitPrice (required, >= 0)
/// </param>
Task<Order> CreateOrderAsync(Customer customer, List<OrderItem> items);
```

## 🔗 Related Features

- **Tool Calling**: [TOOL_CALLING.md](./TOOL_CALLING.md) - General tool calling documentation
- **Streaming**: [STREAMING.md](./STREAMING.md) - Text responses can be streamed
- **Cost Tracking**: [COST_TRACKING.md](./COST_TRACKING.md) - Track costs of tool executions
- **Budget Limit**: [BUDGET_LIMIT.md](./BUDGET_LIMIT.md) - Control execution costs

## 🎯 Summary

Complex object parameters provide:
- ✅ **Rich Data Exchange**: Pass structured data between LLM and code
- ✅ **Type Safety**: Compile-time checking for all parameters
- ✅ **Automatic Serialization**: JSON-based with System.Text.Json
- ✅ **Nested Objects**: Unlimited nesting depth
- ✅ **Collections**: Lists, arrays, dictionaries fully supported
- ✅ **Flexible**: Mix primitives and complex objects freely

Perfect for enterprise applications requiring rich, structured interactions with AI! 🚀
