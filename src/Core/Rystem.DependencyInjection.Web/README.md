### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.DependencyInjection.Web

`Rystem.DependencyInjection.Web` adds a runtime-rebuild layer on top of `Rystem.DependencyInjection` for ASP.NET Core applications.

The package is built for scenarios where the application must keep serving requests while the underlying `IServiceCollection` changes at runtime. Instead of treating the container as immutable after startup, it keeps track of the current service collection, rebuilds a fresh provider when needed, and routes later requests through that latest provider.

This is a specialized package. It is most useful for:

- dynamic feature activation
- runtime factory expansion
- test/demo environments that register services on demand
- multi-tenant or plug-in style setups where registrations are not fully known at startup

Most examples below come from the current source and from the runtime integration tests in `src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs` plus the sample API used by those tests in `src/Extensions/Tests/Test/Rystem.Test.TestApi`.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.DependencyInjection.Web
```

The current `10.x` package targets `net10.0` and references:

- `Microsoft.AspNetCore.App`
- `Rystem.DependencyInjection`

This package assumes the abstractions from [`Rystem.DependencyInjection`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection/README.md), which in turn builds on the lower-level utility package [`Rystem`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem/README.md).

## Package Architecture

The package is intentionally small and is organized around two modules.

| Module | Purpose |
|---|---|
| `RuntimeServiceProvider` | Tracks the current `IServiceCollection`, current built `IServiceProvider`, request-time swapping, and rebuild operations |
| `Fallback` | Integrates with `IFactory<T>` so missing factory names can register new services and rebuild automatically |

At a high level, the flow is:

- call `AddRuntimeServiceProvider()` during service registration
- call `UseRuntimeServiceProvider()` after the app is built
- mutate the tracked `IServiceCollection` later
- call `RebuildAsync()` to build the next provider
- future resolutions and future HTTP requests use the newest provider

## Table of Contents

- [Package Architecture](#package-architecture)
- [Setup](#setup)
- [How Runtime Rebuilding Works](#how-runtime-rebuilding-works)
- [Read the Current Collection and Provider](#read-the-current-collection-and-provider)
- [Add Services at Runtime](#add-services-at-runtime)
- [Thread-safe Runtime Registration](#thread-safe-runtime-registration)
- [RebuildAsync](#rebuildasync)
  - [Preserve existing singleton instances](#preserve-existing-singleton-instances)
  - [Concurrent rebuild behavior](#concurrent-rebuild-behavior)
- [Factory Fallback with Auto-Rebuild](#factory-fallback-with-auto-rebuild)
  - [Generic fallback registration](#generic-fallback-registration)
  - [Runtime type fallback registration](#runtime-type-fallback-registration)
  - [FallbackBuilderForServiceCollection](#fallbackbuilderforservicecollection)
- [Practical Notes](#practical-notes)
- [Repository Examples](#repository-examples)

---

## Setup

Register runtime rebuilding during startup:

```csharp
builder.Services.AddRuntimeServiceProvider();

