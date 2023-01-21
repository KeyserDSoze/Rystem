namespace System.Threading.Concurrent
{
    public interface IRaceCodition
    {
        Task<RaceConditionResponse> ExecuteAsync(Func<Task> action, string? key = null, TimeSpan? timeWindow = null);
    }
}
