using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryFramework.InMemory
{
    internal class InMemoryStorage<T, TKey> : IRepository<T, TKey>, IServiceWithFactoryWithOptions<RepositoryBehaviorSettings<T, TKey>>, IDefaultIntegration
        where TKey : notnull
    {
        public void SetOptions(RepositoryBehaviorSettings<T, TKey> options)
        {
            Options = options;
        }
        public void SetFactoryName(string name)
        {
            return;
        }
        public RepositoryBehaviorSettings<T, TKey>? Options { get; set; }
        private readonly ConcurrentDictionary<string, Entity<T, TKey>> _values = new();
        public InMemoryStorage(RepositoryBehaviorSettings<T, TKey>? options = null)
        {
            Options = options;
        }
        public static string GetKeyAsString(TKey key)
            => KeySettings<TKey>.Instance.AsString(key);

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
            var settings = Options!.Get(method);
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
                if (_values.ContainsKey(keyAsString))
                    return SetState(_values.TryRemove(keyAsString, out _), default!, key);
                return false;
            }, cancellationToken);

        public async Task<T?> GetAsync(TKey key, CancellationToken cancellationToken = default)
        {
            var settings = Options!.Get(RepositoryMethods.Get);
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
                    return _values.TryGetValue(keyAsString, out var value) ? (value.Value != null ? value.Value.ToJson().FromJson<T>() : default) : default;
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
                value = value.ToJson().FromJson<T>();
                if (!_values.ContainsKey(keyAsString))
                {
                    _values.TryAdd(keyAsString, Entity.Default(value, key));
                    return SetState(true, value, key);
                }
                else
                    return SetState(false, value, key);
            }, cancellationToken);

        public Task<State<T, TKey>> UpdateAsync(TKey key, T value, CancellationToken cancellationToken = default)
            => ExecuteAsync(RepositoryMethods.Update, () =>
            {
                var keyAsString = GetKeyAsString(key);
                value = value.ToJson().FromJson<T>();
                if (_values.ContainsKey(keyAsString))
                {
                    _values[keyAsString] = Entity.Default(value, key);
                    return SetState(true, value, key);
                }
                else
                    return false;
            }, cancellationToken);

        public async IAsyncEnumerable<Entity<T, TKey>> QueryAsync(IFilterExpression filter,
             [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var settings = Options!.Get(RepositoryMethods.Query);
            await Task.Delay(GetRandomNumber(settings.MillisecondsOfWait), cancellationToken).NoContext();
            if (!cancellationToken.IsCancellationRequested)
            {
                var exception = GetException(settings.ExceptionOdds);
                if (exception != null)
                {
                    await Task.Delay(GetRandomNumber(settings.MillisecondsOfWaitWhenException), cancellationToken).NoContext();
                    throw exception;
                }
                foreach (var item in filter.Apply(_values.Select(x => x.Value.Value)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var value = _values.First(x => x.Value.Value.Equals(item)).Value;
                    if (value.Value != null)
                        yield return Entity.Default(value.Value != null ? value.Value.ToJson().FromJson<T>() : default, value.Key);
                    else
                        yield return value;
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
            var settings = Options!.Get(RepositoryMethods.Operation);
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
                    var filtered = filter.Apply(_values.Select(x => x.Value.Value));
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
                return _values.ContainsKey(keyAsString);
            }, cancellationToken);

        public async IAsyncEnumerable<BatchResult<T, TKey>> BatchAsync(BatchOperations<T, TKey> operations,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var operation in operations.Values)
            {
                switch (operation.Command)
                {
                    case CommandType.Delete:
                        yield return BatchResult<T, TKey>.CreateDelete(operation.Key, await DeleteAsync(operation.Key, cancellationToken).NoContext());
                        break;
                    case CommandType.Insert:
                        yield return BatchResult<T, TKey>.CreateInsert(operation.Key, await InsertAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                    case CommandType.Update:
                        yield return BatchResult<T, TKey>.CreateUpdate(operation.Key, await UpdateAsync(operation.Key, operation.Value!, cancellationToken).NoContext());
                        break;
                }
            }
        }
        //todo: implement this method and avoid the use of the AddFactoryAsync and the IOptionsBuilderAsync
        public ValueTask<bool> BootstrapAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);
    }
}
