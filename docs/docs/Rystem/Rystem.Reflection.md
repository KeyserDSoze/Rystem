# Documentation: `ReflectionHelper` Class in `Rystem.Reflection` Namespace

The `Rystem.Reflection` namespace contains helper methods related to Reflection in .NET. One of the classes found within this namespace is `ReflectionHelper`. This static class contains a method to fetch the class name for a given stack frame at a specified depth.

## Method Details

### `NameOfCallingClass` Method

**Method Name**: `NameOfCallingClass`

**Description**: Determines the name of the class that invoked a method. It does so by examining the method execution's stack trace. The search starts from the given depth and continues upwards until a class name is found that is not from the core `mscorlib.dll`.

**Parameters**:
   - `int deep`: The initial depth from which the search for the calling class begins. Default value is 1.
   - `bool full`: Flag defining whether to return the full name of the class (with namespace) or just the class name. For `true`, it returns the full class name including namespace. For `false` (default value), it returns just the class name.

**Return Value**: `string` name of the calling class. If no calling class is found, the method returns the name of the method at the examined stack depth.

**Usage Example**: 

```C#
string className = ReflectionHelper.NameOfCallingClass(2, true);
Console.WriteLine(className);  // Outputs the full name of the class that is two frames up in the stack.
```

This method is helpful when you want to identify the class that called a particular operation, especially useful in logging or debugging scenarios. The ability to control the depth allows flexibility in terms of call stack examination.

Including the invoking class name in your application logs can give you a clearer picture of the execution flow and help speed up the troubleshooting process. As such, `NameOfCallingClass` might be frequently used within your exception handling or logging infrastructure.

Remember that, due to potential performance implications, use reflection judiciously and only when required. Always validate the stack depth you pass to avoid potential StackOverflowExceptions or arguments outside the stack trace range.