### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Concurrency.Redis

`Rystem.Concurrency.Redis` swaps the default in-memory `ILockable` from `Rystem.Concurrency` with a Redis-backed distributed implementation.

Use it when lock and race-condition state must be shared across:

- multiple application instances
- multiple worker processes
- multiple machines or containers

The package keeps the same higher-level APIs from `Rystem.Concurrency`:

- `ILock`
- `IRaceCodition`
- `ILockable`

Only the storage backend changes.

## Resources

- Complete Documentation: [https://rystem.net](https://rystem.net)
- MCP Server for AI: [https://rystem.cloud/mcp](https://rystem.cloud/mcp)
- Discord Community: [https://discord.gg/tkWvy4WPjt](https://discord.gg/tkWvy4WPjt)
- Support the Project: [https://www.buymeacoffee.com/keyserdsoze](https://www.buymeacoffee.com/keyserdsoze)

## Installation

```bash
dotnet add package Rystem.Concurrency.Redis
```

The current `10.x` package targets `net10.0` and builds on top of:

- `StackExchange.Redis`
- [`Rystem.Concurrency`](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency/README.md)

## Package Architecture

The package is intentionally small and only adds the Redis-specific pieces.

| Piece | Purpose |
|---|---|
| `RedisConfiguration` | Holds the Redis connection string |
| `RedisLock` | Implements `ILockable` on top of `StackExchange.Redis` |
| `AddRedisLock(...)` | Registers `ILock` plus the Redis lockable |
| `AddRaceConditionWithRedis(...)` | Registers `IRaceCodition` plus the Redis lock stack |
| `AddRedisLockable(...)` | Registers only the Redis `ILockable` backend |

Conceptually, this package does not replace the concurrency model. It only replaces the backend used by that model.

## Table of Contents

- [Package Architecture](#package-architecture)
- [Distributed Async Lock](#distributed-async-lock)
- [Distributed Race Condition](#distributed-race-condition)
- [Redis Lockable Only](#redis-lockable-only)
- [Redis Backend Behavior](#redis-backend-behavior)
- [Configuration and Registration Notes](#configuration-and-registration-notes)
- [Repository Examples](#repository-examples)

---

## Distributed Async Lock

Register a distributed `ILock` with Redis as the backing store:

```csharp
services.AddRedisLock(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"]!;
});
```

Internally this does two things:

- registers the standard `ILock` executor
- registers `RedisLock` as the active `ILockable`

Usage stays identical to the in-memory version from `Rystem.Concurrency`:

```csharp
using System.Threading.Concurrent;

LockResponse response = await distributedLock.ExecuteAsync(
    async () =>
    {
        await Task.Delay(15);
        await SaveAsync();
    },
    key: "inventory");

if (response.InException)
    throw response.Exceptions!;
```

The repository test in `src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs` uses this registration and starts 100 concurrent lock executions. The final counter still ends at `200`, confirming that the distributed backend preserves the same serialized behavior as the in-memory version.

---

## Distributed Race Condition

Register the race-condition guard with Redis:

```csharp
services.AddRaceConditionWithRedis(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"]!;
});
```

This composes:

- `AddRedisLock(...)`
- `AddRaceConditionExecutor()`

So the high-level race-condition logic remains the same as the base package, but the lock state is now shared through Redis.

Usage also stays the same:

```csharp
using System.Threading.Concurrent;

RaceConditionResponse response = await raceCondition.ExecuteAsync(
    async () =>
    {
        await Task.Delay(15);
        await RefreshCacheAsync();
    },
    key: "cache-refresh",
    timeWindow: TimeSpan.FromSeconds(30));

if (response.IsExecuted)
{
    // this instance won and executed the action
}
```

There is no separate Redis-specific race-condition test in the repository at the moment, so this section is source-backed rather than test-backed. The important implementation detail is that the Redis package reuses the same `RaceConditionExecutor` from the base concurrency package.

---

## Redis Lockable Only

If you want Redis as the backend but your own higher-level executor, register only `ILockable`:

```csharp
services.AddRedisLockable(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"]!;
});
```

Then add your own executor on top:

```csharp
services.AddLockExecutor<MyCustomLock>();
services.AddRaceConditionExecutor<MyCustomRaceCondition>();
```

This is the lowest-level integration point exposed by the package.

---

## Redis Backend Behavior

`RedisLock` implements `ILockable` like this:

- `AcquireAsync(key, maxWindow)` uses `StringSetAsync(..., When.NotExists)`
- `IsAcquiredAsync(key)` checks whether the key currently exists
- `ReleaseAsync(key)` uses a Lua script so the key is deleted only when the stored value matches the expected key value

That gives you two useful properties:

- acquisition is atomic across all Redis-connected processes
- release is guarded, so one caller does not accidentally delete another caller's lock entry

The optional `maxWindow` parameter is important for race-condition scenarios because it becomes the Redis key expiration window.

---

## Configuration and Registration Notes

`RedisConfiguration` is intentionally minimal:

```csharp
public sealed class RedisConfiguration
{
    public string? ConnectionString { get; set; }
}
```

The package creates the `IConnectionMultiplexer` immediately during registration:

```csharp
services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConfiguration.ConnectionString));
```

So connection issues can surface as soon as the service collection is being built.

Also note the registration order behavior:

- `AddRedisLockable(...)` uses `TryAddSingleton<ILockable, RedisLock>()`
- `AddRedisLock(...)` and `AddRaceConditionWithRedis(...)` rely on that registration

That means if another `ILockable` has already been registered earlier, Redis will not override it automatically. In practice, use the Redis registration path as the primary setup for the service collection instead of mixing it with `AddLock()` or `AddRaceCondition()` first.

---

## Repository Examples

The most useful references for this package are:

- Redis lock registration: [src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lock/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lock/ServiceCollectionExtensions.cs)
- Redis race-condition registration: [src/Extensions/Concurrency/Rystem.Concurrency.Redis/RaceCondition/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/RaceCondition/ServiceCollectionExtensions.cs)
- Redis lockable registration: [src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lockable/ServiceCollectionExtensions.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lockable/ServiceCollectionExtensions.cs)
- Redis backend implementation: [src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lockable/RedisLock.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/Lockable/RedisLock.cs)
- Redis configuration model: [src/Extensions/Concurrency/Rystem.Concurrency.Redis/Configurations/RedisConfiguration.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Rystem.Concurrency.Redis/Configurations/RedisConfiguration.cs)
- Distributed lock test: [src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs](https://github.com/KeyserDSoze/Rystem/blob/master/src/Extensions/Concurrency/Test/Rystem.Concurrency.UnitTest/RedisLockTest.cs)

This README stays focused because `Rystem.Concurrency.Redis` is a backend package, not a separate concurrency model. The API shape comes from `Rystem.Concurrency`; Redis only supplies the shared lock state.
