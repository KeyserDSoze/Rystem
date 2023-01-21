namespace System.Threading.Concurrent
{
    internal sealed class RaceConditionExecutor : IRaceCodition
    {
        private readonly ILockable _lockable;
        public RaceConditionExecutor(ILockable lockable)
        {
            _lockable = lockable;
        }
        public async Task<RaceConditionResponse> ExecuteAsync(Func<Task> action, string? key = null, TimeSpan? timeWindow = null)
        {
            key ??= string.Empty;
            timeWindow ??= TimeSpan.FromMinutes(1);
            DateTime nextRelease = DateTime.UtcNow.Add(timeWindow.Value);
            var isTheFirst = false;
            var isWaiting = false;
            await WaitAsync().NoContext();
            if (!isWaiting)
            {
                if (await _lockable.AcquireAsync(key, timeWindow).NoContext())
                    isTheFirst = true;
                if (!isTheFirst)
                    await WaitAsync().NoContext();
            }
            Exception exception = default!;
            if (isTheFirst && !isWaiting)
            {
                try
                {
                    await action.Invoke().NoContext();
                    while (DateTime.UtcNow < nextRelease)
                    {
                        int milliseconds = (int)nextRelease.Subtract(DateTime.UtcNow).TotalMilliseconds;
                        if (milliseconds < 0)
                            break;
                        await Task.Delay(milliseconds).NoContext();
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                await _lockable.ReleaseAsync(key).NoContext();
            }
            return new RaceConditionResponse(isTheFirst && !isWaiting, exception != default ? new List<Exception>() { exception } : new());

            async Task WaitAsync()
            {
                while (await _lockable.IsAcquiredAsync(key).NoContext())
                {
                    isWaiting = true;
                    await Task.Delay(4).NoContext();
                }
            }
        }
    }
}
