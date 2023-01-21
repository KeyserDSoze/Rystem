using System.Collections.Concurrent;

namespace Rystem.Queue
{
    internal sealed class MemoryQueue<T> : IQueue<T>
    {
        private readonly ConcurrentQueue<T> Queues = new();
        public Task AddAsync(T entity)
        {
            Queues.Enqueue(entity);
            return Task.CompletedTask;
        }
        public Task<int> CountAsync()
            => Task.FromResult(Queues.Count);
        public Task<IEnumerable<T>> DequeueAsync(int? top = null)
        {
            List<T> entities = new();
            int count = Queues.Count;
            for (int i = 0; i < count && (top == null || i < top); i++)
            {
                Queues.TryDequeue(out T? value);
                if (value != null)
                    entities.Add(value);
            }
            return Task.FromResult(entities as IEnumerable<T>);
        }

        public Task<IEnumerable<T>> ReadAsync(int? top = null)
            => Task.FromResult(Queues.Take(top.HasValue && top.Value < Queues.Count ? top.Value : Queues.Count));
    }
}