# Documentation of the System.Text library

This documentation is for the _System.Text_ library from the Rystem Nuget package. The classes described in this document are part of the base encoding logic that uses Fare, System.Linq.Dynamic.Core, System.Interactive and System.Interactive.Async libraries. The library provides extensions for the string type, along with extensions for various encoding and decoding.

## Base45Extensions Class

This class provides extension methods to encode and decode data into and from Base45 format.

### Method Name: ToBase45 (from string value)

- **Purpose**: Converts a UTF-8 string into Base45.
- **Parameters**: 
  - `value` (_string_): The string to be converted into Base45.
- **Return Value**: The converted string in Base45 format (Type: `string`).
- **Usage Example**: 
```csharp
string base45Encoded = "Hello World!".ToBase45();
```

### Method Name: ToBase45 (from T entity)

- **Purpose**: Converts a given object into a JSON string, then encodes it into Base45.
- **Parameters**: 
  - `entity` (_T_): The object to be converted into Base45.
- **Return Value**: The encoded object in Base45 format (Type: `string`).
- **Usage Example**: 
```csharp
Person person = new Person { Name = "John Doe", Age = 35 };
string base45Encoded = person.ToBase45();
```

### Method Name: FromBase45 (from string encodedValue)

- **Purpose**: Decodes an encoded Base45 string back into a UTF-8 string.
- **Parameters**: 
  - `encodedValue` (_string_): The Base45 encoded string.
- **Return Value**: The decoded UTF-8 string (Type: `string`).
- **Usage Example**: 
```csharp
string originalValue = base45Encoded.FromBase45();
```

### Method Name: FromBase45 (from T type)

- **Purpose**: Decodes an encoded Base45 string into a specific object type.
- **Parameters**: 
  - `encodedValue` (_string_): The Base45 encoded string.
- **Return Value**: The decoded object of type T (Type: `T`).
- **Usage Example**: 
```csharp
Person originalPerson = base45Encoded.FromBase45<Person>();
```

## Base64Extensions Class

This class provides extension methods to encode and decode data into and from Base64 format.

### Method Name: ToBase64 (from string value)

- **Purpose**: Converts a UTF-8 string into Base64.
- **Parameters**: 
  - `value` (_string_): The string to be converted into Base64.
- **Return Value**: The converted string in Base64 format (Type: `string`).
- **Usage Example**: 
```csharp
string base64Encoded = "Hello World!".ToBase64();
```

### Method Name: ToBase64 (from T entity)

- **Purpose**: Converts a given object into a JSON string, then encodes it into Base64.
- **Parameters**: 
  - `entity` (_T_): The object to be converted into Base64.
- **Return Value**: The encoded object in Base64 format (Type: `string`).
- **Usage Example**: 
```csharp
Person person = new Person { Name = "John Doe", Age = 35 };
string base64Encoded = person.ToBase64();
```

### Method Name: FromBase64 (from string encodedValue)

- **Purpose**: Decodes an encoded Base64 string back into a UTF-8 string.
- **Parameters**: 
  - `encodedValue` (_string_): The Base64 encoded string.
- **Return Value**: The decoded UTF-8 string (Type: `string`).
- **Usage Example**: 
```csharp
string originalValue = base64Encoded.FromBase64();
```

### Method Name: FromBase64 (from T type)

- **Purpose**: Decodes an encoded Base64 string into a specific object type.
- **Parameters**: 
  - `encodedValue` (_string_): The Base64 encoded string.
- **Return Value**: The decoded object of type T (Type: `T`).
- **Usage Example**: 
```csharp
Person originalPerson = base64Encoded.FromBase64<Person>();
```

## StringExtensions Class

This class provides a wide range of extension methods to manipulate and convert strings, byte arrays, and data streams.

For brevity, only a few key methods are documented below:

### Method Name: ToUpperCaseFirst

- **Purpose**: Capitalizes the first letter of a string and converts the rest into lowercase.
- **Parameters**: 
  - `value` (_string_): The string to be converted.
- **Return Value**: The converted string with the first letter in uppercase and the rest in lowercase (Type: `string`).
- **Usage Example**: 
```csharp
string capitalized = "hello world!".ToUpperCaseFirst();
```

### Method Name: ConvertToStringAsync (from Stream entity)

- **Purpose**: Reads an entire stream asynchronously and returns its content as a UTF-8 encoded string.
- **Parameters**: 
  - `entity` (_Stream_): The stream to be read.
- **Return Value**: The content of the stream as a UTF-8 encoded string (Type: `Task<string>`).
- **Usage Example**: 
```csharp
string content = await stream.ConvertToStringAsync();
```

### Method Name: ToStream (from string entity)

- **Purpose**: Converts a string into a memory stream using the UTF-8 encoding.
- **Parameters**: 
  - `entity` (_string_): The string to be converted into a stream.
- **Return Value**: The converted string as a memory stream (Type: `Stream`).
- **Usage Example**: 
```csharp
Stream stream = "Hello World!".ToStream();
```

Note: The full list of methods includes additional string manipulations, conversions to/from byte arrays, stream manipulations, and encoder/decoder converters for various encoding types. Refer to the provided class list for the complete method signatures.

## EncodingType Enum

This enumeration is used in all encoding/decoding related functions in this library. It includes all the possible encodings to be used in these functions:
- Default,
- ASCII,
- UTF8,
- UTF7,
- UTF32,
- Latin1,
- BigEndianUnicode.
