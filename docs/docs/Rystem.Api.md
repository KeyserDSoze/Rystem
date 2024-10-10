# DOCUMENTATION

## ServiceCollectionExtensions Class

### ClassName: Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions 

## Method: ConfigureEndpoints()

### Method Name: ConfigureEndpoints()

- **Purpose**: This function helps configure the endpoints of services and adds the endpointsManager as a singleton to the services collection.

- **Parameters**:
  - `IServiceCollection services`: The service collection to which the endpoints need to be added.
  - `Action<EndpointsManager> configurator`: The action that consists of the configuration logic for the endpoints.

- **Return Value**: The function returns an `IServiceCollection` that includes the singleton service added.
   
- **Usage**:

```csharp
services.ConfigureEndpoints(configurator => 
 { 
     // configuration logic 
 });
```

### Method: AddEndpoint<TService>

- **Purpose**: This function is used to add a certain endpoint to the service collection.

- **Parameters**:
  - `IServiceCollection services`: The service collection to which the endpoints need to be added.
  - `Action<ApiEndpointBuilder<TService>> builder`: This action represents the building logic for the endpoint to be added.
  - `string? name`: An optional parameter that represents the name of the factory.

- **Return Value**: It returns an `IServiceCollection` after adding the specified endpoint.
   
- **Usage**:

```csharp
services.AddEndpoint<IService>(builder => 
{ 
    // build logic
}, "name");
```

## ApiEndpointBuilder Class

### ClassName: Microsoft.AspNetCore.Builder.ApiEndpointBuilder

It is a class that provides various methods to customize the API Endpoint as per requirements.

### Method: SetEndpointName()

- **Purpose**: This method allows you to set a name for the endpoint.

- **Parameters**:
  `string name`: The name to be assigned to the endpoint.

- **Return Value**: This method returns an instance of the ApiEndpointBuilder.

- **Usage Example**:

```csharp
var builder = new ApiEndpointBuilder<T>();
builder.SetEndpointName("newName");
```

### Method: SetMethodName()

- **Purpose**: This method allows you to set a name for the method.

- **Parameters**:
  - `string name`: The name to be assigned to the method.
  - It has two version, one takes `Expression<Func<T, Delegate>> method` as a parameter, and the other takes `MethodInfo methodInfo`.

- **Return Value**: This method returns an instance of the ApiEndpointBuilder.

- **Usage Example**:

```csharp
var builder = new ApiEndpointBuilder<T>();
builder.SetMethodName(//method or MethodInfo, "newMethodName");
```

### Method: Remove()

- **Purpose**: This method allows you to remove a method.

- **Parameters**:
  - It has two versions, one takes `string methodName` as a parameter, and the other takes `MethodInfo methodInfo` as a parameter.

- **Return Value**: This method returns an instance of the ApiEndpointBuilder.

- **Usage Example**:

```csharp
var builder = new ApiEndpointBuilder<T>();
builder.Remove(//method or MethodInfo);
```

### Method: AddAuthorization()

- **Purpose**: This method allows you to add authorization to the method.

- **Parameters**:
  - `Expression<Func<T, Delegate>> method`: method expression for a delegate.
  - `params string[] policies`: policy expression.

- **Return Value**: This method returns an instance of the ApiEndpointBuilder with authorization added to the method.

- **Usage Example**:

```csharp
var builder = new ApiEndpointBuilder<T>();
builder.AddAuthorization(//method, "policy1", "policy2");
```

### Method: SetupParameter()

- **Purpose**: This method allows you to set up a parameter.

- **Parameters**:
  - `string parameterName`: the name of the parameter.
  - `Action<EndpointMethodParameterValue> setup`: action to set the parameter value.

- **Return Value**: This method returns an instance of the ApiEndpointBuilder.

- **Usage Example**:

```csharp
var builder = new ApiEndpointBuilder<T>();
builder.SetupParameter(//method or MethodInfo, "paramName", //an action);
```

For other methods, the usage is similar.

Note: It's suggested that the developer have an intermediate understanding of C# action and func delegates to handle the Action or Func objects being used in these methods. Also, the developers are assumed to have a basic understanding of the ASP.dotnet core environment to use these classes and methods.