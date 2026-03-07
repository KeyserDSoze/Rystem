### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem

`Rystem` is the core utilities package of the Rystem ecosystem.

It extends familiar BCL namespaces such as `System`, `System.Linq`, `System.Linq.Expressions`, `System.Reflection`, `System.Text`, and `System.Threading.Tasks` with pragmatic helpers for:

- discriminated unions in C#
- expression serialization and dynamic LINQ/queryable composition
- reflection, mocking, runtime model generation, and IL inspection
- JSON, CSV, minimization, hashing, and encoding helpers
- task orchestration and small concurrent collections

Most examples below are adapted from the repository test suite so the README stays aligned with real usage.
For brevity, short snippets sometimes use obvious sample types such as `User`, `Order`, `MyType`, or `MyDto`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem
```

The current `10.x` package targets `net10.0`.

After installation, most APIs are available through standard `using` directives that match the namespace they extend. The main exception is `ReflectionHelper`, which lives in `Rystem.Reflection`.

If you move deeper into the ecosystem, [`Rystem.DependencyInjection`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection/README.md) builds DI modules on top of this package, and [`Rystem.DependencyInjection.Web`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection.Web/README.md) adds ASP.NET Core runtime rebuilding on top of the DI layer.

## Table of Contents

- [Package Architecture](#package-architecture)
- [Discriminated Unions in C#](#discriminated-unions-in-c)
  - [Define a union](#define-a-union)
  - [Match, Switch, and TryGet](#match-switch-and-tryget)
  - [JSON serialization and deserialization](#json-serialization-and-deserialization)
  - [Resolving ambiguous JSON with selectors](#resolving-ambiguous-json-with-selectors)
  - [Class-level selectors, regex selectors, and default types](#class-level-selectors-regex-selectors-and-default-types)
- [Core Utilities](#core-utilities)
- [Stopwatch](#stopwatch)
- [Try / Retry](#try--retry)
- [Cast extensions](#cast-extensions)
- [Copy extensions](#copy-extensions)
- [Enum extensions](#enum-extensions)
- [Text extensions](#text-extensions)
- [Encoding: Base64 and Base45](#encoding-base64-and-base45)
  - [Base64](#base64)
  - [Base45](#base45)
- [Cryptography (Hashing)](#cryptography-hashing)
- [JSON extensions](#json-extensions)
- [CSV and Minimization](#csv-and-minimization)
- [CSV](#csv)
- [Minimization](#minimization)
- [LINQ and Expression Utilities](#linq-and-expression-utilities)
- [Serialize and deserialize lambda expressions](#serialize-and-deserialize-lambda-expressions)
- [Dynamic lambda utilities](#dynamic-lambda-utilities)
- [Dynamic IQueryable helpers](#dynamic-iqueryable-helpers)
- [RemoveWhere](#removewhere)
- [AllAsync and AnyAsync](#allasync-and-anyasync)
- [Non-generic IEnumerable helpers](#non-generic-ienumerable-helpers)
- [Reflection and Runtime Tools](#reflection-and-runtime-tools)
- [Name of the calling class](#name-of-the-calling-class)
- [Cached reflection helpers](#cached-reflection-helpers)
- [Type relationship helpers](#type-relationship-helpers)
- [Create default instances](#create-default-instances)
- [Mock abstract classes and interfaces](#mock-abstract-classes-and-interfaces)
- [Construct with the best dynamic fit](#construct-with-the-best-dynamic-fit)
- [Nullability inspection](#nullability-inspection)
- [Property showcase](#property-showcase)
- [IL inspection and method signatures](#il-inspection-and-method-signatures)
- [Runtime model builder](#runtime-model-builder)
- [Generic method invocation](#generic-method-invocation)
- [Tasks and Collections](#tasks-and-collections)
- [NoContext and ToResult](#nocontext-and-toresult)
- [ToListAsync](#tolistasync)
- [TaskManager](#taskmanager)
  - [WhenAll](#whenall)
  - [WhenAtLeast](#whenatleast)
- [ConcurrentList](#concurrentlist)
- [Utilities](#utilities)
- [AsyncEnumerable.Empty](#asyncenumerableempty)
- [Programming language conversion](#programming-language-conversion)
- [Repository Examples](#repository-examples)

---

## Package Architecture

| Area                   | Main APIs                                                                                                                                                       |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Discriminated unions   | `AnyOf<T0, ...>`, `Match`, `MatchAsync`, `Switch`, `SwitchAsync`, `TryGetTn`, JSON selectors and defaults                                                       |
| Core helpers           | `Stopwatch`, `Try`, `Cast<T>`, `ToDeepCopy`, `CopyPropertiesFrom`, enum and string helpers                                                                      |
| Text and data          | `ToByteArray`, `ToStream`, `ReadLinesAsync`, `ToJson`, `ToHash`, `ToBase64`, `ToBase45`, `ToCsv`, `ToMinimize`                                                  |
| LINQ and expressions   | `Serialize`, `Deserialize`, `DeserializeAsDynamic`, `ChangeReturnType`, `InvokeAsync`, `Where(LambdaExpression)`, `Select(LambdaExpression)`, `CallMethodAsync` |
| Reflection and runtime | `FetchProperties`, `CreateWithDefault`, `CreateInstance`, `ConstructWithBestDynamicFit`, `ToShowcase`, `GetBodyAsString`, `Model.Create(...)`                   |
| Tasks and collections  | `NoContext`, `ToResult`, `ToListAsync`, `TaskManager`, `ConcurrentList<T>`, `AsyncEnumerable<T>.Empty`                                                          |
| Conversion             | `ConvertAs(ProgrammingLanguageType.Typescript)`                                                                                                                 |

If you want a single package with a broad set of low-level .NET utilities, this is the entry point of the Rystem ecosystem.

---

## Discriminated Unions in C#

`AnyOf<T0, T1, ...>` brings discriminated unions to C# with built-in `System.Text.Json` integration.

The package ships `AnyOf` variants from 2 to 10 generic arguments.

### Define a union

```csharp
AnyOf<int, string, bool> value = "hello";

