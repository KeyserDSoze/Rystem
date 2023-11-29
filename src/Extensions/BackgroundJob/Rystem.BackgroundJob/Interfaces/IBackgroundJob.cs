namespace System.Timers
{
    public interface IBackgroundJob
    {
        Task ActionToDoAsync(CancellationToken cancellationToken = default);
        Task OnException(Exception exception);
    }
}
