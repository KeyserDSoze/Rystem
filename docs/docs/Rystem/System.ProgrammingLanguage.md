# Documentation

## Class: `IProgrammingLanguage`

Provides an interface to define methods that are to be implemented by different programming languages. Each method in the interface defines a part of transforming a given `Type` to a string representation that fits a specific programming language.

### Methods:

- `Start(Type type, string name)`

  **Purpose**: Generate the initiation string of a type definition in a specific programming language. The type becomes the name of the entity being defined in the language.

  **Parameters**:
  - `type (Type)`: The .NET Type that is being converted.
  - `name (string)`: The name of the entity being defined.

  **Return Value**: The initiation string of type definition for the specific programming language.

- `GetMimeType()`

  **Purpose**: Retrieve the MIME type of the programming language.

  **Return Value**: The MIME type of the specific programming language.

- `SetProperty(string name, string type)`

  **Purpose**: Generate a string presenting a property in the specific programming language.

  **Parameters**:
  - `name (string)`: The name of the property.
  - `type (string)`: The type of the property.

  **Return Value**: A string showing how to declare a property with the `name` and `type` in a specific programming language.

- `GetPrimitiveType(Type)`

  **Purpose**: Prepare the string representation of a primitive type in a particular programming language.

  **Parameters**:
  - `type (Type)`: The primitive .NET Type that is being converted.

  **Return Value**: The representation of the primitive `Type` in the specific programming language.

- `GetNonPrimitiveType(Type)`

  **Purpose**: Prepare the string representation of a non-primitive type (like: `List`, `Array`, `Map`, etc.) in a particular programming

  **Parameters**:
  - `type (Type)`: The non-primitive .NET Type that is being converted.

  **Return Value**: The representation of the non-primitive `Type` in the specific programming language.

- `End()`

  **Purpose**: Generate the string ending a type definition in a specific programming language.

  **Return Value**: The string ending the type definition for the specific programming language.

- `ConvertEnum(string name, Type type)`

  **Purpose**: Generate the string representation of an Enum type in the specific programming language.

  **Parameters**:
  - `name (string)`: The name of the Enum.
  - `type (Type)`: The Enum type that is being converted.

  **Return Value**: The representation of the Enum in the specific programming language.

## Class: `ProgrammingLanguageExtensions`

Provides some extension methods to convert a Type or a collection of Types to their representation in a specific programming language.

### Methods:

- `ConvertAs(this IEnumerable<Type> types, ProgrammingLanguageType programmingLanguage)`

  **Purpose**: Convert a collection of types to their representation in a specific programming language in a bulk operation.

  **Parameters**:
  - `types (IEnumerable<Type>)`: The collection of .NET types that are being converted.
  - `programmingLanguage (ProgrammingLanguageType)`: The programming language to convert to.

  **Return Value**: An object of type `ProgrammingLanguangeResponse` that stores the text of all the types' representation and the MIME type of the programming language.

  **Usage Example**:
  ```csharp
  List<Type> types = new List<Type> {typeof(int), typeof(string)};
  var response = types.ConvertAs(ProgrammingLanguageType.Typescript);
  ```

- `ConvertAs(this Type type, ProgrammingLanguageType programmingLanguage, string? name = null)`

  **Purpose**: Convert a type to its representation in a specific programming language.

  **Parameters**:
  - `types (Type)`: The .NET type that is being converted.
  - `programmingLanguage (ProgrammingLanguageType)`: The programming language to convert to.
  - `name (string?)`: Optional parameter for the name of the entity. If not provided, the type Name will be used.

  **Return Value**: An object of type `ProgrammingLanguangeResponse` that stores the text of the type's representation and the MIME type of the programming language.

  **Usage Example**:
  ```csharp
  var response = typeof(int).ConvertAs(ProgrammingLanguageType.Typescript, "MyInt");
  ```

## Class: `ProgrammingLanguangeResponse`

Simple container that stores the result of a conversion to a different programming language. It includes the text of the new representation and the MIME type of the programing language.

### Properties:

- `Text (string)`: The text representation of the converted type.
- `MimeType (string)`: The MIME type of the specific programming language.

## Class: `TypeScript`

This class implements the `IProgrammingLanguage` interface to provide functionality specific to the TypeScript language.

**Note**: The method's documentation is the same as the one defined in the `IProgrammingLanguage`, but specific to TypeScript.

### Usage Example

```csharp
string name = "MyClass";
Type type = typeof(MyClass);
IProgrammingLanguage ts = new TypeScript();
var start = ts.Start(type, name);
``` 

**Note**: You will hardly need to call the `TypeScript` methods directly; this class is mainly used by calling the methods of the `ProgrammingLanguageExtensions` class.

  