Console.WriteLine(value.Index); // 1
Console.WriteLine(value.IsT1);  // true
Console.WriteLine(value.AsT1);  // hello

value = 42;
int number = value.CastT0;
```

Useful members include:

- `Index`
- `IsT0`, `IsT1`, ...
- `AsT0`, `AsT1`, ...
- `CastT0`, `CastT1`, ...
- `Is<T>()`
- `TryGetT0(out value)`, `TryGetT1(out value)`, ...

### Match, Switch, and TryGet

```csharp
AnyOf<int, string, bool> value = "hello";

string description = value.Match(
    number => $"int: {number}",
    text => $"string: {text}",
    flag => $"bool: {flag}")!;

value.Switch(
    number => Console.WriteLine(number),
    text => Console.WriteLine(text),
    flag => Console.WriteLine(flag));

string? asyncDescription = await value.MatchAsync(
    async number => await Task.FromResult($"int: {number}"),
    async text => await Task.FromResult($"string: {text}"),
    async flag => await Task.FromResult($"bool: {flag}"));

await value.SwitchAsync(
    number => { Console.WriteLine(number); return ValueTask.CompletedTask; },
    text => { Console.WriteLine(text); return ValueTask.CompletedTask; },
    flag => { Console.WriteLine(flag); return ValueTask.CompletedTask; });

if (value.TryGetT1(out var textValue))
    Console.WriteLine(textValue);
```

Notes:

- `MatchAsync` delegates return `Task<TResult?>`
- `SwitchAsync` delegates return `ValueTask`
- `TryGetTn` is the safest way to branch without exceptions

### JSON serialization and deserialization

The active value is serialized directly, and deserialization automatically chooses the correct target type.

```csharp
public sealed class Wrapper
{
    public AnyOf<FirstClass, string>? Data { get; set; }
}

public sealed class FirstClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

var wrapper = new Wrapper
{
    Data = new FirstClass
    {
        FirstProperty = "alpha",
        SecondProperty = "beta"
    }
};

string json = wrapper.ToJson();
Wrapper copy = json.FromJson<Wrapper>();

Console.WriteLine(copy.Data!.AsT0!.FirstProperty); // alpha
```

By default, union deserialization uses the JSON payload signature, meaning the set of available property names is used to find the best candidate.

```csharp
public sealed class SignatureTestClass
{
    public AnyOf<SignatureClassOne, SignatureClassTwo>? Test { get; set; }
}

public sealed class SignatureClassOne
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public sealed class SignatureClassTwo
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

var payload = new SignatureTestClass
{
    Test = new SignatureClassTwo
    {
        FirstProperty = "FirstProperty",
        SecondProperty = "SecondProperty"
    }
};

var copy = payload.ToJson().FromJson<SignatureTestClass>();

// Both candidate types have the same signature,
// so the first matching type wins.
Console.WriteLine(copy.Test!.Is<SignatureClassOne>()); // true
```

If no candidate can be matched, the union property is deserialized as `null`.

### Resolving ambiguous JSON with selectors

When multiple candidate types share the same shape, add selectors to tell the deserializer how to choose.

`AnyOfJsonSelector` works with both strings and non-string values.

```csharp
public sealed class ChosenClass
{
    public AnyOf<TheFirstChoice, TheSecondChoice>? Value { get; set; }
}

