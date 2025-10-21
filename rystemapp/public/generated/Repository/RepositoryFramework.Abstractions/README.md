### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

### Interfaces
Based on CQRS we could split our repository pattern in two main interfaces, one for update (write, delete) and one for read.

#### Command (Write-Delete)
    public interface ICommandPattern<T, TKey> : ICommandPattern
        where TKey : notnull
    {
        Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default);
        Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default);
        Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
    }

#### Query (Read)
    public interface IQueryPattern<T, TKey> : IQueryPattern
        where TKey : notnull
    {
        Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default);
        Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<IEntity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default);
        ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
    }

#### Repository Pattern (Write-Delete-Read)
Repository pattern is a sum of CQRS interfaces.

    public interface IRepositoryPattern<T, TKey> : ICommandPattern<T, TKey>, IQueryPattern<T, TKey>, IRepositoryPattern, ICommandPattern, IQueryPattern
        where TKey : notnull
    {
        Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default);
        Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default);
        Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default);
        Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default);
        Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default);
        IAsyncEnumerable<IEntity<T, TKey>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default);
        ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default);
    }

### Examples

#### Model
    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }
#### Command
Your storage class has to extend ICommand, and use it on injection

    public class UserWriter : ICommand<User, string>
    {
        public Task<State<User, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            //delete on with DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> InsertAsync(string key, User value, CancellationToken cancellationToken = default)
        {
            //insert on DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> UpdateAsync(string key, User value, CancellationToken cancellationToken = default)
        {
            //update on DB or storage context
            throw new NotImplementedException();
        }
        public Task<BatchResults<User, string>> BatchAsync(BatchOperations<User, string> operations, CancellationToken cancellationToken = default)
        {
            //insert, update or delete some items on DB or storage context
            throw new NotImplementedException();
        }
    }

#### Query
Your storage class has to extend IQuery, and use it on injection

    public class UserReader : IQuery<User, string>
    {
        public Task<User?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            //get an item by key from DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            //check if an item by key exists in DB or storage context
            throw new NotImplementedException();
        }
        public IAsyncEnumerable<IEntity<User, string>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            //get a list of items by a predicate with top and skip from DB or storage context
            throw new NotImplementedException();
        }
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            //get an items count by a predicate with top and skip from DB or storage context or max or min or some other operations
            throw new NotImplementedException();
        }
    }
    
#### Alltogether as repository pattern 
if you don't have CQRS infrastructure (usually it's correct to use CQRS when you have minimum two infrastructures one for write and delete and at least one for read).
You may choose to extend IRepository, but when you inject you have to use IRepository

    public class UserRepository : IRepository<User, string>, IQuery<User, string>, ICommand<User, string>
    {
        public Task<State<User, string>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            //delete on with DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> InsertAsync(string key, User value, CancellationToken cancellationToken = default)
        {
            //insert on DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> UpdateAsync(string key, User value, CancellationToken cancellationToken = default)
        {
            //update on DB or storage context
            throw new NotImplementedException();
        }
        public Task<BatchResults<User, string>> BatchAsync(BatchOperations<User, string> operations, CancellationToken cancellationToken = default)
        {
            //insert, update or delete some items on DB or storage context
            throw new NotImplementedException();
        }
        public Task<User?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            //get an item by key from DB or storage context
            throw new NotImplementedException();
        }
        public Task<State<User, string>> ExistAsync(string key, CancellationToken cancellationToken = default)
        {
            //check if an item by key exists in DB or storage context
            throw new NotImplementedException();
        }
        public IAsyncEnumerable<IEntity<User, string>> QueryAsync(IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            //get a list of items by a predicate with top and skip from DB or storage context
            throw new NotImplementedException();
        }
        public ValueTask<TProperty> OperationAsync<TProperty>(OperationType<TProperty> operation, IFilterExpression filter, CancellationToken cancellationToken = default)
        {
            //get an items count by a predicate with top and skip from DB or storage context or max or min or some other operations
            throw new NotImplementedException();
        }
    }

### How to use it
In DI you install the service. Here an example on how to set a custom storage,
prepare a translation (to translate name of your properties for query during filtering),
and AddBusiness for your integration. Furthermore you may use the factory integration from Rystem.

    var factoryName = "storage";
     services.AddRepository<AppUser, AppUserKey>(builder =>
    {
        builder.SetStorage<AppUserStorage>(factoryName);
        builder.Translate<User>()
            .With(x => x.Id, x => x.Identificativo)
            .With(x => x.Username, x => x.Nome)
            .With(x => x.Email, x => x.IndirizzoElettronico);
        builder
            .AddBusiness()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness2>();
    });

