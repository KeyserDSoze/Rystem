namespace System.Threading.Concurrent
{
    public sealed class LockResponse
    {
        public TimeSpan ExecutionTime { get; }
        public AggregateException? Exceptions { get; }
        public bool InException => this.Exceptions != default;
        public LockResponse(TimeSpan executionTime, IList<Exception> exceptions)
        {
            ExecutionTime = executionTime;
            if (exceptions?.Count > 0)
                Exceptions = new AggregateException(exceptions);
        }
    }
}
