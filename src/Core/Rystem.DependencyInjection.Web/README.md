### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.DependencyInjection.Web

This package extends the DI system with **runtime service registration** for ASP.NET Core applications, allowing you to add, replace, and rebuild the service container while the application is running.

## 📦 Installation

```bash
dotnet add package Rystem.DependencyInjection.Web
```

## Table of Contents

- [Rystem.DependencyInjection.Web](#rystemdependencyinjectionweb)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [Setup](#setup)
- [Get Service Collection and Provider](#get-service-collection-and-provider)
- [Add Service at Runtime](#add-service-at-runtime)
- [Add Service at Runtime with Lock](#add-service-at-runtime-with-lock)
- [RebuildAsync](#rebuildasync)
- [Factory Fallback with Auto-Rebuild](#factory-fallback-with-auto-rebuild)
  - [FallbackBuilderForServiceCollection](#fallbackbuilderforservicecollection)

---

## Setup

Register the runtime service provider during startup, then activate it after `Build()`:

```csharp
// Program.cs
builder.Services.AddRuntimeServiceProvider();

var app = builder.Build();
app.UseRuntimeServiceProvider();
```

`UseRuntimeServiceProvider` intercepts every HTTP request and routes it through the most recently rebuilt `IServiceProvider`. It also patches the internal host's `Services` property so that scoped services created outside of HTTP context always use the latest provider.

You can optionally disable disposal of the old provider:

```csharp
app.UseRuntimeServiceProvider(disposeOldServiceProvider: false);
```

---

## Get Service Collection and Provider

At any point after setup, retrieve the live `IServiceCollection` or `IServiceProvider`:

```csharp
// Get the mutable service collection (unlocks the read-only flag internally)
IServiceCollection services = RuntimeServiceProvider.GetServiceCollection();

// Get the current service provider (built after the last RebuildAsync call)
IServiceProvider provider = RuntimeServiceProvider.GetServiceProvider();
```

---

## Add Service at Runtime

Obtain the service collection, add your service, then rebuild:

```csharp
await RuntimeServiceProvider
    .GetServiceCollection()
    .AddSingleton<MyNewService>()
    .RebuildAsync();
```

`RebuildAsync` is an extension method on `IServiceCollection` that rebuilds the container, preserving existing singleton instance values by default.

---

## Add Service at Runtime with Lock

When multiple threads may modify the service collection concurrently, use the thread-safe helper:

```csharp
await RuntimeServiceProvider
    .AddServicesToServiceCollectionWithLock(services =>
    {
        services.AddSingleton(myServiceInstance);
    })
    .RebuildAsync();
```

`AddServicesToServiceCollectionWithLock` acquires an internal lock before calling the configuration delegate, preventing race conditions during concurrent registration.

---

## RebuildAsync

`RebuildAsync` rebuilds the DI container and warms up all registered services. By default it preserves instance values for existing singleton descriptors (migrating them into the new container):

```csharp
// Extension on IServiceCollection
await services.RebuildAsync();

// Or via the static helper (uses the internally tracked collection)
await RuntimeServiceProvider.RebuildAsync();

// Disable singleton preservation (all singletons are re-instantiated)
await RuntimeServiceProvider.RebuildAsync(preserveValueForSingletonServices: false);
```

The method is concurrency-safe: if multiple rebuild calls race, only the one that added the most services wins.

---

## Factory Fallback with Auto-Rebuild

When combined with `IFactory<T>`, you can register a fallback that **automatically registers and rebuilds** the container the first time an unknown factory key is requested.

```csharp
// Setup: pre-register one known key
services.AddFactory<Factorized>("known-key");

// Register a fallback that fires for any unknown key
services.AddActionAsFallbackWithServiceCollectionRebuilding<Factorized>(async context =>
{
    // context.Name       — the key that was requested (AnyOf<string, Enum>)
    // context.ServiceProvider — scoped provider from the current request
    // context.ServiceColletionBuilder — set this to register the new service

    await Task.Delay(1); // simulate async work (e.g. fetching config)

    var singleton = context.ServiceProvider.GetService<SingletonService>();
    if (singleton != null)
    {
        // Dynamically add the requested key to the factory
        context.ServiceColletionBuilder = sc => sc.AddFactory<Factorized>(context.Name);
    }
});
```

The fallback is implemented via `IFactoryFallback<T>`. When `IFactory<T>.Create(name)` is called with an unknown key, the fallback:
1. Runs your async delegate with a `FallbackBuilderForServiceCollection` context.
2. Calls `AddServicesToServiceCollectionWithLock` with the `ServiceColletionBuilder` you set.
3. Calls `RebuildAsync()` to apply the new registration.
4. Returns the newly created service.

### FallbackBuilderForServiceCollection

| Property | Type | Description |
|---|---|---|
| `Name` | `AnyOf<string, Enum>?` | The factory key that triggered the fallback |
| `ServiceProvider` | `IServiceProvider` | Scoped provider for the current invocation |
| `ServiceColletionBuilder` | `Action<IServiceCollection>` | Set this to register new services; defaults to no-op |

You can also register a fallback for a non-generic (runtime) type:

```csharp
RuntimeServiceProvider.AddServicesToServiceCollectionWithLock(sc =>
{
    sc.AddActionAsFallbackWithServiceCollectionRebuilding(serviceType, async context =>
    {
        context.ServiceColletionBuilder = inner => inner.AddFactory(serviceType, context.Name);
    });
});
await RuntimeServiceProvider.RebuildAsync();
```