public sealed class TheFirstChoice
{
    [AnyOfJsonSelector("first")]
    public string Type { get; init; } = null!;

    [AnyOfJsonSelector(2, 3, 4)]
    public int Flexy { get; set; }
}

public sealed class TheSecondChoice
{
    [AnyOfJsonSelector("first", "second")]
    public string Type { get; init; } = null!;

    [AnyOfJsonSelector(1)]
    public int Flexy { get; set; }
}

var payload = new ChosenClass
{
    Value = new TheSecondChoice
    {
        Type = "first",
        Flexy = 1
    }
};

var copy = payload.ToJson().FromJson<ChosenClass>();
Console.WriteLine(copy.Value!.Is<TheSecondChoice>()); // true

payload = new ChosenClass
{
    Value = new TheSecondChoice
    {
        Type = "first",
        Flexy = 2
    }
};

copy = payload.ToJson().FromJson<ChosenClass>();
Console.WriteLine(copy.Value!.Is<TheFirstChoice>()); // true
```

In the second case, the raw object was originally `TheSecondChoice`, but the selector values identify `TheFirstChoice` as the correct deserialization target.

### Class-level selectors, regex selectors, and default types

You can apply selectors to a whole class instead of a single property.

```csharp
[AnyOfJsonClassSelector(nameof(FirstProperty), "first.F")]
public sealed class FirstGetClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

