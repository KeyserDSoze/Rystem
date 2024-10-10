# **Class: CastExtensions**

This class contains extension methods for casting a value of any given type to another specified type. The type conversions include the common primitive types and some system types.

## **Method: Cast<T>**

This method allows you to cast an object to another type.

**Parameters:**
- **entity** `(object)`: The object that needs to be cast.

**Return Value:**
The method returns a value of type T. Returns a default value if the cast isn't successful.

**Usage Example:**
```csharp
string myStr = "100";
int myConvertedStr = myStr.Cast<int>();
Console.WriteLine(myConvertedStr);  // Output: 100
```
## **Method: Cast**

This method allows you to cast an object to a type specified at runtime.

**Parameters:**
- **entity** `(object)`: The object that requires casting.
- **typeToCast** `(Type)`: The type to which the `entity` needs to be cast.

**Return Value:**
The method returns an object that has been cast to `typeToCast`. Returns a default value if the cast fails.

**Usage Example:**
```csharp
string myStr = "100";
Type myType = typeof(int);
dynamic myConvertedStr = CastExtensions.Cast(myStr, myType);
Console.WriteLine(myConvertedStr);  // Output: 100
```

# **Class: CopyExtensions**

This class contains methods that help to deep copy an object or copy properties from one object to another object of the same type.

## **Method: ToDeepCopy<T>**

This method creates a deep copy of the source object of generic type T.

**Parameters:**
- **source** `(T)`: The object that needs to be deeply copied.

**Return Value:**
The method returns a new object with the cloned values from the provided `source`. Returns a default value if the object to be copied is null.

**Usage Example:**
```csharp
Student student1 = new Student { Name = "John", Age = 20 };
Student student2 = student1.ToDeepCopy();
```

## **Method: ToDeepCopy**

This method creates a deep copy of the source object.

**Parameters:**
- **source** `(object)`: The object that needs to be deeply copied.

**Return Value:**
The method returns a new object with cloned values from the provided `source`. Returns a default value if the object to be copied is null.

**Usage Example:**
```csharp
object myObject1 = new Student { Name = "John", Age = 20 };
object myObject2 = myObject1.ToDeepCopy();
```
# **Class: EnumExtensions**

This class provides extension methods for Enum and string types to convert them to specific Enum type.

## **Method: ToEnum<TEnum> (for Enum types)**

This method converts a source Enum to another Enum type TEnum.

**Parameters:**
- **source** `(Enum)`: The source Enum which needs to be converted.

**Return Value:**
The method returns an Enum of type TEnum.

**Usage Example:**
```csharp
public enum MyEnum { One = 1, Two = 2 }
...
DayOfWeek day = DayOfWeek.Monday;
MyEnum myEnum = day.ToEnum<MyEnum>();  // Output: MyEnum.One
```
# **Class: StringExtensions**

This class provides methods to manipulate strings, such as replacing occurrences of a substring.

## **Method: Replace**

This method replaces `occurrences` times of the substring `oldValue` with a `newValue` in the provided `value`.

**Parameters:**
- **value** `(string)`: The source string.
- **oldValue** `(string)`: The substring that needs to be replaced.
- **newValue** `(string)`: The new substring to replace `oldValue`.
- **occurrences** `(int)`: The number of occurrences of `oldValue` that should be replaced.

**Return Value:**
The method returns a modified string with replaced values.

**Usage Example:**
```csharp
string myStr = "Hello world! Hello world!";
string newStr = myStr.Replace("world", "universe", 1);
Console.WriteLine(newStr);  // Output: "Hello universe! Hello world!"
```

# **Class: Try**

This class provides methods that execute other methods and handle exceptions with the option for retry behavior.

## **Method: WithDefaultOnCatch**

This method executes a function and catches any thrown exceptions.

**Parameters:**
- **function** `(Func<T>)`: The function to execute.
- **behavior** `(Action<TryBehavior>?)`: This optional action defines the retry policy.

**Return Value:**
The method returns an instance of `TryResponse<T>`. This includes both the result of `function` and any exception that might have occurred.

**Usage Example:**
```csharp
TryResponse<int> myTry = Try.WithDefaultOnCatch(() => { 
    // your code that may throw exception.
    return 1;
});
```

# **Class: Stopwatch**

This class provides methods to monitor the execution time of operations.

## **Method: Monitor**

This method measures the execution time of an action.

**Parameters:**
- **action** `(Action)`: The action whose execution time needs to be measured.

**Return Value:**
The method returns a `StopwatchResult` which contains information about the start and stop time of the action's execution.

**Usage Example:**
```csharp
StopwatchResult result = Stopwatch.Monitor(() => {
    // your code here (e.g., a lengthy operation)
});
Console.WriteLine(result.Span.TotalSeconds);
```

# **Class: StopwatchStart**

## **Method: Stop**

This method stops the stopwatch and returns the elapsed time between start and stop as a `StopwatchResult` instance.

**Return Value:**
The method returns an instance of `StopwatchResult`.

**Usage Example:**
```csharp
StopwatchStart stopwatch = Stopwatch.Start();
Thread.Sleep(2000);
StopwatchResult result = stopwatch.Stop();
Console.WriteLine(result.Span.TotalSeconds);  // Output: 2
```