And you may inject the object
## Please, use IRepository and not IRepositoryPattern
    
    IRepository<AppUser, AppUserKey> repository

### Query and Command
In DI you install the services

    services.AddCommand<AppUser, AppUserKey>(...);
    services.AddQuery<AppUser, AppUserKey>(...);

And you may inject the objects
## Please, use ICommand, IQuery and not ICommandPattern, IQueryPattern

    ICommand<AppUser, AppUserKey> command
    IQuery<AppUser, AppUserKey> query

### TKey when it's not a primitive
You can use a class or record. 
Record is better in my opinion, for example, if you want to use the Equals operator from key, with record you don't check it by the refence but by the value of the properties in the record.
My key:

    public class MyKey 
    {
        public int Id { get; set; }
        public int Id2 { get; set; }
    }

the DI
    
    services.AddRepository<User, MyKey>(...);

and you may inject (for ICommand and IQuery is the same)

    IRepository<User, MyKey> repository

### IKey interface
You may implement the IKey interface to decide how to work with your key.
Here an example with Parse and AsString method and custom implementation with separator $.

    public class ClassicKey : IKey
    {
        public string A { get; set; }
        public int B { get; set; }
        public double C { get; set; }

        public static IKey Parse(string keyAsString)
        {
            var splitted = keyAsString.Split('$');
            return new ClassicKey { A = splitted[0], B = int.Parse(splitted[1]), C = double.Parse(splitted[2]) };
        }

        public string AsString()
        {
            return $"{A}${B}${C}";
        }
    }

### IDefaultKey
You may implement IDefaultKey if you want a simple key preconstructed parser.

    public class DefaultKey : IDefaultKey
    {
        public string A { get; set; }
        public int B { get; set; }
        public double C { get; set; }
    }

Automatically you can call AsString to receive a string composed by all properties separated by triple |, for instance {A}|||{B}|||{C}.
You can decide during startup the separator in two ways.
One with ServiceCollectionExtensions

    builder.Services.AddDefaultSeparatorForDefaultKeyInterface("$$$");

the other one with a static method offered by IDefaultKey interface

    IDefaultKey.SetDefaultSeparator("$$$");

### Default TKey record
You may use the default record key in repository framework namespace.
It's not really useful when used with no-primitive or no-struct objects (in terms of memory usage [Heap]).
For 1 value (it's not really useful I know, but I liked to create it).

    new Key<int>(2);

or for 2 values (useful)
    
    new Key<int, int>(2, 4);

or for 3 values (unuseful)
    
    new Key<int, int, string>(2, 4, "312");

or for 4 values (useful)
    
    new Key<int, int, double, Guid>(2, 4, 3, Guid.NewGuid());

or for 5 values (unuseful)
    
    new Key<int, int, string, Guid, string>(2, 4, "312", Guid.NewGuid(), "3232");

the DI
    
    services.AddRepository<User, Key<int, int>, UserRepository>();

and you may inject (for ICommand and IQuery is the same)

    IRepository<User, Key<int, int>> repository

### Translation
In some cases you need to "translate" your query for your database context query, for example in case of EF integration.

    services.AddDbContext<SampleContext>(options =>
    {
        options.UseSqlServer(configuration["ConnectionString:Database"]);
    }, ServiceLifetime.Scoped);
    services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
    {
        repositoryBuilder.SetStorage<AppUserStorage>();
        repositoryBuilder.Translate<User>()
            .With(x => x.Id, x => x.Identificativo)
            .With(x => x.Username, x => x.Nome)
            .With(x => x.Email, x => x.IndirizzoElettronico);
    });
    

In this case I'm helping the Filter class to understand how to transform itself when used in a different context.
Use Filter methods to help to translate and apply to your context the right query.

    await foreach (var user in filter.ApplyAsAsyncEnumerable(_context.Users))
        yield return new AppUser(user.Identificativo, user.Nome, user.IndirizzoElettronico, new());

You may use Filter for queryable, FilterAsEnumerable for Enumerable and FilterAsAsyncEnumerable for async enumerable context.

