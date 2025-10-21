---
title: Text Extensions
description: Fast text and stream utilities - includes ToByteArray(), ToStream(), ReadLinesAsync(), ToUpperCaseFirst(), ContainsAtLeast() for efficient string/byte/stream conversions
---

# Text Extensions

Fast **text and stream utilities** for efficient conversions between strings, byte arrays, and streams.

---

## Installation

```bash
dotnet add package Rystem --version 9.1.3
```

---

## String ↔ Byte Array

### String to Byte Array

```csharp
using Rystem;

string text = "Hello, World!";
byte[] bytes = text.ToByteArray();
```

### Byte Array to String

```csharp
byte[] bytes = new byte[] { 72, 101, 108, 108, 111 };
string text = bytes.ConvertToString();

Console.WriteLine(text); // Outputs: "Hello"
```

**Complete Example:**

```csharp
string original = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
var bytes = original.ToByteArray();
string restored = bytes.ConvertToString();

Assert.Equal(original, restored);
```

---

## String ↔ Stream

### String to Stream

```csharp
string text = "Hello, World!";
Stream stream = text.ToStream();
```

### Stream to String

```csharp
Stream stream = GetStreamFromSomewhere();
string text = stream.ConvertToString();
```

**Complete Example:**

```csharp
string original = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
var stream = original.ToStream();
string restored = stream.ConvertToString();

Assert.Equal(original, restored);
```

---

## Read Lines Asynchronously

Read a string with **line breaks** as an `IAsyncEnumerable<string>`:

```csharp
string text = "Line 1\nLine 2\nLine 3";
var stream = text.ToStream();

var lines = new List<string>();
await foreach (var line in stream.ReadLinesAsync())
{
    lines.Add(line);
    Console.WriteLine(line);
}

// Output:
// Line 1
// Line 2
// Line 3
```

**With File Stream:**

```csharp
using var fileStream = File.OpenRead("largefile.txt");

await foreach (var line in fileStream.ReadLinesAsync())
{
    // Process each line without loading entire file into memory
    ProcessLine(line);
}
```

---

## Uppercase First Character

Make the **first character uppercase**:

```csharp
string text = "hello world";
string capitalized = text.ToUpperCaseFirst();

Console.WriteLine(capitalized); // Outputs: "Hello world"
```

**Example:**

```csharp
string dasda = "dasda";
string result = dasda.ToUpperCaseFirst();

Assert.Equal("Dasda", result);
```

---

## Contains At Least X Times

Check if a **character appears at least X times**:

```csharp
string value = "abcderfa";

bool containsAtLeastTwoA = value.ContainsAtLeast(2, 'a');
Console.WriteLine(containsAtLeastTwoA); // Outputs: True

bool containsAtLeastThreeA = value.ContainsAtLeast(3, 'a');
Console.WriteLine(containsAtLeastThreeA); // Outputs: False
```

**Use Cases:**

```csharp
// Password validation: at least 2 special characters
string password = "P@ssw0rd!";
bool isValid = password.ContainsAtLeast(2, '@', '!', '#', '$', '%');

// Email validation: exactly one @
string email = "user@example.com";
bool hasOneAt = email.ContainsAtLeast(1, '@') && !email.ContainsAtLeast(2, '@');
```

---

## Real-World Examples

### File Upload Processing

```csharp
public async Task<string> ProcessUploadAsync(Stream fileStream)
{
    // Convert stream to string
    string content = fileStream.ConvertToString();
    
    // Process content
    var lines = new List<string>();
    await foreach (var line in content.ToStream().ReadLinesAsync())
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            lines.Add(line.ToUpperCaseFirst());
        }
    }
    
    return string.Join("\n", lines);
}
```

### API Response Handling

```csharp
public async Task<ApiResponse> CallExternalApiAsync()
{
    var response = await httpClient.GetAsync("/api/endpoint");
    var stream = await response.Content.ReadAsStreamAsync();
    
    // Convert stream to string
    string jsonResponse = stream.ConvertToString();
    
    return jsonResponse.FromJson<ApiResponse>();
}
```

### Log File Analysis

```csharp
public async Task<Dictionary<string, int>> AnalyzeLogFileAsync(string filePath)
{
    var errorCounts = new Dictionary<string, int>();
    
    using var fileStream = File.OpenRead(filePath);
    
    await foreach (var line in fileStream.ReadLinesAsync())
    {
        if (line.ContainsAtLeast(1, "ERROR", "WARN"))
        {
            var errorType = line.Contains("ERROR") ? "ERROR" : "WARN";
            errorCounts[errorType] = errorCounts.GetValueOrDefault(errorType) + 1;
        }
    }
    
    return errorCounts;
}
```

### CSV Processing

```csharp
public async Task<List<Product>> ParseCsvAsync(Stream csvStream)
{
    var products = new List<Product>();
    
    await foreach (var line in csvStream.ReadLinesAsync())
    {
        var parts = line.Split(',');
        if (parts.Length >= 3)
        {
            products.Add(new Product
            {
                Name = parts[0].ToUpperCaseFirst(),
                Price = decimal.Parse(parts[1]),
                Category = parts[2]
            });
        }
    }
    
    return products;
}
```

### Password Validation

```csharp
public class PasswordValidator
{
    public bool IsValid(string password)
    {
        if (password.Length < 8)
            return false;
        
        // At least 2 uppercase letters
        if (!password.ContainsAtLeast(2, "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()))
            return false;
        
        // At least 2 digits
        if (!password.ContainsAtLeast(2, "0123456789".ToCharArray()))
            return false;
        
        // At least 1 special character
        if (!password.ContainsAtLeast(1, '@', '!', '#', '$', '%', '&', '*'))
            return false;
        
        return true;
    }
}
```

---

## Benefits

- ✅ **Performance**: Fast conversions without allocations
- ✅ **Memory Efficient**: Stream-based line reading
- ✅ **Convenience**: Simple extension methods
- ✅ **Async Support**: ReadLinesAsync for large files
- ✅ **No Dependencies**: Built-in utilities

---

## Related Tools

- **[CSV Serialization](https://rystem.net/mcp/tools/rystem-csv.md)** - CSV utilities using text extensions
- **[JSON Extensions](https://rystem.net/mcp/tools/rystem-json-extensions.md)** - JSON serialization
- **[Content Repository](https://rystem.net/mcp/resources/content-repo.md)** - File upload/download with streams

---

## References

- **NuGet Package**: [Rystem](https://www.nuget.org/packages/Rystem) v9.1.3
- **Documentation**: https://rystem.net
- **GitHub**: https://github.com/KeyserDSoze/Rystem
