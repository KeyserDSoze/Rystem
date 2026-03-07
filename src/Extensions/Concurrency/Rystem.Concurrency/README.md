### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Concurrency

`Rystem.Concurrency` adds two small async concurrency primitives on top of the Rystem DI stack:

- `ILock` for serialized async execution
- `IRaceCodition` for deduplicating concurrent calls within a time window

Both abstractions are built on `ILockable`, which defaults to an in-memory implementation and can be swapped for another backend such as Redis.

The package is most useful for:

- guarding critical async sections
- de-duplicating cache refreshes and polling work
- coordinating lightweight background tasks
- keeping the same API while switching from local memory to distributed locking

The public types live in `System.Threading.Concurrent`, so that is the namespace you usually import when consuming the package.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.Concurrency
```

Optional distributed backend:

```bash
dotnet add package Rystem.Concurrency.Redis
```

The current `10.x` package targets `net10.0` and builds on top of [`Rystem.DependencyInjection`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Core/Rystem.DependencyInjection/README.md).

## Package Architecture

The package is organized in three layers.

| Layer | Purpose |
|---|---|
| `ILockable` | Lowest-level acquire / inspect / release abstraction |
| `ILock` | Serialized execution of async work for a key |
| `IRaceCodition` | First-wins execution with a configurable deduplication window |

The DI registrations mirror that layering:

- `AddInMemoryLockable()` registers only the in-memory `ILockable`
- `AddLockExecutor()` registers only the `ILock` executor
- `AddLock()` wires both together for the default lock setup
- `AddRaceConditionExecutor()` registers only the `IRaceCodition` executor
- `AddRaceCondition()` wires the full in-memory race-condition stack

That split is important when you want to plug in a custom or distributed backend without changing the calling code.

## Table of Contents

- [Package Architecture](#package-architecture)
- [Async Lock](#async-lock)
  - [Setup](#setup)
  - [ILock contract](#ilock-contract)
  - [Behavior](#behavior)
  - [LockResponse](#lockresponse)
- [Race Condition Guard](#race-condition-guard)
  - [Setup](#setup-1)
  - [IRaceCodition contract](#iracecodition-contract)
  - [Behavior](#behavior-1)
  - [RaceConditionResponse](#raceconditionresponse)
- [ILockable and Custom Backends](#ilockable-and-custom-backends)
  - [ILockable contract](#ilockable-contract)
  - [Built-in in-memory backend](#built-in-in-memory-backend)
  - [Custom executor registration](#custom-executor-registration)
- [Distributed Locking with Redis](#distributed-locking-with-redis)
- [Repository Examples](#repository-examples)

---

## Async Lock

`ILock` is the async equivalent of a critical section keyed by a string.

Use it when all callers for the same key must execute one after another instead of overlapping.

### Setup

```csharp
services.AddLock();
```

This registers:

- `ILock` -> `LockExecutor`
- `ILockable` -> `MemoryLock`

### ILock contract

```csharp
public interface ILock
{
    Task<LockResponse> ExecuteAsync(Func<Task> action, string? key = null);
}
```

Typical usage:

```csharp
using System.Threading.Concurrent;

public sealed class InventoryService
{
    private readonly ILock _lock;

    public InventoryService(ILock @lock)
    {
        _lock = @lock;
    }

    public async Task UpdateAsync()
    {
        LockResponse response = await _lock.ExecuteAsync(
            async () =>
            {
                await Task.Delay(15);
                await SaveAsync();
            },
            key: "inventory");

        if (response.InException)
            throw response.Exceptions!;
    }

    private Task SaveAsync() => Task.CompletedTask;
}
```

### Behavior

The repository test in `src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/LockTest.cs` is the clearest example of the intended behavior.

It starts 100 concurrent calls against the same lock:

```csharp
var locking = provider.CreateScope().ServiceProvider.GetService<ILock>();

for (int i = 0; i < 100; i++)
    tasks.Add(locking!.ExecuteAsync(() => CountAsync(2)));
```

Because all calls share the same keyless default lock, they are serialized and the final counter is deterministic:

```csharp
Assert.Equal(100 * 2, counter);
```

Important details from the implementation:

- `key: null` becomes `string.Empty`, so omitted keys all share one common lock
- different keys can run in parallel
- `ExecutionTime` includes both waiting time and action time because timing starts before acquisition
- exceptions are captured in the response instead of being rethrown directly

### LockResponse

```csharp
public sealed class LockResponse
{
    public TimeSpan ExecutionTime { get; }
    public AggregateException? Exceptions { get; }
    public bool InException => this.Exceptions != default;
}
```

Use `InException` as the quick status check and `Exceptions` when you want the captured failure details.

---

## Race Condition Guard

`IRaceCodition` is a first-wins guard for async work.

When multiple callers hit the same key inside the guarded window:

- the first caller executes the action
- the later callers wait until the guard is released
- those later callers return without executing the action

The interface name is intentionally documented as `IRaceCodition` because that is the current public API surface in the package.

### Setup

```csharp
services.AddRaceCondition();
```

This wires:

- `ILockable` -> `MemoryLock`
- `ILock` -> `LockExecutor`
- `IRaceCodition` -> `RaceConditionExecutor`

### IRaceCodition contract

```csharp
public interface IRaceCodition
{
    Task<RaceConditionResponse> ExecuteAsync(
        Func<Task> action,
        string? key = null,
        TimeSpan? timeWindow = null);
}
```

Typical usage:

```csharp
using System.Threading.Concurrent;

