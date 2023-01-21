namespace System
{
    public sealed class StopwatchStart
    {
        public DateTime Start { get; } = DateTime.UtcNow;
        internal StopwatchStart() { }
        public StopwatchResult Stop() 
            => new(Start, DateTime.UtcNow);
    }
}