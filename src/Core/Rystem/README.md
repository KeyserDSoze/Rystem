### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

---
# Table of Contents

- [What is Rystem?](#what-is-rystem)
- [Discriminated Union in C#](#discriminated-union-in-c)
  - [What is a Discriminated Union?](#what-is-a-discriminated-union)
  - [UnionOf Classes](#unionof-classes)
  - [JSON Integration](#json-integration)
- [Usage Examples](#usage-examples)
  - [Defining Unions](#defining-unions)
  - [Serializing and Deserializing JSON](#serializing-and-deserializing-json)
- [Benefits of Using It](#benefits-of-using-it)
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

This library introduces a `UnionOf<T0, T1, ...>` class that implements discriminated unions in C#. Discriminated unions are a powerful type system feature that allows variables to store one of several predefined types, enabling type-safe and concise programming. This library also includes integration with JSON serialization and deserialization.

## What is a Discriminated Union?

A discriminated union is a type that can hold one of several predefined types at a time. It provides a way to represent and operate on data that may take different forms, ensuring type safety and improving code readability.

For example, a union can represent a value that is either an integer, a string, or a boolean:

```csharp
UnionOf<int, string, bool> value;
```

The `value` can hold an integer, a string, or a boolean, but never more than one type at a time.

---

## UnionOf Classes

The `UnionOf` class is implemented as follows:

```csharp
[JsonConverter(typeof(UnionConverterFactory))]
public class UnionOf<T0, T1> : IUnionOf
{
    private Wrapper[]? _wrappers;
    public int Index { get; private protected set; } = -1;
    public T0? AsT0 => TryGet<T0>(0);
    public T1? AsT1 => TryGet<T1>(1);
    private protected virtual int MaxIndex => 2;
    public UnionOf(object? value)
    {
        UnionOfInstance(value);
    }
    private protected void UnionOfInstance(object? value)
    {
        _wrappers = new Wrapper[MaxIndex];
        var check = SetWrappers(value);
        if (!check)
            throw new ArgumentException($"Invalid value in UnionOf. You're passing an object of type: {value?.GetType().FullName}", nameof(value));
    }
    private protected Q? TryGet<Q>(int index)
    {
        if (Index != index)
            return default;
        var value = _wrappers![index];
        if (value?.Entity == null)
            return default;
        var entity = (Q)value.Entity;
        return entity;
    }
    private protected virtual bool SetWrappers(object? value)
    {
        foreach (var wrapper in _wrappers!)
        {
            if (wrapper?.Entity != null)
                wrapper.Entity = null;
        }
        Index = -1;
        if (value == null)
            return true;
        else if (Set<T0>(0, value))
            return true;
        else if (Set<T1>(1, value))
            return true;
        return false;
    }
    private protected bool Set<T>(int index, object? value)
    {
        if (value is T v)
        {
            Index = index;
            _wrappers![index] = new(v);
            return true;
        }
        return false;
    }
    public T? Get<T>() => Value is T value ? value : default;
    public object? Value
    {
        get
        {
            foreach (var wrapper in _wrappers!)
            {
                if (wrapper?.Entity != null)
                    return wrapper.Entity;
            }
            return null;
        }
        set
        {
            SetWrappers(value);
        }
    }
    public static implicit operator UnionOf<T0, T1>(T0 entity)
        => new(entity);
    public static implicit operator UnionOf<T0, T1>(T1 entity)
        => new(entity);
    public override string? ToString()
        => Value?.ToString();
    public override bool Equals(object? obj)
    {
        if (obj == null && Value == null)
            return true;
        var dynamicValue = ((dynamic)obj!).Value;
        return Value?.Equals(dynamicValue) ?? false;
    }
    public override int GetHashCode()
        => RuntimeHelpers.GetHashCode(Value);
}

public interface IUnionOf
{
    object? Value { get; set; }
    int Index { get; }
    T? Get<T>();
}
```

The library defines `UnionOf<T0, T1, ..., Tn>` classes, supporting up to 8 types:

- `UnionOf<T0, T1>`
- `UnionOf<T0, T1, T2>`
- ...
- `UnionOf<T0, T1, ..., T7>`

Each union class contains methods and properties for:

1. Accessing the current type value.
2. Performing safe operations based on the active type.
3. Ensuring type safety during assignments.

---

## JSON Integration

This library supports seamless JSON serialization and deserialization for discriminated unions. It uses a mechanism called **"Signature"** to identify the correct class during deserialization. The "Signature" is constructed based on the names of all properties that define each class in the union.

### How "Signature" Works

1. During deserialization, the library analyzes the properties present in the JSON.
2. The "Signature" matches the property names in the JSON to a predefined signature for each class in the union.
3. Once a match is found, the correct class is instantiated and populated with the data.

---

## Usage Examples

### Defining Unions

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
        SecondClass = new SecondClass { FirstProperty = "Nested.FirstProperty", SecondProperty = "Nested.SecondProperty" },
        ListOfSecondClasses = new List<SecondClass>
        {
            new SecondClass { FirstProperty = "List.Item1.FirstProperty", SecondProperty = "List.Item1.SecondProperty" },
            new SecondClass { FirstProperty = "List.Item2.FirstProperty", SecondProperty = "List.Item2.SecondProperty" }
        },
        DictionaryItems = new Dictionary<string, string>
        {
            { "Key1", "Value1" },
            { "Key2", "Value2" }
        },
        ArrayOfStrings = new[] { "ArrayElement1", "ArrayElement2" },
        ObjectDictionary = new Dictionary<string, SecondClass>
        {
            { "DictKey1", new SecondClass { FirstProperty = "Dict.Value1.FirstProperty", SecondProperty = "Dict.Value1.SecondProperty" } },
            { "DictKey2", new SecondClass { FirstProperty = "Dict.Value2.FirstProperty", SecondProperty = "Dict.Value2.SecondProperty" } }
        }
    }
};
```

Notice that the implicit conversion allows you to directly assign values of compatible types (e.g., `FirstClass` and `SecondClass`) without explicitly constructing a `UnionOf<T0, T1>` instance.

### Serializing and Deserializing JSON

Here is an example that demonstrates JSON integration:

#### Classes

```csharp
public class FirstClass {
    public string FirstProperty { get; set; }
    public string SecondProperty { get; set; }
}

public class SecondClass {
    public string FirstProperty { get; set; }
    public string SecondProperty { get; set; }
}

public class ThirdClass {
    public string Stringable { get; set; }
    public SecondClass SecondClass { get; set; }
    public List<SecondClass> ListOfSecondClasses { get; set; }
    public Dictionary<string, string> DictionaryItems { get; set; }
    public string[] ArrayOfStrings { get; set; }
    public Dictionary<string, SecondClass> ObjectDictionary { get; set; }
}
```

#### Example JSON

```json
{
  "OneClass_String": {
    "FirstProperty": "OneClass_String.FirstProperty",
    "SecondProperty": "OneClass_String.SecondProperty"
  },
  "SecondClass_OneClass": {
    "FirstProperty": "SecondClass_OneClass.FirstProperty",
    "SecondProperty": "SecondClass_OneClass.SecondProperty"
  }
}
```

#### Deserialization

```csharp
var json = "{\"OneClass_String\":{\"FirstProperty\":\"OneClass_String.FirstProperty\",\"SecondProperty\":\"OneClass_String.SecondProperty\"},\"SecondClass_OneClass\":{\"FirstProperty\":\"SecondClass_OneClass.FirstProperty\",\"SecondProperty\":\"SecondClass_OneClass.SecondProperty\"}}";
var deserialized = json.FromJson<CurrentTestClass>();
Console.WriteLine(deserialized.OneClass_String.AsT0.FirstProperty); // Outputs: OneClass_String.FirstProperty
```

#### Serialization

```csharp
var serializedJson = testClass.ToJson();
Console.WriteLine(serializedJson); // Outputs the JSON representation of testClass
```

---

## Benefits of Using It

1. **Type Safety**: Ensures only predefined types are used.
2. **JSON Support**: Automatically identifies and deserializes the correct type using "Signature".
3. **Code Clarity**: Reduces boilerplate code for type management and error handling.


## Extension methods

### Stopwatch
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