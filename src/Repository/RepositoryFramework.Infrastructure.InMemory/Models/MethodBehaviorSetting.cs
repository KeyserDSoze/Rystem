namespace RepositoryFramework.InMemory
{
    public class MethodBehaviorSetting
    {
        public Range MillisecondsOfWait { get; set; }
        public Range MillisecondsOfWaitWhenException { get; set; }
        public List<ExceptionOdds> ExceptionOdds { get; set; } = new();
        public static MethodBehaviorSetting Default { get; } = new()
        {
            MillisecondsOfWait = default,
            MillisecondsOfWaitWhenException = default
        };
    }
}