using System.Collections.Concurrent;

namespace System.Threading.Concurrent
{
    public sealed class MemoryLock : ILockable
    {
        private readonly object _semaphore = new();
        private readonly ConcurrentDictionary<string, bool> _isLocked = new();
        public Task<bool> AcquireAsync(string key, TimeSpan? maxWindow = null)
        {
            if (!_isLocked.ContainsKey(key))
                _isLocked.TryAdd(key, false);

            if (!_isLocked[key])
                lock (_semaphore)
                {
                    if (!_isLocked[key])
                    {
                        _isLocked[key] = true;
                        return Task.FromResult(true);
                    }
                }
            return Task.FromResult(false);
        }
        public Task<bool> IsAcquiredAsync(string key)
            => Task.FromResult(_isLocked.ContainsKey(key) && _isLocked[key]);
        public Task<bool> ReleaseAsync(string key)
        {
            _isLocked[key] = false;
            return Task.FromResult(true);
        }
    }
}
