### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Concurrency

Two concurrency primitives for async code: an **async lock** (serialise execution without blocking the thread pool) and a **race condition guard** (deduplicate concurrent calls within a time window). Both use an in-memory `ILockable` by default and can be swapped for a distributed backend (e.g. Redis).

## 📦 Installation

```bash
dotnet add package Rystem.Concurrency

# Optional: distributed locking via Redis
dotnet add package Rystem.Concurrency.Redis
```

## Table of Contents

- [Async Lock](#async-lock)
- [LockResponse](#lockresponse)
- [Race Condition](#race-condition)
- [RaceConditionResponse](#raceconditionresponse)
- [ILockable — Custom Backends](#ilockable--custom-backends)
- [Distributed Locking with Redis](#distributed-locking-with-redis)

---

## Async Lock

The C# `lock` keyword cannot be used with `async` code. `ILock` provides the same serialised-queue behaviour for async methods.

### Setup

```csharp
services.AddLock(); // registers ILock + in-memory ILockable
```

### Usage

```csharp
public class MyService
{
    private readonly ILock _lock;

    public MyService(ILock @lock) => _lock = @lock;

    public async Task SafeWriteAsync()
    {
        var response = await _lock.ExecuteAsync(
            async () => await WriteAsync(),
            key: "my-resource");     // key isolates independent lock queues

        if (response.InException)
            throw response.Exceptions!;

        Console.WriteLine($"Done in {response.ExecutionTime.TotalMilliseconds} ms");
    }
}
```

`ExecuteAsync` acquires the lock for the given `key`, runs the action, then releases it. Concurrent callers for the same key are queued and execute in order. Callers with different keys run in parallel.

### ILock interface

```csharp
Task<LockResponse> ExecuteAsync(Func<Task> action, string? key = null);
```

| Parameter | Description |
|---|---|
| `action` | The async work to execute exclusively |
| `key` | Isolation key; `null` / omitted uses a shared default key |

---

## LockResponse

| Property | Type | Description |
|---|---|---|
| `ExecutionTime` | `TimeSpan` | Wall-clock time from acquire to release |
| `InException` | `bool` | `true` if the action threw |
| `Exceptions` | `AggregateException?` | The caught exception, if any |

---

## Race Condition

Prevents duplicate execution of the same logical operation within a configurable time window. Only **the first** caller executes the action; all subsequent callers that arrive while the window is still open skip execution and wait for it to close.

See [Race condition — Wikipedia](https://en.wikipedia.org/wiki/Race_condition) for background.

### Setup

```csharp
services.AddRaceCondition(); // registers IRaceCodition + ILock + in-memory ILockable
```

### Usage

```csharp
public class PriceService
{
    private readonly IRaceCodition _race;

    public PriceService(IRaceCodition race) => _race = race;

    public async Task RefreshPriceAsync(string productId)
    {
        var response = await _race.ExecuteAsync(
            async () => await FetchAndCachePriceAsync(productId),
            key: productId,                        // one guard per product
            timeWindow: TimeSpan.FromSeconds(10)); // dedupe window

        if (response.IsExecuted)
            Console.WriteLine("Prices refreshed.");
        else
            Console.WriteLine("Another call already running — skipped.");

        if (response.InException)
            throw response.Exceptions!;
    }
}
```

### IRaceCodition interface

```csharp
Task<RaceConditionResponse> ExecuteAsync(
    Func<Task> action,
    string? key = null,
    TimeSpan? timeWindow = null);
```

| Parameter | Description |
|---|---|
| `action` | The async work — executed only by the first caller |
| `key` | Isolation key; separate keys run independently |
| `timeWindow` | How long to block duplicate calls; defaults to **1 minute** |

---

## RaceConditionResponse

| Property | Type | Description |
|---|---|---|
| `IsExecuted` | `bool` | `true` only for the winning (first) caller that ran the action |
| `InException` | `bool` | `true` if the action threw |
| `Exceptions` | `AggregateException?` | The caught exception, if any |

---

## ILockable — Custom Backends

Both `ILock` and `IRaceCodition` delegate the actual locking to an `ILockable` implementation. By default an in-memory implementation is used. You can replace it with any custom backend:

```csharp
public interface ILockable
{
    Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null);
    Task<bool> IsAcquiredAsync(string key);
    Task<bool> ReleaseAsync(string key);
}
```

Register a custom implementation:

```csharp
services.AddLockableIntegration<MyDistributedLock>();
```

To register a custom `ILock` executor instead:

```csharp
services.AddLockExecutor<MyCustomLock>();
```

To register a custom `IRaceCodition` executor:

```csharp
services.AddRaceConditionExecutor<MyCustomRaceCondition>();
```

---

## Distributed Locking with Redis

Install `Rystem.Concurrency.Redis` and replace the in-memory lockable with a Redis-backed one. All callers in a distributed environment share the same lock state.

### Distributed async lock with Redis

```csharp
services.AddRedisLock(options =>
{
    options.ConnectionString = "localhost:6379";
});
```

### Distributed race condition with Redis

```csharp
services.AddRaceConditionWithRedis(options =>
{
    options.ConnectionString = "localhost:6379";
});
```

### Redis-only lockable (use your own executor)

```csharp
services.AddRedisLockable(options =>
{
    options.ConnectionString = "localhost:6379";
});
```

### RedisConfiguration

| Property | Type | Description |
|---|---|---|
| `ConnectionString` | `string?` | Standard StackExchange.Redis connection string |
	