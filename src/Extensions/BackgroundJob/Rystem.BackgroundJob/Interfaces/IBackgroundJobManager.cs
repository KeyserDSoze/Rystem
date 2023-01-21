namespace System.Timers
{
    public interface IBackgroundJobManager
    {
        Task RunAsync(IBackgroundJob job,
            BackgroundJobOptions options,
            Func<IBackgroundJob>? factory = null,
            CancellationToken cancellationToken = default);
    }
}