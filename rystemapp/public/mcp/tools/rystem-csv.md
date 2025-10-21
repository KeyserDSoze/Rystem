# CSV and Minimization

Transform any `IEnumerable` data into **CSV strings** or use **minimization** for even more compact serialization.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## CSV Serialization

### Convert to CSV

Transform any collection into a **CSV string**:

```csharp
using Rystem;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

var products = new List<Product>
{
    new Product { Id = Guid.NewGuid(), Name = "Laptop", Price = 999.99m, IsActive = true },
    new Product { Id = Guid.NewGuid(), Name = "Mouse", Price = 29.99m, IsActive = true },
    new Product { Id = Guid.NewGuid(), Name = "Keyboard", Price = 79.99m, IsActive = false }
};

string csv = products.ToCsv();

Console.WriteLine(csv);
```

**Output:**

```csv
Id,Name,Price,IsActive
a1b2c3d4-e5f6-7890-1234-567890abcdef,Laptop,999.99,True
f1e2d3c4-b5a6-7890-1234-567890abcdef,Mouse,29.99,True
g1h2i3j4-k5l6-7890-1234-567890abcdef,Keyboard,79.99,False
```

---

## Minimization (Compact Serialization)

**Minimization** is a brand new serialization format based on CSV that produces **smaller output than JSON** while maintaining type safety.

### Serialize with ToMinimize

```csharp
var products = new List<Product>
{
    new Product { Id = Guid.NewGuid(), Name = "Laptop", Price = 999.99m, IsActive = true },
    new Product { Id = Guid.NewGuid(), Name = "Mouse", Price = 29.99m, IsActive = true }
};

string minimized = products.ToMinimize();

Console.WriteLine(minimized);
// Output is compact, similar to CSV but with type information
```

### Deserialize with FromMinimization

```csharp
string minimized = GetMinimizedDataFromSomewhere();

var products = minimized.FromMinimization<List<Product>>();

foreach (var product in products)
{
    Console.WriteLine($"{product.Name}: ${product.Price}");
}
```

---

## Comparison: JSON vs CSV vs Minimization

**Example Data:**

```csharp
public class CsvModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

var models = new List<CsvModel>
{
    new CsvModel { Id = 1, Name = "Product A", Price = 10.50m },
    new CsvModel { Id = 2, Name = "Product B", Price = 20.75m },
    new CsvModel { Id = 3, Name = "Product C", Price = 30.00m }
};
```

**JSON Serialization:**

```json
[
  {"Id":1,"Name":"Product A","Price":10.50},
  {"Id":2,"Name":"Product B","Price":20.75},
  {"Id":3,"Name":"Product C","Price":30.00}
]
```

**Size**: ~140 bytes

**CSV Serialization:**

```csv
Id,Name,Price
1,Product A,10.50
2,Product B,20.75
3,Product C,30.00
```

**Size**: ~65 bytes (53% smaller than JSON)

**Minimization:**

```
1|Product A|10.50|2|Product B|20.75|3|Product C|30.00
```

**Size**: ~55 bytes (60% smaller than JSON)

---

## Real-World Examples

### Export to CSV File

```csharp
public async Task ExportProductsToCsvAsync(List<Product> products, string filePath)
{
    string csv = products.ToCsv();
    await File.WriteAllTextAsync(filePath, csv);
}

// Usage
await ExportProductsToCsvAsync(products, "products.csv");
```

### API Response with Minimization

```csharp
[HttpGet("orders/minimized")]
public async Task<IActionResult> GetOrdersMinimizedAsync()
{
    var orders = await orderRepository.QueryAsync(x => x.Status == OrderStatus.Completed);
    
    string minimized = orders.ToMinimize();
    
    return Content(minimized, "text/plain");
}
```

**Client-side:**

```csharp
var response = await httpClient.GetStringAsync("/api/orders/minimized");
var orders = response.FromMinimization<List<Order>>();
```

### Cache with Minimization

```csharp
public class ProductCache
{
    private readonly IDistributedCache _cache;
    
    public async Task SetProductsAsync(List<Product> products)
    {
        string minimized = products.ToMinimize();
        byte[] bytes = minimized.ToByteArray();
        
        await _cache.SetAsync("products", bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
    }
    
    public async Task<List<Product>> GetProductsAsync()
    {
        var bytes = await _cache.GetAsync("products");
        if (bytes == null) return null;
        
        string minimized = bytes.ConvertToString();
        return minimized.FromMinimization<List<Product>>();
    }
}
```

**Benefits:**
- ✅ **Smaller cache size** (60% smaller than JSON)
- ✅ **Faster serialization/deserialization**
- ✅ **Lower bandwidth usage**

### Bulk Data Transfer

```csharp
public class DataSyncService
{
    public async Task SyncOrdersAsync()
    {
        // Get 10,000 orders
        var orders = await orderRepository.QueryAsync(x => x.CreatedDate > DateTime.UtcNow.AddDays(-30));
        
        // Minimize for transfer (much smaller than JSON)
        string minimized = orders.ToMinimize();
        
        // Send to remote server
        await httpClient.PostAsync("/api/sync/orders", new StringContent(minimized));
        
        logger.LogInformation(
            "Synced {Count} orders. Size: {Size} bytes (vs {JsonSize} bytes for JSON)",
            orders.Count,
            minimized.Length,
            orders.ToJson().Length
        );
    }
}
```

### Report Generation

```csharp
public class ReportGenerator
{
    public async Task<string> GenerateSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        var sales = await salesRepository.QueryAsync(x => 
            x.Date >= startDate && x.Date <= endDate);
        
        var reportData = sales.Select(s => new
        {
            s.Date,
            s.ProductName,
            s.Quantity,
            s.Total
        }).ToList();
        
        // Generate CSV for Excel/Google Sheets
        return reportData.ToCsv();
    }
}

// Usage
string csv = await reportGenerator.GenerateSalesReportAsync(
    DateTime.UtcNow.AddMonths(-1),
    DateTime.UtcNow
);

await File.WriteAllTextAsync("sales_report.csv", csv);
```

---

## When to Use What?

| Format | Use Case | Pros | Cons |
|--------|----------|------|------|
| **JSON** | APIs, human-readable data | Standard, widely supported | Larger size, slower |
| **CSV** | Excel exports, reports | Human-readable, standard | No nested objects |
| **Minimization** | Cache, bulk transfers | Smallest size, fast | Not human-readable |

---

## Benefits

- ✅ **CSV**: Standard format for Excel/Google Sheets
- ✅ **Minimization**: 60% smaller than JSON
- ✅ **Type Safety**: FromMinimization maintains types
- ✅ **Performance**: Faster serialization/deserialization
- ✅ **Easy to Use**: Simple extension methods

---

## Related Tools

- **[JSON Extensions](https://rystem.net/mcp/tools/rystem-json-extensions.md)** - JSON serialization
- **[Text Extensions](https://rystem.net/mcp/tools/rystem-text-extensions.md)** - String/byte/stream utilities
- **[Content Repository](https://rystem.net/mcp/resources/content-repo.md)** - File storage

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
