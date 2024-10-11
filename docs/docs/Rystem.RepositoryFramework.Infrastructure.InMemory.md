# RepositoryFramework.InMemory Documentation

## 1. ExceptionOdds Class
This class defines the mapping for exceptions that may arise during a method's operations.

### Method Details
None. This class has no methods.

### Attributes:

**Percentage (Getter and Setter)**:
This attribute can set/get the percentage chance for an exception to occur. The percentage is a floating point value, which can range from 0.000000000000000000000000001% and 100%. This percentage corresponds to values between 0.000000000000000000000000001 and 100 under this attribute.

- Type: double

**Exception (Getter and Setter)**:
This attribute can set/get the associated Exception that will be thrown based on the percentage attribute.

- Type: Exception?

## 2. MethodBehaviorSetting Class
This class defines the behavior of a method such as waiting time and exceptions to throw.

### Method Details
None. This class has no methods.

### Attributes:

**MillisecondsOfWait (Getter and Setter)**:
This attribute represents the waiting time (in range) for each request.

- Type: Range

**MillisecondsOfWaitWhenException (Getter and Setter)**:
This attribute represents the waiting time (in range) after an exception occurs.

- Type: Range

**ExceptionOdds (Getter and Setter)**:
This attribute represents a list of the ExceptionOdds class which has an Exception and its corresponding odds of occurrence.

- Type: List<ExceptionOdds>

**Default (Getter only)**:
This attribute stores the default values of an instance of MethodBehaviorSetting. 

- Type: MethodBehaviorSetting

## 3. RepositoryBehaviorSettings Class
This class permits settings of behavior for specific repository methods.

### Method Details

**AddForRepositoryPattern**
This method applies behavior settings for all methods in a repository.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void
   
   
**AddForCommandPattern**
This method applies behavior settings for command methods in a repository (insert, update, delete, batch).

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

   
**AddForQueryPattern**
This method applies behavior settings for query methods in a repository (get, query, exist, operation).

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void
  

**AddForInsert**
This method applies behavior settings for insert method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

   
**AddForUpdate**
This method applies behavior settings for the update method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void
  

**AddForDelete**
This method applies behavior settings for the delete method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void


**AddForBatch**
This method applies behavior settings for the batch method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void


**AddForGet**
This method applies behavior settings for the get method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

**AddForQuery**
This method applies behavior settings for the query method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

**AddForExist**
This method applies behavior settings for the exist method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

**AddForCount**
This method applies behavior settings for the count method.

- Parameters: setting (MethodBehaviorSetting)
- Return Value: Void

**Get**
This method retrieves the behavior settings for the particular method. If it's not found, then it retrieves settings for all methods, or returns the default method behavior setting.

- Parameters: method (RepositoryMethods)
- Return Value: MethodBehaviorSetting

## 4. RepositoryBuilderExtensions Class:

This class contains extension methods for RepositoryBuilder class. These methods provide the convenience of adding in-memory storage to the repository, command, or query patterns.

### Method Details

**WithInMemory (RepositoryBuilder version)**
This method adds an in-memory storage to your repository pattern.

- Parameters: 
  - builder (IRepositoryBuilder<T, TKey>)
  - inMemoryBuilder (Action<IRepositoryInMemoryBuilder<T, TKey>>)
  - name (string)
- Return Value: IRepositoryBuilder<T, TKey>

**WithInMemory (CommandBuilder version)**
This method adds an in-memory storage to your command pattern.

- Parameters: 
  - builder (ICommandBuilder<T, TKey>)
  - inMemoryBuilder (Action<IRepositoryInMemoryBuilder<T, TKey>>)
  - name (string)
- Return Value: ICommandBuilder<T, TKey>

**WithInMemory (QueryBuilder version)**
This method adds an in-memory storage to your query pattern.

- Parameters: 
  - builder (IQueryBuilder<T, TKey>)
  - inMemoryBuilder (Action<IRepositoryInMemoryBuilder<T, TKey>>)
  - name (string)
- Return Value: IQueryBuilder<T, TKey>

These methods return an Interface that corresponds to the type of pattern that you added in-memory storage to. The returned interface can be used to store and retrieve data from the in-memory storage you added using these methods.
