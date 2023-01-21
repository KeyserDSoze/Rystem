namespace System.Threading.Concurrent
{
    public interface ILock
    {
        Task<LockResponse> ExecuteAsync(Func<Task> action, string? key = null);
    }
}