You can add more translations for the same model

    services.AddDbContext<SampleContext>(options =>
    {
        options.UseSqlServer(configuration["ConnectionString:Database"]);
    }, ServiceLifetime.Scoped);
    services.AddRepository<AppUser, AppUserKey>(repositoryBuilder =>
    {
        repositoryBuilder.SetStorage<AppUserStorage>();
        repositoryBuilder.Translate<User>()
            .With(x => x.Id, x => x.Identificativo)
            .With(x => x.Username, x => x.Nome)
            .With(x => x.Email, x => x.IndirizzoElettronico);
        repositoryBuilder
            .AddBusiness()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness>()
                .AddBusinessBeforeInsert<AppUserBeforeInsertBusiness2>();
    });

### Entity framework examples
[Here you may find the example](https://github.com/KeyserDSoze/RepositoryFramework/tree/master/src/RepositoryFramework.Test/RepositoryFramework.Test.Infrastructure.EntityFramework)
[Repository pattern applied](https://github.com/KeyserDSoze/RepositoryFramework/blob/master/src/RepositoryFramework.Test/RepositoryFramework.Test.Infrastructure.EntityFramework/AppUser.cs)
[Unit test flow](https://github.com/KeyserDSoze/RepositoryFramework/blob/master/src/RepositoryFramework.Test/RepositoryFramework.UnitTest/Tests/AllIntegration/AllIntegrationTest.cs)

## Business Manager
You have the chance to write your business methods to execute them before or after a command or query.
For instance, you have to check before an update or insert the value of an entity and deny the final insert/update on the database.

### Example
In this example BeforeInsertAsync runs before InsertAsync of IRepository/ICommand and AfterInsertAsync runs after InsertAsync of IRepository/ICommand.

    .AddRepository<Animal, long>(builder => {
        builder.
            WithInMemory();
        builder
            .AddBusiness()
                .AddBusinessAfterInsert<AnimalBusiness>()
                .AddBusinessBeforeInsert<AnimalBusiness>();
    });
        

more interesting usage comes to move business in another project, you can add to your infrastructure in the following way

    .AddBusinessForRepository<Animal, long>(builder => {
        builder
            .AddBusiness()
                .AddBusinessAfterInsert<AnimalBusiness>()
                .AddBusinessBeforeInsert<AnimalBusiness>();
    });
       

Then, you could have a library for infrastructure (or more than one) and a library for business to separate furthermore the concepts.

The animal business to inject will be the following one.

    public sealed class AnimalBusiness : IRepositoryBusinessBeforeInsert<Animal, long>, IRepositoryBusinessAfterInsert<Animal, long>
    {
        public static int After;
        public Task<State<Animal>> AfterInsertAsync(State<Animal, long> state, Entity<Animal, long> entity, CancellationToken cancellationToken = default)
        {
            After++;
            return Task.FromResult(state);
        }

        public static int Before;
        public Task<State<Animal, long>> BeforeInsertAsync(Entity<Animal, long> entity, CancellationToken cancellationToken = default)
        {
            Before++;
            return Task.FromResult(State.Ok(entity));
        }
    }

You have to create the class as public to allow the dependency injection to instastiate it. It's added directly to DI.

## Factory
Integration with factory from rystem is hidden in the framework, and it's ready to be used.
For instance here i'm installing two different repositories for the same model and key.

    var builder = WebApplication.CreateBuilder(args);
    builder.Services
    .AddRepository<SuperUser, string>(settins =>
    {
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(120, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        });
        settins.WithInMemory(builder =>
        {
            builder
                .PopulateWithRandomData(2, 5)
                .WithPattern(x => x.Value!.Email, @"[a-z]{5,10}@gmail\.com");
        }, "inmemory");
    });

Usage

    var serviceProvider = ....;
    IFactory<IRepository<SuperUser, string>> superUserFactory = serviceProvider.GetRequiredService<IFactory<IRepository<SuperUser, string>>>();
    var firstIntegration = superUserFactory.Create();
    var secondIntegration = superUserFactory.Create("inmemory");

By default is injected directly the last one repository integration installed.

    var serviceProvider = ....;
    IRepository<SuperUser, string> secondIntegration = serviceProvider.GetRequiredService<IRepository<SuperUser, string>>();

Here you find the "inmemory" integration.
[You can use the decorator pattern offered by Rystem](https://github.com/KeyserDSoze/Rystem/tree/master/src/Core/Rystem)
for your integration to decorate an IRepository<T, TKey> or ICommand<T, TKey> or IQuery<T, TKey>.

