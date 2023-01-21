namespace System
{
    public static partial class Try
    {
        public static TryResponse<T> WithDefaultOnCatch<T>(Func<T> function)
        {
            try
            {
                return new(function.Invoke());
            }
            catch (Exception ex)
            {
                return new(default, ex);
            }
        }
        public static Exception? WithDefaultOnCatch(Action function)
        {
            try
            {
                function.Invoke();
                return default;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
        public static async Task<TryResponse<T>> WithDefaultOnCatchAsync<T>(Func<Task<T>> function)
        {
            try
            {
                return new(await function.Invoke());
            }
            catch (Exception ex)
            {
                return new(default, ex);
            }
        }
        public static async Task<Exception?> WithDefaultOnCatchAsync(Func<Task> function)
        {
            try
            {
                await function.Invoke();
                return default;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
