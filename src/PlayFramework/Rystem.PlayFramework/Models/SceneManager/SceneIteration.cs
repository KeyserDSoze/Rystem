namespace Rystem.PlayFramework
{
    public sealed class SceneIteration
    {
        public required string Name { get; set; }
        public List<AiSceneResponse> Responses { get; } = [];
        public required DateTime StartTime { get; set; }
    }
}
