namespace System
{
    public sealed class TryBehavior
    {
        public Func<Exception, bool>? RetryUntil { get; set; }
        public int MaxRetry { get; set; }
        public int WaitBetweenRetry { get; set; }
        public static TryBehavior Default { get; } = new();
    }
    public static partial class Try
    {
        private static async Task<TryResponse<T>> ExecuteWithTryAndRetryEngineAsync<T>(Func<Task<T>> function, Action<TryBehavior>? behavior = null)
        {
            var retry = 0;
            var tryBehavior = TryBehavior.Default;
            if (behavior != null)
            {
                tryBehavior = new();
                behavior.Invoke(tryBehavior);
            }
            Exception? lastException = null;
            while (retry <= tryBehavior.MaxRetry)
            {
                try
                {
                    var response = await function().NoContext();
                    return new(response, default);
                }
                catch (Exception exception)
                {
                    if (tryBehavior.RetryUntil?.Invoke(exception) != true)
                        return new(default, exception);
                    lastException = exception;
                    retry++;
                }
                if (tryBehavior.WaitBetweenRetry > 0)
                    await Task.Delay(tryBehavior.WaitBetweenRetry);
                retry++;
            }
            return new(default, lastException);
        }
        public static TryResponse<T> WithDefaultOnCatch<T>(Func<T> function, Action<TryBehavior>? behavior = null)
            => ExecuteWithTryAndRetryEngineAsync(() => Task.FromResult(function.Invoke()), behavior).ToResult();
        public static Exception? WithDefaultOnCatch(Action function, Action<TryBehavior>? behavior = null)
        {
            var response = ExecuteWithTryAndRetryEngineAsync(() =>
            {
                function.Invoke();
                return Task.FromResult(true);
            }, behavior).ToResult();
            return response.Exception;
        }
        public static Task<TryResponse<T>> WithDefaultOnCatchAsync<T>(Func<Task<T>> function, Action<TryBehavior>? behavior = null)
            => ExecuteWithTryAndRetryEngineAsync(function, behavior);
        public static async Task<Exception?> WithDefaultOnCatchAsync(Func<Task> function, Action<TryBehavior>? behavior = null)
        {
            var response = await ExecuteWithTryAndRetryEngineAsync(async () =>
            {
                await function.Invoke().NoContext();
                return true;
            }, behavior).NoContext();
            return response.Exception;
        }
        public static async Task<TryResponse<T>> WithDefaultOnCatchValueTaskAsync<T>(Func<ValueTask<T>> function, Action<TryBehavior>? behavior = null)
        {
            var response = await ExecuteWithTryAndRetryEngineAsync(async () =>
            {
                return await function.Invoke().NoContext();
            }, behavior).NoContext();
            return response;
        }
        public static async Task<Exception?> WithDefaultOnCatchValueTaskAsync(Func<ValueTask> function, Action<TryBehavior>? behavior = null)
        {
            var response = await ExecuteWithTryAndRetryEngineAsync(async () =>
            {
                await function.Invoke().NoContext();
                return true;
            }, behavior).NoContext();
            return response.Exception;
        }
    }
}
