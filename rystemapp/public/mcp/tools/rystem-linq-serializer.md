# LINQ Expression Serializer

Serialize and deserialize **LINQ expressions** as strings. This allows you to:
- Store expressions in databases
- Send expressions over the network
- Build dynamic queries at runtime
- Create configurable filters and sorting

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## Quick Start

### Serialize Expression to String

```csharp
using Rystem;

Expression<Func<Product, bool>> expression = p => p.Price > 100 && p.IsActive;

string serialized = expression.Serialize();

Console.WriteLine(serialized);
// Output: "p => ((p.Price > 100) AndAlso p.IsActive)"
```

### Deserialize String to Expression

```csharp
string expressionString = "p => ((p.Price > 100) AndAlso p.IsActive)";

var expression = expressionString.Deserialize<Product, bool>();

// Use with LINQ
var filteredProducts = products.Where(expression.Compile()).ToList();
```

---

## Serialize and Deserialize

### Basic Example

```csharp
public class MakeIt
{
    public string X { get; set; }
    public List<string> Samules { get; set; }
    public bool Sol { get; set; }
    public Guid E { get; set; }
    public int Id { get; set; }
    public MakeType Type { get; set; }
}

public enum MakeType
{
    Yes = 1,
    No = 2
}

// Variables used in expression
string q = "dasda";
string k = "ccccde";
bool IsOk = true;
Guid id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");
int V = 32;
MakeType qq = MakeType.No;

// Complex expression
Expression<Func<MakeIt, bool>> expression = ƒ => 
    ƒ.X == q && 
    ƒ.Samules.Any(x => x == k) && 
    ƒ.Sol && 
    (ƒ.X.Contains(q) || ƒ.Sol.Equals(IsOk)) && 
    (ƒ.E == id | ƒ.Id == V) && 
    (ƒ.Type == MakeType.Yes || ƒ.Type == qq);

// Serialize
var serialized = expression.Serialize();

Console.WriteLine(serialized);
// Output:
// "ƒ => ((((((ƒ.X == \"dasda\") AndAlso ƒ.Samules.Any(x => (x == \"ccccde\"))) AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains(\"dasda\") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))"

// Deserialize
var newExpression = serialized.Deserialize<MakeIt, bool>();

// Use with LINQ
var result = makes.Where(newExpression.Compile()).ToList();
```

---

## Deserialize and Compile in One Step

Use `DeserializeAndCompile()` to get a compiled expression directly:

```csharp
string expressionString = "p => p.Price > 100";

var compiledFunc = expressionString.DeserializeAndCompile<Product, bool>();

// Use directly with LINQ
var filteredProducts = products.Where(compiledFunc).ToList();
```

---

## Dynamic LINQ with DeserializeAsDynamic

For **dynamic queries** with `OrderBy`, `ThenBy`, etc., use `DeserializeAsDynamic()`:

```csharp
public class MakeIt
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Serialize an expression
Expression<Func<MakeIt, int>> expression = x => x.Id;
string value = expression.Serialize();

// Deserialize as dynamic
LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();

// Use with dynamic LINQ
var got = makes.AsQueryable();
var sorted = got.OrderByDescending(newLambda)
                .ThenByDescending(newLambda)
                .ToList();
```

---

## Change Return Type

You can **change the return type** of a lambda expression at runtime:

```csharp
Expression<Func<Product, int>> expression = p => p.Id;
string value = expression.Serialize();

LambdaExpression newLambda = value.DeserializeAsDynamic<Product>();

// Change return type to bool
newLambda = newLambda.ChangeReturnType<bool>();

// Or with Type
newLambda = newLambda.ChangeReturnType(typeof(bool));
```

---

## Limitations

⚠️ **Only primitives are allowed** in the expression body:
- `int`, `string`, `bool`, `decimal`, `Guid`, `DateTime`, etc.
- Enums
- Method calls on primitives (e.g., `string.Contains()`, `Guid.Parse()`)

