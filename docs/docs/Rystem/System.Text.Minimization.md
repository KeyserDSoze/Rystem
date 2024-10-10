# System.Text.Minimization

This namespace consists of several classes and interfaces that principally help in the process of optimizing (minimizing) the data in certain ways, such as serialization and deserialization. Here, we will provide detailed documentation for each class and its public methods.

---

## **1. MinimizationConvertExtensions**

### **Method: ToMinimize**

This method minimizes the given data of generic type.

- **Parameters**:
  - **data (T)**: Data of any type that needs to be minimized.
  - **startSeparator (char?)**: An optional character to use as the starting separator. the default value is null.
  
- **Return Value**:
  It returns a string which is the minimized version of the input data.

- **Usage Example**:
    ```csharp
    var data = new List<int> {1, 2, 3};
    var minimizedData = data.ToMinimize();
    ```

### **Method: FromMinimization**

This method converts a minimized string back to its original form.

- **Parameters**:
  - **value (string)**: The minimized string value.
  - **startSeparator (char?)**: An optional character that was used as the starting separator during minimization. the default value is null.

- **Return Value**:
  It returns the original form of the minimized data of type T.

- **Usage Example**:
    ```csharp
    var minimizedData = "1,2,3";
    var originalData = minimizedData.FromMinimization<List<int>>();
    ```
---

## **2. MinimizationIgnore**

Used to mark certain parameters to be ignored during the Minimization process. This class doesn't have any public methods defined, it's an attribute used to define how other methods behave. It's used like this:

```csharp
[MinimizationIgnore]
public int PropertyName { get; set; }
```
In this example, the attribute would exclude `PropertyName` from being minimized.

---

## **3. MinimizationPropertyAttribute**

Represents an attribute to mark certain properties for the Minimization process.

### **Properties: Column**
The order of the column in the minimization process. 

### **Usage Example**:
```csharp
[MinimizationPropertyAttribute(1)]
public int PropertyName { get; set; }
```
In this example, the property `PropertyName` would take the 1st position in the process of minimization.

---

The internal classes (**ArraySerializer**, **DictionarySerializer**, **EnumerableSerializer**, **ObjectSerializer**, **PrimitiveSerializer**, **Serializer**, and **IMinimizationInterpreter** interface) are utilized internally to perform serialization and deserialization of different data types and structures during the process of minimization. These classes are not designed for direct use outside the library.

Note: The namespace also includes important attributes like `MinimizationIgnore`and `MinimizationPropertyAttribute` which are used to control how objects are minimized, down to the parameter level.

---

## **Test class: MinimizationTest**
The test class includes two test methods `Test1` and `Test2`, each of which sequentially uses the extention methods `ToMinimize` and `FromMinimization` to make sure the minimized string can be correctly converted back to its original form. The tests assert that the Minimization procedures are working correctly.

**Usage examples from test cases:**

```csharp
var value = s_models.ToMinimize('&');
var models2 = value.FromMinimization<List<CsvModel>>('&');
```
In this example, `ToMinimize` is used with the `&` character as the start separator to minimize a list of `CsvModel`, and `FromMinimization` is utilized to convert it back. 

```csharp
var value = s_models.ToMinimize();
var models2 = value.FromMinimization<List<CsvModel>>();
```
In this example, `ToMinimize` and `FromMinimization` are used without specifying a start separator and the library handles the choice of separator character.