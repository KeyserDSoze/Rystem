namespace Rystem.Queue
{
    public interface IQueueManager<in T>
    {
        Task ManageAsync(IEnumerable<T> items);
    }
}