[AnyOfJsonRegexClassSelector(nameof(FirstProperty), "secon[^.]*.[^.]*")]
public sealed class SecondGetClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public sealed class FourthGetClass
{
    [AnyOfJsonSelector("fourth.F")]
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public sealed class FifthGetClass
{
    [AnyOfJsonRegexSelector("fift[^.]*.")]
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

[AnyOfJsonDefault]
public sealed class SixthGetClass
{
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}
```

This lets you combine:

- exact value selectors
- regex selectors
- class-level selectors
- a default fallback type when no other selector matches

---

## Core Utilities

## Stopwatch

Measure actions, tasks, or task-returning functions.

```csharp
var started = Stopwatch.Start();
await Task.Delay(2000);
var result = started.Stop();

Console.WriteLine(result.Span.TotalMilliseconds);
```

```csharp
var result = await Stopwatch.MonitorAsync(async () =>
{
    await Task.Delay(2000);
});

Console.WriteLine(result.Span.TotalMilliseconds);
```

```csharp
var result = await Stopwatch.MonitorAsync(async () =>
{
    await Task.Delay(2000);
    return 3;
});

Console.WriteLine(result.Result);            // 3
Console.WriteLine(result.Stopwatch.Span);    // elapsed time
```

There is also a synchronous overload:

```csharp
var result = Stopwatch.Monitor(() => DoWork());
```

## Try / Retry

`Try` wraps synchronous, `Task`, and `ValueTask` execution and returns the value plus any exception.

```csharp
var success = Try.WithDefaultOnCatch(() => 42);
int value = success;

Console.WriteLine(value);                    // 42
Console.WriteLine(success.Exception == null); // true
```

```csharp
var failed = Try.WithDefaultOnCatch(() => int.Parse("abc"));

Console.WriteLine((int)failed);              // 0
Console.WriteLine(failed.Exception != null); // true
```

```csharp
var asyncFailed = await Try.WithDefaultOnCatchAsync(async () =>
{
    await Task.Delay(10);
    return int.Parse("abc");
});

Console.WriteLine(asyncFailed.Exception != null); // true
```

```csharp
var valueTaskResult = await Try.WithDefaultOnCatchValueTaskAsync(async () =>
{
    await Task.Delay(10);
    return 12;
});
```

You can also configure retry behavior.

```csharp
var response = await Try.WithDefaultOnCatchAsync(
    async () => await FlakyServiceAsync(),
    behavior =>
    {
        behavior.MaxRetry = 3;
        behavior.WaitBetweenRetry = 200;
        behavior.RetryUntil = exception => exception is HttpRequestException;
    });
```

Prefer checking `response.Exception` to detect failures. That is the most explicit and reliable success signal.

## Cast extensions

Use `Cast<T>()` for safe conversions across numeric values, strings, runtime types, and inheritance hierarchies.

```csharp
int x = 2;
decimal result = x.Cast<decimal>();

int? nullable = null;
decimal result2 = nullable.Cast<decimal>();   // 0
decimal? result3 = nullable.Cast<decimal?>(); // null

string guid = Guid.NewGuid().ToString();
Guid parsed = guid.Cast<Guid>();
```

```csharp
object entity = new User();
Type targetType = typeof(UserDto);
object converted = entity.Cast(targetType)!;
```

## Copy extensions

Use `ToDeepCopy()` to create a detached clone, or `CopyPropertiesFrom(...)` to copy property values onto an existing instance.

```csharp
var original = new User { Id = 3 };
var copy = original.ToDeepCopy();

Console.WriteLine(ReferenceEquals(original, copy)); // false
Console.WriteLine(copy.Id);                         // 3
```

```csharp
var source = new User { Id = 10 };
var target = new User();

target.CopyPropertiesFrom(source);
Console.WriteLine(target.Id); // 10
```

## Enum extensions

Convert strings or other enum values into a target enum and read `[Display]` names.

```csharp
enum Color
{
    Red,
    Green,
    Blue
}

Color c1 = "green".ToEnum<Color>();
Color c2 = ConsoleColor.Green.ToEnum<Color>();
```

```csharp
public enum Status
{
    [Display(Name = "In Progress")]
    Working
}

string label = Status.Working.GetDisplayName(); // In Progress
```

## Text extensions

Convert between strings, byte arrays, and streams with minimal ceremony.

```csharp
string text = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";

byte[] bytes = text.ToByteArray();
string restored = bytes.ConvertToString();
```

```csharp
string text = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";

Stream stream = text.ToStream();
string restored = stream.ConvertToString();
```

`ReadLinesAsync()` turns a stream into `IAsyncEnumerable<string>`.

```csharp
string text = "line 1\nline 2\nline 3";
Stream stream = text.ToStream();

await foreach (var line in stream.ReadLinesAsync())
{
    Console.WriteLine(line);
}
```

Other small helpers:

```csharp
string title = "dasda".ToUpperCaseFirst();          // Dasda
bool hasTwoAs = "abcderfa".ContainsAtLeast(2, 'a');
string replaced = "aaa".Replace("a", "b", 2);    // bba
```

## Encoding: Base64 and Base45

### Base64

```csharp
string encoded = "Hello World".ToBase64();
string decoded = encoded.FromBase64();

var payload = new User { Id = 7, Name = "Ada" };
string encodedObject = payload.ToBase64();
User decodedObject = encodedObject.FromBase64<User>();
```

### Base45

Base45 is handy when you want a compact, QR-code-friendly character set.

```csharp
string encoded = "Hello World".ToBase45();
string decoded = encoded.FromBase45();

var payload = new User { Id = 7, Name = "Ada" };
string encodedObject = payload.ToBase45();
User decodedObject = encodedObject.FromBase45<User>();
```

Both object overloads serialize through JSON first.

## Cryptography (Hashing)

`ToHash()` creates a deterministic SHA-512 hexadecimal hash from a string or any serializable object.

```csharp
string hash = "my secret".ToHash();

var foo = new Foo
{
    Values = new[] { "aa", "bb", "cc" },
    X = true
};

string hash2 = foo.ToHash();
Console.WriteLine(foo.ToHash() == hash2); // true
```

```csharp
Guid id = Guid.Parse("41e2c840-8ba1-4c0b-8a9b-781747a5de0c");
string hash = id.ToHash();
```

## JSON extensions

The JSON helpers are intentionally tiny wrappers over `System.Text.Json`.

```csharp
var users = new List<User>
{
    new User { Id = 1, Name = "Ada" },
    new User { Id = 2, Name = "Grace" }
};

string json = users.ToJson();
List<User> copy = json.FromJson<List<User>>();
```

```csharp
object? dynamicCopy = json.FromJson(typeof(List<User>));
```

```csharp
await using var stream = json.ToStream();
List<User> fromStream = await stream.FromJsonAsync<List<User>>();
```

---

## CSV and Minimization

## CSV

`ToCsv()` flattens objects, nested objects, and enumerable members into a tabular representation.

```csharp
string csv = models.ToCsv();
```

You can configure headers, delimiters, Excel-friendly quoting, and excluded properties.

```csharp
string csv = users.ToCsv(configuration =>
{
    configuration.ForExcel = true;
    configuration.UseExtendedName = false;
    configuration.ConfigureHeader(x => x.Id, "Identifier");
    configuration.ConfigureHeader(x => x.Groups.First().Name, "GroupName");
    configuration.AvoidProperty(x => x.Password);
    configuration.Delimiter = ";";
});
```

Configuration options include:

- `UseHeader`
- `Delimiter`
- `ForExcel`
- `UseExtendedName`
- `ConfigureHeader(...)`
- `AvoidProperty(...)`

## Minimization

`ToMinimize()` is a compact serializer designed to occupy less space than JSON for many object graphs.

```csharp
string minimized = models.ToMinimize();
List<CsvModel> restored = minimized.FromMinimization<List<CsvModel>>();
```

You can also choose the starting separator explicitly.

```csharp
string minimized = models.ToMinimize('&');
List<CsvModel> restored = minimized.FromMinimization<List<CsvModel>>('&');
```

Use `MinimizationPropertyAttribute` when you want deterministic property ordering.

```csharp
public sealed class CompactUser
{
    [MinimizationProperty(0)]
    public int Id { get; set; }

    [MinimizationProperty(1)]
    public string? Name { get; set; }
}
```

---

## LINQ and Expression Utilities

## Serialize and deserialize lambda expressions

Rystem can serialize expression trees to strings and build them back into executable expressions.

```csharp
var q = "dasda";
var id = Guid.Parse("bf46510b-b7e6-4ba2-88da-cef208aa81f2");

Expression<Func<MakeIt, bool>> expression =
    x => x.X == q &&
         x.Samules!.Any(y => y == "ccccde") &&
         x.Sol &&
         (x.X.Contains(q) || x.Sol.Equals(true)) &&
         (x.E == id | x.Id == 32);

string serialized = expression.Serialize();
Func<MakeIt, bool> compiled = serialized.DeserializeAndCompile<MakeIt, bool>();

List<MakeIt> filtered = makes.Where(compiled).ToList();
```

The test suite covers scenarios such as:

- `Contains(...)`
- `Any(...)`
- enum comparisons
- `DateTime` comparisons
- `TimeSpan` comparisons
- unary `!`
- nested member access

## Dynamic lambda utilities

You can deserialize to `LambdaExpression`, inspect the inferred result type, convert the return type, and cast back to typed expressions.

```csharp
Expression<Func<User, int>> selector = x => x.Id;
string text = selector.Serialize();

LambdaExpression dynamicSelector = text.DeserializeAsDynamic<User>();
Expression<Func<User, int>> asInt = dynamicSelector.AsExpression<User, int>();
Expression<Func<User, decimal>> asDecimal = dynamicSelector.AsExpression<User, decimal>();

decimal average = new List<User> { new User { Id = 13 } }.Average(asDecimal.Compile());
```

```csharp
var inferred = "x => x.Id == 25".DeserializeAsDynamicAndRetrieveType<User>();

Console.WriteLine(inferred.Type); // System.Boolean
```

```csharp
Expression<Func<User, ValueTask<int>>> expression = x => GetUserIdAsync(x);
LambdaExpression lambda = expression;

decimal id = await lambda.InvokeAsync<decimal>(new User { Id = 13 });
```

`InvokeAndTransform(...)` is also available when you want a converted return type from a compiled expression.

```csharp
Expression<Func<User, int>> expression = x => x.Id;
decimal result = expression.InvokeAndTransform<User, int, decimal>(new User { Id = 13 })!;
```

You can also extract a property directly from an expression.

```csharp
PropertyInfo? property = ((Expression<Func<User, int>>)(x => x.Id)).GetPropertyFromExpression();
```

## Dynamic IQueryable helpers

The package exposes dynamic `IQueryable` operators that accept `LambdaExpression` instead of strongly typed selectors.

Supported helpers include:

- `Average`
- `Count`
- `DistinctBy`
- `GroupBy`
- `LongCount`
- `Max`
- `Min`
- `OrderBy`
- `OrderByDescending`
- `Select`
- `Sum`
- `ThenBy`
- `ThenByDescending`
- `Where`

```csharp
Expression<Func<MakeIt, int>> orderExpression = x => x.Id;
Expression<Func<MakeIt, bool>> predicateExpression = x => x.Id >= 10;

LambdaExpression orderBy = orderExpression.Serialize().DeserializeAsDynamic<MakeIt>();
LambdaExpression predicate = predicateExpression.Serialize().DeserializeAsDynamic<MakeIt>();

var query = makes.AsQueryable();

var ordered = query.OrderByDescending(orderBy).ThenBy(orderBy).ToList();
var filtered = query.Where(predicate).ToList();
var grouped = query.GroupBy(orderBy).ToList();
var projected = query.Select<MakeIt, decimal>(orderBy).ToList();

var average = query.Average(orderBy);
var sum = query.Sum(orderBy);
var count = query.Count(predicate);
```

`CallMethodAsync(...)` lets you invoke custom async query operators dynamically.

```csharp
public static class QueryableExtensions
{
    public static async Task<IQueryable<T>> GetAsync<T>(
        this IQueryable<T> queryable,
        Expression<Func<T, bool>> expression,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0);
        return queryable.Where(expression);
    }
}

var result = await query.CallMethodAsync<MakeIt, IQueryable<MakeIt>>(
    "GetAsync",
    predicate,
    typeof(QueryableExtensions));
```

This is especially useful when you need to bridge runtime-generated selectors with LINQ providers such as Entity Framework.

## RemoveWhere

`RemoveWhere(...)` works on arrays, `ICollection<T>`, and plain `IEnumerable<T>`.

```csharp
int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
numbers = numbers.RemoveWhere(x => x == 8 || x == 9);
```

```csharp
var filtered = myEnumerable.RemoveWhere(x => x.IsExpired);
```

```csharp
List<Order> orders = GetOrders();
int removed = orders.RemoveWhere(x => x.Status == OrderStatus.Cancelled);
```

## AllAsync and AnyAsync

Use async predicates over a synchronous `IEnumerable<T>`.

```csharp
bool allValid = await items.AllAsync(x => ValueTask.FromResult(x.Id >= 0));
bool anyZero = await items.AnyAsync(x => Task.FromResult(x.Id == 0));
```

Both `Task<bool>` and `ValueTask<bool>` predicates are supported.

## Non-generic IEnumerable helpers

The non-generic helpers are useful when you receive `IEnumerable` at runtime and still need indexed operations.

```csharp
IEnumerable items = GetItems();

object? value = items.ElementAt(10);
items.SetElementAt(10, newValue);

bool removed = items.RemoveElementAt(10, out IEnumerable newItems, out object? removedValue);
```

They work with both `Array` and `IList` implementations.

---

## Reflection and Runtime Tools

## Name of the calling class

`ReflectionHelper` lives in `Rystem.Reflection`.

```csharp
using Rystem.Reflection;

string name = ReflectionHelper.NameOfCallingClass(deep: 1);
string fullName = ReflectionHelper.NameOfCallingClass(deep: 1, full: true);
```

Increase `deep` when you want to walk further up the call stack.

## Cached reflection helpers

The package caches repeated reflection lookups for better reuse.

```csharp
PropertyInfo[] properties = typeof(MyType).FetchProperties();
ConstructorInfo[] constructors = typeof(MyType).FecthConstructors();
FieldInfo[] fields = typeof(MyType).FetchFields();
MethodInfo[] methods = typeof(MyType).FetchMethods();
MethodInfo[] staticMethods = typeof(MyType).FetchStaticMethods();
```

`FetchProperties(...)` can ignore properties decorated with specific attributes.

```csharp
PropertyInfo[] visibleProperties = typeof(MyType).FetchProperties(typeof(JsonIgnoreAttribute));
```

## Type relationship helpers

```csharp
bool sameOrSon = typeof(Folli).IsTheSameTypeOrASon(typeof(Sulo));
bool sameOrParent = typeof(Sulo).IsTheSameTypeOrAParent(typeof(Folli));
bool hasInterface = typeof(MyService).HasInterface<IDisposable>();
```

Instance overloads are available too.

```csharp
Zalo zalo = new();
Sulo sulo = new();

Console.WriteLine(zalo.IsTheSameTypeOrASon(sulo));
Console.WriteLine(sulo.IsTheSameTypeOrAParent(zalo));
```

## Create default instances

`CreateWithDefault()` builds an instance by recursively creating default constructor arguments and common collection implementations.

```csharp
public sealed class Foo
{
    public IEnumerable<string> Values { get; }
    public Foo(IEnumerable<string> values) => Values = values;
}

Foo foo = typeof(Foo).CreateWithDefault<Foo>()!;
(foo.Values as List<string>)!.Add("aaa");
```

`CreateWithDefaultConstructorPropertiesAndField()` goes further and populates settable properties and fields as well.

```csharp
Foo2 foo = typeof(Foo2).CreateWithDefaultConstructorPropertiesAndField<Foo2>()!;

foo.Complex.Add("x", "y");
(foo.Tiny.Values as List<string>)!.Add("aaa");
```

## Mock abstract classes and interfaces

Rystem can generate runtime implementations for abstract classes and interfaces.

```csharp
public abstract class Alzio
{
    private protected string X { get; }
    public string O => X;
    public string A { get; set; }