❌ **Not supported**:
- Complex object initialization inside expressions
- Anonymous types
- Method calls on non-primitive types (unless they are primitives like `string`, `Guid`)

---

## Real-World Examples

### Store Filters in Database

```csharp
public class SavedFilter
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Expression { get; set; }
}

// Save filter
var filter = new SavedFilter
{
    Id = Guid.NewGuid(),
    Name = "Active Products Over $100",
    Expression = ((Expression<Func<Product, bool>>)(p => p.Price > 100 && p.IsActive)).Serialize()
};

await filterRepository.InsertAsync(filter);

// Load and apply filter
var savedFilter = await filterRepository.GetAsync(filterId);
var expression = savedFilter.Expression.Deserialize<Product, bool>();
var products = await productRepository.QueryAsync(expression.Compile());
```

### Dynamic Sorting from User Input

```csharp
public class SortOptions
{
    public string SortBy { get; set; } // "Id", "Name", "Price"
    public bool Descending { get; set; }
}

public async Task<List<Product>> GetProductsAsync(SortOptions options)
{
    var products = await productRepository.QueryAsync(x => x.IsActive);
    
    // Build expression from user input
    Expression<Func<Product, object>> sortExpression = options.SortBy switch
    {
        "Id" => p => p.Id,
        "Name" => p => p.Name,
        "Price" => p => p.Price,
        _ => p => p.Id
    };
    
    // Serialize and deserialize as dynamic
    var dynamicExpression = sortExpression.Serialize().DeserializeAsDynamic<Product>();
    
    // Apply sorting
    var query = products.AsQueryable();
    return options.Descending
        ? query.OrderByDescending(dynamicExpression).ToList()
        : query.OrderBy(dynamicExpression).ToList();
}
```

### API Query Parameters

```csharp
[HttpGet]
public async Task<IActionResult> GetOrders([FromQuery] string? filter)
{
    IQueryable<Order> query = dbContext.Orders;
    
    if (!string.IsNullOrEmpty(filter))
    {
        // Client sends: "o => o.Total > 500 && o.Status == 1"
        var expression = filter.Deserialize<Order, bool>();
        query = query.Where(expression.Compile()).AsQueryable();
    }
    
    return Ok(await query.ToListAsync());
}
```

### Dynamic Reports

```csharp
public class ReportDefinition
{
    public string Name { get; set; }
    public string FilterExpression { get; set; }
    public string GroupByExpression { get; set; }
}

public async Task<ReportResult> GenerateReportAsync(ReportDefinition definition)
{
    // Apply filter
    var filterExpr = definition.FilterExpression.Deserialize<Order, bool>();
    var orders = await orderRepository.QueryAsync(filterExpr.Compile());
    
    // Apply grouping
    var groupByExpr = definition.GroupByExpression.DeserializeAsDynamic<Order>();
    var grouped = orders.AsQueryable().GroupBy(groupByExpr).ToList();
    
    return new ReportResult
    {
        Name = definition.Name,
        Groups = grouped
    };
}
```

---

## Benefits

- ✅ **Store Expressions**: Save filters/queries in database
- ✅ **Dynamic Queries**: Build queries at runtime from user input
- ✅ **Type Safety**: Still uses strongly-typed expressions
- ✅ **LINQ Integration**: Works with `Where`, `OrderBy`, `GroupBy`, etc.
- ✅ **Serializable**: Send expressions over network or save to storage

---

## Related Tools

- **[Repository Pattern](https://rystem.net/mcp/tools/repository-setup.md)** - Use with repository Query methods
- **[Repository API Server](https://rystem.net/mcp/tools/repository-api-server.md)** - Send LINQ expressions via API
- **[JSON Extensions](https://rystem.net/mcp/tools/rystem-json-extensions.md)** - JSON utilities for serialization

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
- **Unit Tests**: [SystemLinq.cs](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Linq/SystemLinq.cs)
