---
title: JSON Extensions
description: Simple JSON serialization and deserialization with ToJson() and FromJson<T>() - no more JsonSerializer.Serialize boilerplate
---

# JSON Extensions

Simple **JSON serialization and deserialization** extension methods. No more `JsonSerializer.Serialize` boilerplate!

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Quick Start

### Serialize to JSON

```csharp
using Rystem;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

var user = new User
{
    Id = Guid.NewGuid(),
    Name = "John Doe",
    Email = "john@example.com"
};

string json = user.ToJson();

Console.WriteLine(json);
// {"Id":"a1b2c3d4-e5f6-7890-1234-567890abcdef","Name":"John Doe","Email":"john@example.com"}
```

### Deserialize from JSON

```csharp
string json = "{\"Id\":\"a1b2c3d4-e5f6-7890-1234-567890abcdef\",\"Name\":\"John Doe\",\"Email\":\"john@example.com\"}";

var user = json.FromJson<User>();

Console.WriteLine($"Name: {user.Name}, Email: {user.Email}");
```

---

## Before and After

### Before (Standard C#)

```csharp
// Serialize
var options = new JsonSerializerOptions { WriteIndented = false };
string json = JsonSerializer.Serialize(user, options);

// Deserialize
var user = JsonSerializer.Deserialize<User>(json, options);
```

### After (With Rystem)

```csharp
// Serialize
string json = user.ToJson();

// Deserialize
var user = json.FromJson<User>();
```

**Benefits:**
- ✅ Less boilerplate
- ✅ Cleaner code
- ✅ Easier to read
- ✅ No need to remember JsonSerializer class name

---

## Real-World Examples

### API Response Handling

```csharp
public async Task<User> GetUserAsync(Guid userId)
{
    var response = await httpClient.GetAsync($"/api/users/{userId}");
    var json = await response.Content.ReadAsStringAsync();
    
    return json.FromJson<User>();
}
```

### Configuration Loading

```csharp
public class ConfigManager
{
    public AppConfig LoadConfig(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return json.FromJson<AppConfig>();
    }
    
    public void SaveConfig(AppConfig config, string filePath)
    {
        string json = config.ToJson();
        File.WriteAllText(filePath, json);
    }
}
```

### Cache Storage

```csharp
public class CacheService
{
    private readonly IDistributedCache _cache;
    
    public async Task SetAsync<T>(string key, T value)
    {
        string json = value.ToJson();
        byte[] bytes = json.ToByteArray();
        await _cache.SetAsync(key, bytes);
    }
    
    public async Task<T> GetAsync<T>(string key)
    {
        var bytes = await _cache.GetAsync(key);
        if (bytes == null) return default;
        
        string json = bytes.ConvertToString();
        return json.FromJson<T>();
    }
}
```

### Logging

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public async Task ProcessOrderAsync(Order order)
    {
        _logger.LogInformation("Processing order: {OrderJson}", order.ToJson());
        
        try
        {
            // Process order
            await ProcessAsync(order);
            
            _logger.LogInformation("Order processed successfully: {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order: {OrderJson}", order.ToJson());
            throw;
        }
    }
}
```

### Message Queue

```csharp
public class QueuePublisher
{
    private readonly IQueue _queue;
    
    public async Task PublishAsync<T>(T message)
    {
        string json = message.ToJson();
        byte[] body = json.ToByteArray();
        
        await _queue.SendAsync(body);
    }
}

public class QueueConsumer
{
    public async Task<T> ReceiveAsync<T>()
    {
        var body = await _queue.ReceiveAsync();
        string json = body.ConvertToString();
        
        return json.FromJson<T>();
    }
}
```

### Deep Clone

```csharp
public static class ObjectExtensions
{
    public static T DeepClone<T>(this T obj)
    {
        string json = obj.ToJson();
        return json.FromJson<T>();
    }
}

// Usage
var originalOrder = new Order { Id = Guid.NewGuid(), Total = 100 };
var clonedOrder = originalOrder.DeepClone();

clonedOrder.Total = 200;
// originalOrder.Total is still 100
```

### Event Sourcing

```csharp
public class EventStore
{
    public async Task SaveEventAsync<T>(T domainEvent) where T : IDomainEvent
    {
        var eventData = new EventData
        {
            EventId = Guid.NewGuid(),
            EventType = typeof(T).Name,
            Data = domainEvent.ToJson(),
            Timestamp = DateTime.UtcNow
        };
        
        await eventRepository.InsertAsync(eventData);
    }
    
    public async Task<T> LoadEventAsync<T>(Guid eventId) where T : IDomainEvent
    {
        var eventData = await eventRepository.GetAsync(eventId);
        return eventData.Data.FromJson<T>();
    }
}
```

---

## Working with Collections

### List Serialization

```csharp
var users = new List<User>
{
    new User { Id = Guid.NewGuid(), Name = "John", Email = "john@example.com" },
    new User { Id = Guid.NewGuid(), Name = "Jane", Email = "jane@example.com" }
};

string json = users.ToJson();
var deserializedUsers = json.FromJson<List<User>>();
```

### Dictionary Serialization

```csharp
var settings = new Dictionary<string, string>
{
    { "Theme", "Dark" },
    { "Language", "en-US" }
};

string json = settings.ToJson();
var deserializedSettings = json.FromJson<Dictionary<string, string>>();
```

---

## Integration with Other Rystem Tools

### With AnyOf (Discriminated Unions)

```csharp
var result = new AnyOf<SuccessResponse, ErrorResponse>(
    new SuccessResponse { Data = "OK" }
);

string json = result.ToJson();
var deserialized = json.FromJson<AnyOf<SuccessResponse, ErrorResponse>>();
```

### With Repository Pattern

```csharp
public class ProductService
{
    private readonly IRepository<Product> _repository;
    private readonly ILogger _logger;
    
    public async Task<Product> CreateProductAsync(string productJson)
    {
        var product = productJson.FromJson<Product>();
        
        _logger.LogInformation("Creating product: {ProductJson}", product.ToJson());
        
        return await _repository.InsertAsync(product);
    }
}
```

---

## Benefits

- ✅ **Simplicity**: No more `JsonSerializer.Serialize` boilerplate
- ✅ **Readability**: Cleaner, more readable code
- ✅ **Consistency**: Same pattern across entire codebase
- ✅ **Less Typing**: Shorter method names
- ✅ **Easier Testing**: Simple to serialize/deserialize in tests

---

## Related Tools

- **[CSV and Minimization](https://rystem.net/mcp/tools/rystem-csv.md)** - Alternative serialization formats
- **[Text Extensions](https://rystem.net/mcp/tools/rystem-text-extensions.md)** - String/byte/stream utilities
- **[Discriminated Union](https://rystem.net/mcp/tools/rystem-discriminated-union.md)** - Type-safe unions with JSON support
- **[Repository API](https://rystem.net/mcp/tools/repository-api-server.md)** - JSON APIs

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