var app = builder.Build();
app.UseRuntimeServiceProvider();
```

This does two important things:

- stores the application `IServiceCollection` so it can be mutated later
- inserts middleware that replaces `HttpContext.RequestServices` with a scope created from the latest rebuilt provider

You can also choose not to dispose the previous request service provider when the middleware swaps it out:

```csharp
app.UseRuntimeServiceProvider(disposeOldServiceProvider: false);
```

That flag matters only if you need to keep the previous request-scoped provider alive longer than the default handoff behavior.

## How Runtime Rebuilding Works

The package keeps global static references to:

- the tracked `IServiceCollection`
- the latest built `IServiceProvider`
- the `IApplicationBuilder` used to patch the host-level `Services` property

When `RebuildAsync()` runs, it:

1. reads the tracked service collection
2. optionally migrates singleton instances from the old provider into matching singleton descriptors
3. builds a new provider
4. swaps the global current provider if that rebuild still represents the newest service count
5. runs `WarmUpAsync()` on the rebuilt provider

This design means the package is best suited to application-level dynamic composition, not to isolated per-module container ownership.

## Read the Current Collection and Provider

After setup, you can read either the mutable collection or the latest provider.

```csharp
IServiceCollection services = RuntimeServiceProvider.GetServiceCollection();
IServiceProvider provider = RuntimeServiceProvider.GetServiceProvider();
```

`GetServiceCollection()` also clears the internal read-only flag on the collection before returning it, so later `Add...` calls can keep modifying the same collection instance.

`GetServiceProvider()` throws until the runtime provider has been initialized through `UseRuntimeServiceProvider()`.

---

## Add Services at Runtime

The simplest runtime update flow is:

```csharp
await RuntimeServiceProvider
    .GetServiceCollection()
    .AddSingleton<MyNewService>()
    .RebuildAsync();
```

That pattern comes directly from the test API used by `RuntimeServiceProviderTest`.

For example, the sample controller checks whether `AddedService` exists and, if not, registers it and rebuilds immediately:

```csharp
var value = _serviceProvider.GetService<AddedService>();
if (value == null)
{
    await RuntimeServiceProvider.GetServiceCollection()
         .AddSingleton<AddedService>()
         .RebuildAsync();
}
```

The integration test confirms the effect:

- first request does not see `AddedService`
- second request does see it
- existing singleton instances stay the same by default
- scoped and transient services are recreated as usual

---

## Thread-safe Runtime Registration

If multiple threads may mutate the tracked service collection, use the built-in lock helper:

```csharp
await RuntimeServiceProvider
    .AddServicesToServiceCollectionWithLock(services =>
    {
        services.AddSingleton(myServiceInstance);
    })
    .RebuildAsync();
```

Use this helper when registration can happen concurrently, for example from multiple requests or parallel background tasks.

The runtime tests also exercise this pattern by adding many mocked service types in parallel and rebuilding after each change.

---

## RebuildAsync

You can rebuild either from the tracked collection instance or through the static shortcut:

```csharp
await services.RebuildAsync();
await RuntimeServiceProvider.RebuildAsync();
```

Every rebuild ends by calling `WarmUpAsync()` on the latest provider, so warm-up actions registered through `Rystem.DependencyInjection` still participate in the runtime-rebuild flow.

### Preserve existing singleton instances

By default, rebuild keeps the current singleton instance values when matching singleton descriptors still exist in the new collection.

```csharp
await RuntimeServiceProvider.RebuildAsync(preserveValueForSingletonServices: true);
```

Or disable that behavior:

```csharp
await RuntimeServiceProvider.RebuildAsync(preserveValueForSingletonServices: false);
```

That default preservation is why the runtime test can add `AddedService` while keeping the original ids for already-created singleton services.

### Concurrent rebuild behavior

The implementation tracks the current number of registered services and only promotes the rebuilt provider if that rebuild still represents the largest service collection seen so far.

Practically, this means that when multiple rebuilds race:

- older/smaller rebuilds do not overwrite a newer/larger one
- the provider that wins is the one associated with the latest widest collection snapshot

The tests stress this behavior with repeated sequential and parallel rebuilds.

---

## Factory Fallback with Auto-Rebuild

This package becomes especially powerful when combined with `IFactory<T>` from `Rystem.DependencyInjection`.

You can register a fallback that reacts when `factory.Create(name)` is called for an unknown name. Inside that fallback you dynamically add the missing factory registration, rebuild the service provider, and then return the newly available service.

### Generic fallback registration

```csharp
services.AddFactory<Factorized>("1");

