# Class Documentation

## Class: JsonExtensions

This class is located in the `System.Text.Json` namespace and provides methods to work with JSON. This includes converting an entity to JSON and extracting an entity from a JSON representation.

### Method: ToJson

**Description**:  
This method allows you to convert a provided entity into a JSON representation. The method utilizes the JsonSerializer class that is part of .NET's `System.Text.Json` library.

**Parameters**:  

- `T entity`: Specifies the entity input that you are trying to convert to a JSON representation. The entity could be of any type (T).
- `JsonSerializerOptions? options = default`: This optional parameter allows you to specify settings that configure parsing behavior when using `JsonSerializer`.

**Return Value**:  
This method returns a `string` that represents the JSON representation of the input entity.

**Usage Example**:

```csharp
var student = new Student(){ Name = "John", Age = 22 };
string studentJson = student.ToJson();
```

### Method: FromJson (Overload 1)

**Description**:  
This method allows you to convert a JSON string into a specified entity type.

**Parameters**:  

- `string entity`: The JSON string that you want to convert to an entity.
- `JsonSerializerOptions? options = default`: This optional parameter lets you specify settings for parsing JSON when using `JsonSerializer`.

**Return Value**:  
This method returns an entity of the type (T), which was extracted from its JSON representation.

**Usage Example**:

```csharp
string studentJson = "{\"Name\":\"John\", \"Age\":22}";
Student student = studentJson.FromJson<Student>();
```

### Method: FromJson (Overload 2)

**Description**:  
This method converts a byte array into a specified entity type.

**Parameters**:

- `byte[] entity`: The byte array of a JSON string that you want to convert to an entity.
- `JsonSerializerOptions? options = default`: Optional settings for parsing JSON data with `JsonSerializer`.

**Return Value**:  
The method returns a deserialized entity of type (T) represented by the byte array.

**Usage Example**:

```csharp
byte[] studentJsonBytes = Encoding.Default.GetBytes("{\"Name\":\"John\", \"Age\":22}");
Student student = studentJsonBytes.FromJson<Student>();
```

### Method: FromJson (Overload 3)

**Description**:  
This method converts a JSON string into a specific entity type known at runtime.

**Parameters**:

- `string entity`: The JSON string to convert to an object.
- `Type type`: The `System.Type` of the object to convert the JSON string to.
- `JsonSerializerOptions? options = default`: Optional settings to use when parsing JSON data.

**Return Value**:  
The method returns a deserialized object of the given `Type`.

**Usage Example**:

```csharp
string studentJson = "{\"Name\":\"John\", \"Age\":22}";
object student = studentJson.FromJson(typeof(Student));
```

### Method: FromJsonAsync

**Description**:  
This method asynchronously converts a Stream to a specific entity type.

**Parameters**:

- `Stream entity`: The stream containing the JSON text to convert to an entity.
- `JsonSerializerOptions? options = default`: The Deserialize options.

**Return Value**:  
The method returns an awaitable `Task<T>` that will result in a deserialized entity of specified type (T) upon completion.

**Usage Example**:

```csharp
Stream studentJsonStream = new MemoryStream(Encoding.UTF8.GetBytes("{\"Name\":\"John\", \"Age\":22}"));
Student student = await studentJsonStream.FromJsonAsync<Student>();
```