public sealed class PriceCacheService
{
    private readonly IRaceCodition _raceCondition;

    public PriceCacheService(IRaceCodition raceCondition)
    {
        _raceCondition = raceCondition;
    }

    public async Task RefreshAsync(string productId)
    {
        var response = await _raceCondition.ExecuteAsync(
            async () =>
            {
                await Task.Delay(15);
                await RefreshCoreAsync(productId);
            },
            key: productId,
            timeWindow: TimeSpan.FromSeconds(10));

        if (response.InException)
            throw response.Exceptions!;

        if (response.IsExecuted)
        {
            // this caller won and executed the action
        }
    }

    private Task RefreshCoreAsync(string productId) => Task.CompletedTask;
}
```

### Behavior

The repository test in `src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RaceConditionTest.cs` runs 100 concurrent calls but alternates between only two keys:

```csharp
for (int i = 0; i < 100; i++)
    tasks.Add(raceCondition!.ExecuteAsync(
        () => CountAsync(2),
        (i % 2).ToString(),
        TimeSpan.FromSeconds(2)));
```

Only the first call for key `0` and the first call for key `1` execute, so the final result is:

```csharp
Assert.Equal(4, counter);
```

Important details from the implementation:

- default `timeWindow` is `TimeSpan.FromMinutes(1)`
- omitted keys also collapse to `string.Empty`
- the winner keeps the lock until the action finishes and the time window has elapsed, whichever is later in the in-memory flow
- non-winning callers wait for release, then return `IsExecuted = false`

### RaceConditionResponse

```csharp
public sealed class RaceConditionResponse
{
    public bool IsExecuted { get; }
    public AggregateException? Exceptions { get; }
    public bool InException => this.Exceptions != default;
}
```

- `IsExecuted = true` only for the winning caller
- `InException` and `Exceptions` reflect failures from the winning execution

---

## ILockable and Custom Backends

`ILock` and `IRaceCodition` both delegate the actual locking primitive to `ILockable`.

### ILockable contract

```csharp
public interface ILockable
{
    Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null);
    Task<bool> IsAcquiredAsync(string key);
    Task<bool> ReleaseAsync(string key);
}
```

`maxWindow` matters mostly for backends that can encode expiration directly, such as Redis.

### Built-in in-memory backend

The built-in implementation is `MemoryLock`, registered through:

```csharp
services.AddInMemoryLockable();
```

If you only want the low-level backend and plan to wire your own executors, this is the smallest registration unit.

To replace the backend entirely:

```csharp
services.AddLockableIntegration<MyDistributedLockable>();
```

### Custom executor registration

If you want to keep the lockable but swap the higher-level behavior:

```csharp
services.AddLockExecutor<MyCustomLock>();
services.AddRaceConditionExecutor<MyCustomRaceCondition>();
```

There are also non-generic registrations for the default executors only:

```csharp
services.AddLockExecutor();
services.AddRaceConditionExecutor();
```

Those methods register the executors but do not automatically add a lockable backend, so pair them with `AddInMemoryLockable()`, `AddLockableIntegration<T>()`, or the Redis package.

---

## Distributed Locking with Redis

For multi-process or multi-host coordination, use [`Rystem.Concurrency.Redis`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/README.md).

That companion package exposes:

- `AddRedisLock(...)`
- `AddRaceConditionWithRedis(...)`
- `AddRedisLockable(...)`

Example:

```csharp
services.AddRedisLock(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"]!;
});
```

The Redis-backed lock test in `src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs` uses the same `ILock` API as the in-memory version, which is exactly the point of the `ILockable` abstraction.

---

## Repository Examples

The most useful sources for this package are:

- Lock registration and executor: [src/Extensions/Concurrency/Rystem.Concurrency/Lock/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/Lock/ServiceCollectionExtensions.cs)
- Lock implementation: [src/Extensions/Concurrency/Rystem.Concurrency/Lock/LockExecutor.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/Lock/LockExecutor.cs)
- Race condition registration and executor: [src/Extensions/Concurrency/Rystem.Concurrency/RaceCondition/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/RaceCondition/ServiceCollectionExtensions.cs)
- Race condition implementation: [src/Extensions/Concurrency/Rystem.Concurrency/RaceCondition/RaceConditionExecutor.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/RaceCondition/RaceConditionExecutor.cs)
- In-memory backend: [src/Extensions/Concurrency/Rystem.Concurrency/Lockable/MemoryLock.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/Lockable/MemoryLock.cs)
- In-memory lock test: [src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/LockTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/LockTest.cs)
- Race-condition test: [src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RaceConditionTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RaceConditionTest.cs)
- Redis lock test: [src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs)

This README stays intentionally focused because `Rystem.Concurrency` is a small package with a layered design: one low-level lockable abstraction and two higher-level execution patterns built on top of it.
