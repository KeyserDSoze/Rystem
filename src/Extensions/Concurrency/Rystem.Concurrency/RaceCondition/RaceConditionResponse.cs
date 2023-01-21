namespace System.Threading.Concurrent
{
    public sealed class RaceConditionResponse
    {
        public bool IsExecuted { get; }
        public AggregateException? Exceptions { get; }
        public bool InException => this.Exceptions != default;
        public RaceConditionResponse(bool isExecuted, IList<Exception> exceptions)
        {
            this.IsExecuted = isExecuted;
            if (exceptions?.Count > 0)
                this.Exceptions = new AggregateException(exceptions);
        }
    }
}
