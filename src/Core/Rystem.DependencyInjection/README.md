### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.DependencyInjection

`Rystem.DependencyInjection` extends `Microsoft.Extensions.DependencyInjection` with a set of practical modules built around the default .NET container.

The package is not a replacement container. It stays on top of `IServiceCollection`, `IServiceProvider`, keyed services, and standard lifetimes, then adds higher-level building blocks for:

- warm-up logic after the service provider is built
- temporary provider execution during registration time
- service registration helpers for runtime lifetimes
- keyed service helpers with the same ergonomics
- named abstract factories with per-name options
- decorators that also work with named factories
- assembly scanning with marker interfaces and lifetime overrides
- random object population for tests and seed data

Most examples below are based on the current source code and repository tests. Short snippets use small sample types such as `MyService`, `MyOptions`, or `Order` when the exact test models would add too much noise.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.DependencyInjection
```

The current `10.x` package targets `net10.0` and builds on top of:

- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.DependencyModel`
- `Rystem`

If you only need the underlying utility layer, start with [`Rystem`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem/README.md). If you need runtime ASP.NET Core container rebuilding on top of these DI helpers, continue with [`Rystem.DependencyInjection.Web`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection.Web/README.md).

## Package Architecture

The package is organized in independent modules, each mapped to a folder in the source tree.

| Module | Purpose |
|---|---|
| `Warmup` | Register actions to run after provider build, each executed inside its own scope |
| `ExecuteServicesBeforeStart` | Build a temporary provider and execute code immediately during setup |
| `ServiceHelper` | Add, query, remove, override, and try-add services with runtime `ServiceLifetime` |
| `KeyedServiceHelper` | Same idea for keyed services |
| `AbstractFactory` | Named registrations, per-name options, fallback, and name discovery |
| `Decorator` | Service decoration for both plain registrations and named factories |
| `Scanner` | Assembly scanning with explicit interfaces or marker interfaces |
| `System.Population.Random` | Random object generation and population customization |
| `ProxyService` | Low-level runtime proxy registration API |
| `System.Threading.Tasks` | Small helper to align `NoContext()` behavior with the starting thread |

Conceptually, the most important relationship is this:

- service helpers and keyed helpers are the low-level registration layer
- abstract factory is built on top of keyed registrations
- decorators can be applied to both plain services and factory registrations
- scanning and population are separate productivity modules on top of the same DI container

## Table of Contents

