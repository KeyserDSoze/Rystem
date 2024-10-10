# Rystem.DependencyInjection Documentation

## Class: PopulationSettings

This class acts as a collection of settings that influence the generation of random population data for testing or development purposes.

### Properties:

- **Dictionary<string, string[]> RegexForValueCreation**: 
  - This dictionary is used to create values using regular expressions. The dictionary key corresponds to the name of a specific property on an object, and the array of strings is a set of regular expressions it can match.
  
- **Dictionary<string, Func<dynamic>> DelegatedMethodForValueCreation**:
  - This dictionary allows you to specify methods to generate values for specific properties. The key of the dictionary is the name of the property, and the value is a delegate function that returns the value for that property.

- **Dictionary<string, Func<IServiceProvider, Task<dynamic>>> DelegatedMethodForValueRetrieving**:
  - If you need to retrieve a value asynchronously using an IServiceProvider, you can define a method that takes the IServiceProvider as a parameter and returns a Task<dynamic>. The key of this dictionary should be the name of the property for which the value is retrieved.

- **Dictionary<string, Func<IServiceProvider, Task<IEnumerable<dynamic>>>> DelegatedMethodWithRandomForValueRetrieving**:
  - Similar to `DelegatedMethodForValueRetrieving`, but for methods that return an IEnumerable of dynamic objects.

- **Dictionary<string, dynamic> AutoIncrementations**:
  - This property allows for the automatic increment of certain values. The key corresponds to the property on an object, and the value is the measure of the auto-increment.

- **Dictionary<string, RandomPopulationFromRystemSettings> RandomValueFromRystem**:
  - This dictionary hooks up the random population generator with specific properties. The key or the dictionary is the property name, and the value is a `RandomPopulationFromRystemSettings` object.

- **Dictionary<string, Type> ImplementationForValueCreation**:
  - This dictionary allows you to map a type to a specific property for creating a value.

- **Dictionary<string, int> ForcedNumberOfElementsForEnumerable**:
  - This dictionary lets you dictate the number of elements for an IEnumerable property. The key is the property name, and the value is the number of elements.

- **int NumberOfElements**: 
  - General number of elements be created by the population generator.

## Class: PopulationSettings<T>

This class simply inherits from `PopulationSettings`, allowing the users to define settings that are strongly typed to a specific class.

## Class: RandomPopulationFromRystemSettings

A settings store governing decisions about the random population generated values.

### Properties:
- **bool UseTheSameRandomValuesForTheSameType**:
  - If set to true, this ensures that all properties of the same type get populated with the same random value.

- **Type StartingType**:
  - Manually specify the seed type used for generating random values. 

- **Func<dynamic>? Creator**:
  - A delegate function that can be utilized to create custom random values.

- **string? ForcedKey**:
  - Defines a forced key under which the value can be retrieved.
