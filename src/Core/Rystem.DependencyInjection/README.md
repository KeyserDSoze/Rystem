### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## 📚 Resources

- **📖 Complete Documentation**: [https://rystem.net](https://rystem.net)
- **🤖 MCP Server for AI**: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- **💬 Discord Community**: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- **☕ Support the Project**: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

---

# Table of Contents

- [Table of Contents](#table-of-contents)
  - [Warm Up](#warm-up)
  - [Execute Until Now](#execute-until-now)
  - [Service Helper](#service-helper)
    - [AddService](#addservice)
    - [HasService](#hasservice)
    - [RemoveService](#removeservice)
    - [AddOrOverrideService](#addoroverrideservice)
    - [TryAddService](#tryaddservice)
    - [TryAddSingletonAndGetService / GetSingletonService](#tryaddsingletonandgetservice--getsingletonservice)
  - [Keyed Service Helper](#keyed-service-helper)
    - [AddKeyedService](#addkeyedservice)
    - [HasKeyedService](#haskeyedservice)
  - [Abstract Factory](#abstract-factory)
    - [IFactory Interface](#ifactory-interface)
    - [Registration](#registration)
    - [Usage](#usage)
    - [HasFactory](#hasfactory)
    - [Factory Fallback](#factory-fallback)
  - [Decorator](#decorator)
    - [Decorator with Abstract Factory](#decorator-with-abstract-factory)
  - [Scan Dependency Injection](#scan-dependency-injection)
    - [Manual scan with an explicit interface](#manual-scan-with-an-explicit-interface)
    - [IScannable marker interface](#iscannable-marker-interface)
    - [Override lifetime per class](#override-lifetime-per-class)
    - [Assembly source helpers](#assembly-source-helpers)
  - [Population Service](#population-service)

---

## Warm Up

When you use the DI pattern in your .Net application you could need a warm up after the build of your services. And with Rystem you can simply do it.

```csharp
builder.Services.AddWarmUp(() => somethingToDo());
// or async
builder.Services.AddWarmUp(async serviceProvider =>
{
    await serviceProvider.GetRequiredService<MyService>().InitAsync();
});
```

After the build, trigger the warm up:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

---

## Execute Until Now

Build a temporary service provider, optionally warm it up, and execute a function against it — all in one call. Useful in test setups or pre-build initialization logic.

```csharp
// Build, resolve TService, execute
string result = await services.ExecuteUntilNowAsync((MyService svc) =>
    Task.FromResult(svc.DoWork()));

// Build, resolve via IServiceProvider
string result2 = await services.ExecuteUntilNowAsync((IServiceProvider sp) =>
    Task.FromResult(sp.GetRequiredService<MyService>().DoWork()));

// Build + WarmUp, then execute
string result3 = await services.ExecuteUntilNowWithWarmUpAsync((MyService svc) =>
    Task.FromResult(svc.DoWork()));
```

---

## Service Helper

A set of extension methods on `IServiceCollection` to register, query, and remove services without specifying the lifetime as `AddTransient` / `AddScoped` / `AddSingleton` separately.

### AddService

Register a service with a runtime-determined `ServiceLifetime`:

```csharp
// Implementation = service type
services.AddService<MyService>(ServiceLifetime.Singleton);

// Interface → implementation
services.AddService<IMyService, MyService>(ServiceLifetime.Scoped);

// With factory
services.AddService<IMyService>(sp => new MyService(sp.GetRequiredService<IDep>()), ServiceLifetime.Transient);

// Interface + factory returning implementation
services.AddService<IMyService, MyService>(sp => new MyService(), ServiceLifetime.Singleton);

// Non-generic overloads
services.AddService(typeof(IMyService), typeof(MyService), ServiceLifetime.Scoped);
```

### HasService

Check whether a service type (optionally requiring a specific implementation) is already registered:

```csharp
bool registered = services.HasService<IMyService>(out ServiceDescriptor? descriptor);

// Require a specific implementation type
bool exact = services.HasService<IMyService, MyService>(out ServiceDescriptor? descriptor2);
```

### RemoveService

Remove all non-keyed registrations for a given service type:

```csharp
services.RemoveService<IMyService>();

// Non-generic
services.RemoveService(typeof(IMyService));
```

### AddOrOverrideService

Remove any existing registration and add the new one atomically:

```csharp
services.AddOrOverrideService<IMyService, MyServiceV2>(ServiceLifetime.Scoped);

// With factory
services.AddOrOverrideService<IMyService, MyServiceV2>(sp => new MyServiceV2(), ServiceLifetime.Singleton);

// Singleton with a concrete instance
services.AddOrOverrideSingleton<IMyService>(new MyService());
services.AddOrOverrideSingleton<IMyService, MyService>(new MyService());
```

### TryAddService

Register only if no registration exists yet for the service type:

```csharp
// Only adds if IMyService is not already registered
services.TryAddService<IMyService, MyService>(ServiceLifetime.Scoped);

// With a concrete instance
services.TryAddService<IMyService>(new MyService(), ServiceLifetime.Singleton);

// With a factory
services.TryAddService<IMyService>(sp => new MyService(), ServiceLifetime.Transient);
```

### TryAddSingletonAndGetService / GetSingletonService

Add a singleton only if not yet registered, and immediately return the registered instance. Useful during the DI configuration phase.

```csharp
// Add if absent, return the instance (existing or newly created)
MyConfig config = services.TryAddSingletonAndGetService(new MyConfig { Setting = "value" });

// Auto-new() if not present
MyConfig config2 = services.TryAddSingletonAndGetService<MyConfig>();

// Interface + implementation
IMyConfig cfg = services.TryAddSingletonAndGetService<IMyConfig, MyConfig>(new MyConfig());

// Read an already-registered singleton from IServiceCollection (before Build)
MyConfig? existing = services.GetSingletonService<MyConfig>();
```

---

## Keyed Service Helper

Wrappers around the .NET 8 keyed DI API that accept a runtime `ServiceLifetime` parameter.

### AddKeyedService

```csharp
// Self-registered
services.AddKeyedService<MyService>("myKey", ServiceLifetime.Singleton);

// Interface → implementation
services.AddKeyedService<IMyService, MyService>("myKey", ServiceLifetime.Scoped);

// With factory
services.AddKeyedService<IMyService>("myKey",
    (sp, key) => new MyService(key?.ToString()),
    ServiceLifetime.Transient);

// Non-generic
services.AddKeyedService(typeof(IMyService), "myKey", typeof(MyService), ServiceLifetime.Scoped);
```

### HasKeyedService

```csharp
bool exists = services.HasKeyedService<IMyService>("myKey", out ServiceDescriptor? descriptor);

// With implementation constraint
bool exact = services.HasKeyedService<IMyService, MyService>("myKey", out _);

// Non-generic
bool raw = services.HasKeyedService(typeof(IMyService), "myKey", out _);
```

---

## Abstract Factory

Use the abstract factory when you need multiple named instances of the same service type registered with different options and/or lifetimes.

### IFactory Interface

```csharp
public interface IFactory<out TService>
{
    TService? Create(AnyOf<string?, Enum>? name = null);
    TService? CreateWithoutDecoration(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAll(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAllWithoutDecoration(AnyOf<string?, Enum>? name = null);
    bool Exists(AnyOf<string?, Enum>? name = null);
}
```

- `Create(name)` — resolve the named service (runs through any decorator)
- `CreateWithoutDecoration(name)` — resolve skipping decorators
- `CreateAll(name)` — resolve all registrations for that name
- `Exists(name)` — check whether a registration exists

### Registration

Services implement `IServiceWithOptions<TOptions>` to receive their per-name configuration:

```csharp
public interface IMyService { string GetName(); }

public class MyOptions { public string ServiceName { get; set; } }

public class MyService : IMyService, IServiceWithOptions<MyOptions>
{
    public MyOptions Options { get; set; }
    public string Id { get; } = Guid.NewGuid().ToString();
    public string GetName() => $"{Options.ServiceName} with id {Id}";
}
```

Register with `AddFactory`:

```csharp
// Synchronous
services.AddFactory<IMyService, MyService, MyOptions>(
    options => { options.ServiceName = "singleton"; },
    "singleton",
    ServiceLifetime.Singleton);

services.AddFactory<IMyService, MyService, MyOptions>(
    options => { options.ServiceName = "transient"; },
    "transient",
    ServiceLifetime.Transient);

services.AddFactory<IMyService, MyService, MyOptions>(
    options => { options.ServiceName = "scoped"; },
    "scoped",
    ServiceLifetime.Scoped);
```

For async options builders implement `IServiceOptions<TOptions>`:

```csharp
public class MyBuiltOptions : IServiceOptions<MyOptions>
{
    public string ServiceName { get; set; }
    public Task<Func<MyOptions>> BuildAsync()
        => Task.FromResult(() => new MyOptions { ServiceName = ServiceName });
}

await services.AddFactoryAsync<IMyService, MyService, MyBuiltOptions, MyOptions>(
    opts => { opts.ServiceName = "async-scoped"; },
    "async-scoped");
```

### Usage

```csharp
var factory = serviceProvider.GetRequiredService<IFactory<IMyService>>();

var singleton = factory.Create("singleton");   // same instance every time
var transient = factory.Create("transient");   // new instance every time
var scoped    = factory.Create("scoped");      // same within the scope

bool exists  = factory.Exists("singleton");    // true
bool missing = factory.Exists("unknown");      // false

// Resolve all services registered under the same name
IEnumerable<IMyService> all = factory.CreateAll("singleton");
```

Singleton / transient / scoped lifetime semantics are honoured exactly as in standard DI:

```csharp
Assert.Equal(factory.Create("singleton").Id, factory.Create("singleton").Id);    // same
Assert.NotEqual(factory.Create("transient").Id, factory.Create("transient").Id); // different
Assert.Equal(factory.Create("scoped").Id,    factory.Create("scoped").Id);       // same within scope
```

### HasFactory

Check whether a named factory registration exists before the service provider is built:

```csharp
bool registered = services.HasFactory<IMyService>("singleton");
bool byEnum     = services.HasFactory<IMyService>(MyEnum.Singleton);

// Non-generic
bool raw = services.HasFactory(typeof(IMyService), "singleton");
```

### Factory Fallback

A fallback is invoked when `factory.Create(name)` is called with a name that has no registration.

```csharp
// Implement IFactoryFallback<TService>
public class MyFallback : IFactoryFallback<IMyService>
{
    public IMyService Create(AnyOf<string?, Enum>? name = null)
        => new DefaultService(name?.AsString() ?? "default");
}

services.AddFactoryFallback<IMyService, MyFallback>();

// Or use a delegate directly
services.AddActionAsFallbackWithServiceProvider<IMyService>(builder =>
    new DefaultService(builder.Name ?? "default"));
```

---

## Decorator

A decorator replaces the registered service and receives the previous registration via `SetDecoratedService`, declared by the `IDecoratorService<TService>` interface.

```csharp
public class MyServiceDecorator : IMyService, IDecoratorService<IMyService>
{
    public string Id { get; } = Guid.NewGuid().ToString();
    private IMyService _inner;

    public void SetDecoratedService(IMyService service) => _inner = service;
    public void SetFactoryName(string name) { }

    public string GetName() => $"[decorated] {_inner.GetName()}";
}
```

Setup:

```csharp
services.AddService<IMyService, MyService>(ServiceLifetime.Scoped);
services.AddDecoration<IMyService, MyServiceDecorator>(null, ServiceLifetime.Scoped);
```

Usage:

```csharp
// Resolved service is the decorator
var decorated = provider.GetRequiredService<IMyService>();

// Access the inner (pre-decoration) service directly
var inner = provider.GetRequiredService<IDecoratedService<IMyService>>();
```

### Decorator with Abstract Factory

Decorate only one named factory registration:

```csharp
services.AddFactory<IMyService, MyService, MyOptions>(
    opts => { opts.ServiceName = "special"; },
    "special",
    ServiceLifetime.Scoped);

services.AddDecoration<IMyService, MyServiceDecorator>("special", ServiceLifetime.Scoped);

// Usage
var factory = provider.GetRequiredService<IFactory<IMyService>>();
var decorated = factory.Create("special");                  // goes through decorator
var raw       = factory.CreateWithoutDecoration("special"); // bypasses decorator
```

---

## Scan Dependency Injection

Automatically register all implementations of an interface found in one or more assemblies.

### Manual scan with an explicit interface

```csharp
public interface IAnything { }
internal class ScanModels : IAnything { }

serviceCollection.Scan<IAnything>(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

### IScannable marker interface

Implement `IScannable<TService>` on the class so it is picked up by the untyped `Scan` overload:

```csharp
internal class ScanModels : IAnything, IScannable<IAnything> { }

serviceCollection.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

### Override lifetime per class

Override the scan lifetime for a specific class with `ISingletonScannable`, `IScopedScannable`, or `ITransientScannable`:

```csharp
// All others are Scoped — ScanModels is registered as Singleton
internal class ScanModels : IAnything, IScannable<IAnything>, ISingletonScannable { }

serviceCollection.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

### Assembly source helpers

```csharp
serviceCollection.ScanDependencyContext(ServiceLifetime.Scoped);
serviceCollection.ScanCallingAssembly(ServiceLifetime.Scoped);
serviceCollection.ScanCurrentDomain(ServiceLifetime.Scoped);
serviceCollection.ScanEntryAssembly(ServiceLifetime.Scoped);
serviceCollection.ScanExecutingAssembly(ServiceLifetime.Scoped);
serviceCollection.ScanFromType<T>(ServiceLifetime.Scoped);
serviceCollection.ScanFromTypes<T1, T2>(ServiceLifetime.Scoped);
```

With `ScanWithReferences` you can scan a set of assemblies **plus all their transitive references**:

```csharp
serviceCollection.ScanWithReferences(ServiceLifetime.Scoped, myAssembly);
```

---

## Population Service

Generate randomly populated instances of any type — useful for testing and seeding data.

```csharp
IServiceCollection services = new ServiceCollection();
services.AddPopulationService();

var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
var population = serviceProvider.GetRequiredService<IPopulation<PopulationModelTest>>();

var results = population
    .Setup()
    .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
    .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}")
    .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
    .WithPattern(x => x.I!.A!, "[a-z]{4,5}")
    .WithPattern(x => x.II!.A!, "[a-z]{4,5}")
    .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!)
    .Populate();
```

You can also set up custom random value providers for specific properties across multiple populations using `AddPopulationSettings`:

```csharp
services
    .AddPopulationSettings<MyEntity>()
    .WithRandomValue(x => x.Groups, async serviceProvider =>
    {
        return new List<Group>
        {
            new Group { Id = "2", Name = "admin" },
            new Group { Id = "3", Name = "user" }
        };
    });