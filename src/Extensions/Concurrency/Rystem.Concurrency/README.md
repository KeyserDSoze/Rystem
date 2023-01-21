### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Concurrency

### Async Lock
A lock keyword is used in C# to lock a memory address to have a sort of execution queue. But, unfortunately, you cannot use async methods in the lock statement.
With async lock you also may have the lock behavior for your async methods.
In DI you have to add the lock service

	services.AddLock();

Inject ILock and use it

	ILock locking = _serviceProvider.CreateScope().ServiceProvider.GetService<ILock>();
	await locking!.ExecuteAsync(() => CountAsync(2), "SampleKey");

You have the method to execute, a key for more than one concurrent lock.

### Race condition
First of all, you have to understand the race condition [here](https://en.wikipedia.org/wiki/Race_condition)
In DI you have to add the lock service

	services.AddRaceCondition();
	
Inject IRaceCodition and use it

	 var raceCondition = _serviceProvider.CreateScope().ServiceProvider.GetService<IRaceCodition>();
	 raceCondition!.ExecuteAsync(() => CountAsync(2), (i % 2).ToString(), TimeSpan.FromSeconds(2));

You have the method to execute, a key for more than one concurrent race, and a time span for time window, if you put 2 seconds, you block the execution of further methods for 2 seconds from when first method started.

### ILockable
You can inject the ILockable interface for your purpose, like a distributed lock with Blob storage, or redis cache for instance.

	services.AddLockableIntegration<BlobLockIntegration>();
	