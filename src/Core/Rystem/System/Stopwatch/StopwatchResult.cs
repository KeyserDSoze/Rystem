namespace System
{
    public sealed record StopwatchResult(DateTime Start, DateTime Stop)
    {
        public TimeSpan Span => Stop - Start;
    }
}