- [Package Architecture](#package-architecture)
- [Warm-up and Pre-build Execution](#warm-up-and-pre-build-execution)
  - [Warm Up](#warm-up)
  - [Execute Until Now](#execute-until-now)
- [Service Helpers](#service-helpers)
  - [AddService](#addservice)
  - [HasService](#hasservice)
  - [RemoveService](#removeservice)
  - [AddOrOverrideService](#addoroverrideservice)
  - [TryAddService](#tryaddservice)
  - [TryAddSingletonAndGetService and GetSingletonService](#tryaddsingletonandgetservice-and-getsingletonservice)
- [Keyed Service Helpers](#keyed-service-helpers)
  - [AddKeyedService](#addkeyedservice)
  - [HasKeyedService](#haskeyedservice)
  - [RemoveKeyedService](#removekeyedservice)
  - [AddOrOverrideKeyedService](#addoroverridekeyedservice)
  - [TryAddKeyedService](#tryaddkeyedservice)
  - [TryAddKeyedSingletonAndGetService and GetSingletonKeyedService](#tryaddkeyedsingletonandgetservice-and-getsingletonkeyedservice)
- [Abstract Factory](#abstract-factory)
  - [Why use it](#why-use-it)
  - [Core contracts](#core-contracts)
  - [Registration](#registration)
  - [Options-aware registrations](#options-aware-registrations)
  - [Synchronous options builders](#synchronous-options-builders)
  - [Asynchronous options builders](#asynchronous-options-builders)
  - [Usage](#usage)
  - [HasFactory](#hasfactory)
  - [IFactoryNames](#ifactorynames)
  - [Factory fallback](#factory-fallback)
  - [Registration families](#registration-families)
- [Decorator](#decorator)
  - [Plain service decoration](#plain-service-decoration)
  - [Decorator with abstract factory](#decorator-with-abstract-factory)
- [Assembly Scanning](#assembly-scanning)
  - [Manual scan with an explicit interface](#manual-scan-with-an-explicit-interface)
  - [Marker interfaces](#marker-interfaces)
  - [Override lifetime per class](#override-lifetime-per-class)
  - [Assembly source helpers](#assembly-source-helpers)
  - [ScanResult](#scanresult)
- [Population Service](#population-service)
  - [Basic usage](#basic-usage)
  - [Reusable settings at registration time](#reusable-settings-at-registration-time)
  - [Common builder methods](#common-builder-methods)
  - [Extending the population engine](#extending-the-population-engine)
- [Other Utilities in this Package](#other-utilities-in-this-package)
  - [Keep NoContext on the original thread](#keep-nocontext-on-the-original-thread)
  - [Dynamic proxy registration](#dynamic-proxy-registration)
- [Repository Examples](#repository-examples)

---

## Warm-up and Pre-build Execution

## Warm Up

Use warm-up actions when some initialization should happen only after the provider is built.

There are two overloads:

- `AddWarmUp(Action<IServiceProvider>)`
- `AddWarmUp(Func<IServiceProvider, Task>)`

```csharp
builder.Services.AddWarmUp(serviceProvider =>
{
    var foo = serviceProvider.GetRequiredService<Foo>();
    var foo2 = serviceProvider.GetRequiredService<Foo2>();

    Console.WriteLine(foo.Hello() + foo2.Hello());
});

builder.Services.AddWarmUp(async serviceProvider =>
{
    await serviceProvider.GetRequiredService<MyService>().InitAsync();
});
```

After the app or provider is built, execute all registered warm-up actions:

```csharp
var app = builder.Build();
await app.Services.WarmUpAsync();
```

`WarmUpAsync()` returns the same provider instance, so it can be chained.

Implementation detail that matters in practice: each warm-up action runs inside a fresh scope.

## Execute Until Now

`ExecuteUntilNowAsync(...)` is useful when you want to build a temporary provider, resolve a service, execute some logic immediately, and dispose the temporary scope.

```csharp
string result = await services.ExecuteUntilNowAsync((Foo foo) =>
{
    return Task.FromResult(foo.Hello());
});
```

You can also resolve from `IServiceProvider` directly.

```csharp
string result = await services.ExecuteUntilNowAsync(serviceProvider =>
{
    var foo = serviceProvider.GetRequiredService<Foo>();
    var foo2 = serviceProvider.GetRequiredService<Foo2>();
    return Task.FromResult(foo.Hello() + foo2.Hello());
});
```

If you want warm-up to run before the callback, use the dedicated variant:

```csharp
string result = await services.ExecuteUntilNowWithWarmUpAsync((Foo foo) =>
{
    return Task.FromResult(foo.Hello());
});
```

This is especially handy in:

- test setup code
- one-off migrations or bootstrap steps
- validating DI registrations before host startup

---

## Service Helpers

The service helper extensions wrap the standard `AddTransient`, `AddScoped`, and `AddSingleton` methods behind a runtime `ServiceLifetime` parameter.

## AddService

```csharp
services.AddService<MyService>(ServiceLifetime.Singleton);
services.AddService<IMyService, MyService>(ServiceLifetime.Scoped);
services.AddService(typeof(IMyService), typeof(MyService), ServiceLifetime.Transient);
```

There are also factory-based overloads.

```csharp
services.AddService<IMyService>(
    serviceProvider => new MyService(serviceProvider.GetRequiredService<IDependency>()),
    ServiceLifetime.Transient);
```

Use these helpers when lifetime is only known at runtime and you do not want to branch manually on `ServiceLifetime`.

## HasService

Check whether a non-keyed registration already exists.

```csharp
bool exists = services.HasService<IMyService>(out ServiceDescriptor? descriptor);
bool exact = services.HasService<IMyService, MyService>(out ServiceDescriptor? exactDescriptor);
```

This is useful for defensive registration code or package bootstrappers.

## RemoveService

Remove every non-keyed registration for a given service type.

```csharp
services.RemoveService<IMyService>();
services.RemoveService(typeof(IMyService));
```

## AddOrOverrideService

Replace an existing non-keyed service registration with a new one.

```csharp
services.AddOrOverrideService<IMyService, MyServiceV2>(ServiceLifetime.Scoped);
services.AddOrOverrideSingleton<IMyService>(new MyServiceV2());
services.AddOrOverrideSingleton<IMyService, MyServiceV2>(new MyServiceV2());
```

Use this when your package or module must become the effective registration for a given service type.

## TryAddService

Register only if the service type is still absent.

```csharp
services.TryAddService<IMyService, MyService>(ServiceLifetime.Scoped);
services.TryAddService<MyService>(ServiceLifetime.Singleton);
```

There are additional overloads for:

- implementation instances
- runtime `Type`
- factory-based registrations

## TryAddSingletonAndGetService and GetSingletonService

These helpers are useful while you are still configuring `IServiceCollection` and want to keep one shared singleton instance around.

```csharp
MySettings settings = services.TryAddSingletonAndGetService(new MySettings
{
    Environment = "test"
});
```

```csharp
MySettings settings = services.TryAddSingletonAndGetService<MySettings>();
IMySettings typed = services.TryAddSingletonAndGetService<IMySettings, MySettings>(new MySettings());
```

```csharp
MySettings? existing = services.GetSingletonService<MySettings>();
```

This pattern is handy for package-level maps, registries, or shared configuration objects that are built directly during registration. `GetSingletonService(...)` is most useful for singleton instances that were added directly to `IServiceCollection`, especially through these same helpers.

---

## Keyed Service Helpers

The keyed helpers provide the same ergonomics for .NET keyed DI registrations.

They are especially useful when you want one service contract with multiple named or keyed implementations, but you do not need the higher-level abstract factory API.

## AddKeyedService

```csharp
services.AddKeyedService<MyService>("default", ServiceLifetime.Singleton);
services.AddKeyedService<IMyService, MyService>("west", ServiceLifetime.Scoped);
services.AddKeyedService(typeof(IMyService), "east", typeof(MyService), ServiceLifetime.Transient);
```

Factory-based keyed registration is also supported.

```csharp
services.AddKeyedService<IMyService>(
    "west",
    (serviceProvider, key) => new MyService(key?.ToString() ?? "unknown"),
    ServiceLifetime.Transient);
```

## HasKeyedService

```csharp
bool exists = services.HasKeyedService<IMyService>("west", out ServiceDescriptor? descriptor);
bool exact = services.HasKeyedService<IMyService, MyService>("west", out _);
bool raw = services.HasKeyedService(typeof(IMyService), "west", out _);
```

## RemoveKeyedService

```csharp
services.RemoveKeyedService<IMyService>("west");
services.RemoveKeyedService(typeof(IMyService), "west");
```

## AddOrOverrideKeyedService

Replace the registration associated with a specific key.

```csharp
services.AddOrOverrideKeyedService<MyWorker>("main", ServiceLifetime.Singleton);
services.AddOrOverrideKeyedService(typeof(MyWorker), "backup", ServiceLifetime.Scoped);
services.AddOrOverrideKeyedSingleton<IMySettings>("tenant-a", new MySettings());
```

## TryAddKeyedService

```csharp
services.TryAddKeyedService<IMyService, MyService>("west", ServiceLifetime.Scoped);
services.TryAddKeyedService<MyService>("default", ServiceLifetime.Singleton);
```

As with the non-keyed helper family, there are also overloads for runtime types and keyed implementation factories.

## TryAddKeyedSingletonAndGetService and GetSingletonKeyedService

```csharp
MySettings settings = services.TryAddKeyedSingletonAndGetService(new MySettings(), "tenant-a");
MySettings autoCreated = services.TryAddKeyedSingletonAndGetService<MySettings>("tenant-b");
```

```csharp
MySettings? existing = services.GetSingletonKeyedService<MySettings>("tenant-a");
```

This is the keyed equivalent of the singleton-get helper from the non-keyed service module and is mainly intended for keyed singleton instances stored directly in the collection.

---

## Abstract Factory

## Why use it

Use the abstract factory module when you need multiple named registrations of the same service contract, each with its own lifetime and optional per-name options.

This is the most important high-level feature in the package.

Factory names can be either:

- `string`
- `Enum`

Internally, names are normalized through `AnyOf<string?, Enum>`. When you use an enum, its display name is used if a `[Display(Name = ...)]` attribute exists.

## Core contracts

The public contracts are:

```csharp
public interface IFactory<out TService>
    where TService : class
{
    TService? Create(AnyOf<string?, Enum>? name = null);
    TService? CreateWithoutDecoration(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAll(AnyOf<string?, Enum>? name = null);
    IEnumerable<TService> CreateAllWithoutDecoration(AnyOf<string?, Enum>? name = null);
    bool Exists(AnyOf<string?, Enum>? name = null);
}
```

```csharp
public interface IServiceForFactory
{
    void SetFactoryName(string name);
    bool FactoryNameAlreadySetup { get; set; }
}

public interface IServiceWithFactoryWithOptions : IServiceForFactory
{
    bool OptionsAlreadySetup { get; set; }
}

public interface IServiceWithFactoryWithOptions<in TOptions> : IServiceWithFactoryWithOptions
    where TOptions : class, IFactoryOptions
{
    void SetOptions(TOptions options);
}
```

If a factory-created service implements `IServiceForFactory`, the factory name is injected automatically during creation.

If it also implements `IServiceWithFactoryWithOptions<TOptions>`, the per-name options are injected automatically too.

## Registration

Here is the simplest options-aware registration flow, adapted from the tests.

```csharp
public interface ITestService
{
    string Id { get; }
    string FactoryName { get; }
}

public sealed class TestOptions : IFactoryOptions
{
    public string ClassicName { get; set; } = string.Empty;
}

public sealed class TestService : ITestService, IServiceWithFactoryWithOptions<TestOptions>
{
    public string Id => Options.ClassicName;
    public string FactoryName { get; private set; } = string.Empty;
    public TestOptions Options { get; private set; } = null!;

    public bool FactoryNameAlreadySetup { get; set; }
    public bool OptionsAlreadySetup { get; set; }

    public void SetFactoryName(string name) => FactoryName = name;
    public void SetOptions(TestOptions options) => Options = options;
}

services.AddFactory<ITestService, TestService, TestOptions>(
    options =>
    {
        options.ClassicName = "singleton";
    },
    name: "singleton",
    lifetime: ServiceLifetime.Singleton);

services.AddFactory<ITestService, TestService, TestOptions>(
    options =>
    {
        options.ClassicName = "scoped";
    },
    name: "scoped",
    lifetime: ServiceLifetime.Scoped);

services.AddFactory<ITestService, TestService, TestOptions>(
    options =>
    {
        options.ClassicName = "transient";
    },
    name: "transient",
    lifetime: ServiceLifetime.Transient);
```

The factory supports both string and enum names.

```csharp
public enum TestKind
{
    Singleton,
    Scoped,
    Transient
}

services.AddFactory<ITestService, TestService, TestOptions>(
    options => options.ClassicName = "enum-singleton",
    TestKind.Singleton,
    ServiceLifetime.Singleton);
```

## Options-aware registrations

You only need options when the same service type must behave differently per factory name.

If you do not need custom options, the basic overload is enough.

```csharp
services.AddFactory<ITestService, TestService>("default", ServiceLifetime.Scoped);
```

There are also overload families for:

- self-registered services
- implementation instances
- implementation factories
- runtime `Type` arguments

## Synchronous options builders

If the configuration object must be built through a builder step, implement `IOptionsBuilder<TBuiltOptions>`.

```csharp
public sealed class BuiltOptions : IFactoryOptions
{
    public string ServiceName { get; set; } = string.Empty;
}

public sealed class BuiltOptionsBuilder : IOptionsBuilder<BuiltOptions>
{
    public string Prefix { get; set; } = string.Empty;

    public Func<IServiceProvider, BuiltOptions> Build()
        => _ => new BuiltOptions
        {
            ServiceName = $"{Prefix}-built"
        };
}

public sealed class BuiltService : ITestService, IServiceWithFactoryWithOptions<BuiltOptions>
{
    public string Id => Options.ServiceName;
    public string FactoryName { get; private set; } = string.Empty;
    public BuiltOptions Options { get; private set; } = null!;

    public bool FactoryNameAlreadySetup { get; set; }
    public bool OptionsAlreadySetup { get; set; }

    public void SetFactoryName(string name) => FactoryName = name;
    public void SetOptions(BuiltOptions options) => Options = options;
}

services.AddFactory<ITestService, BuiltService, BuiltOptionsBuilder, BuiltOptions>(
    options => options.Prefix = "sync",
    name: "sync-builder",
    lifetime: ServiceLifetime.Transient);
```

## Asynchronous options builders

If options must be prepared asynchronously, implement `IOptionsBuilderAsync<TBuiltOptions>` and use `AddFactoryAsync(...)`.

```csharp
public sealed class AsyncBuiltOptions : IFactoryOptions
{
    public string ServiceName { get; set; } = string.Empty;
}

public sealed class AsyncBuiltOptionsBuilder : IOptionsBuilderAsync<AsyncBuiltOptions>
{
    public string Prefix { get; set; } = string.Empty;

    public Task<Func<IServiceProvider, AsyncBuiltOptions>> BuildAsync()
    {
        return Task.FromResult<Func<IServiceProvider, AsyncBuiltOptions>>(_ =>
            new AsyncBuiltOptions
            {
                ServiceName = $"{Prefix}-async"
            });
    }
}

public sealed class AsyncBuiltService : ITestService, IServiceWithFactoryWithOptions<AsyncBuiltOptions>
{
    public string Id => Options.ServiceName;
    public string FactoryName { get; private set; } = string.Empty;
    public AsyncBuiltOptions Options { get; private set; } = null!;

    public bool FactoryNameAlreadySetup { get; set; }
    public bool OptionsAlreadySetup { get; set; }

    public void SetFactoryName(string name) => FactoryName = name;
    public void SetOptions(AsyncBuiltOptions options) => Options = options;
}

await services.AddFactoryAsync<ITestService, AsyncBuiltService, AsyncBuiltOptionsBuilder, AsyncBuiltOptions>(
    options => options.Prefix = "async",
    name: "async-builder",
    lifetime: ServiceLifetime.Scoped);
```

## Usage

Resolve the factory from DI and create named services on demand.

```csharp
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var factory = scope.ServiceProvider.GetRequiredService<IFactory<ITestService>>();

var singleton = factory.Create("singleton");
var transient = factory.Create("transient");
var scoped = factory.Create("scoped");

bool exists = factory.Exists("singleton");
bool missing = factory.Exists("missing");

IEnumerable<ITestService> all = factory.CreateAll("singleton");
```

Lifetime semantics are still the standard DI ones.

```csharp
var factory = scope.ServiceProvider.GetRequiredService<IFactory<ITestService>>();

string singletonId1 = factory.Create("singleton")!.Id;
string singletonId2 = factory.Create("singleton")!.Id;

string transientId1 = factory.Create("transient")!.Id;
string transientId2 = factory.Create("transient")!.Id;

Console.WriteLine(singletonId1 == singletonId2); // true
Console.WriteLine(transientId1 == transientId2); // false
```

`CreateWithoutDecoration(...)` and `CreateAllWithoutDecoration(...)` are useful when decorators are involved and you explicitly want the undecorated pipeline.

## HasFactory

You can check a registration before the provider is built.

```csharp
bool byString = services.HasFactory<ITestService>("singleton");
bool byEnum = services.HasFactory<ITestService>(TestKind.Singleton);
bool raw = services.HasFactory(typeof(ITestService), "singleton");
```

## IFactoryNames

`IFactoryNames<TService>` exposes the list of registered names for a service contract.

```csharp
var names = provider.GetRequiredService<IFactoryNames<ITestService>>().List();
```

This is useful for discovery scenarios such as settings pages, admin UIs, or dynamic feature selection.

## Factory fallback

Fallbacks are invoked when `Create(name)` is called for a name that has no registration.

```csharp
public sealed class DefaultService : ITestService
{
    public DefaultService(string name)
    {
        Id = name;
        FactoryName = name;
    }

    public string Id { get; }
    public string FactoryName { get; }
}

public sealed class MyFallback : IFactoryFallback<ITestService>
{
    public ITestService Create(AnyOf<string?, Enum>? name = null)
        => new DefaultService(name?.AsString() ?? "default");
}

services.AddFactoryFallback<ITestService, MyFallback>();
```

Or use a delegate that also receives the service provider:

```csharp
services.AddActionAsFallbackWithServiceProvider<ITestService>(builder =>
{
    var resolvedName = builder.Name?.AsString() ?? "default";
    return new DefaultService(resolvedName);
});
```

## Registration families

In addition to `AddFactory(...)`, the package also exposes these families:

- `TryAddFactory(...)`
- `AddOrOverrideFactory(...)`
- `AddNewFactory(...)`
- `TryAddFactoryAsync(...)`
- `AddOrOverrideFactoryAsync(...)`
- `AddNewFactoryAsync(...)`

They exist for the same combinations of:

- self-registered services
- service + implementation types
- instance registrations
- implementation factories
- plain registrations
- options-aware registrations
- sync and async options builders

Use:

- `TryAddFactory` when you want a `bool` success signal and no replacement
- `AddOrOverrideFactory` when the latest registration should win
- `AddNewFactory` when you want an explicitly new registration path instead of silently reusing an existing one

---

## Decorator

The decorator module replaces a service registration with a decorator and keeps the original service accessible through `IDecoratedService<TService>`.

The correct interface is:

```csharp
public interface IDecoratorService<in TService> : IServiceForFactory
    where TService : class
{
    void SetDecoratedServices(IEnumerable<TService> services);
}
```

The decorator receives all decorated services as an enumerable so it can support multiple layers.

## Plain service decoration

```csharp
public interface ITestWithoutFactoryService
{
    string Id { get; }
}

public sealed class TestWithoutFactoryService : ITestWithoutFactoryService
{
    public string Id { get; } = Guid.NewGuid().ToString();
}

public sealed class TestWithoutFactoryServiceDecorator :
    ITestWithoutFactoryService,
    IDecoratorService<ITestWithoutFactoryService>
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public ITestWithoutFactoryService Inner { get; private set; } = null!;

    public bool FactoryNameAlreadySetup { get; set; }
    public void SetFactoryName(string name) { }

    public void SetDecoratedServices(IEnumerable<ITestWithoutFactoryService> services)
        => Inner = services.First();
}

services.AddService<ITestWithoutFactoryService, TestWithoutFactoryService>(ServiceLifetime.Scoped);
services.AddDecoration<ITestWithoutFactoryService, TestWithoutFactoryServiceDecorator>(
    name: null,
    lifetime: ServiceLifetime.Scoped);
```

Usage:

```csharp
var decorated = provider.GetRequiredService<ITestWithoutFactoryService>();
var raw = provider.GetRequiredService<IDecoratedService<ITestWithoutFactoryService>>().Service;
```

`decorated` is the decorator. `raw` is the original pre-decoration service.

## Decorator with abstract factory

Decorators also work with named factory registrations.

```csharp
services.AddFactory<ITestService, TestService, TestOptions>(
    options => options.ClassicName = "special",
    name: "special",
    lifetime: ServiceLifetime.Scoped);

services.AddDecoration<ITestService, DecoratorTestService>(
    name: "special",
    lifetime: ServiceLifetime.Scoped);
```

```csharp
var factory = provider.GetRequiredService<IFactory<ITestService>>();

var decorated = factory.Create("special");
var raw = factory.CreateWithoutDecoration("special");
```

This is one of the strongest combinations in the package:

- `IFactory<TService>` chooses the named implementation
- `IDecoratorService<TService>` layers cross-cutting behavior on top
- `CreateWithoutDecoration(...)` lets you bypass the decorator when needed

---

## Assembly Scanning

The scanning module can register implementations explicitly by interface or implicitly through marker interfaces.

It also tracks previously scanned type pairs to avoid duplicate registrations for the same service and implementation.

## Manual scan with an explicit interface

```csharp
public interface IAnything { }
internal sealed class ScanModels : IAnything { }

ScanResult result = services.Scan<IAnything>(
    ServiceLifetime.Scoped,
    typeof(IAnything).Assembly);
```

Equivalent non-generic overloads exist too.

## Marker interfaces

If the implementation class declares `IScannable<TService>`, you can use the untyped scan overload.

```csharp
public interface IAnything { }

internal sealed class ScanModels : IAnything, IScannable<IAnything>
{
}

services.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

That is useful when the service-to-implementation relationship should stay close to the implementation itself.

## Override lifetime per class

You can override the scan lifetime with one of these marker interfaces:

- `ISingletonScannable`
- `IScopedScannable`
- `ITransientScannable`

```csharp
internal sealed class ScanModels :
    IAnything,
    IScannable<IAnything>,
    ISingletonScannable
{
}

services.Scan(ServiceLifetime.Scoped, typeof(IAnything).Assembly);
```

In this example the default scan lifetime is `Scoped`, but `ScanModels` is registered as `Singleton`.

## Assembly source helpers

The package includes several helpers to decide where assemblies come from.

```csharp
services.ScanDependencyContext(ServiceLifetime.Scoped);
services.ScanCallingAssembly(ServiceLifetime.Scoped);
services.ScanCurrentDomain(ServiceLifetime.Scoped);
services.ScanEntryAssembly(ServiceLifetime.Scoped);
services.ScanExecutingAssembly(ServiceLifetime.Scoped);
services.ScanFromType<MyMarker>(ServiceLifetime.Scoped);
services.ScanFromTypes<MyMarker1, MyMarker2>(ServiceLifetime.Scoped);
```

`ScanDependencyContext(...)` also supports an optional assembly predicate.

```csharp
services.ScanDependencyContext(
    ServiceLifetime.Scoped,
    assembly => assembly.GetName().Name!.StartsWith("MyCompany."));
```

If you want transitive references too, use `ScanWithReferences(...)`.

```csharp
services.ScanWithReferences(ServiceLifetime.Scoped, typeof(MyMarker).Assembly);
```

## ScanResult

Every scan method returns a `ScanResult`.

```csharp
ScanResult result = services.Scan(ServiceLifetime.Scoped, typeof(MyMarker).Assembly);

Console.WriteLine(result.Count);
foreach (var type in result.Implementations)
{
    Console.WriteLine(type.FullName);
}
```

This is convenient for diagnostics or registration reports.

---

## Population Service

The population module generates random object graphs and lets you override the generated values with patterns, delegates, implementations, and shared random queues.

It is especially useful for:

- integration tests
- seed/demo data
- random-but-valid object generation
- stress-testing APIs with large model graphs

## Basic usage

Register the service once:

```csharp
IServiceCollection services = new ServiceCollection();
services.AddPopulationService();
```

Resolve `IPopulation<T>` and either populate immediately or create a reusable builder with `Setup()`.

```csharp
using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var population = scope.ServiceProvider.GetRequiredService<IPopulation<PopulationModelTest>>();

List<PopulationModelTest> items = population.Populate();
IPopulationBuilder<PopulationModelTest> setup = population.Setup();
```

From the tests, a more realistic setup looks like this:

```csharp
var population = scope.ServiceProvider.GetRequiredService<IPopulation<PopulationModelTest>>();

var setup = population
    .Setup()
    .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
    .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}")
    .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
    .WithPattern(x => x.I!.A!, "[a-z]{4,5}")
    .WithPattern(x => x.II!.A!, "[a-z]{4,5}")
    .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);

List<PopulationModelTest> items = setup.Populate();
```

`Populate(...)` accepts:

- `numberOfElements` - how many root objects to create
- `numberOfElementsWhenEnumerableIsFound` - how many elements to generate inside collections by default

```csharp
List<PopulationModelTest> items = setup.Populate(50, 4);
```

## Reusable settings at registration time

If a configuration should apply every time `IPopulation<T>` is resolved, register it during DI setup with `AddPopulationSettings<T>()`.

```csharp
services
    .AddPopulationSettings<PopulationModelTest>()
    .WithAutoIncrement(x => x.A, 1);
```

That configuration becomes the default for `IPopulation<PopulationModelTest>`.

The tests also show wrapper models using settings during registration time.

```csharp
services
    .AddPopulationSettings<Entity<PopulationModelTest, int>>()
    .WithAutoIncrement(x => x.Value.A, 1)
    .WithAutoIncrement(x => x.Key, 1)
    .WithPattern(x => x.Value.J!.First().A, "[a-z]{4,5}")
    .WithImplementation(x => x.Value.I, typeof(MyInnerInterfaceImplementation));
```

## Common builder methods

`IPopulationBuilder<T>` exposes a rich customization surface.

### WithPattern

Generate values matching one or more regex patterns.

```csharp
population.Setup()
    .WithPattern(x => x.Name, "[a-z]{4,10}")
    .WithPattern(x => x.Code, "[A-Z]{3}[0-9]{2}");
```

### WithAutoIncrement

Useful for deterministic ids across repeated generations.

```csharp
population.Setup()
    .WithAutoIncrement(x => x.Id, 1);
```

### WithValue

Set values explicitly using a local function or an async service-based resolver.

```csharp
population.Setup()
    .WithValue(x => x.Environment, () => "test");
```

```csharp
population.Setup()
    .WithValue(x => x.UserName, async serviceProvider =>
    {
        var source = serviceProvider.GetRequiredService<IUserNameSource>();
        return await source.NextAsync();
    });
```

### WithRandomValue

Provide a pool of allowed values and let the population engine choose randomly.

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
```

There are overloads both for scalar properties and enumerable properties.

### WithSpecificNumberOfElements

Override the default number of generated items for a specific collection property.

```csharp
population.Setup()
    .WithSpecificNumberOfElements(x => x.Tags, 3);
```

### WithImplementation

Choose the concrete type used for interface or abstract properties.

```csharp
population.Setup()
    .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
    .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
```

### WithRandomValueFromRystem

Share values through the Rystem random queues.

```csharp
population.Setup()
    .WithRandomValueFromRystem(x => x.CountryCode, useTheSameRandomValuesForTheSameType: true)
    .WithRandomValueFromRystemWithSpecificQueue(x => x.RegionCode, "shared-region-queue");
```

This is useful when related properties across many generated entities should reuse the same random pool instead of being fully independent.

## Extending the population engine

The module is extensible through DI.

```csharp
services.AddPopulationService();
services.AddPopulationService<MyPopulationService>();
services.AddPopulationService<MyEntity, MyPopulationForEntity>();
services.AddPopulationStrategyService<MyEntity, MyPopulationStrategy>();
services.AddRegexService<MyRegexService>();
services.AddRandomPopulationService<MyCustomRandomPopulationService>();
```

This lets you override:

- the global population service
- the strategy for a specific `T`
- the regex generator
- any low-level random population provider by priority

---

## Other Utilities in this Package

## Keep NoContext on the original thread

The package includes a tiny bridge to the core `RystemTask.WaitYourStartingThread` switch.

```csharp
services.AddWaitingTheSameThreadThatStartedTheTaskWhenUseNoContext();
```

This simply sets `RystemTask.WaitYourStartingThread = true` so `NoContext()` returns to the starting thread when that behavior is needed.

This is useful in UI-style environments where resuming on the original thread matters.

## Dynamic proxy registration

There is also a low-level runtime proxy API:

```csharp
(Type proxyInterface, Type proxyImplementation) = services.AddProxy(
    interfaceType: typeof(IMyService),
    implementationType: typeof(MyService),
    interfaceName: "IMyServiceProxy",
    className: "MyServiceProxy",
    lifetime: ServiceLifetime.Scoped);
```

`AddProxy(...)` dynamically emits a new interface and implementation type and registers that proxy interface in DI.

This is an advanced API and is not covered by the repository tests as thoroughly as the modules above, so treat it as a source-level power feature rather than the mainline package entry point.

---

## Repository Examples

The best example sources for this package are:

- Warm-up and ExecuteUntilNow: [src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/DependencyInjectionTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/DependencyInjectionTest.cs)
- Abstract factory: [src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/AbstractFactoryTests.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/AbstractFactoryTests.cs)
- Factory + decorator together: [src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/DecoratorFactoryTests.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/DecoratorFactoryTests.cs)
- Decorator support models: [src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/Decoration/Decoration.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/Decoration/Decoration.cs)
- Scanning: [src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/ScanTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/Microsoft.Extensions.DependencyInjection/ScanTest.cs)
- Population service: [src/Core/Test/Rystem.Test.UnitTest/System.Population.Random/PopulationTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/System.Population.Random/PopulationTest.cs)

This README is intentionally long because `Rystem.DependencyInjection` is not a single feature. It is a set of composable DI modules that can be adopted independently or together.
