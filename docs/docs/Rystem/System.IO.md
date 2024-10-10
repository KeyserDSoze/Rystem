# Documentation 

## Class: StreamExtensions

Located under the `System.IO` namespace, the `StreamExtensions` class provides utility methods that extend the capabilities of streams and byte arrays. This class is static, meaning all its methods are also static.

---

###  - Method Name: ToStream

**Description**: This method converts byte arrays to their stream equivalent.

**Parameters**:
1. `byte[] bytes`: This parameter is a byte array to be converted into a stream.

**Return Value**: A stream derived from the input byte array (`Stream`).

**Usage Example**:
```csharp
byte[] myBytes = new byte[] { 0, 1, 2, 3 };
Stream myStream = myBytes.ToStream();
```

---

### - Method Name: ToArray

**Description**: Converts a stream to a byte array.

**Parameters**:
1. `Stream stream`: The input stream to be converted into a byte array.

**Return Value**: A byte array (`byte[]`) that represents the input stream.

**Usage Example**:
```csharp
Stream myStream = new MemoryStream();
byte[] myBytes = myStream.ToArray();
```
---

### - Method Name: ToArrayAsync

**Description**: Asynchronously converts a stream to a byte array.

**Parameters**:
1. `Stream stream`: The input stream to be converted into a byte array.

**Return Value**: An asynchronous task that yields a byte array (`Task<byte[]>`), representing the input stream.

**Usage Example**:
```csharp
Stream myStream = new MemoryStream();
byte[] myBytes = await myStream.ToArrayAsync();
```
---

The StreamExtensions class is particularly useful when dealing with I/O operations where conversion between byte arrays and streams are frequently required. For instance, reading and writing to files or network streams frequently requires such conversions.