namespace Rystem.PlayFramework
{
    public sealed class SceneContext
    {
        public required string InputMessage { get; set; }
        public string? CurrentSceneName { get; set; }
        public List<SceneIteration> Iterations { get; } = [];
        public IChatClient? CurrentChatClient { get; set; }
    }
}
