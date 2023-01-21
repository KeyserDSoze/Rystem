namespace System.Timers
{
    public interface IBackgroundJob
    {
        Task ActionToDoAsync();
        Task OnException(Exception exception);
    }
}