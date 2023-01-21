namespace Rystem.Queue
{
    public interface IQueue<T>
    {
        Task AddAsync(T entity);
        Task<IEnumerable<T>> DequeueAsync(int? top = null);
        Task<IEnumerable<T>> ReadAsync(int? top = null);
        Task<int> CountAsync();
    }
}