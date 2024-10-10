# Class Documentation

## `System.Linq.LinqAsyncExtensions`

### 1. Method: `AllAsync<TSource>`
**What it does:** Evaluates all elements in an `IEnumerable<TSource>` collection based on a given condition specified by a `Expression<Func<TSource, ValueTask<bool>>>` (a predicate).

**Parameters:**
- `source`: A collection of type `TSource`. It serves as the input data.
- `expression`: An expression that defines the condition for each item in the source collection. 

**Return value:** Returns `ValueTask<bool>`. If all elements satisfy the condition, the method returns `true`. Otherwise, it returns `false`.

**Usage Example:**
```csharp
IEnumerable<int> collection = new List<int>() { 1, 2, 3, 4, 5 };
Expression<Func<int, ValueTask<bool>>> expression = num => new ValueTask<bool>(num > 0);
bool result = await collection.AllAsync(expression);
```

### 2. Method: `AnyAsync<TSource>`
**What it does:** Evaluates if any element in a collection satisfies a condition defined by a `Expression<Func<TSource, ValueTask<bool>>>`.

**Parameters:**
- `source`: A collection of type `TSource`. It serves as the input data.
- `expression`: An expression that defines the condition for each item in the source collection. 

**Return value:** Returns a `ValueTask<bool>`. If any element satisfy the condition, the method returns `true`. Otherwise, it returns `false`.

**Usage Example:**
```csharp
IEnumerable<int> collection = new List<int>() { 1, 2, 3, 4, 5 };
Expression<Func<int, ValueTask<bool>>> expression = num => new ValueTask<bool>(num > 5);
bool result = await collection.AnyAsync(expression);
```


## `System.Linq.QueryableLinqExtensions`

The `QueryableLinqExtensions` class provides extension methods for `IQueryable` data types, such as entities in a database query. These methods inherently support asynchronous operation.

### 3. Method: `CallMethod<TSource, TResult>`
**What it does:** Invokes a generic version of a method for a query with a given name, based on the provided `LambdaExpression`.

**Parameters:**
* `query`: An `IQueryable<TSource>` where the method will be applied.
* `methodName`: A `string` that specifies the name of the method to be invoked.
* `expression`: A `LambdaExpression` to be passed to the method.
* `typeWhereToSearchTheMethod`: A `Type`, which specifies where to find the method; by default, it examines `System.Linq.Queryable`.

**Return value:** Returns an object of type `TResult`, which is the result of the method invocation.

**Usage Example:**
```csharp
IQueryable<int> query = new List<int>() { 1, 2, 3, 4, 5 }.AsQueryable();
string methodName = "Average";
LambdaExpression expression = Expression.Lambda<Func<int, int>>(Expression.Constant(1), Expression.Parameter(typeof(int)));
var result = query.CallMethod<int, decimal>(methodName, expression);
```

### 4. Method: `GroupBy<TSource, TKey>`
**What it does:** Groups items in an `IQueryable<TSource>` according to a specified `LambdaExpression`.

**Parameters:**
* `source`: An `IQueryable<TSource>` to be grouped.
* `keySelector`: A `LambdaExpression` that defines the selector function.

**Return value:** Returns an `IQueryable<IGrouping<TKey, TSource>>` where each `IGrouping<TKey,TSource>` object contains a collection of `TSource` objects and a key.

**Usage Example:**
```csharp
IQueryable<int> source = new List<int>() { 1, 1, 2, 3, 4, 5 }.AsQueryable();
LambdaExpression keySelector = Expression.Lambda<Func<int, int>>(Expression.Constant(1), Expression.Parameter(typeof(int)));
var result = source.GroupBy<int, int>(keySelector);
```

And many other methods available in the `QueryableLinqExtensions` follow similar patterns, extending the `IQueryable<TSource>`, and can be used in similar ways as illustrated above.