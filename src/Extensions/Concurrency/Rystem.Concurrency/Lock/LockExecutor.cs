namespace System.Threading.Concurrent
{
    internal sealed class LockExecutor : ILock
    {
        private DateTime _lastExecutionPlusExpirationTime;
        internal bool IsExpired => DateTime.UtcNow > _lastExecutionPlusExpirationTime;
        private readonly ILockable _lockable;
        public LockExecutor(ILockable lockable)
        {
            _lockable = lockable;
        }
        public async Task<LockResponse> ExecuteAsync(Func<Task> action, string? key = null)
        {
            key ??= string.Empty;
            DateTime start = DateTime.UtcNow;
            _lastExecutionPlusExpirationTime = start.AddDays(1);
            while (true)
            {
                if (await _lockable.AcquireAsync(key).NoContext())
                    break;
                await Task.Delay(2).NoContext();
            }
            Exception exception = default!;
            try
            {
                await action.Invoke().NoContext();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            await _lockable.ReleaseAsync(key).NoContext();
            return new LockResponse(DateTime.UtcNow.Subtract(start), exception != null ? new List<Exception>() { exception } : new());
        }
    }
}