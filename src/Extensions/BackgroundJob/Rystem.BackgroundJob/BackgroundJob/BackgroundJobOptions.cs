namespace System.Timers
{
    public sealed class BackgroundJobOptions
    {
        public string? Key { get; set; }
        public bool RunImmediately { get; set; }
        public string Cron { get; set; } = "* * * * *";
    }
}