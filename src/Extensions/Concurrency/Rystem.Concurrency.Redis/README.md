### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

## Rystem.Concurrency.Redis

Redis backend for `Rystem.Concurrency`. Replaces the default in-memory `ILockable` with a Redis-backed distributed implementation, so locks and race-condition guards work across **multiple processes and hosts**.

For full documentation of `ILock`, `IRaceCodition`, and the concurrency model see [Rystem.Concurrency](../Rystem.Concurrency/README.md).

## 📦 Installation

```bash
dotnet add package Rystem.Concurrency.Redis
```

## Table of Contents

- [Rystem.Concurrency.Redis](#rystemconcurrencyredis)
- [📦 Installation](#-installation)
- [Table of Contents](#table-of-contents)
- [Distributed Async Lock](#distributed-async-lock)
- [Distributed Race Condition](#distributed-race-condition)
- [Redis Lockable Only](#redis-lockable-only)
- [RedisConfiguration](#redisconfiguration)

---

## Distributed Async Lock

Registers `ILock` backed by Redis. All application instances sharing the same Redis connection and key compete for the same lock.

```csharp
services.AddRedisLock(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"];
});
```

Usage is identical to the in-memory version — inject `ILock` and call `ExecuteAsync`:

```csharp
var response = await _lock.ExecuteAsync(
    async () => await WriteAsync(),
    key: "my-resource");

if (response.InException)
    throw response.Exceptions!;
```

---

## Distributed Race Condition

Registers `IRaceCodition` backed by Redis. Only the first caller across the entire cluster executes the action within the time window; all others skip.

```csharp
services.AddRaceConditionWithRedis(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"];
});
```

Usage is identical to the in-memory version — inject `IRaceCodition` and call `ExecuteAsync`:

```csharp
var response = await _race.ExecuteAsync(
    async () => await RefreshCacheAsync(),
    key: "cache-refresh",
    timeWindow: TimeSpan.FromSeconds(30));

if (response.IsExecuted)
    Console.WriteLine("Cache refreshed by this instance.");
else
    Console.WriteLine("Another instance already handling it.");
```

---

## Redis Lockable Only

If you want to use the Redis `ILockable` with a custom `ILock` or `IRaceCodition` executor, register only the lockable:

```csharp
services.AddRedisLockable(options =>
{
    options.ConnectionString = configuration["ConnectionString:Redis"];
});
```

Then register your own executor separately:

```csharp
services.AddLockExecutor<MyCustomLock>();
// or
services.AddRaceConditionExecutor<MyCustomRaceCondition>();
```

---

## RedisConfiguration

| Property | Type | Description |
|---|---|---|
| `ConnectionString` | `string?` | Standard StackExchange.Redis connection string (e.g. `"localhost:6379"` or `"host:port,password=..."`) |
	