using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace RepositoryFramework.InMemory
{
    internal class InMemoryStorage<T, TKey> : IRepository<T, TKey>
        where TKey : notnull
    {
        private readonly RepositoryBehaviorSettings<T, TKey> _settings;
        public InMemoryStorage(RepositoryBehaviorSettings<T, TKey> settings)
        {
            _settings = settings;
        }
        private static string GetKeyAsString(TKey key)
        {
            if (key is IKey customKey)
                return customKey.AsString();
            return key.ToString()!;
        }
        private static ConcurrentDictionary<string, Entity<T, TKey>> Values { get; } = new();
        internal static void AddValue(TKey key, T value)
            => Values.TryAdd(GetKeyAsString(key), Entity.Default(value, key));
        private static int GetRandomNumber(Range range)
        {
            var maxPlusOne = range.End.Value + 1 - range.Start.Value;
            return RandomNumberGenerator.GetInt32(maxPlusOne) + range.Start.Value;
        }
        private static Exception? GetException(List<ExceptionOdds> odds)
        {
            if (odds.Count == 0)
                return default;
            var oddBase = (int)Math.Pow(10, odds.Select(x => x.Percentage.ToString()).OrderByDescending(x => x.Length).First().Split('.').Last().Length);
            List<ExceptionOdds> normalizedOdds = new();
            foreach (var odd in odds)
            {
                normalizedOdds.Add(new ExceptionOdds
                {
                    Exception = odd.Exception,
                    Percentage = odd.Percentage * oddBase
                });
            }
            Range range = new(0, 100 * oddBase);
            var result = GetRandomNumber(range);
            var total = 0;
            foreach (var odd in normalizedOdds)
            {
                var value = (int)odd.Percentage;
                if (result >= total && result < total + value)
                    return odd.Exception;
                total += value;
            }
            return default;
        }
        private async Task<State<T, TKey>> ExecuteAsync(RepositoryMethods method, Func<State<T, TKey>> action, CancellationToken cancellationToken = default)
        {
            var settings = _settings.Get(method);
            await Task.Delay(GetRandomNumber(settings.MillisecondsOfWait), cancellationToken).NoContext();
            if (!cancellationToken.IsCancellationRequested)
            {
                var exception = GetException(settings.ExceptionOdds);
                if (exception != null)
                {
                    await Task.Delay(GetRandomNumber(settings.MillisecondsOfWaitWhenException), cancellationToken).NoContext();
                    return false;
                }
                if (!cancellationToken.IsCancellationRequested)
                    return action.Invoke();
                else
                    return false;
            }
            else
                return false;
        }
        public Task<State<T, TKey>> DeleteAsync(TKey key, CancellationToken cancellationToken = default)
            => ExecuteAsync(RepositoryMethods.Delete, () =>
            {
                var keyAsString = GetKeyAsString(key);
                if (Values.ContainsKey(keyAsString))
                    return SetState(Values.TryRemove(keyAsString, out _), default!, key);
                return false;
            }, cancellationToken);

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var settings = _settings.Get(RepositoryMethods.Get);
            await Task.Delay(GetRandomNumber(settings.MillisecondsOfWait), cancellationToken).NoContext();
            if (!cancellationToken.IsCancellationRequested)
            {
                var exception = GetException(settings.ExceptionOdds);
                if (exception != null)
                {
                    await Task.Delay(GetRandomNumber(settings.MillisecondsOfWaitWhenException), cancellationToken).NoContext();
                    throw exception;
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    var keyAsString = GetKeyAsString(key);
                    return Values.TryGetValue(keyAsString, out var value) ? value.Value : default;
                }
                else
                    throw new TaskCanceledException();
            }
            else
                throw new TaskCanceledException();
        }
        private static State<T, TKey> SetState(bool isOk, T value, TKey key)
            => State.Default(isOk, value, key);
        public Task<State<T, TKey>> InsertAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => ExecuteAsync(RepositoryMethods.Insert, () =>
            {
                var keyAsString = GetKeyAsString(key);
                if (!Values.ContainsKey(keyAsString))
                {
                    Values.TryAdd(keyAsString, Entity.Default(value, key));
                    return SetState(true, value, key);
                }
                else
                    return SetState(false, value, key);
            }, cancellationToken);

        public Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => ExecuteAsync(RepositoryMethods.Update, () =>
            {
                var keyAsString = GetKeyAsString(key);
                if (Values.ContainsKey(keyAsString))
                {
                    Values[keyAsString] = Entity.Default(value, key);
                    return SetState(true, value, key);
                }
                else
                    return false;
            }, cancellationToken);

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var settings = _settings.Get(RepositoryMethods.Query);
            await Task.Delay(GetRandomNumber(settings.MillisecondsOfWait), cancellationToken).NoContext();
            if (!cancellationToken.IsCancellationRequested)
            {
                var exception = GetException(settings.ExceptionOdds);
                if (exception != null)
                {
                    await Task.Delay(GetRandomNumber(settings.MillisecondsOfWaitWhenException), cancellationToken).NoContext();
                    throw exception;
                }
                foreach (var item in filter.Apply(Values.Select(x => x.Value.Value)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return Values.First(x => x.Value.Value.Equals(item)).Value;
                }
            }
            else
                throw new TaskCanceledException();
        }
        public async ValueTask<TProperty> OperationAsync<TProperty>(
           OperationType<TProperty> operation,
           IFilterExpression filter,
           CancellationToken cancellationToken = default)
        {
            var settings = _settings.Get(RepositoryMethods.Operation);
            await Task.Delay(GetRandomNumber(settings.MillisecondsOfWait), cancellationToken).NoContext();
            if (!cancellationToken.IsCancellationRequested)
            {
                var exception = GetException(settings.ExceptionOdds);
                if (exception != null)
                {
                    await Task.Delay(GetRandomNumber(settings.MillisecondsOfWaitWhenException), cancellationToken).NoContext();
                    throw exception;
                }
                if (!cancellationToken.IsCancellationRequested)
                {
                    var filtered = filter.Apply(Values.Select(x => x.Value.Value));
                    var selected = filter.ApplyAsSelect(filtered);
                    return (await operation.ExecuteDefaultOperationAsync(
                        () => Invoke<TProperty>(selected.Count()),
                        () => Invoke<TProperty>(selected.Sum(x => ((object)x).Cast<decimal>())),
                        () => Invoke<TProperty>(selected.Max()!),
                        () => Invoke<TProperty>(selected.Min()!),
                        () => Invoke<TProperty>(selected.Average(x => ((object)x).Cast<decimal>()))))!;
                }
                else
                    throw new TaskCanceledException();
            }
            else
                throw new TaskCanceledException();
        }
        private static ValueTask<TProperty> Invoke<TProperty>(object value)
            => ValueTask.FromResult((TProperty)Convert.ChangeType(value, typeof(TProperty)));
        public Task<State<T, TKey>> ExistAsync(TKey key, CancellationToken cancellationToken = default)
            => ExecuteAsync(RepositoryMethods.Exist, () =>
            {
                var keyAsString = GetKeyAsString(key);
                return Values.ContainsKey(keyAsString);
            }, cancellationToken);

        public async Task<BatchResults<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations, CancellationToken cancellationToken = default)
        {
            BatchResults<T, TKey> results = new();
            foreach (var operation in operations.Values)
            {
                switch (operation.Command)
                {
                    case CommandType.Delete:
                        results.AddDelete(operation.Key, await DeleteAsync(operation.Key, cancellationToken).NoContext());
                        break;
                    case CommandType.Insert:
                        results.AddInsert(operation.Key, await InsertAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                    case CommandType.Update:
                        results.AddUpdate(operation.Key, await UpdateAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                }
            }
            return results;
        }
    }
}
