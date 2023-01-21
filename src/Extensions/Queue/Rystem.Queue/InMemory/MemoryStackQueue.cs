using System.Collections.Concurrent;

namespace Rystem.Queue
{
    internal sealed class MemoryStackQueue<T> : IQueue<T>
    {
        private readonly ConcurrentStack<T> Queues = new();
        public Task AddAsync(T entity)
        {
            Queues.Push(entity);
            return Task.CompletedTask;
        }

        public Task<int> CountAsync()
            => Task.FromResult(Queues.Count);

        public Task<IEnumerable<T>> DequeueAsync(int? top = null)
        {
            int count = Queues.Count;
            T[] array = new T[count];
            Queues.TryPopRange(array, 0, top.HasValue && top.Value < count ? top.Value : count);
            return Task.FromResult(array.Select(x => x));
        }

        public Task<IEnumerable<T>> ReadAsync(int? top = null) 
            => Task.FromResult(Queues.Take(top.HasValue && top.Value < Queues.Count ? top.Value : Queues.Count));
    }
}