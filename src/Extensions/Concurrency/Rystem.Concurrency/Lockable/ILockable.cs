namespace System.Threading.Concurrent
{
    public interface ILockable
    {
        Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null);
        Task<bool> IsAcquiredAsync(string key);
        Task<bool> ReleaseAsync(string key);
    }
}