    protected Alzio(string x) => X = x;
}

var mocked = typeof(Alzio).CreateInstance("AAA") as Alzio;
mocked!.A = "rrrr";

Console.WriteLine(mocked.O); // AAA
Console.WriteLine(mocked.A); // rrrr
```

You can also configure the generated type.

```csharp
Alzio alzio = null!;

var configured = alzio.CreateInstance(configuration =>
{
    configuration.IsSealed = false;
    configuration.CreateNewOneIfExists = true;
}, "AAA");
```

## Construct with the best dynamic fit

`ConstructWithBestDynamicFit(...)` matches runtime arguments first against constructor parameters and then against settable properties.

```csharp
var entity = (MySuperClass)typeof(MySuperClass).ConstructWithBestDynamicFit(3, 4, 5, 6)!;
var entity2 = Constructor.InvokeWithBestDynamicFit<MySuperClass>(5, 6, 7, 8);
var interfaceInstance = Constructor.InvokeWithBestDynamicFit<IMogalo>(9, 10, 11, 21);
```

This is useful when the values are only known at runtime and you still want a best-effort, exact-type match.

## Nullability inspection

`IsNullable()` works on properties, fields, and parameters.

```csharp
var type = typeof(InModel);

var constructorParameters = type.GetConstructors().First().GetParameters();
var methodParameters = type.GetMethod(nameof(InModel.SetSomething))!.GetParameters();
var properties = type.GetProperties();
var fields = type.GetFields();

Console.WriteLine(constructorParameters[0].IsNullable());
Console.WriteLine(methodParameters[0].IsNullable());
Console.WriteLine(properties[0].IsNullable());
Console.WriteLine(fields[0].IsNullable());
```

## Property showcase

`ToShowcase()` builds a structured description of a type and can attach extra computed metadata to each flattened property.

```csharp
var showcase = typeof(Something).ToShowcase(
    IFurtherParameter.Create("Bootstrap", x => new BootstrapProperty(x)),
    IFurtherParameter.Create("Title", x => x.NavigationPath));

var first = showcase.FlatProperties.First();
string title = first.GetProperty<string>("Title");
BootstrapProperty bootstrap = first.GetProperty<BootstrapProperty>("Bootstrap");
```

This is handy for metadata-driven UI generation, forms, and schema exploration.

## IL inspection and method signatures

Inspect the body of a method as text or as decoded IL instructions.

```csharp
MethodInfo method = typeof(Sulo).GetMethod(nameof(Sulo.Something), BindingFlags.Public | BindingFlags.Instance)!;

string body = method.GetBodyAsString();
List<ILInstruction> instructions = method.GetInstructions();
string signature = method.ToSignature();
```

This is useful for diagnostics, analysis tools, or advanced runtime inspection.

## Runtime model builder

Create types dynamically at runtime.

```csharp
var modelName = "MyBestModel";

Type modelType = Model
    .Create(modelName)
    .AddProperty("Primary", typeof(int))
    .AddProperty("Secondary", typeof(bool))
    .AddProperty("Name", typeof(string))
    .AddProperty("Id", typeof(Guid))
    .AddProperty("InModel", typeof(InModel))
    .AddParent<SomethingNew>()
    .Build();

dynamic instance = Model.Construct(modelName);
instance.Primary = 45;
instance.B = "Aloa";
```

This gives you a runtime-generated type plus a strongly discoverable builder API.

## Generic method invocation

`Generics.With(...)` and `Generics.WithStatic(...)` let you bind generic methods by runtime type and invoke them later.

```csharp
var staticResult = await Generics
    .WithStatic<SystemReflection>(nameof(StaticCreateAsync), typeof(int))
    .InvokeAsync(3);

var host = new SystemReflection();

var instanceResult = await Generics
    .With<SystemReflection>(nameof(CreateAsync), typeof(int))
    .InvokeAsync(host, 3);
```

```csharp
int value = Generics
    .WithStatic<SystemReflection>(nameof(StaticCreate), typeof(int))
    .Invoke<int>(3)!;
```

---

## Tasks and Collections

## NoContext and ToResult

`NoContext()` is a small convenience wrapper around `ConfigureAwait(...)`, controlled by `RystemTask.WaitYourStartingThread`.

```csharp
await DoSomethingAsync().NoContext();
int result = GetValueAsync().ToResult();
```

If you need to preserve the original synchronization context, set:

```csharp
RystemTask.WaitYourStartingThread = true;
```

## ToListAsync

The package adds a small `IAsyncEnumerable<T>` helper.

```csharp
await using var stream = "line 1\nline 2\nline 3".ToStream();
List<string> lines = await stream.ReadLinesAsync().ToListAsync();
```

## TaskManager

`TaskManager` helps you run a bounded number of tasks concurrently.

### WhenAll

```csharp
var bag = new ConcurrentBag<int>();

await TaskManager.WhenAll(ExecuteAsync, times: 45, concurrentTasks: 12, runEverytimeASlotIsFree: true).NoContext();

async Task ExecuteAsync(int i, CancellationToken cancellationToken)
{
    await Task.Delay(i * 20, cancellationToken).NoContext();
    bag.Add(i);
}
```

It also works with collections of objects.

```csharp
await TaskManager.WhenAll(ProcessAsync, orders, concurrentTasks: 5).NoContext();

async Task ProcessAsync(Order order, CancellationToken cancellationToken)
{
    await SaveAsync(order, cancellationToken).NoContext();
}
```

### WhenAtLeast

`WhenAtLeast(...)` stops waiting as soon as the requested number of tasks has completed.

```csharp
var bag = new ConcurrentBag<int>();

await TaskManager.WhenAtLeast(ExecuteAsync, times: 45, atLeast: 16, concurrentTasks: 12).NoContext();

async Task ExecuteAsync(int i, CancellationToken cancellationToken)
{
    await Task.Delay(i * 20, cancellationToken).NoContext();
    bag.Add(i);
}
```

## ConcurrentList

`ConcurrentList<T>` is a small thread-safe wrapper around `List<T>` for common list operations.

```csharp
var items = new ConcurrentList<MyClass>();

items.Add(new MyClass());
items.Insert(0, new MyClass());
items.RemoveAt(0);

Console.WriteLine(items.Count);
```

Use it when you want a simple `IList<T>` implementation with locking around the common mutating APIs.

---

## Utilities

## AsyncEnumerable.Empty

`AsyncEnumerable<T>.Empty` gives you a typed empty `IAsyncEnumerable<T>`.

```csharp
using System.Collection.Generics;

IAsyncEnumerable<MyClass> empty = AsyncEnumerable<MyClass>.Empty;

await foreach (var item in empty)
{
    // never reached
}
```

## Programming language conversion

The package can convert CLR types into other language representations. Right now it supports TypeScript.

```csharp
using System.ProgrammingLanguage;

ProgrammingLanguangeResponse ts = typeof(MyDto)
    .ConvertAs(ProgrammingLanguageType.Typescript);

Console.WriteLine(ts.Text);
Console.WriteLine(ts.MimeType);
```

```csharp
ProgrammingLanguangeResponse ts = new[] { typeof(OrderDto), typeof(ProductDto) }
    .ConvertAs(ProgrammingLanguageType.Typescript);
```

```csharp
ProgrammingLanguangeResponse renamed = typeof(MyInternalClass)
    .ConvertAs(ProgrammingLanguageType.Typescript, "PublicDto");
```

The generated output understands common primitives, arrays, enumerables, dictionaries, enums, and nested types. It also respects `JsonPropertyNameAttribute` when present.

---

## Repository Examples

If you want to see more real-world examples, the best references are the repository tests:

- Discriminated unions: [src/Core/Test/Rystem.Test.UnitTest/System/DiscriminatedUnionTests.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System/DiscriminatedUnionTests.cs)
- LINQ and dynamic queryable helpers: [src/Core/Test/Rystem.Test.UnitTest/System.Linq/SystemLinq.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Linq/SystemLinq.cs)
- Expression serialization and dynamic lambdas: [src/Core/Test/Rystem.Test.UnitTest/System.Linq.Expression/SystemLinqExpressions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Linq.Expression/SystemLinqExpressions.cs)
- Reflection helpers: [src/Core/Test/Rystem.Test.UnitTest/System.Reflection/ReflectionTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Reflection/ReflectionTest.cs)
- Generic reflection and constructor fitting: [src/Core/Test/Rystem.Test.UnitTest/System.Reflection/SystemReflection.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Reflection/SystemReflection.cs)
- Property showcase: [src/Core/Test/Rystem.Test.UnitTest/System.Reflection/PropertyHandlerTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Reflection/PropertyHandlerTest.cs)
- Runtime model builder: [src/Core/Test/Rystem.Test.UnitTest/System.Reflection/ModelTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Reflection/ModelTest.cs)
- IL reader: [src/Core/Test/Rystem.Test.UnitTest/System.Reflection/IlReaderTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Reflection/IlReaderTest.cs)
- CSV: [src/Core/Test/Rystem.Test.UnitTest/System.Text.Csv/CsvTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Text.Csv/CsvTest.cs)
- Minimization: [src/Core/Test/Rystem.Test.UnitTest/System.Text.Minimization/MinimizationTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Text.Minimization/MinimizationTest.cs)
- Task helpers: [src/Core/Test/Rystem.Test.UnitTest/System.Threading.Tasks/TasksTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Threading.Tasks/TasksTest.cs)

The current README is intentionally long because this package covers a lot of independent utilities.