services.AddActionAsFallbackWithServiceCollectionRebuilding<Factorized>(async context =>
{
    await Task.Delay(1);

    var singletonService = context.ServiceProvider.GetService<SingletonService>();
    if (singletonService != null)
    {
        context.ServiceColletionBuilder = serviceCollection =>
            serviceCollection.AddFactory<Factorized>(context.Name);
    }
});
```

That exact pattern is used in `src/Extensions/Tests/Test/Rystem.Test.TestApi/Extensions/ServiceExtensions.cs`.

After this is registered, the first call to an unknown factory name can materialize the missing registration on demand:

```csharp
var factory = serviceProvider.GetRequiredService<IFactory<Factorized>>();
var created = factory.Create("dynamic-name");
```

The runtime tests validate this behavior both for one service and for many services in parallel.

### Runtime type fallback registration

There is also a non-generic overload for runtime service types:

```csharp
RuntimeServiceProvider.AddServicesToServiceCollectionWithLock(sc =>
{
    sc.AddActionAsFallbackWithServiceCollectionRebuilding(serviceType, async context =>
    {
        await Task.Delay(1);
        context.ServiceColletionBuilder = inner => inner.AddFactory(serviceType, context.Name);
    });
});

await RuntimeServiceProvider.RebuildAsync();
```

This is useful when the service contract itself is discovered dynamically.

### FallbackBuilderForServiceCollection

The fallback delegate receives `FallbackBuilderForServiceCollection`:

| Property | Type | Purpose |
|---|---|---|
| `Name` | `AnyOf<string, Enum>?` | The missing factory name that triggered the fallback |
| `ServiceProvider` | `IServiceProvider` | A fresh scope created from the current runtime provider |
| `ServiceColletionBuilder` | `Action<IServiceCollection>` | The action that will mutate the tracked service collection before rebuild |

Note that the public property name is actually `ServiceColletionBuilder` in the source, including the typo. The README keeps that spelling because it is the real API surface.

Internally the flow is:

1. the fallback builds a `FallbackBuilderForServiceCollection`
2. your delegate populates `ServiceColletionBuilder`
3. the package runs that action under `AddServicesToServiceCollectionWithLock(...)`
4. it calls `RebuildAsync()`
5. it resolves the requested service again from the rebuilt `IFactory<T>`

---

## Practical Notes

- This package uses static global state, so it is designed for one active application container per process.
- `UseRuntimeServiceProvider()` relies on reflection to patch internal hosting fields and to unfreeze the service collection.
- Request pipelines only see the updated provider for future requests; already-running requests continue on the scope they already have.
- `RebuildAsync()` returns the current `IServiceProvider`, so you can chain additional startup logic after a rebuild.
- The package is much easier to reason about when paired with the lower-level helpers from `Rystem.DependencyInjection`, especially `AddFactory(...)`, warm-up, and service helper APIs.

---

## Repository Examples

The most useful sources for this package are:

- Runtime integration tests: [src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Test/Rystem.Test.UnitTest/RuntimeServiceProvider/RuntimeServiceProviderTest.cs)
- Test API startup wiring: [src/Extensions/Tests/Test/Rystem.Test.TestApi/Extensions/ServiceExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Test/Rystem.Test.TestApi/Extensions/ServiceExtensions.cs)
- Test API controller scenarios: [src/Extensions/Tests/Test/Rystem.Test.TestApi/Controllers/ServiceController.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Tests/Test/Rystem.Test.TestApi/Controllers/ServiceController.cs)
- Runtime provider implementation: [src/Core/Rystem.DependencyInjection.Web/RuntimeServiceProvider/RuntimeServiceProvider.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection.Web/RuntimeServiceProvider/RuntimeServiceProvider.cs)
- Auto-rebuild fallback implementation: [src/Core/Rystem.DependencyInjection.Web/Fallback/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection.Web/Fallback/ServiceCollectionExtensions.cs)

This README is intentionally architecture-first because `Rystem.DependencyInjection.Web` is not just one extension method. It is a runtime composition model for ASP.NET Core built on top of the base Rystem DI package.
