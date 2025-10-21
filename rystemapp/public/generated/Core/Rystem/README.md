### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---
# Table of Contents

- [What is Rystem?](#what-is-rystem)
- [Discriminated Union in C#](#discriminated-union-in-c)
  - [What is a Discriminated Union?](#what-is-a-discriminated-union)
  - [AnyOf Classes](#anyof-classes)
  - [JSON Integration](#json-integration)
  - [Matching and Switching](#matching-and-switching)
  - [Usage Examples](#usage-examples)
    - [Defining Unions](#defining-unions)
    - [Serializing and Deserializing JSON](#serializing-and-deserializing-json)
    - [Advanced Matching and Switching](#advanced-matching-and-switching)
    - [Deserialization with Attributes](#deserialization-with-attributes)
- [Extension Methods](#extension-methods)
  - [Stopwatch](#stopwatch)
  - [LINQ Expression Serializer](#linq-expression-serializer)
  - [Reflection Helper](#reflection-helper)
    - [Name of Calling Class](#name-of-calling-class)
    - [Extensions for Type Class](#extensions-for-type-class)
    - [Mock a Type](#mock-a-type)
    - [Check Nullability for Properties, Fields, and Parameters](#check-nullability-for-properties-fields-and-parameters)
  - [Text Extensions](#text-extensions)
  - [Character Separated-Value (CSV)](#character-separated-value-csv)
  - [Minimization of a Model](#minimization-of-a-model)
  - [Extensions for JSON](#extensions-for-json)
  - [Extensions for Task](#extensions-for-task)
  - [TaskManager](#taskmanager)
- [Concurrency](#concurrency)
  - [ConcurrentList](#concurrentlist)

---

## Discriminated Union in C#

This library introduces a `AnyOf<T0, T1, ...>` class that implements discriminated unions in C#. Discriminated unions are a powerful type system feature that allows variables to store one of several predefined types, enabling type-safe and concise programming. 

**This library also includes integration with JSON serialization and deserialization.**

### What is a Discriminated Union?

A discriminated union is a type that can hold one of several predefined types at a time. It provides a way to represent and operate on data that may take different forms, ensuring type safety and improving code readability.

For example, a union can represent a value that is either an integer, a string, or a boolean:

```csharp
AnyOf<int, string, bool> value;
```

The `value` can hold an integer, a string, or a boolean, but never more than one type at a time.

---

### AnyOf Classes

The `AnyOf` class provides the ability to define a discriminated union of up to 8 types.

- `AnyOf<T0, T1>`
- `AnyOf<T0, T1, T2>`
- ...
- `AnyOf<T0, T1, ..., T7>`

Each class supports the following features:

1. **Implicit Conversion**: Allows seamless assignment of any supported type to the union.
```csharp
public AnyOf<int, string> GetSomething(bool check)
{
    if (check)
        return 42;
    else
        return "Hello";
}
```
2. **Type Checking and Casting**: Methods to check and retrieve the stored type.
3. **Serialization Support**: Built-in JSON serialization and deserialization integration.
4. **Matching and Switching**: Methods to process the stored value using delegates or lambdas.

---

### JSON Integration

This library supports seamless JSON serialization and deserialization for discriminated unions. It uses a mechanism called **"Signature"** to identify the correct class during deserialization. The "Signature" is constructed based on the names of all properties that define each class in the union.

#### Serialization and Deserialization

The `AnyOf` class integrates with JSON serialization and deserialization. The integration supports:

1. **Implicit Serialization**: The active value in the union is serialized to JSON directly.
2. **Signature-Based Deserialization**: Uses property names ("signatures") to determine the correct type during deserialization.

#### How "Signature" Works

1. During deserialization, the library analyzes the properties present in the JSON.
2. The "Signature" matches the property names in the JSON to a predefined signature for each class in the union.
3. Once a match is found, the correct class is instantiated and populated with the data.

---

## Matching and Switching

The `AnyOf` class includes methods to simplify processing the stored value:

### Match Method

The `Match` method allows you to provide delegates for each possible type, returning a value based on the stored type.

#### Example:

```csharp
var union = new AnyOf<int, string>(42);
var result = union.Match(
    i => $"Integer: {i}",
    s => $"String: {s}"
);
Console.WriteLine(result); // Outputs: "Integer: 42"
```

### Switch Method

The `Switch` method allows you to perform different actions based on the stored type without returning a value.

#### Example:

```csharp
var union = new AnyOf<int, string>("Hello");
union.Switch(
    i => Console.WriteLine($"Integer: {i}"),
    s => Console.WriteLine($"String: {s}")
);
```

### Async Matching and Switching

Async versions of `Match` and `Switch` are also available for asynchronous operations.

#### Async Match Example:

```csharp
var union = new AnyOf<int, string>("Hello");
var result = await union.MatchAsync(
    async i => await Task.FromResult($"Integer: {i}"),
    async s => await Task.FromResult($"String: {s}")
);
Console.WriteLine(result); // Outputs: "String: Hello"
```

#### Async Switch Example:

```csharp
await union.SwitchAsync(
    async i => { await Task.Delay(100); Console.WriteLine($"Integer: {i}"); },
    async s => { await Task.Delay(100); Console.WriteLine($"String: {s}"); }
);
```

---

### Usage Examples

#### Defining Unions

Here’s how to define and use a discriminated union:

```csharp
var testClass = new CurrentTestClass
{
    OneClass_String = new FirstClass { FirstProperty = "OneClass_String.FirstProperty", SecondProperty = "OneClass_String.SecondProperty" },
    SecondClass_OneClass = new SecondClass
    {
        FirstProperty = "SecondClass_OneClass.FirstProperty",
        SecondProperty = "SecondClass_OneClass.SecondProperty"
    },
    OneClass_string__2 = "ExampleString",
    Bool_Int = 3,
    Decimal_Bool = true,
    OneCLass_SecondClass_Int = 42,
    FirstClass_SecondClass_Int_ThirdClass = new ThirdClass
    {
        Stringable = "StringContent",
        SecondClass = new SecondClass { FirstProperty = "Nested.FirstProperty", SecondProperty = "Nested.SecondProperty" }
    }
};
```

#### Serializing and Deserializing JSON

##### Example:

```csharp
var json = testClass.ToJson();
var deserialized = json.FromJson<CurrentTestClass>();
Console.WriteLine(deserialized.OneClass_String.AsT0.FirstProperty); // Outputs: OneClass_String.FirstProperty
```

---

#### Deserialization with Attributes

Attributes allow more control over deserialization, specifying how the correct type is chosen:

##### Example:

```csharp

public sealed class ChosenClass
{
    public AnyOf<TheFirstChoice, TheSecondChoice>? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public sealed class TheFirstChoice
{
    [AnyOfJsonSelector("first")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}
public sealed class TheSecondChoice
{
    [AnyOfJsonSelector("third", "second")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}

var testClass = new ChosenClass
{
    FirstProperty = new TheSecondChoice
    {
        Type = "first",
        Flexy = 1,
    }
};
var json = testClass.ToJson();
var deserialized = json.FromJson<ChosenClass>();
Assert.True(deserialized.FirstProperty.Is<TheFirstChoice>());
```

I want to use this example that you find in the unit test to explain the attribute AnyOfJsonSelector. 
In this example FirstProperty of ChosenClass has a value for TheSecondChoice with a value in Type equal to "first".
The attribute AnyOfJsonSelector is used to define the correct class to use during deserialization, therefore when the deserialization happens
the library will use the class TheFirstChoice because the value of Type is "first". Both classes have the same properties and so the signatures are equals.
Follow the next example to understand completely.

```csharp
public sealed class ChosenClass
{
    public AnyOf<TheFirstChoice, TheSecondChoice>? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}

public sealed class TheFirstChoice
{
    [AnyOfJsonSelector("first")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}
public sealed class TheSecondChoice
{
    [AnyOfJsonSelector("third", "second")]
    public string Type { get; init; }
    public int Flexy { get; set; }
}

var testClass = new ChosenClass
{
    FirstProperty = new TheSecondChoice
    {
        Type = "third",
        Flexy = 1,
    }
};
var json = testClass.ToJson();
var deserialized = json.FromJson<ChosenClass>();
Assert.True(deserialized.FirstProperty.Is<TheSecondChoice>());
```

In this second example the property has a value of "third" and so the library will use the class TheSecondChoice.

#### Further attributes

Set a class as default class for AnyOf

```csharp
 [AnyOfJsonDefault]
public sealed class RunResult : ApiBaseResponse
{
}
```

Use regex as selector

```csharp
public sealed class FifthGetClass
{
    [AnyOfJsonRegexSelector("fift[^.]*.")]
    public string? FirstProperty { get; set; }
    public string? SecondProperty { get; set; }
}
```

Use over a class and not only over a property, like a value or like a regex.

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
```

---

#### Benefits of Using It

1. **Type Safety**: Ensures only predefined types are used.
2. **JSON Support**: Automatically identifies and deserializes the correct type using "Signature".
3. **Code Clarity**: Reduces boilerplate code for type management and error handling.


# Extension methods

## Stopwatch
You can monitor the time spent on an action, task or in a method.
Some examples from Unit test.

	var started = Stopwatch.Start();
    //do something
    await Task.Delay(2000);
    var result = started.Stop();

or

    var result = await Stopwatch.MonitorAsync(async () =>
    {
        await Task.Delay(2000);
    });

or with a return value

     var result = await Stopwatch.MonitorAsync(async () =>
    {
        await Task.Delay(2000);
        return 3;
    });

## Linq expression serializer
Usually a linq expression is not serializable as string. With this method you can serialize your expression with some limits. Only primitives are allowed in the expression body.
An example from Unit test.

    Expression<Func<MakeIt, bool>> expression = ƒ => ƒ.X == q && ƒ.Samules.Any(x => x == k) && ƒ.Sol && (ƒ.X.Contains(q) || ƒ.Sol.Equals(IsOk)) && (ƒ.E == id | ƒ.Id == V) && (ƒ.Type == MakeType.Yes || ƒ.Type == qq);
    var serialized = expression.Serialize();

with result

    "ƒ => ((((((ƒ.X == \"dasda\") AndAlso ƒ.Samules.Any(x => (x == \"ccccde\"))) AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains(\"dasda\") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))"

with deserialization

    var newExpression = expressionAsString.Deserialize<MakeIt, bool>();

and usage, for instance, with Linq

    var result = makes.Where(newExpression.Compile()).ToList();

you can deserialize and compile at the same time with

    var newExpression = expressionAsString.DeserializeAndCompile<MakeIt, bool>();

you can deserialize as dynamic and use the linq dynamic methods

    Expression<Func<MakeIt, int>> expression = x => x.Id;
    string value = expression.Serialize();
    LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();
    var got = makes.AsQueryable();
    var cut = got.OrderByDescending(newLambda).ThenByDescending(newLambda).ToList();

please see the unit test [here](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Linq/SystemLinq.cs) to understand better how it works
You may deal with return type of your lambda expression:

     LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();
     newLambda =  newLambda.ChangeReturnType<bool>();

or

     newLambda =  newLambda.ChangeReturnType(typeof(bool));


## Reflection helper

### Name of calling class
You can find the name of the calling class from your method, with deep = 1 the calling class of your method, with deep = 2 the calling class that calls the class that calls your method, and so on, with fullName set to true you obtain the complete name of the discovered class.

    ReflectionHelper.NameOfCallingClass(deep, fullName);

### Extensions for Type class
You can get the properties, fields and constructors for your class (and singleton them to save time during new requests)

    Type.FetchProperties();
    Type.FecthConstructors();
    Type.FetchFields();

You can check if a Type is a son or a father or both of other type (in the example Zalo and Folli are Sulo).
You may find more information in unit test [here](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Reflection/ReflectionTest.cs)
    
    Zalo zalo = new();
    Zalo zalo2 = new();
    Folli folli = new();
    Sulo sulo = new();
    object quo = new();
    int x = 2;
    decimal y = 3;
    Assert.True(zalo.IsTheSameTypeOrASon(sulo));
    Assert.True(folli.IsTheSameTypeOrASon(sulo));
    Assert.True(zalo.IsTheSameTypeOrASon(zalo2));
    Assert.True(zalo.IsTheSameTypeOrASon(quo));
    Assert.False(sulo.IsTheSameTypeOrASon(zalo));
    Assert.True(sulo.IsTheSameTypeOrAParent(zalo));
    Assert.False(y.IsTheSameTypeOrAParent(x));

### Mock a Type
If you need to create a type over an abstract class or interface you may use the mocking system of Rystem.
For example, if you have an abstract class like this one down below.

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

you can create an instace of it or simply mock it with

    var mocked = typeof(Alzio).CreateInstance("AAA") as Alzio;
    mocked.A = "rrrr";

and you can use the class like a real class. You also may do it with

    Alzio alzio = null!;
    var mocked = alzio.CreateInstance("AAA");
    mocked.A = "rrrr";

or

    Mocking.CreateInstance<Alzio>("AAA");

you may see "AAA" as argument for your constructor in abstract class.

## Check nullability for properties, fields and parameters.
Following an example from unit test.

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
    [Fact]
    public void Test1()
    {
        var type = typeof(InModel);
        var constructorParameters = type.GetConstructors().First().GetParameters().ToList();
        Assert.True(constructorParameters[0].IsNullable());
        Assert.False(constructorParameters[1].IsNullable());
        var methodParameters = type.GetMethod(nameof(InModel.SetSomething)).GetParameters().ToList();
        Assert.True(methodParameters[0].IsNullable());
        Assert.False(methodParameters[1].IsNullable());
        var properties = type.GetProperties().ToList();
        Assert.True(properties[0].IsNullable());
        Assert.False(properties[1].IsNullable());
        var fields = type.GetFields().ToList();
        Assert.True(fields[0].IsNullable());
        Assert.False(fields[1].IsNullable());
    }

## Text extensions
You may convert as fast as possible byte[] to string or stream to byte[] or byte[] to stream or stream to string or string to stream.
For example, string to byte array and viceversa.

    string olfa = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
    var bytes = olfa.ToByteArray();
    string value = bytes.ConvertToString();

For example, string to stream and viceversa.

    string olfa = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
    var stream = olfa.ToStream();
    string value = stream.ConvertToString();

You may read a string with break lines as an enumerable of string

    string olfa = "daskemnlandxioasndslam\ndasmdpoasmdnasndaslkdmlasmv\nasmdsa";
    var stream = olfa.ToStream();
    var strings = new List<string>();
    await foreach (var x in stream.ReadLinesAsync())
    {
        strings.Add(x);
    }

A simple method to make uppercase the first character.

    string olfa = "dasda";
    var olfa2 = olfa.ToUpperCaseFirst();

A simple method to check if a char is contained at least X times.

    string value = "abcderfa";
    bool containsAtLeastTwoAChar = value.ContainsAtLeast(2, 'a');

## Character separated-value (CSV)
Transform any kind of IEnumerable data in a CSV string.

    string value = _models.ToCsv();


## Minimization of a model (based on CSV concept)
It's a brand new idea to serialize any kind of objects (with lesser occupied space of json), the idea comes from Command separated-value standard.
To serialize

    string value = _models.ToMinimize();

To deserialize (for instance in a List of a class named CsvModel)

    value.FromMinimization<List<CsvModel>>();

## Extensions for json
I don't know if you are fed up to write JsonSerializer.Serialize, I do, and so, you may use the extension method to serialize faster.
To serialize

    var text = value.ToJson();

To deserialize in a class (for instance a class named User)

    var value = text.FromJson<User>();

## Extensions for Task
I don't know if you still are fed up to write .ConfigureAwait(false) to eliminate the context waiting for a task. I do.
[Why should I set the configure await to false?](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
To set configure await to false

    await {your async task}.NoContext();

Instead, to get the result as synchronous result but with a configure await set to false.

    {your async task}.ToResult();

You may change the behavior of your NoContext() or ToResult(), setting (in the bootstrap of your application for example)

    RystemTask.WaitYourStartingThread = true;

When do I need a true? In windows application for example you have to return after a button clicked to the same thread that started the request.

## TaskManager
When you need to run a list of tasks concurrently you may use this static method.

In the next example with TaskManager.WhenAll you may run a method ExecuteAsync {times} times with {concurrentTasks} times in concurrency, and running them when a time slot is free.
For example if you run this function with 8 times and 3 concurrentsTasks and true in runEverytimeASlotIsFree
You will have this behavior: first 3 tasks starts and since the fourth the implementation waits the end of one of the 3 started before. As soon as one of the 3 started is finished the implementation starts to run the fourth.

    var bag = new ConcurrentBag<int>();
    await TaskManager.WhenAll(ExecuteAsync, times, concurrentTasks, runEverytimeASlotIsFree).NoContext();

    Assert.Equal(times, bag.Count);

    async Task ExecuteAsync(int i, CancellationToken cancellationToken)
    {
        await Task.Delay(i * 20, cancellationToken).NoContext();
        bag.Add(i);
    }

You may run a {atLeast} times of tasks and stopping to wait the remaining tasks with TaskManager.WhenAtLeast

    var bag = new ConcurrentBag<int>();
    await TaskManager.WhenAtLeast(ExecuteAsync, times, atLeast, concurrentTasks).NoContext();

    Assert.True(bag.Count < times);
    Assert.True(bag.Count >= atLeast);

    async Task ExecuteAsync(int i, CancellationToken cancellationToken)
    {
        await Task.Delay(i * 20, cancellationToken).NoContext();
        bag.Add(i);
    }

## Concurrency

### ConcurrentList
You can use the ConcurrentList implementation to have the List behavior with lock operations.

    var items = new ConcurrentList<ItemClass>();