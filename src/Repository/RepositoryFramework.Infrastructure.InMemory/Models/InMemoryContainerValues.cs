using System.Collections.Concurrent;

namespace RepositoryFramework.InMemory
{
    internal sealed class InMemoryContainerValues<T, TKey>
        where TKey : notnull
    {
        public ConcurrentDictionary<string, Entity<T, TKey>> Values { get; } = new();
        internal void AddValue(TKey key, T value)
            => Values.TryAdd(InMemoryStorage<T, TKey>.GetKeyAsString(key), Entity.Default(value, key));
    }
}
